using System;
using System.Collections.Generic;
using UnityEngine;

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
