/*
  HLTools CLI
  Copyright © 2006-2024 Juraj Novák (Yuraj)
  
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
using System.Text;
using System.Collections.Generic;

namespace HLTools {
    class HLToolsCLI
    {
        private static string sprDetect = "Sprite file detected...";
        private static string wadDetect = "WAD file detected...";
        private static string helpStr =
@"Half-Life Texture Tools - a GoldSource texture extractor
Usage: hltools.exe [infile.wad/.spr] [outfolder]



PROTIP: you can also use this utility to determine if a
        file without a valid extension is actually
        GoldSrc compatible.";
        private static int fileType = -1;
        static void Main(string[] args)
        {
            if (args.Length > 2 || args.Length < 1)
            {
                Console.WriteLine(helpStr);
                Environment.Exit(0);
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
                    } else if(Directory.Exists(args[i]))
                    {
                        fileType = 2; // recursive scan
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
                        Console.WriteLine(string.Concat("Reading WADfile ", Path.GetFileName(args[0]), "..."));
                        wad3l.LoadFile(args[0]); // this only loads the wad, other stuff is done with other stuff

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
                                textures.Add(wad3l.GetLumpImage(j)); // same deal as sprites, omit transparancy param as it's default is good
                                textureNames.Add(wad3l.LumpsInfo[j].Name);
                            }
                        }
                        // literal copy-paste from sprites
                        for (int j = 0; j < textures.Count; j++)
                        {
                            Console.WriteLine(string.Concat("Extracting texture ", textureNames[j], " from WAD ", Path.GetFileName(args[0]), "..."));
                            textures[j].Image.Save(string.Concat(args[1], "/", textureNames[j], ".png"));
                        }
                        // close wad reader gracefully
                        wad3l.Close();
                    }
                    else if (fileType == 0)
                    {
                        SpriteLoader sl = new SpriteLoader();

                        Console.WriteLine(string.Concat("Reading ", Path.GetFileName(args[0]), "..."));
                        SpriteLoader.Frame[] outSpr = sl.LoadFile(args[0]); // transparency will be ignored by default, so omit that parameter

                        for (int j = 0; j < outSpr.Length; j++)
                        {
                            Console.WriteLine(string.Concat("Extracting ", Path.GetFileName(args[0]), " frame ", j, "..."));
                            outSpr[j].Image.Save(string.Concat(args[1], "/", Path.GetFileNameWithoutExtension(args[0]), j, ".png"));
                        }
                        // close sprite reader gracefully
                        sl.Close();
                    }
                    else if (fileType == 2)
                    {
                        string[] sprFiles = Directory.GetFiles(args[0], "*.spr", SearchOption.AllDirectories);
                        SpriteLoader sl = new SpriteLoader();

                        Console.WriteLine(string.Concat("Recursively scanning ", args[0], " for Sprites..."));
                        for (int k = 0; k < sprFiles.Length; k++)
                        {
                            SpriteLoader.Frame[] outSpr = sl.LoadFile(sprFiles[k]); // transparency will be ignored by default, so omit that parameter
                            string relativePathTo = Path.GetDirectoryName(Path.GetRelativePath(args[0], sprFiles[k]));
                            // create directory
                            Directory.CreateDirectory(string.Concat(args[1], "/", relativePathTo));

                            for (int j = 0; j < outSpr.Length; j++)
                            {
                                Console.WriteLine(string.Concat("Extracting ", Path.GetFileName(sprFiles[k]), " frame ", j, "..."));
                                outSpr[j].Image.Save(string.Concat(args[1], "/", relativePathTo, "/", Path.GetFileNameWithoutExtension(sprFiles[k]), j, ".png"));
                            }
                        }


                        string[] wadFiles = Directory.GetFiles(args[0], "*.wad", SearchOption.AllDirectories);
                        WAD3Loader wad3l = new WAD3Loader();


                        Console.WriteLine(string.Concat("Recursively scanning ", args[0], " for WADs..."));
                        for (int k = 0; k < wadFiles.Length; k++)
                        {
                            wad3l.LoadFile(wadFiles[k]);
                            List<WAD3Loader.Texture> textures = new List<WAD3Loader.Texture>();
                            List<string> textureNames = new List<string>();

                            // loop through WAD lumps, only grab textures (I mean do GoldSource wads store anything else?)
                            for (int j = 0; j < wad3l.LumpsInfo.Count; j++)
                            {
                                byte type = wad3l.LumpsInfo[j].Type;

                                // see WAD3Loader.cs for documentation on what these are
                                if (type == 0x40 || type == 0x42 || type == 0x43 || type == 0x46)
                                {
                                    textures.Add(wad3l.GetLumpImage(j)); // same deal as sprites, omit transparancy param as it's default is good
                                    textureNames.Add(wad3l.LumpsInfo[j].Name);
                                }
                            }

                            // create directory
                            Directory.CreateDirectory(string.Concat(args[1], "/wads/", Path.GetFileNameWithoutExtension(wadFiles[k])));
                            Console.WriteLine(string.Concat("Extracting ", Path.GetFileName(wadFiles[k]), "..."));

                            // literal copy-paste from sprites
                            for (int j = 0; j < textures.Count; j++)
                            {
                                Console.WriteLine(string.Concat("Extracting texture ", textureNames[j], " from WAD ", Path.GetFileName(wadFiles[k]), "..."));
                                textures[j].Image.Save(string.Concat(args[1], "/wads/", Path.GetFileNameWithoutExtension(wadFiles[k]), "/", textureNames[j], ".png"));
                            }

                        }

                        sl.Close();
                        wad3l.Close();
                    }
                }
            }

        }
    }
}
