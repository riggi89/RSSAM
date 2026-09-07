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
using System.Runtime.InteropServices;

namespace RSSAM.API
{
    public abstract class NativeWrapper<TNativeFunctions> : INativeWrapper
    {
        protected IntPtr ObjectAddress;
        protected TNativeFunctions Functions;

        public override string ToString()
        {
            return $"Steam Interface<{typeof(TNativeFunctions)}> #0x{this.ObjectAddress.ToInt64():X}";
        }

        public void SetupFunctions(IntPtr objectAddress)
        {
            if (objectAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Steam returned a null object pointer for interface {typeof(TNativeFunctions).Name}.");
            }

            this.ObjectAddress = objectAddress;

            var ifaceObject = Marshal.PtrToStructure(
                this.ObjectAddress,
                typeof(NativeClass));

            if (ifaceObject is not NativeClass iface)
            {
                throw new InvalidOperationException(
                    $"Steam interface {typeof(TNativeFunctions).Name} could not be marshalled as a native class.");
            }

            if (iface.VirtualTable == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Steam returned an invalid virtual table for interface {typeof(TNativeFunctions).Name}.");
            }

            var functionsObject = Marshal.PtrToStructure(
                iface.VirtualTable,
                typeof(TNativeFunctions));

            if (functionsObject is not TNativeFunctions functions)
            {
                throw new InvalidOperationException(
                    $"Steam virtual table for {typeof(TNativeFunctions).Name} could not be marshalled.");
            }

            this.Functions = functions;
        }

        private readonly Dictionary<IntPtr, Delegate> _FunctionCache = new();

        protected Delegate GetDelegate<TDelegate>(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Steam returned a null function pointer for {typeof(TDelegate).Name} in {typeof(TNativeFunctions).Name}.");
            }

            if (this._FunctionCache.TryGetValue(pointer, out var function) == false)
            {
                function = Marshal.GetDelegateForFunctionPointer(pointer, typeof(TDelegate));
                this._FunctionCache[pointer] = function;
            }
            return function;
        }

        protected TDelegate GetFunction<TDelegate>(IntPtr pointer)
            where TDelegate : class
        {
            return (TDelegate)((object)this.GetDelegate<TDelegate>(pointer));
        }

        protected void Call<TDelegate>(IntPtr pointer, params object[] args)
        {
            this.GetDelegate<TDelegate>(pointer).DynamicInvoke(args);
        }

        protected TReturn Call<TReturn, TDelegate>(IntPtr pointer, params object[] args)
        {
            return (TReturn)this.GetDelegate<TDelegate>(pointer).DynamicInvoke(args);
        }
    }
}
