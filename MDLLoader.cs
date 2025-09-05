/*
  MDL component for Half-Life Texture Tools
  Copyleft Max Parry 2025

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FreeImageAPI;

namespace HLTools
{

    /// <summary>
    /// GoldSrc MDL Parser
    /// Written by Max Parry
    /// </summary>
    public class MDLLoader
    {
        /// <summary>
        /// Model header.
        /// Based off documentation at https://github.com/malortie/assimp/wiki/MDL:-Half-Life-1-file-format and the HLSDK
        /// </summary>
        public struct MDLHeader
        {
            public char[] id; // NOTE: we only accept IDST not IDSQ as Q seems to be for 
            public int version;
            public char[] name;
            public int length;
            // BIG gap here
            public int numTextures;
            public int textureIndex;
            public int textureDataIndex;


        }

        /// <summary>
        /// Representation of a MDL file's texture.
        /// </summary>
        public struct MDLTexture
        {
            public char[] name;
            public UInt32 flags;
            public int width;
            public int height;
            public int index;
        }

        public MDLHeader ModelHeader { get; private set; }
        public string Filename { get; private set; }

        public const string ModelHeaderId = "IDST";
        private static readonly Encoding DefaultEncoding = Encoding.ASCII;

        private BinaryReader binReader;
        private FileStream fs;
        public List<MDLTexture> mstudioTextures = new List<MDLTexture>();
        public List<Bitmap> outTextures = new List<Bitmap>();


        /// <summary>
        /// Load and read a MDL file.
        /// </summary>
        /// <param name="inputFile">Input file.</param>
        /// <param name="nonFatal">Whether not to throw fatal errors.</param>
        /// <exception cref="HLToolsUnsupportedFile"></exception>
        public bool LoadFile(string inputFile, bool transparency = false)
        {
            bool nonFatal = true;
            Filename = inputFile;

            // reset previous texture list and file handlers
            mstudioTextures = new List<MDLTexture>();
            outTextures = new List<Bitmap>();
            Close();

            fs = new FileStream(inputFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            binReader = new BinaryReader(fs, DefaultEncoding);

            // try to get header ID
            MDLHeader modelHeader = new MDLHeader();
            modelHeader.id = binReader.ReadChars(4);

            string magic = new string(modelHeader.id);
            if (magic != ModelHeaderId) // the model is invalid
            {
                Console.WriteLine(
                    String.Concat("Non-MDL file found with MDL extension. Ignoring...\nheader=", magic)
                );
                // non-fatal error
                Close();
                return false;
            }

            modelHeader.version = binReader.ReadInt32();
            modelHeader.name = binReader.ReadChars(64);
            modelHeader.length = binReader.ReadInt32(); /* This is an annoying limitation
                                                           of files only being up to ~2GB,
                                                           which may seem fine, but in modern
                                                           standards is unacceptable.
                                                           
                                                           One use case for this being unacceptable
                                                           would be programs that use merge model
                                                           systems, such as the GoldSrc.one server.
                                                        */

            fs.Position = (64 + (12 * 5) + (4 * 14));   /* 
                                                           Should be equal to fs.Position += ((12 * 5) + (4 * 11));
                                                           Goes to 64 + 60 + 56 = 180 byte seek
                                                        */

            // version specific hacky hacks
            if (modelHeader.version == 6)
            {
                /* 
                   As this model version (used in 0.52 alpha for
                   most models, with some exceptions) uses a
                   different header structure, a different
                   section to skim to is needed.

                   NOTE: for some reason my previous index here was off by 2; hexdump sucked maybe?
                */
                fs.Position = 0x64;
            }
            modelHeader.numTextures = binReader.ReadInt32();
            Console.WriteLine(modelHeader.numTextures);
            modelHeader.textureIndex = binReader.ReadInt32();
            modelHeader.textureDataIndex = binReader.ReadInt32(); // goes unused, here for reasons I cannot and will not explain

            ModelHeader = modelHeader;
            // before we give the go-ahead, check it's not 0 (no idea why, but that's what the HLSDK does)
            // also do an extra check for negatives, because FileStream.Position doesn't like that for obvious reasons
            if (modelHeader.textureIndex <= 0 && !nonFatal)
            {
                throw new HLToolsUnsupportedFile("Header's first texture index is 0 or negative! Are you trying to extract an untextured file?");
            }
            else if (modelHeader.textureIndex <= 0)
            {
                Console.WriteLine("Header's first texture index is 0 or negative! Are you trying to extract an untextured file?\nIgnoring...");

                Close();
                return false;
            }
            // don't shut file reader and stuff for fatal exceptions as that is handled by the OS/Mono on exit

            fs.Position = modelHeader.textureIndex;

            MDLTexture tmptex;
            for (int i = 0; i < modelHeader.numTextures; i++)
            {
                tmptex = new MDLTexture();
                tmptex.name = binReader.ReadChars(64);
                tmptex.flags = binReader.ReadUInt32();
                tmptex.width = binReader.ReadInt32();
                tmptex.height = binReader.ReadInt32();
                tmptex.index = binReader.ReadInt32();
                mstudioTextures.Add(tmptex);
                Console.WriteLine(string.Concat("flags=", Convert.ToString(tmptex.flags, 2).PadLeft(32, '0')));
            }
            for (int i = 0; i < mstudioTextures.Count; i++)
            {
                tmptex = mstudioTextures[i];

                fs.Position = tmptex.index;
                // Console.WriteLine(string.Concat("Extracting texture ", new string(tmptex.name), "..."));
                var tmpBitmap = new Bitmap(tmptex.width, tmptex.height, PixelFormat.Format8bppIndexed);
                
                byte[] pixels = binReader.ReadBytes(tmptex.width * tmptex.height);

                // code from the Sprite Loader, it's here because it's better
                BitmapData tBdata = tmpBitmap.LockBits(
                    new Rectangle(0, 0, tmpBitmap.Width, tmpBitmap.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format8bppIndexed
                );
                Marshal.Copy(pixels, 0, tBdata.Scan0, pixels.Length);
                tmpBitmap.UnlockBits(tBdata);

                byte[] palBytes = binReader.ReadBytes(256 * 3);

                ColorPalette pal = tmpBitmap.Palette;
                for (int pI = 0; pI < 256; pI++)
                {
                    pal.Entries[pI] = Color.FromArgb(
                        palBytes[pI * 3],
                        palBytes[pI * 3 + 1],
                        palBytes[pI * 3 + 2]
                    );
                    if (transparency) {
                        if ((tmptex.flags & 0x0040) != 0) // STUDIO_NF_MASKED
                        {
                            // are we the last in the palette
                            if (pI == 255)
                            {
                                pal.Entries[pI] = Color.FromArgb(0, pal.Entries[pI]);
                                Console.WriteLine("Transparency activated (STUDIO_NF_MASKED)!");
                            }
                        }
                        if ((tmptex.flags & 0x0020) != 0) // STUDIO_NF_ADDITIVE
                        {
                            // we don't support that yet
                        }
                    }
                }
                tmpBitmap.Palette = pal;

                outTextures.Add(tmpBitmap);
        
            }
            return true;

        }
        /// <summary>
        /// Close file.
        /// </summary>
        public void Close()
        {
            binReader?.Close();
            fs?.Close();
        }

    }
}