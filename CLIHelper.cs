/*
  HLTools CLI - Helper
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
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Text;

namespace HLTools
{
    /// <summary>
    /// Helper functions for the command line utility.
    /// </summary>
    class CLIHelper
    {
        public struct PngTransparency
        {
            public int length;
            public byte[] name;
            public byte[] data;
            public int crc;
        }
        /* Table of CRCs of all 8-bit messages. */
        public ulong[] crc_table = new ulong[256];

        /* Flag: has the table been computed? Initially false. */
        private bool crc_table_computed = false;

        public bool debugHex = false;

        /* Make the table for a fast CRC. */
        public void make_crc_table()
        {
            ulong c;
            int n, k;

            for (n = 0; n < 256; n++)
            {
                c = (ulong)n;
                for (k = 0; k < 8; k++)
                {
                    if ((c & 1) != 0)
                        c = 0xedb88320L ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crc_table[n] = c;
            }
            crc_table_computed = true;
        }


        /// <summary>
        /// Transparency adder for indexed color Bitmaps.
        /// </summary>
        /// <param name="fullSavePath">Path the file was saved.</param>
        /// <param name="imageBitmap">Bitmap to extract palette data from.</param>
        public void addTransparency(
            string fullSavePath,
            Bitmap imageBitmap
        )
        {
            bool isTransPal = false;
            for (int i = 0; i < imageBitmap.Palette.Entries.Length; i++)
            {
                if (imageBitmap.Palette.Entries[i].A != 255)
                    isTransPal = true; // there is transparency here!
            }
            if (!isTransPal)
                return; // no cleaning up here, this is why it's at the start
            FileStream oldPng = new FileStream(fullSavePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            BinaryReader oldPngReader = new BinaryReader(oldPng, Encoding.ASCII);
            byte[] oldPngArr = oldPngReader.ReadBytes((int)oldPng.Length);

            List<byte> reEncPng = new List<byte>();


            uint plteChunkIEnd = 0;
            if ((Encoding.ASCII.GetString(oldPngArr).IndexOf("PLTE") - 4) >= 0)
                plteChunkIEnd = (uint)Encoding.ASCII.GetString(oldPngArr).IndexOf("PLTE") - 4;
            oldPng.Position = plteChunkIEnd;
            uint plteChunkLen = BinaryPrimitives.ReverseEndianness(oldPngReader.ReadUInt32());

            plteChunkIEnd = plteChunkIEnd + plteChunkLen + 4 + 4 + 4;
            // add data length, type length, length length, and crc length. All of which are 4 except data length.

            oldPngReader.Close();
            oldPng.Close();

            for (int i = 0; i < plteChunkIEnd; i++)
            {
                reEncPng.Add(oldPngArr[i]);
            }
            PngTransparency transChunk = new PngTransparency();

            /*
                * 32-bit integer     - length; always 256
                * 4-byte string      - name; always tRNS
                * 256-byte uint8 arr - data; palette data
                * 32-bit integer     - CRC32 checksum of name and palette data, no length included
                */
            transChunk.length = 256;
            transChunk.name = Encoding.ASCII.GetBytes("tRNS");
            transChunk.data = new byte[transChunk.length];
            for (int i = 0; i < transChunk.length; i++)
            {
                transChunk.data[i] = imageBitmap.Palette.Entries[i].A;
            }
            // TODO: this stupid little transparency thing is so memory inefficient I am literally in tears
            byte[] toCheckSum = new byte[transChunk.length + transChunk.name.Length];
            Buffer.BlockCopy(transChunk.name, 0, toCheckSum, 0, transChunk.name.Length);
            Buffer.BlockCopy(transChunk.data, 0, toCheckSum, transChunk.name.Length, transChunk.length);

            if (debugHex)
            {
                for (int i = 0; i < toCheckSum.Length; i++)
                {
                    if (i % 20 == 0)
                        Console.Write("\n");
                    Console.Write("{0:X2} ", toCheckSum[i]);
                }
                Console.Write("\n");
            }

            transChunk.crc = (int)crc(toCheckSum);

            int transChunkSize = 4 + transChunk.name.Length + transChunk.length + 4;
            byte[] transChunkBytes = new byte[transChunkSize];


            // LENGTH
            byte[] transChunkAttrLen = BitConverter.GetBytes(transChunk.length);
            // we don't check for endianness type here, because if we were big endian, everything would else would be screwed up
            Array.Reverse(transChunkAttrLen);
            for (int i = 0; i < transChunkAttrLen.Length; i++)
            {
                transChunkBytes[0 + i] = transChunkAttrLen[i];
            }

            // COPY FROM TOCHECKSUM
            Buffer.BlockCopy(toCheckSum, 0, transChunkBytes, transChunkAttrLen.Length, toCheckSum.Length);

            // CRC
            byte[] transChunkAttrCrc = BitConverter.GetBytes(transChunk.crc);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(transChunkAttrCrc);
            for (int i = 0; i < transChunkAttrCrc.Length; i++)
            {
                transChunkBytes[transChunkAttrLen.Length + toCheckSum.Length + i] = transChunkAttrCrc[i];
            }

            if (debugHex)
            {
                for (int i = 0; i < transChunkBytes.Length; i++)
                {
                    if (i % 20 == 0)
                        Console.Write("\n");
                    Console.Write("{0:X2} ", transChunkBytes[i]);
                }
                Console.Write("\n");
            }


            // it took years of work and hours of... uh... more work, but we are finally here
            for (int i = 0; i < transChunkBytes.Length; i++)
            {
                reEncPng.Add(transChunkBytes[i]);
            }

            for (uint i = plteChunkIEnd; i < oldPngArr.Length; i++)
            {
                reEncPng.Add(oldPngArr[i]);
            }

            FileStream newPng = new FileStream(string.Concat(fullSavePath, ".trans"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            BinaryWriter newPngWriter = new BinaryWriter(newPng, Encoding.ASCII, true);

            newPngWriter.Write(reEncPng.ToArray());

            newPngWriter.Close();
            newPng.Close();
        }

        /* Update a running CRC with the bytes buf[0..len-1]--the CRC
        should be initialized to all 1's, and the transmitted value
        is the 1's complement of the final running CRC (see the
        crc() routine below). */
        public ulong update_crc(ulong crc, byte[] buf)
        {
            ulong c = crc;

            if (!crc_table_computed)
                make_crc_table();
            for (int n = 0; n < buf.Length; n++)
            {
                c = crc_table[(c ^ buf[n]) & 0xff] ^ (c >> 8);
            }
            return c;
        }

        /* Return the CRC of the bytes buf[0..len-1]. */
        public ulong crc(byte[] buf)
        {
            return update_crc(0xffffffffL, buf) ^ 0xffffffffL;
        }
    }
}