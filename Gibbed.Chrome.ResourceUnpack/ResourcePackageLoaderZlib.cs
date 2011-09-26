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
using Gibbed.Chrome.FileFormats.ResourcePackage;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Gibbed.Chrome.ResourceUnpack
{
    public class ResourcePackageLoaderZlib : IResourcePackageLoader, IDisposable
    {
        private Dictionary<TypeDefinition, MemoryStream>
            LoadedTypes = new Dictionary<TypeDefinition, MemoryStream>();

        ~ResourcePackageLoaderZlib()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                foreach (var kv in this.LoadedTypes)
                {
                    if (kv.Value != null)
                    {
                        kv.Value.Dispose();
                    }
                }

                this.LoadedTypes.Clear();
            }
        }

        protected MemoryStream GetType(Stream input, TypeDefinition type)
        {
            if (this.LoadedTypes.ContainsKey(type) == false)
            {
                input.Seek(type.Offset, SeekOrigin.Begin);

                var zlib = new InflaterInputStream(input);
                this.LoadedTypes[type] =
                    zlib.ReadToMemoryStream(type.UncompressedSize);
            }

            return this.LoadedTypes[type];
        }

        public void Read(Stream input, DataDefinition data, Stream output)
        {
            switch (data.Type.CompressionMode)
            {
                case CompressionMode.CompressedPerType:
                {
                    var buffer = this.GetType(input, data.Type);
                    buffer.Seek(data.Offset, SeekOrigin.Begin);
                    output.WriteFromStream(buffer, data.UncompressedSize);
                    return;
                }

                case CompressionMode.CompressedPerResource:
                {
                    input.Seek(data.Type.Offset, SeekOrigin.Begin);
                    input.Seek(data.Offset, SeekOrigin.Current);
                    var zlib = new InflaterInputStream(input);
                    output.WriteFromStream(zlib, data.UncompressedSize);
                    return;
                }

                default:
                {
                    throw new NotImplementedException();
                }
            }
        }

        public MemoryStream Read(Stream input, DataDefinition data)
        {
            switch (data.Type.CompressionMode)
            {
                case CompressionMode.CompressedPerType:
                {
                    var buffer = this.GetType(input, data.Type);
                    buffer.Seek(data.Offset, SeekOrigin.Begin);
                    return buffer.ReadToMemoryStream(data.UncompressedSize);
                }

                case CompressionMode.CompressedPerResource:
                {
                    input.Seek(data.Type.Offset, SeekOrigin.Begin);
                    input.Seek(data.Offset, SeekOrigin.Current);

                    var zlib = new InflaterInputStream(input);
                    return zlib.ReadToMemoryStream(data.UncompressedSize);
                }

                default:
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
