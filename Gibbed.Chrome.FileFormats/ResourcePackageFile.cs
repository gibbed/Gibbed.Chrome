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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;

namespace Gibbed.Chrome.FileFormats
{
    public class ResourcePackageFile
    {
        public bool LittleEndian;
        public uint Version;
        public ResourcePackage.PackageFlags Flags;
        public uint Alignment;

        public List<ResourcePackage.TypeDefinition> Types
            = new List<ResourcePackage.TypeDefinition>();
        public List<ResourcePackage.EntryDefinition> Entries
            = new List<ResourcePackage.EntryDefinition>();

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadString(3, Encoding.ASCII);
            if (magic != "RP5") // RP5 = 'Resource Pack, Chrome Engine 5'?
            {
                throw new FormatException();
            }

            var endian = input.ReadValueU8();
            if (endian != 0x4C && // 'L'
                endian != 0x42) // 'B'
            {
                throw new FormatException();
            }

            this.LittleEndian = endian == 0x4C; // 'L'

            this.Version = input.ReadValueU32(this.LittleEndian);
            if (this.Version != 36)
            {
                throw new FormatException();
            }

            this.Flags = input.ReadValueEnum<ResourcePackage.PackageFlags>(
                this.LittleEndian);
            var dataCount = input.ReadValueU32(this.LittleEndian);
            var typeCount = input.ReadValueU32(this.LittleEndian);
            var nameCount = input.ReadValueU32(this.LittleEndian);
            var nameDataSize = input.ReadValueU32(this.LittleEndian);
            var entryCount = input.ReadValueU32(this.LittleEndian);
            this.Alignment = input.ReadValueU32(this.LittleEndian);

            var typeHeaders = new ResourcePackage.TypeHeader[typeCount];
            for (uint i = 0; i < typeCount; i++)
            {
                typeHeaders[i] = new ResourcePackage.TypeHeader();
                typeHeaders[i].Deserialize(input, this.LittleEndian);

                if (typeHeaders[i].UnknownFlags != 0)
                {
                    throw new NotImplementedException();
                }
            }

            var dataHeaders = new ResourcePackage.DataHeader[dataCount];
            for (uint i = 0; i < dataCount; i++)
            {
                dataHeaders[i] = new ResourcePackage.DataHeader();
                dataHeaders[i].Deserialize(input, this.LittleEndian);
            }

            var entryHeaders = new ResourcePackage.EntryHeader[entryCount];
            for (uint i = 0; i < entryCount; i++)
            {
                entryHeaders[i] = new ResourcePackage.EntryHeader();
                entryHeaders[i].Deserialize(input, this.LittleEndian);
            }

            var names = new string[nameCount];
            {
                var nameOffsets = new uint[nameCount];
                for (uint i = 0; i < nameCount; i++)
                {
                    nameOffsets[i] = input.ReadValueU32();
                }

                using (var nameData = input.ReadToMemoryStream(nameDataSize))
                {
                    for (uint i = 0; i < nameCount; i++)
                    {
                        nameData.Seek(nameOffsets[i], SeekOrigin.Begin);
                        names[i] = nameData.ReadStringZ(Encoding.ASCII);
                    }
                }
            }

            var types = new ResourcePackage.TypeDefinition[typeCount];
            for (uint i = 0; i < typeCount; i++)
            {
                var typeHeader = typeHeaders[i];

                types[i] = new ResourcePackage.TypeDefinition()
                {
                    Flags = typeHeader.Flags,

                    DuplicatesRemoval = typeHeader.DuplicatesRemoval,
                    PreloadDisabled = typeHeader.PreloadDisabled,
                    CompressionMode = typeHeader.CompressionMode,
                    Alignment = typeHeader.Alignment,
                    Type = typeHeader.Type,

                    Offset = typeHeader.Offset,
                    UncompressedSize = typeHeader.UncompressedSize,
                    CompressedSize = typeHeader.CompressedSize,
                };

                if (dataHeaders.Where(dh => dh.TypeIndex == i).Count() !=
                    typeHeader.ResourceCount)
                {
                    throw new FormatException();
                }
            }

            this.Types.Clear();
            this.Types.AddRange(types);

            this.Entries.Clear();
            for (uint i = 0; i < entryCount; i++)
            {
                var entryHeader = entryHeaders[i];

                var entry = new ResourcePackage.EntryDefinition()
                {
                    Flags = entryHeader.Flags,
                    Type = entryHeader.Type,
                    Streaming = entryHeader.Streaming,
                };

                for (uint j = 0; j < entryHeader.NameCount; j++)
                {
                    entry.Names.Add(names[entryHeader.FirstNameIndex + j]);
                }

                for (uint j = 0; j < entryHeader.ResourceCount; j++)
                {
                    var dataHeader = dataHeaders[entryHeader.FirstDataIndex + j];

                    var data = new ResourcePackage.DataDefinition()
                    {
                        Flags = dataHeader.Flags,
                        Offset = dataHeader.Offset,
                        UncompressedSize = dataHeader.UncompressedSize,
                        CompressedSize = dataHeader.CompressedSize,
                        Type = types[dataHeader.TypeIndex],
                    };
                    entry.Datas.Add(data);
                }

                this.Entries.Add(entry);
            }
        }
    }
}
