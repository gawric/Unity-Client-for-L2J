using System;

namespace L2_login
{
    public class GameCrypt
    {
        private readonly byte[] _inKey = new byte[16];
        private readonly byte[] _outKey = new byte[16];

        public void SetKey(byte[] key)
        {
            key.CopyTo(_inKey, 0);
            key.CopyTo(_outKey, 0);
        }

        public void Encrypt(byte[] raw, int offset, int size)
        {
            int temp = 0;
            for (int i = 0; i < size; i++)
            {
                int temp2 = raw[offset + i] & 0xFF;
                temp = temp2 ^ _outKey[i & 15] ^ temp;
                raw[offset + i] = (byte)temp;
            }

            AdvanceKey(_outKey, size);
        }

        public void Decrypt(byte[] raw, int offset, int size)
        {
            int temp = 0;
            for (int i = 0; i < size; i++)
            {
                int temp2 = raw[offset + i] & 0xFF;
                raw[offset + i] = (byte)(temp2 ^ _inKey[i & 15] ^ temp);
                temp = temp2;
            }

            AdvanceKey(_inKey, size);
        }

        private static void AdvanceKey(byte[] key, int size)
        {
            int old = key[8] & 0xff;
            old |= key[9] << 8 & 0xff00;
            old |= key[10] << 0x10 & 0xff0000;
            old |= (int)(key[11] << 0x18 & 0xff000000);
            old += size;

            key[8] = (byte)(old & 0xff);
            key[9] = (byte)(old >> 0x08 & 0xff);
            key[10] = (byte)(old >> 0x10 & 0xff);
            key[11] = (byte)(old >> 0x18 & 0xff);
        }
    }
}
