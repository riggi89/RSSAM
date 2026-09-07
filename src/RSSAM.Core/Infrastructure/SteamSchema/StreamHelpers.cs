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
using System.Globalization;
using System.IO;
using System.Text;

namespace RSSAM.Infrastructure.SteamSchema
{
    internal static class StreamHelpers
    {
        private const int MaxStringByteLength = 16 * 1024 * 1024;

        public static byte ReadValueU8(this Stream stream)
        {
            var value = stream.ReadByte();
            return value < 0
                ? throw new EndOfStreamException()
                : (byte)value;
        }

        public static int ReadValueS32(this Stream stream)
        {
            var data = new byte[4];
            stream.ReadExactly(data);
            return BitConverter.ToInt32(data, 0);
        }

        public static uint ReadValueU32(this Stream stream)
        {
            var data = new byte[4];
            stream.ReadExactly(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public static ulong ReadValueU64(this Stream stream)
        {
            var data = new byte[8];
            stream.ReadExactly(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public static float ReadValueF32(this Stream stream)
        {
            var data = new byte[4];
            stream.ReadExactly(data);
            return BitConverter.ToSingle(data, 0);
        }

        internal static string ReadStringInternalDynamic(this Stream stream, Encoding encoding, char end)
        {
            int characterSize = encoding.GetByteCount("e");
            if (characterSize is not (1 or 2 or 4))
                throw new NotSupportedException($"Unsupported character size: {characterSize} bytes.");

            string characterEnd = end.ToString(CultureInfo.InvariantCulture);

            int i = 0;
            var data = new byte[128 * characterSize];

            while (true)
            {
                if (i + characterSize > data.Length)
                {
                    if (data.Length >= MaxStringByteLength)
                        throw new FormatException("The binary KeyValue string exceeds the supported size.");

                    Array.Resize(ref data, data.Length + (128 * characterSize));
                }

                stream.ReadExactly(data, i, characterSize);

                if (encoding.GetString(data, i, characterSize) == characterEnd)
                {
                    break;
                }

                i += characterSize;
            }

            if (i == 0)
            {
                return "";
            }

            return encoding.GetString(data, 0, i);
        }

        public static string ReadStringAscii(this Stream stream)
        {
            return stream.ReadStringInternalDynamic(Encoding.ASCII, '\0');
        }

        public static string ReadStringUnicode(this Stream stream)
        {
            return stream.ReadStringInternalDynamic(Encoding.UTF8, '\0');
        }
    }
}
