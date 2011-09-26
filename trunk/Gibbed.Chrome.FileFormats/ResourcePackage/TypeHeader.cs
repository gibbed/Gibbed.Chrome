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

using System.IO;
using Gibbed.IO;

namespace Gibbed.Chrome.FileFormats.ResourcePackage
{
    internal class TypeHeader
    {
        public uint Flags;
        public uint Offset;
        public uint UncompressedSize;
        public uint CompressedSize;
        public uint ResourceCount;

        public void Deserialize(Stream input, bool littleEndian)
        {
            this.Flags = input.ReadValueU32(littleEndian);
            this.Offset = input.ReadValueU32(littleEndian);
            this.UncompressedSize = input.ReadValueU32(littleEndian);
            this.CompressedSize = input.ReadValueU32(littleEndian);
            this.ResourceCount = input.ReadValueU32(littleEndian);
        }

        public bool DuplicatesRemoval
        {
            get { return (this.Flags & 0x10000000) != 0; }
            set
            {
                this.Flags &= ~0x10000000u;
                this.Flags |= value == true ? 0x10000000u : 0x00000000u;
            }
        }

        public bool PreloadDisabled
        {
            get { return (this.Flags & 0x20000000) != 0; }
            set
            {
                this.Flags &= ~0x20000000u;
                this.Flags |= value == true ? 0x20000000u : 0x00000000u;
            }
        }

        public uint UnknownFlags
        {
            get { return this.Flags & 0xC0000000u; }
        }

        public CompressionMode CompressionMode
        {
            get { return (CompressionMode)((this.Flags & 0x0F000000u) >> 24); }
            set
            {
                this.Flags &= ~0x0F000000u;
                this.Flags |= (((uint)value) << 24) & 0x0F000000u;
            }
        }

        public uint Alignment
        {
            get
            {
                return 1u << (int)((this.Flags & 0x00FF0000u) >> 16);
            }

            set
            {
                int alignment = -1;
                while (value > 0)
                {
                    alignment++;
                    value >>= 1;
                }
                this.Flags &= ~0x00FF0000u;
                this.Flags |= ((uint)((byte)alignment)) << 16;
            }
        }

        public ResourceType Type
        {
            get { return (ResourceType)(this.Flags & 0x0000FFFF); }
            set
            {
                this.Flags &= ~0x0000FFFFu;
                this.Flags |= (ushort)value;
            }
        }
    }
}
