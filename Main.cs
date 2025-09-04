/*
  HLTools CLI
  Copyright © 2025 Max Pary (Buggem)
  
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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace HLTools {
    class HLToolsCLI
    {
        private static string sprDetect = "Sprite file detected...";
        private static string wadDetect = "WAD file detected...";
        private static string mdlDetect = "Model file detected...";
        private static string helpStr =
@"Half-Life Texture Tools - a GoldSource texture extractor
Usage: hltools.exe [infile.wad/.spr/.mdl] [outfolder]



PROTIP: you can also use this utility to determine if a
        file without a valid extension is actually
        GoldSrc compatible.";
        private static int fileType = -1;
        public static CLIHelper FuncHelper;
        /// <summary>
        /// Extract and save a file.
        /// </summary>
        /// <param name="fileType">The type of file. 0=spr, 1=wad, 2=mdl</param>
        /// <param name="filename">Path to file to extract.</param>
        /// <param name="savedir">Path to extract files to.</param>
        /// <exception cref="HLToolsUnsupportedFile"></exception>
        static void ExtractFile(
            string filename,
            string savedir,
            WAD3Loader wad3l,
            bool transparency = false
        )
        {
            Console.WriteLine(string.Concat("Reading WADfile ", Path.GetFileName(filename), "..."));
            wad3l.LoadFile(filename); // this only loads the wad, other stuff is done with other stuff

            // we texture, and we name. let's texturename!
            List<WAD3Loader.Texture> textures = new List<WAD3Loader.Texture>();
            List<string> textureNames = new List<string>();

            // loop through WAD lumps, only grab textures (I mean do GoldSource wads store anything else?)
            for (int j = 0; j < wad3l.LumpsInfo.Count; j++)
            {
                byte type = wad3l.LumpsInfo[j].Type;

                // see WAD3Loader.cs for documentation on what these are
                if (type == 0x40 || type == 0x42 || type == 0x43 || type == 0x46)
                {
                    textures.Add(wad3l.GetLumpImage(j, transparency));
                    textureNames.Add(wad3l.LumpsInfo[j].Name);
                }
            }
            // literal copy-paste from sprites
            for (int j = 0; j < textures.Count; j++)
            {
                Console.WriteLine(string.Concat("Extracting texture ", textureNames[j], " from WAD ", Path.GetFileName(filename), "..."));

                string fullSavePath = string.Concat(savedir, "/", textureNames[j], ".png");

                textures[j].Image.Save(fullSavePath);

                if (transparency)
                {
                    FuncHelper.addTransparency(fullSavePath, textures[j].Image); // this one line goes deeper than you think! check CLIHelper.cs for more information
                }
            }
        }
        static void ExtractFile(
            string filename,
            string savedir,
            SpriteLoader sl,
            bool transparency = false
        )
        {
            Console.WriteLine(string.Concat("Reading model ", Path.GetFileName(filename), "..."));
            SpriteLoader.Frame[] outSpr = sl.LoadFile(filename, transparency); // transparency will be ignored by default, so omit that parameter

            for (int j = 0; j < outSpr.Length; j++)
            {
                Console.WriteLine(string.Concat("Extracting ", Path.GetFileName(filename), " frame ", j, "..."));

                string fullSavePath = string.Concat(savedir, "/", Path.GetFileNameWithoutExtension(filename), j, ".png");
                outSpr[j].Image.Save(fullSavePath);

                if (transparency)
                {
                    FuncHelper.addTransparency(fullSavePath, outSpr[j].Image); // this one line goes deeper than you think! check CLIHelper.cs for more information
                }
            }
        }
        static void ExtractFile(
            string filename,
            string savedir,
            MDLLoader mdll,
            bool transparency = false
        )
        {
            Console.WriteLine(string.Concat("Reading ", Path.GetFileName(filename), "..."));
            if (!mdll.LoadFile(filename, transparency))
            {
                Console.WriteLine("[ERROR] General failure!");
                return;
            }

            for (int j = 0; j < mdll.mstudioTextures.Count; j++)
            {
                string parsedTexName = string.Empty;
                for (int k = 0; k < mdll.mstudioTextures[j].name.Length; k++)
                {
                    if (mdll.mstudioTextures[j].name[k] == '\0')
                        break;
                    if (
                            Path.GetInvalidPathChars().Contains(mdll.mstudioTextures[j].name[k]) ||
                            Path.GetInvalidFileNameChars().Contains(mdll.mstudioTextures[j].name[k])
                    )
                    {
                        parsedTexName += '_';
                    }
                    else
                    {
                        parsedTexName += mdll.mstudioTextures[j].name[k];
                    }
                }
                //Console.WriteLine(string.Concat(parsedTexName, " ---- ", new string(mdll.mstudioTextures[j].name), " ---- ", parsedTexName == new string(mdll.mstudioTextures[j].name)));
                Console.WriteLine(string.Concat("Extracting ", Path.GetFileName(filename), " texture ", Path.GetFileName(parsedTexName), "..."));
                //Console.WriteLine(mdll.outTextures[j] == null);

                string fullSavePath = string.Concat(savedir, "/", Path.GetFileNameWithoutExtension(parsedTexName), ".png");
                mdll.outTextures[j].Save(fullSavePath);

                if (transparency)
                {
                    FuncHelper.addTransparency(fullSavePath, mdll.outTextures[j]); // this one line goes deeper than you think! check CLIHelper.cs for more information
                }
            }
        }
        static void Main(string[] args)
        {
            if (args.Length > 3 || args.Length < 1)
            {
                Console.WriteLine(helpStr);
                Environment.Exit(0);
            }

            bool transparency = false;
            if (args.Length > 2)
            {
                transparency = (args[2] == "-t");
            }

            // no more argument handling here
            FuncHelper = new CLIHelper();

            // do a test, only exit if we need it though
            ulong crcTestResult = FuncHelper.crc(new byte[] { 0x68, 0x61, 0x68, 0x61 });
            if (crcTestResult != 22155654 && transparency)
            {
                Console.WriteLine(string.Concat("CRC not working as intended, which is required for transparent PNG encoding!\ncrc(haha,4)=", crcTestResult, "\nQuitting!"));
                Environment.Exit(1);
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (i == 0)
                {
                    Console.WriteLine("Handling file input...");
                    if (args[0].EndsWith(".spr"))
                    {
                        Console.WriteLine(sprDetect);
                        fileType = 0;
                    }
                    else if (args[0].EndsWith(".wad"))
                    {
                        Console.WriteLine(wadDetect);
                        fileType = 1;
                    }
                    else if (args[0].EndsWith(".mdl"))
                    {
                        Console.WriteLine(mdlDetect);
                        fileType = 2;
                    }
                    else if (File.Exists(args[i]))
                    {
                        // revert to checking magic number

                        try
                        {
                            FileStream stream = new FileStream(args[i], FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                            BinaryReader binr = new BinaryReader(stream, Encoding.ASCII);
                            // only the first 4 chars are needed to recognise a sprite/wad
                            string header = new string(binr.ReadChars(4));
                            if (header == SpriteLoader.SpriteHeaderId)
                            {
                                Console.WriteLine(sprDetect);
                                fileType = 0;
                            }
                            else if (header == Encoding.ASCII.GetString(WAD3Loader.WadHeaderId))
                            {
                                Console.WriteLine(wadDetect);
                                fileType = 1;
                            }
                            else if (header == MDLLoader.ModelHeaderId)
                            {
                                Console.WriteLine(mdlDetect);
                                fileType = 2;
                            }
                            else
                            {
                                throw new HLToolsUnsupportedFile("Unknown file type!");
                            }

                            binr.Close();
                            stream.Close();
                            Console.WriteLine("Magic number test stream closed.");
                        }
                        catch (IOException e)
                        {
                            throw new HLToolsUnsupportedFile(e.Message);
                        }
                    }
                    else if (Directory.Exists(args[i]))
                    {
                        fileType = 3; // recursive scan
                    }
                    else
                    {
                        throw new HLToolsUnsupportedFile("The file does not exist!");
                    }
                }
                if (i == 1)
                {
                    if (fileType == 1)
                    {
                        WAD3Loader wad3l = new WAD3Loader();

                        ExtractFile(args[0], args[i], wad3l, transparency);

                        // close wad reader gracefully
                        wad3l.Close();
                    }
                    else if (fileType == 0)
                    {
                        SpriteLoader sl = new SpriteLoader();

                        ExtractFile(args[0], args[i], sl, transparency);

                        // close sprite reader gracefully
                        sl.Close();
                    }
                    else if (fileType == 2)
                    {
                        MDLLoader mdll = new MDLLoader();

                        ExtractFile(args[0], args[i], mdll, transparency);

                        // close model reader gracefully
                        mdll.Close();


                    }
                    else if (fileType == 3)
                    {
                        string[] sprFiles = Directory.GetFiles(args[0], "*.spr", SearchOption.AllDirectories);
                        SpriteLoader sl = new SpriteLoader();

                        Console.WriteLine(string.Concat("Recursively scanning ", args[0], " for Sprites..."));
                        for (int k = 0; k < sprFiles.Length; k++)
                        {
                            string relativePathTo = Path.GetDirectoryName(Path.GetRelativePath(args[0], sprFiles[k]));
                            // create directory
                            Directory.CreateDirectory(string.Concat(args[1], "/", relativePathTo));

                            ExtractFile(sprFiles[k], string.Concat(args[1], "/", relativePathTo), sl, transparency);

                        }


                        string[] wadFiles = Directory.GetFiles(args[0], "*.wad", SearchOption.AllDirectories);
                        WAD3Loader wad3l = new WAD3Loader();


                        Console.WriteLine(string.Concat("Recursively scanning ", args[0], " for WADs..."));
                        for (int k = 0; k < wadFiles.Length; k++)
                        {
                            // create directory
                            Directory.CreateDirectory(string.Concat(args[1], "/wads/", Path.GetFileNameWithoutExtension(wadFiles[k])));
                            Console.WriteLine(string.Concat("Extracting ", Path.GetFileName(wadFiles[k]), "..."));

                            // string.Concat(args[1], "/wads/", Path.GetFileNameWithoutExtension(wadFiles[k]))
                            ExtractFile(wadFiles[k], string.Concat(args[1], "/wads/", Path.GetFileNameWithoutExtension(wadFiles[k])), wad3l, transparency);
                        }

                        string[] mdlFiles = Directory.GetFiles(args[0], "*.mdl", SearchOption.AllDirectories);
                        MDLLoader mdll = new MDLLoader();


                        Console.WriteLine(string.Concat("Recursively scanning ", args[0], " for models..."));
                        for (int k = 0; k < mdlFiles.Length; k++)
                        {
                            string relativePathTo = Path.GetDirectoryName(Path.GetRelativePath(args[0], mdlFiles[k]));
                            // create directory
                            Directory.CreateDirectory(string.Concat(args[1], "/", relativePathTo));

                            ExtractFile(mdlFiles[k], string.Concat(args[1], "/", relativePathTo), mdll, transparency);
                        }

                        sl.Close();
                        wad3l.Close();
                        mdll.Close();
                    }
                }
            }

        }
    }
}
