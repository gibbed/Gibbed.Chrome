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
    public class TextureHeader
    {
        public ushort Width;
        public ushort Height;
        public ushort Depth;
        public ushort Count; // 1 = normal texture, 6 = cubemap texture
        public ushort MipMapCount;
        public ushort Flags;
        public TextureFormat Format;
        public readonly uint[] MipMapSizes = new uint[16];

        public void Serialize(Stream output, bool littleEndian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, bool littleEndian)
        {
            this.Width = input.ReadValueU16(littleEndian);
            this.Height = input.ReadValueU16(littleEndian);
            this.Depth = input.ReadValueU16(littleEndian);
            this.Count = input.ReadValueU16(littleEndian);
            this.MipMapCount = input.ReadValueU16(littleEndian);
            this.Flags = input.ReadValueU16(littleEndian);
            this.Format = input.ReadValueEnum<TextureFormat>(littleEndian);
            
            for (uint i = 0; i < 16; i++)
            {
                this.MipMapSizes[i] = input.ReadValueU32(littleEndian);
            }
        }
    }
}
