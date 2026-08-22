using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RSACrypt
{
    private byte[] rsaKey;

    public RSACrypt(byte[] exponent, bool needUnscramble)
    {
        if (needUnscramble)
            UnscrambledRSAKey(exponent);

        rsaKey = exponent;
    }

    public byte[] EncryptRSABlockNoPaddingBoundleCastle(byte[] plain)
    {
        RsaKeyParameters publicKey = LoadPublicKey(rsaKey);
        var engine = new RsaEngine();
        engine.Init(true, publicKey);

        int modulusBytes = (publicKey.Modulus.BitLength + 7) / 8;
        int chunkSize = modulusBytes - 1;

        var outBlocks = new List<byte>();

        for (int offset = 0; offset < plain.Length; offset += chunkSize)
        {
            int len = Math.Min(chunkSize, plain.Length - offset);
            byte[] chunk = new byte[modulusBytes];
            Array.Copy(plain, offset, chunk, modulusBytes - len, len);

            byte[] encrypted = engine.ProcessBlock(chunk, 0, chunk.Length);
            outBlocks.AddRange(encrypted);
        }

        return outBlocks.ToArray();
    }

    public void UnscrambledRSAKey(byte[] rsaKey)
    {
        Debug.Log($"Scrambled RSA: {StringUtils.ByteArrayToString(rsaKey)}");

        for (int i = 0; i < 0x40; i++)
            rsaKey[0x40 + i] = (byte)(rsaKey[0x40 + i] ^ rsaKey[i]);

        for (int i = 0; i < 4; i++)
            rsaKey[0x0d + i] = (byte)(rsaKey[0x0d + i] ^ rsaKey[0x34 + i]);

        for (int i = 0; i < 0x40; i++)
            rsaKey[i] = (byte)(rsaKey[i] ^ rsaKey[0x40 + i]);

        for (int i = 0; i < 4; i++)
        {
            byte temp = rsaKey[0x00 + i];
            rsaKey[0x00 + i] = rsaKey[0x4d + i];
            rsaKey[0x4d + i] = temp;
        }

        Debug.Log($"Unscrambled RSA {rsaKey.Length} : {StringUtils.ByteArrayToString(rsaKey)}");
    }

    public static RsaKeyParameters LoadPublicKey(byte[] modBytes)
    {
        var modulus = new Org.BouncyCastle.Math.BigInteger(1, modBytes);
        var exponent = new Org.BouncyCastle.Math.BigInteger(1, new byte[] { 0x01, 0x00, 0x01 });
        return new RsaKeyParameters(false, modulus, exponent);
    }
}
