using UnityEngine;

public static class BlowFishStaticKey 
{
    public static byte[] GetCreateFullKeyBlowFish(byte[] privateKey)
    {
        byte[] key2 = new byte[16];


        for (int i = 0; i < 8; i++)
        {
            key2[i] = privateKey[i];
        }


        key2[8] = (byte)0xc8;
        key2[9] = (byte)0x27;
        key2[10] = (byte)0x93;
        key2[11] = (byte)0x01;
        key2[12] = (byte)0xa1;
        key2[13] = (byte)0x6c;
        key2[14] = (byte)0x31;
        key2[15] = (byte)0x97;

        return key2;
    }

}
