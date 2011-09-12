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
using Gibbed.IO;
using NDesk.Options;

/* This is extremely, and I mean *extremely* shitty code
 * to exploit an equally shitty system in Dead Island that locks out
 * modding of the main *.pak files
 * 
 * This has the side effect of effectively doubling the size of any
 * 'fixed' ZIP since the original data is duplicated.
 */

/* If I wasn't lazy I would write some code to reverse-collide the
 * target hash instead of copying the original data in.
 */

namespace Gibbed.Chrome.FixZip
{
    internal class Program
    {
        private const int BLOCK_SIZE = 0x10000;

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        /*
        private static uint Adler32(byte[] buffer, int offset, int count, uint hash)
        {
            uint upper = (ushort)((hash & 0xFFFF0000) >> 16);
            uint lower = (ushort)((hash & 0x0000FFFF) >> 0);

            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                lower = (lower + buffer[i]) % 0xFFF1;
                upper = (upper + lower) % 0xFFF1;
            }

            return ((upper << 16) & 0xFFFF0000) | ((lower & 0x0000FFFF) << 0);
        }
        */

        private static uint Adler32(byte[] buffer, int offset, int count, uint hash)
        {
            if (buffer == null)
            {
                return 1;
            }

            uint upper = (ushort)((hash & 0xFFFF0000) >> 16);
            uint lower = (ushort)((hash & 0x0000FFFF) >> 0);

            do
            {
                int run = Math.Min(5552, count);
                int end = offset + run;

                for (; offset < end; offset++)
                {
                    lower += buffer[offset];
                    upper += lower;
                }

                lower %= 0xFFF1;
                upper %= 0xFFF1;

                count -= run;
            }
            while (count > 0);

            return ((upper << 16) & 0xFFFF0000) | ((lower & 0x0000FFFF) << 0);
        }

        private static uint ComputeHashOfBlock(Stream input, long left, uint hash)
        {
            var buffer = new byte[BLOCK_SIZE];

            while (left > 0)
            {
                int block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                if (input.Read(buffer, 0, block) != block)
                {
                    break;
                }
                hash = Adler32(buffer, 0, block, hash);
                left -= block;

                /* This seems like a bug in their hashing scheme!
                 * (which I exploit :-)
                 */
                block = (int)Math.Min(BLOCK_SIZE, left);
                input.Seek(block, SeekOrigin.Current);
            }

            return hash;
        }

        private static uint ComputeHashOfFile(Stream input, uint hash)
        {
            input.Seek(0, SeekOrigin.Begin);
            hash = ComputeHashOfBlock(input, input.Length, hash);
            return hash;
        }

        private static uint ComputeHashOfZip(Stream input, uint hash)
        {
            hash = ComputeHashOfFile(input, hash);

            // get basic zip info
            input.Seek(-22, SeekOrigin.End);
            if (input.ReadValueU32() != 0x06054B50)
            {
                throw new FormatException();
            }

            input.Seek(12, SeekOrigin.Current);
            var indexOffset = input.ReadValueU32();

            input.Seek(indexOffset, SeekOrigin.Begin);
            hash = ComputeHashOfBlock(input, input.Length - indexOffset, hash);
            return hash;
        }

        private static uint ComputeIncompleteHashOfBlock(Stream input, long left, uint hash)
        {
            var buffer = new byte[BLOCK_SIZE];

            while (left > 0)
            {
                int block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                if (input.Read(buffer, 0, block) != block)
                {
                    throw new EndOfStreamException();
                }
                hash = Adler32(buffer, 0, block, hash);
                left -= block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                input.Seek(block, SeekOrigin.Current);
                left -= block;
            }

            return hash;
        }

        private static uint ComputeIncompleteHashOfZip(Stream input, uint hash)
        {
            hash = ComputeHashOfFile(input, hash);

            // get basic zip info
            input.Seek(-22, SeekOrigin.End);
            if (input.ReadValueU32() != 0x06054B50)
            {
                throw new FormatException();
            }

            input.Seek(8, SeekOrigin.Current);
            var indexSize = input.ReadValueU32();
            var indexOffset = input.ReadValueU32();

            input.Seek(indexOffset, SeekOrigin.Begin);
            hash = ComputeIncompleteHashOfBlock(input, indexSize.Align(BLOCK_SIZE), hash);

            return hash;
        }

        private static void UnwindZipData(Stream input, Stream output)
        {
            var buffer = new byte[BLOCK_SIZE];
            long left;

            input.Seek(0, SeekOrigin.Begin);
            left = input.Length;
            while (left > 0)
            {
                int block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                if (input.Read(buffer, 0, block) != block)
                {
                    break;
                }
                output.Write(buffer, 0, block);
                left -= block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                input.Seek(block, SeekOrigin.Current);
            }

            input.Seek(-22, SeekOrigin.End);
            if (input.ReadValueU32() != 0x06054B50)
            {
                throw new FormatException();
            }

            input.Seek(12, SeekOrigin.Current);
            var indexOffset = input.ReadValueU32();

            input.Seek(indexOffset, SeekOrigin.Begin);
            left = input.Length - indexOffset;
            while (left > 0)
            {
                int block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                if (input.Read(buffer, 0, block) != block)
                {
                    break;
                }
                output.Write(buffer, 0, block);
                left -= block;

                block = (int)Math.Min(BLOCK_SIZE, left);
                input.Seek(block, SeekOrigin.Current);
            }
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                },
                {
                    "o|overwrite",
                    "overwrite files",
                    v => overwriteFiles = v != null
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

            if (extras.Count < 2 ||
                extras.Count > 3 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ original_file.zip input_file.zip [output_file.zip]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string originalPath = extras[0];
            string inputPath = extras[1];
            string outputPath = extras.Count > 2 ? extras[2] : inputPath;

            const uint initialHash = 1;

            using (var original = File.OpenRead(originalPath))
            {
                var targetHash = ComputeHashOfZip(original, initialHash);

                if (verbose == true)
                {
                    Console.WriteLine("target hash is {0:X8}", targetHash);
                }

                using (var input = File.OpenRead(inputPath))
                {
                    var checkHash = ComputeHashOfZip(input, initialHash);
                    if (checkHash == targetHash)
                    {
                        Console.WriteLine("File hash is already {0:X8}!!", targetHash);
                        Environment.ExitCode = 0;
                        return;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("actual hash is {0:X8}", checkHash);
                    }
                }

                if (inputPath != outputPath)
                {
                    File.Copy(inputPath, outputPath, overwriteFiles);
                }

                using (var stream = File.Open(outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    var initialLength = stream.Length;

                    stream.Seek(-22, SeekOrigin.End);

                    var pk = new PKEndHeader();
                    pk.Magic = stream.ReadValueU32();
                    if (pk.Magic != 0x06054B50)
                    {
                        throw new FormatException();
                    }

                    pk.LocalDiskNumber = stream.ReadValueU16();
                    pk.DirectoryDiskNumber = stream.ReadValueU16();
                    pk.LocalIndexCount = stream.ReadValueU16();
                    pk.TotalIndexCount = stream.ReadValueU16();
                    pk.IndexSize = stream.ReadValueU32();
                    pk.IndexOffset = stream.ReadValueU32();
                    pk.CommentLength = stream.ReadValueU16();

                    if (pk.CommentLength != 0)
                    {
                        throw new FormatException();
                    }

                    var index = new byte[pk.IndexSize];
                    stream.Seek(pk.IndexOffset, SeekOrigin.Begin);
                    if (stream.Read(index, 0, index.Length) != index.Length)
                    {
                        throw new EndOfStreamException();
                    }

                    // let's weave a web of ~magic~
                    long length = 0;

                    // add aligned data size
                    length += stream.Length.Align(BLOCK_SIZE);

                    var blocks = length / BLOCK_SIZE;
                    if ((blocks % 2) == 0)
                    {
                        blocks++;
                        length += BLOCK_SIZE;
                    }

                    long newIndexOffset = length;
                    long indexSize = index.Length.Align(BLOCK_SIZE);
                    length += indexSize;
                    blocks += indexSize / BLOCK_SIZE;

                    if ((blocks % 2) == 0)
                    {
                        blocks++;
                        length += BLOCK_SIZE;
                    }

                    var magicOffset = length;

                    /* TODO: generate magic here so magic can be longer than one block
                     * if somehow it manages to not fit
                     */
                    blocks++;
                    length += BLOCK_SIZE;

                    var unwoundOffset = length + BLOCK_SIZE;

                    /* This data follows the magic data
                     * 
                     * At this point in hashing, the hash is 0, which then becomes 1,
                     * the initial hash, and then the original file data is hashed
                     * causing our output hash to be the target hash :)
                     */
                    var unwoundData = new MemoryStream();
                    long unwoundLength;
                    using (var temp = new MemoryStream())
                    {
                        temp.Position = 0;
                        temp.WriteValueU8(1); // hello, initial hash!
                        UnwindZipData(original, temp);

                        /* gay code move the unwound data to the end of the aligned
                         * block sizes so I don't have to think about things later
                         * when writing the unwound data out */
                        unwoundLength = temp.Length.Align(BLOCK_SIZE);
                        unwoundData.Seek(unwoundLength - temp.Length, SeekOrigin.Begin);
                        temp.Position = 0;
                        unwoundData.WriteFromStream(temp, temp.Length);
                        //unwoundData.SetLength(unwoundLength);
                        unwoundData.Position = 0;
                    }

                    blocks += (unwoundLength * 2) / BLOCK_SIZE;
                    length += unwoundLength * 2;

                    if ((blocks % 2) == 1)
                    {
                        throw new InvalidOperationException();
                    }

                    var pkEndHeaderOffset = length;
                    length += 22;

                    stream.SetLength(length);
                    stream.Seek(newIndexOffset, SeekOrigin.Begin);
                    stream.Write(index, 0, index.Length);

                    stream.Seek(pkEndHeaderOffset, SeekOrigin.Begin);
                    stream.WriteValueU32(pk.Magic);
                    stream.WriteValueU16(pk.LocalDiskNumber);
                    stream.WriteValueU16(pk.DirectoryDiskNumber);
                    stream.WriteValueU16(pk.LocalIndexCount);
                    stream.WriteValueU16(pk.TotalIndexCount);
                    stream.WriteValueU32(pk.IndexSize);
                    stream.WriteValueU32((uint)newIndexOffset);
                    stream.WriteValueU16(pk.CommentLength);

                    stream.Seek(unwoundOffset, SeekOrigin.Begin);
                    while (unwoundData.Position < unwoundData.Length)
                    {
                        stream.WriteFromStream(unwoundData, Math.Min(BLOCK_SIZE, unwoundData.Length - unwoundData.Position));
                        stream.Seek(BLOCK_SIZE, SeekOrigin.Current);
                    }

                    var baseHash = ComputeIncompleteHashOfZip(stream, initialHash);

                    if (verbose == true)
                    {
                        Console.WriteLine("base hash is {0:X8}", baseHash);
                    }

                    var magic = new byte[BLOCK_SIZE];

                    // now we know the pre-hash, let's reset it to 0 :)
                    uint upper, lower;
                    upper = (ushort)((baseHash & 0xFFFF0000) >> 16);
                    lower = (ushort)((baseHash & 0x0000FFFF) >> 0);

                    /* generates bytes to cause lower to end up as 0xFFF0,
                     * which has the nice effect of causing upper to subtract
                     * by 1 each pass of a 0 byte.
                     */
                    int offset = 0;
                    while (lower < 0xFFF0)
                    {
                        uint d = 0xFFF0 - lower;
                        magic[offset] = (byte)(Math.Min(0xFF, d));
                        lower = (lower + magic[offset]) % 0xFFF1;
                        upper = (upper + lower) % 0xFFF1;
                        offset++;
                    }

                    // these 0 bytes cause upper to become 0xFFF0
                    offset += (int)upper + 1;

                    // and this changes lower to 0
                    magic[offset] = (byte)(0xFFF1 - lower);

                    /* ... FFF0 0000
                     * so if the next byte is 1, hash becomes 1 :)
                     */

                    if (verbose == true)
                    {
                        Console.WriteLine("generated {0} bytes of magic", offset);
                    }

                    stream.Seek(magicOffset, SeekOrigin.Begin);
                    stream.Write(magic, 0, magic.Length);

                    var finalHash = ComputeHashOfZip(stream, 1);
                    if (finalHash != targetHash)
                    {
                        if (verbose == true)
                        {
                            Console.WriteLine("failure! final hash of {0:X8}", finalHash);
                        }

                        Console.WriteLine("Failed to generate a working fixed zip? :(");
                        Environment.ExitCode = -1;
                    }
                    else
                    {
                        if (verbose == true)
                        {
                            Console.WriteLine("success! final hash of {0:X8}", finalHash);
                            Console.WriteLine("size: {0} -> {1}",
                                initialLength, stream.Length);
                        }
                        Environment.ExitCode = 0;
                    }
                }
            }
        }
    }
}
