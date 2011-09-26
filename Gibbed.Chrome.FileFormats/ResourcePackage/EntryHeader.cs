/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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
using System.IO;
using Gibbed.IO;

namespace Gibbed.Chrome.FileFormats.ResourcePackage
{
    internal class EntryHeader
    {
        public uint Flags;
        public uint FirstNameIndex;
        public uint FirstDataIndex;

        public void Deserialize(Stream input, bool littleEndian)
        {
            this.Flags = input.ReadValueU32(littleEndian);
            this.FirstNameIndex = input.ReadValueU32(littleEndian);
            this.FirstDataIndex = input.ReadValueU32(littleEndian);
        }

        public ResourceType Type
        {
            get { return (ResourceType)((this.Flags & 0x00FF0000u) >> 16); }
            set
            {
                this.Flags &= ~0x00FF0000u;
                this.Flags |= (uint)((byte)value << 16);
            }
        }

        public byte NameCount
        {
            get { return (byte)((this.Flags & 0x1F000000u) >> 24); }
            set
            {
                if (value > 31)
                {
                    throw new ArgumentOutOfRangeException();
                }

                this.Flags &= ~0x1F000000u;
                this.Flags |= (uint)value << 24;
            }
        }

        public ushort ResourceCount
        {
            get { return (ushort)(this.Flags & 0x0000FFFFu); }
            set
            {
                this.Flags &= ~0x0000FFFFu;
                this.Flags |= value;
            }
        }

        public bool Streaming
        {
            get { return (this.Flags & 0x20000000) != 0; }
            set
            {
                this.Flags &= ~0x20000000u;
                this.Flags |= value == true ? 0x20000000u : 0x00000000u;
            }
        }
    }
}
