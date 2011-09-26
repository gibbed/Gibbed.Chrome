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
using Gibbed.Chrome.FileFormats;
using Gibbed.IO;
using NDesk.Options;
using DDS = Gibbed.Squish.DDS;
using ResourcePackage = Gibbed.Chrome.FileFormats.ResourcePackage;

namespace Gibbed.Chrome.ResourceUnpack
{
    public class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool verbose = true;

            var options = new OptionSet()
            {
                {
                    "o|overwrite",
                    "overwrite existing files",
                    v => overwriteFiles = v != null
                },
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.rpack [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            using (var input = File.OpenRead(inputPath))
            {
                var rpack = new ResourcePackageFile();
                rpack.Deserialize(input);

                IResourcePackageLoader loader = null;

                if ((rpack.Flags & ResourcePackage.PackageFlags.UseLZMAForCompression) != 0)
                {
                    return;
                    throw new NotSupportedException();
                }
                else
                {
                    loader = new ResourcePackageLoaderZlib();
                }

                using (loader)
                {
                    foreach (var entry in rpack.Entries)
                    {
                        Console.WriteLine(entry.FirstName);

                        switch (entry.Type)
                        {
                            case ResourcePackage.ResourceType.Texture:
                            {
                                if (entry.Datas.Any(
                                    d =>
                                        d.Type.Type != ResourcePackage.ResourceType.Texture &&
                                        d.Type.Type != ResourcePackage.ResourceType.TextureBitmapData &&
                                        d.Type.Type != ResourcePackage.ResourceType.TextureMipBitmapData) == true)
                                {
                                    throw new FormatException();
                                }

                                var headerData = entry.Datas.First();
                                if (headerData.Type.Type != ResourcePackage.ResourceType.Texture)
                                {
                                    throw new FormatException();
                                }

                                if (headerData.UncompressedSize != 80)
                                {
                                    throw new FormatException();
                                }

                                var header = new ResourcePackage.TextureHeader();
                                header.Deserialize(loader.Read(input, headerData), rpack.LittleEndian);

                                var dataPath = Path.Combine(outputPath, "textures");
                                dataPath = Path.Combine(dataPath, entry.FirstName + ".dds");

                                if (overwriteFiles == true ||
                                    File.Exists(dataPath) == false)
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(dataPath));

                                    using (var output = File.Create(dataPath))
                                    {
                                        var ddsHeader = new DDS.Header()
                                        {
                                            Size = 124,
                                            Flags =
                                                DDS.HeaderFlags.Texture |
                                                DDS.HeaderFlags.Mipmap,
                                            Width = header.Width,
                                            Height = header.Height,
                                            MipMapCount = header.MipMapCount,
                                            PixelFormat = GetPixelFormat(header.Format),
                                        };

                                        output.WriteValueU32(0x20534444);
                                        ddsHeader.Serialize(output, rpack.LittleEndian);

                                        for (uint i = 0; i < header.MipMapCount; i++)
                                        {
                                            var size = header.MipMapSizes[i];

                                            var mipData = entry.Datas
                                                .SingleOrDefault(m => m.UncompressedSize == size);
                                            if (mipData == null)
                                            {
                                                output.Seek(size, SeekOrigin.Current);
                                            }
                                            else
                                            {
                                                loader.Read(input, mipData, output);
                                            }
                                        }
                                    }
                                }

                                break;
                            }

                            case ResourcePackage.ResourceType.Animation:
                            case ResourcePackage.ResourceType.AnimationScript:
                            case ResourcePackage.ResourceType.Fx:
                            case ResourcePackage.ResourceType.Material:
                            case ResourcePackage.ResourceType.Mesh:
                            case ResourcePackage.ResourceType.TinyObjects:
                            {
                                // dump out raw data for 'unsupported' types

                                int dataIndex = -1;
                                foreach (var data in entry.Datas)
                                {
                                    dataIndex++;

                                    var dataName = string.Format("{0}.{1}", dataIndex, data.Type.Type);

                                    var dataPath = Path.Combine(outputPath, "__UNKNOWN");
                                    dataPath = Path.Combine(dataPath, entry.Type.ToString());
                                    dataPath = Path.Combine(dataPath, entry.FirstName);
                                    dataPath = Path.Combine(dataPath, dataName);

                                    if (overwriteFiles == true ||
                                        File.Exists(dataPath) == false)
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
                                        using (var output = File.Create(dataPath))
                                        {
                                            loader.Read(input, data, output);
                                        }
                                    }
                                }

                                break;
                            }

                            default:
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }
                }
            }
        }

        private static DDS.PixelFormat GetPixelFormat(ResourcePackage.TextureFormat format)
        {
            if (format == ResourcePackage.TextureFormat.DXT1 ||
                format == ResourcePackage.TextureFormat.DXT3 ||
                format == ResourcePackage.TextureFormat.DXT5)
            {
                uint fourCC;

                switch (format)
                {
                    case ResourcePackage.TextureFormat.DXT1: fourCC = 0x31545844; break;
                    case ResourcePackage.TextureFormat.DXT3: fourCC = 0x33545844; break;
                    case ResourcePackage.TextureFormat.DXT5: fourCC = 0x35545844; break;
                    default: throw new NotSupportedException();
                }

                return new DDS.PixelFormat()
                {
                    Size = 32,
                    Flags = DDS.PixelFormatFlags.FourCC,
                    FourCC = fourCC,
                };
            }
            else if (
                format == ResourcePackage.TextureFormat.L8 ||
                format == ResourcePackage.TextureFormat.L16)
            {
                return new DDS.PixelFormat()
                {
                    Size = 32,
                    Flags = DDS.PixelFormatFlags.Luminance,
                    RGBBitCount = format == ResourcePackage.TextureFormat.L8 ? 8u : 16u,
                    RedBitMask = format == ResourcePackage.TextureFormat.L8 ? 0xFFu : 0xFFFFu,
                };
            }
            else if (format == ResourcePackage.TextureFormat.A8R8G8B8)
            {
                return new DDS.PixelFormat()
                {
                    Size = 32,
                    Flags = DDS.PixelFormatFlags.RGBA,
                    RGBBitCount = 32,
                    FourCC = 0,
                    RedBitMask = 0x00FF0000,
                    GreenBitMask = 0x0000FF00,
                    BlueBitMask = 0x000000FF,
                    AlphaBitMask = 0xFF000000,
                };
            }
            else if (format == ResourcePackage.TextureFormat.X8R8G8B8)
            {
                return new DDS.PixelFormat()
                {
                    Size = 32,
                    Flags = DDS.PixelFormatFlags.RGB,
                    RGBBitCount = 32,
                    FourCC = 0,
                    RedBitMask = 0x00FF0000,
                    GreenBitMask = 0x0000FF00,
                    BlueBitMask = 0x000000FF,
                    AlphaBitMask = 0x00000000,
                };
            }
            else if (format == ResourcePackage.TextureFormat.R5G6B5)
            {
                return new DDS.PixelFormat()
                {
                    Size = 32,
                    Flags = DDS.PixelFormatFlags.RGB,
                    FourCC = 0,
                    RGBBitCount = 16,
                    RedBitMask = 0x0000F800,
                    GreenBitMask = 0x000007E0,
                    BlueBitMask = 0x0000001F,
                    AlphaBitMask = 0x00000000,
                };
            }
            else if (format == ResourcePackage.TextureFormat.R16F)
            {
                return new DDS.PixelFormat()
                {
                    Size = 32,
                    Flags = DDS.PixelFormatFlags.FourCC,
                    FourCC = 111,
                };
            }

            throw new NotSupportedException();
        }
    }
}
