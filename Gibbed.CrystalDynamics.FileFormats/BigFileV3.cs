﻿/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.IO;
using System.Text;

namespace Gibbed.CrystalDynamics.FileFormats
{
    public class BigFileV3
    {
        public Endian Endianness = Endian.Little;
        public uint FileAlignment = 0x7FF00000;
        public List<Big.EntryV2> Entries
            = new List<Big.EntryV2>();

        public uint Unknown04;
        public uint NumberOfFiles;
        public uint Unknown10;
        public string BasePath;

        public static int EstimateHeaderSize(int count)
        {
            return
                (52 + // header
                (16 * count)) // entries
                .Align(2048); // aligned to 2048 bytes
        }

        public void Serialize(Stream output)
        {
            output.WriteValueU32(0x54414653, this.Endianness);
            output.WriteValueU32(this.Unknown04, this.Endianness);
            output.WriteValueU32(this.NumberOfFiles, this.Endianness);
            output.WriteValueS32(this.Entries.Count, this.Endianness);
            output.WriteValueU32(this.Unknown10, this.Endianness);
            output.WriteString(this.BasePath, 32, Encoding.ASCII);

            foreach (var e in this.Entries.OrderBy(e => e.NameHash))
            {
                output.WriteValueU32(e.NameHash, this.Endianness);
                output.WriteValueU32(e.Locale, this.Endianness);
                output.WriteValueU32(e.Size, this.Endianness);
                output.WriteValueU32(e.File | e.Offset, this.Endianness);
            }
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(this.Endianness);

            if (magic != 0x54414653)
                throw new NotSupportedException("Bad magic number");

            this.Unknown04 = input.ReadValueU32(this.Endianness);
            this.NumberOfFiles = input.ReadValueU32(this.Endianness);

            var count = input.ReadValueU32(this.Endianness);

            this.Unknown10 = input.ReadValueU32(this.Endianness);

            this.BasePath = input.ReadString(32, true, Encoding.ASCII);

            this.Entries.Clear();
            for (uint i = 0; i < count; i++)
            {
                var entry = new Big.EntryV2();
                entry.NameHash = input.ReadValueU32(this.Endianness);
                entry.Locale = input.ReadValueU32(this.Endianness);
                entry.Size = input.ReadValueU32(this.Endianness);
                var offset = input.ReadValueU32(this.Endianness);
                entry.Offset = offset & 0xFFFFFF00;
                entry.File = offset & 0xFF;
                this.Entries.Add(entry);
            }
        }
    }
}
