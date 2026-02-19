using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using static UnityEngine.Rendering.STP;

namespace L2_login
{
    class NewCrypt {
        public static bool verifyChecksum(byte[] raw) {
            return verifyChecksum(raw, 0, raw.Length);
        }

        public static bool verifyChecksum(byte[] raw, int offset, int size) {
            // check if size is multiple of 4 and if there is more then only the checksum
            if ((size & 3) != 0 || size <= 4) {
                return false;
            }

            ulong chksum = 0;
            int count = size - 4;
            ulong check = ulong.MaxValue;
            int i;

            for (i = offset; i < count; i += 4) {
                check = (ulong)raw[i] & 0xff;
                check |= (ulong)raw[i + 1] << 8 & 0xff00;
                check |= (ulong)raw[i + 2] << 0x10 & 0xff0000;
                check |= (ulong)raw[i + 3] << 0x18 & 0xff000000;

                chksum ^= check;
            }

            check = (ulong)raw[i] & 0xff;
            check |= (ulong)raw[i + 1] << 8 & 0xff00;
            check |= (ulong)raw[i + 2] << 0x10 & 0xff0000;
            check |= (ulong)raw[i + 3] << 0x18 & 0xff000000;

            return check == chksum;
        }

        public static void appendChecksum(byte[] raw) {
            appendChecksum(raw, 0, raw.Length);
        }

        public static byte[]  TestCheckSumUniversal()
        {
            // offset = 2, total size = 40 (пример)
            int offset = 2;
            int size = 40;
            byte[] raw = new byte[size];

            // Заполняем произвольными данными первые (N-1) слов:
            int wordsCount = (size / 4) - 1; // число слов, участвующих в XOR (без последнего checksum)
            uint[] words = new uint[wordsCount];

            // Заполним первые wordsCount-1 слов случайными или нужными значениями
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            for (int i = 0; i < wordsCount - 1; i++)
            {
                byte[] b = new byte[4];
                rng.GetBytes(b);
                words[i] = (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
            }

            // вычислим предпоследнее слово так, чтобы XOR всех слов стал 0
            uint x = 0;
            for (int i = 0; i < wordsCount - 1; i++) x ^= words[i];
            words[wordsCount - 1] = x;

            // запишем эти слова в raw начиная с offset (little-endian)
            for (int w = 0; w < wordsCount; w++)
            {
                int pos = offset + w * 4;
                uint v = words[w];
                raw[pos + 0] = (byte)(v & 0xFF);
                raw[pos + 1] = (byte)((v >> 8) & 0xFF);
                raw[pos + 2] = (byte)((v >> 16) & 0xFF);
                raw[pos + 3] = (byte)((v >> 24) & 0xFF);
            }

            // поле контрольной суммы (последние 4 байта) оставляем нулями
            int checksumPos = offset + wordsCount * 4;
            raw[checksumPos + 0] = 0;
            raw[checksumPos + 1] = 0;
            raw[checksumPos + 2] = 0;
            raw[checksumPos + 3] = 0;

            return raw;
        }

  

        public static void AppendChecksumWord(List<byte> buf, int offset = 2, int step = 4, bool pad = false)
        {
            try
            {
                if (buf == null) throw new ArgumentNullException(nameof(buf));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (step <= 0 || step > 8) throw new ArgumentOutOfRangeException(nameof(step), "step must be in range 1..8");

                int n = buf.Count;

   
                if (n < offset)
                {
                    if (pad)
                    {
                        for (int i = 0; i < offset - n; i++) buf.Add(0);
                        n = buf.Count;
                    }
                    else
                    {
                        throw new ArgumentException("Buffer too small for offset; use pad=true to auto-extend");
                    }
                }

    
                int rem = (n - offset) % step;
                if (rem != 0)
                {
                    if (pad)
                    {
                        int need = step - rem;
                        for (int i = 0; i < need; i++) buf.Add(0);
                        n = buf.Count;
                    }
                    else
                    {
                        throw new ArgumentException("length-offset is not multiple of step; use pad=true to auto-extend");
                    }
                }


                ulong xorAcc = 0UL;
                ulong mask = (step == 8) ? ulong.MaxValue : ((1UL << (8 * step)) - 1UL);

                for (int i = offset; i < n; i += step)
                {
                    ulong word = 0;
                    for (int b = 0; b < step; b++)
                    {
                        word |= ((ulong)buf[i + b]) << (8 * b); // little-endian
                    }
                    xorAcc ^= word;
                }


                ulong appendValue = xorAcc & mask;


                for (int b = 0; b < step; b++)
                {
                    buf.Add((byte)((appendValue >> (8 * b)) & 0xFF));
                }
     
            }
            catch (Exception ex)
            {
                Debug.LogWarning("AppendChecksumWord НЕ Сработал Ошибка! " + ex.ToString());

            }
        }

        /// <summary>
        /// Вычисляет XOR-слово по словам (little-endian) в buf, начиная с offset с шагом step,
        /// и добавляет в конец buf ещё step байт с таким значением, чтобы общий XOR включая добавленное слово равнялся 0.
        /// Если pad=true и длина (buf.Count - offset) не кратна step, буфер дополняется нулями.
        /// Возвращает записанное значение (ulong).
        /// </summary>

        public static void appendChecksum(byte[] raw, int offset, int size)
        {
            ulong chksum = 0;
            int count = size - 4;
             ulong ecx;
             int i;

            for (i = offset; i < count; i += 4)
            {
              ecx = (ulong)raw[i] & 0xff;
             ecx |= (ulong)raw[i + 1] << 8 & 0xff00;
              ecx |= (ulong)raw[i + 2] << 0x10 & 0xff0000;
              ecx |= (ulong)raw[i + 3] << 0x18 & 0xff000000;

              chksum ^= ecx;
            }

             ecx = (ulong)raw[i] & 0xff;
             ecx |= (ulong)raw[i + 1] << 8 & 0xff00;
             ecx |= (ulong)raw[i + 2] << 0x10 & 0xff0000;
             ecx |= (ulong)raw[i + 3] << 0x18 & 0xff000000;

             raw[i] = (byte)(chksum & 0xff);
             raw[i + 1] = (byte)(chksum >> 0x08 & 0xff);
            raw[i + 2] = (byte)(chksum >> 0x10 & 0xff);
            raw[i + 3] = (byte)(chksum >> 0x18 & 0xff);
        }

        ////public static void appendChecksum(byte[] raw, int offset, int size)
        //{
        //ulong chksum = 0;
        //int count = size - 4;
        /// ulong ecx;
        // int i;

        //for (i = offset; i < count; i += 4)
        //{
        //  ecx = (ulong)raw[i] & 0xff;
        // ecx |= (ulong)raw[i + 1] << 8 & 0xff00;
        //  ecx |= (ulong)raw[i + 2] << 0x10 & 0xff0000;
        //  ecx |= (ulong)raw[i + 3] << 0x18 & 0xff000000;

        //  chksum ^= ecx;
        //}

        // ecx = (ulong)raw[i] & 0xff;
        // ecx |= (ulong)raw[i + 1] << 8 & 0xff00;
        // ecx |= (ulong)raw[i + 2] << 0x10 & 0xff0000;
        // ecx |= (ulong)raw[i + 3] << 0x18 & 0xff000000;

        // raw[i] = (byte)(chksum & 0xff);
        // raw[i + 1] = (byte)(chksum >> 0x08 & 0xff);
        //raw[i + 2] = (byte)(chksum >> 0x10 & 0xff);
        //raw[i + 3] = (byte)(chksum >> 0x18 & 0xff);
        //}

        /**
	     * Packet is first XOR encoded with <code>key</code>
	     * Then, the last 4 bytes are overwritten with the the XOR "key".
	     * Thus this assume that there is enough room for the key to fit without overwriting data.
	     * @param raw The raw bytes to be encrypted
	     * @param key The 4 bytes (int) XOR key
	     */
        public static void encXORPass(byte[] raw, int key) {
            encXORPass(raw, 0, raw.Length, key);
        }

        /**
	     * Packet is first XOR encoded with <code>key</code>
	     * Then, the last 4 bytes are overwritten with the the XOR "key".
	     * Thus this assume that there is enough room for the key to fit without overwriting data.
	     * @param raw The raw bytes to be encrypted
	     * @param offset The begining of the data to be encrypted
	     * @param size Length of the data to be encrypted
	     * @param key The 4 bytes (int) XOR key
	     */
        public static void encXORPass(byte[] raw, int offset, int size, int key) {
            int stop = size - 8;
            int pos = 4 + offset;
            int edx;
            int ecx = key; // Initial xor key

            while (pos < stop) {
                edx = raw[pos] & 0xFF;
                edx |= (raw[pos + 1] & 0xFF) << 8;
                edx |= (raw[pos + 2] & 0xFF) << 16;
                edx |= (raw[pos + 3] & 0xFF) << 24;

                ecx += edx;

                edx ^= ecx;

                raw[pos] = (byte)(edx & 0xFF);
                raw[pos + 1] = (byte)(edx >> 8 & 0xFF);
                raw[pos + 2] = (byte)(edx >> 16 & 0xFF);
                raw[pos + 3] = (byte)(edx >> 24 & 0xFF);
                pos += 4;
            }

            raw[pos++] = (byte)(ecx & 0xFF);
            raw[pos++] = (byte)(ecx >> 8 & 0xFF);
            raw[pos++] = (byte)(ecx >> 16 & 0xFF);
            raw[pos++] = (byte)(ecx >> 24 & 0xFF);
        }


        public static bool decXORPass(byte[] packet) {
            int blen = packet.Length;

            if (blen < 1 || packet == null)
                return false; // TODO: Handle error or throw exception

            // Get XOR key
            int xorOffset = 8;
            uint xorKey = 0;
            xorKey |= packet[blen - xorOffset];
            xorKey |= (uint)(packet[blen - xorOffset + 1] << 8);
            xorKey |= (uint)(packet[blen - xorOffset + 2] << 16);
            xorKey |= (uint)(packet[blen - xorOffset + 3] << 24);

            // Decrypt XOR encrypted portion
            int offset = blen - xorOffset - 4;
            uint ecx = xorKey;
            uint edx = 0;

            while (offset > 2) // Adjust this condition if needed
            {
                edx = (uint)(packet[offset + 0] & 0xFF);
                edx |= (uint)(packet[offset + 1] & 0xFF) << 8;
                edx |= (uint)(packet[offset + 2] & 0xFF) << 16;
                edx |= (uint)(packet[offset + 3] & 0xFF) << 24;

                edx ^= ecx;
                ecx -= edx;

                packet[offset + 0] = (byte)((edx) & 0xFF);
                packet[offset + 1] = (byte)((edx >> 8) & 0xFF);
                packet[offset + 2] = (byte)((edx >> 16) & 0xFF);
                packet[offset + 3] = (byte)((edx >> 24) & 0xFF);
                offset -= 4;
            }
            return true;
        }

      
    }
}
