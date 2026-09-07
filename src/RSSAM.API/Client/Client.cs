/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */
// Modified for RSSAM by Daniel Riggi (riggi89), Copyright (c) 2026.
// This file is an altered version of the original SAM source.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace RSSAM.API
{
    public class Client : IDisposable
    {
        private const int SteamPipeCreateAttempts = 4;
        private const int SteamPipeRetryDelayMilliseconds = 250;
        private static readonly TimeSpan _ClientSessionWaitTimeout = TimeSpan.FromSeconds(30);
        private static readonly SemaphoreSlim _ClientSessionLease = new(1, 1);
        private static readonly object _NativeClientSync = new();

        public Wrappers.SteamClient018 SteamClient;
        public Wrappers.SteamUser012 SteamUser;
        public Wrappers.SteamUserStats013 SteamUserStats;
        public Wrappers.SteamUtils005 SteamUtils;
        public Wrappers.SteamApps001 SteamApps001;
        public Wrappers.SteamApps008 SteamApps008;

        private bool _IsDisposed = false;
        private bool _IsInitialized;
        private bool _ClientSessionLeaseHeld;
        private int _Pipe;
        private int _User;

        private readonly List<ICallback> _Callbacks = new();

        public void Initialize(long appId)
        {
            ObjectDisposedException.ThrowIf(this._IsDisposed, this);

            if (this._IsInitialized)
            {
                throw new InvalidOperationException("The Steam client has already been initialized.");
            }

            if (RuntimeInformation.ProcessArchitecture is not Architecture.X86 and not Architecture.X64)
            {
                throw new ClientInitializeException(
                    ClientInitializeFailure.UnsupportedArchitecture,
                    $"RSSAM Steam interop does not support {RuntimeInformation.ProcessArchitecture}. Use an x86 or x64 build.");
            }

            if (string.IsNullOrEmpty(Steam.GetInstallPath()) == true)
            {
                throw new ClientInitializeException(ClientInitializeFailure.GetInstallPath, "failed to get Steam install path");
            }

            if (_ClientSessionLease.Wait(_ClientSessionWaitTimeout) == false)
            {
                throw new ClientInitializeException(
                    ClientInitializeFailure.SessionBusy,
                    "another Steam client session did not finish in time");
            }
            this._ClientSessionLeaseHeld = true;

            try
            {
                lock (_NativeClientSync)
                {
                    if (appId != 0)
                    {
                        Environment.SetEnvironmentVariable("SteamAppId", appId.ToString(CultureInfo.InvariantCulture));
                    }

                    if (Steam.Load() == false)
                    {
                        throw new ClientInitializeException(ClientInitializeFailure.Load, "failed to load SteamClient");
                    }

                    this.SteamClient = Steam.CreateInterface<Wrappers.SteamClient018>("SteamClient018");
                    if (this.SteamClient == null)
                    {
                        throw new ClientInitializeException(ClientInitializeFailure.CreateSteamClient, "failed to create ISteamClient018");
                    }

                    this._Pipe = CreateSteamPipeWithRetry();
                    if (this._Pipe == 0)
                    {
                        throw new ClientInitializeException(
                            ClientInitializeFailure.CreateSteamPipe,
                            $"failed to create pipe after {SteamPipeCreateAttempts} attempts");
                    }

                    this._User = this.SteamClient.ConnectToGlobalUser(this._Pipe);
                    if (this._User == 0)
                    {
                        throw new ClientInitializeException(ClientInitializeFailure.ConnectToGlobalUser, "failed to connect to global user");
                    }

                    this.SteamUtils = this.SteamClient.GetSteamUtils004(this._Pipe);
                    if (appId > 0 && this.SteamUtils.GetAppId() != (uint)appId)
                    {
                        throw new ClientInitializeException(ClientInitializeFailure.AppIdMismatch, "appID mismatch");
                    }

                    this.SteamUser = this.SteamClient.GetSteamUser012(this._User, this._Pipe);
                    this.SteamUserStats = this.SteamClient.GetSteamUserStats013(this._User, this._Pipe);
                    this.SteamApps008 = this.SteamClient.GetSteamApps008(this._User, this._Pipe);

                    // SteamApps001 is a legacy metadata interface. Keep catalog
                    // ownership available through SteamApps008 if a future Steam
                    // client no longer exposes the older interface.
                    try
                    {
                        this.SteamApps001 = this.SteamClient.GetSteamApps001(this._User, this._Pipe);
                    }
                    catch (InvalidOperationException)
                    {
                        this.SteamApps001 = null;
                    }

                    this._IsInitialized = true;
                }
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        ~Client()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._IsDisposed == true)
            {
                return;
            }

            try
            {
                lock (_NativeClientSync)
                {
                    if (this.SteamClient != null && this._Pipe > 0)
                    {
                        if (this._User > 0)
                        {
                            this.SteamClient.ReleaseUser(this._Pipe, this._User);
                            this._User = 0;
                        }

                        this.SteamClient.ReleaseSteamPipe(this._Pipe);
                        this._Pipe = 0;
                    }
                }
            }
            catch
            {
                // Cleanup is best effort. Handles are invalid after this point and
                // cleanup failures must not mask the original initialization error.
            }
            finally
            {
                this._IsInitialized = false;
                this._IsDisposed = true;

                if (this._ClientSessionLeaseHeld)
                {
                    this._ClientSessionLeaseHeld = false;
                    _ClientSessionLease.Release();
                }
            }
        }

        private int CreateSteamPipeWithRetry()
        {
            for (int attempt = 1; attempt <= SteamPipeCreateAttempts; attempt++)
            {
                int pipe = this.SteamClient.CreateSteamPipe();
                if (pipe != 0)
                {
                    return pipe;
                }

                if (attempt < SteamPipeCreateAttempts)
                {
                    Thread.Sleep(SteamPipeRetryDelayMilliseconds);
                }
            }

            return 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public TCallback CreateAndRegisterCallback<TCallback>()
            where TCallback : ICallback, new()
        {
            ObjectDisposedException.ThrowIf(this._IsDisposed, this);

            if (this._IsInitialized == false)
            {
                throw new InvalidOperationException("The Steam client is not initialized.");
            }

            TCallback callback = new();
            this._Callbacks.Add(callback);
            return callback;
        }

        private bool _RunningCallbacks;

        public void RunCallbacks(bool server)
        {
            ObjectDisposedException.ThrowIf(this._IsDisposed, this);

            if (this._IsInitialized == false || this._Pipe <= 0)
            {
                throw new InvalidOperationException("The Steam client is not initialized.");
            }

            if (this._RunningCallbacks == true)
            {
                return;
            }

            this._RunningCallbacks = true;

            try
            {
                Types.CallbackMessage message;
                while (Steam.GetCallback(this._Pipe, out message, out _) == true)
                {
                    try
                    {
                        var callbackId = message.Id;
                        foreach (ICallback callback in this._Callbacks.Where(
                            candidate => candidate.Id == callbackId &&
                                         candidate.IsServer == server))
                        {
                            callback.Run(message.ParamPointer);
                        }
                    }
                    finally
                    {
                        Steam.FreeLastCallback(this._Pipe);
                    }
                }
            }
            finally
            {
                this._RunningCallbacks = false;
            }
        }
    }
}
