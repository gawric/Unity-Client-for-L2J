using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class RSACrypt
{
    private RSACryptoServiceProvider _rsaProvider;
    private RSA _rsa;
    private RSAParameters _rsaParams;
    // hardcoded modulus
    private byte[] _exponent = new byte[] { 1, 0, 1 };
    private byte[] rsaKey;

    public RSACrypt(byte[] exponent, bool needUnscramble) {
        if(needUnscramble) {
            UnscrambledRSAKey(exponent);
        }
        rsaKey = exponent;
        InitRSACrypt(exponent);
    }

    private void InitRSACrypt(byte[] modulus) {
        _rsaParams = new RSAParameters {
            Modulus = modulus,
            Exponent = _exponent
        };

        _rsaProvider = new RSACryptoServiceProvider();
        _rsaProvider.ImportParameters(_rsaParams);
        _rsa = RSA.Create();
        _rsa.ImportParameters(_rsaParams);
    }

    public byte[] DecryptRSABlock(byte[] encryptedData) {
        try {
            // Encrypt without padding
            return _rsaProvider.Decrypt(encryptedData, false);
        } catch (Exception ex) {
            // Handle other unexpected errors
            Debug.LogError($"Unexpected error during RSA encryption: {ex.Message}");
            return null;
        }
    }

    public byte[] EncryptRSABlockNoPaddingBoundleCastle( byte[] plain)
    {

        RsaKeyParameters publicKey  = LoadPublicKey(rsaKey);
        var engine = new RsaEngine();
        engine.Init(true, publicKey);

        int modulusBytes = (publicKey.Modulus.BitLength + 7) / 8;
        int chunkSize = modulusBytes - 1; // гарантируем, что значение < modulus

        var outBlocks = new List<byte>();

        for (int offset = 0; offset < plain.Length; offset += chunkSize)
        {
            int len = Math.Min(chunkSize, plain.Length - offset);
            byte[] chunk = new byte[modulusBytes]; // left-padded block
            // копируем chunk в конец блока
            Array.Copy(plain, offset, chunk, modulusBytes - len, len);

            byte[] encrypted = engine.ProcessBlock(chunk, 0, chunk.Length); // длина обычно = modulusBytes
            outBlocks.AddRange(encrypted);
        }

        return outBlocks.ToArray();
    

    }

public byte[] EncryptRSANoPadding(byte[] block) {
        try {
            // Encrypt without padding
            return _rsaProvider.Encrypt(block, false);
        } catch (Exception ex) {
            // Handle other unexpected errors
            Debug.LogError($"Unexpected error during RSA encryption: {ex.Message}");
            return null;
        }
    }

    public byte[] EncryptRSAPskc1(byte[] block) {
        try {
            byte[] encryptedBlock = _rsa.Encrypt(block, RSAEncryptionPadding.Pkcs1);
            return encryptedBlock;        
        } catch (CryptographicException ex) {
            // Handle cryptographic errors
            Debug.LogError($"RSA encryption error: {ex.Message}");
        } catch (Exception ex) {
            // Handle other unexpected errors
            Debug.LogError($"Unexpected error during RSA encryption: {ex.Message}");
        }

        return null;
    }

    public void UnscrambledRSAKey(byte[] rsaKey) {
        Debug.Log($"Scrambled RSA: {StringUtils.ByteArrayToString(rsaKey)}");

        // step 4 : xor last 0x40 bytes with  first 0x40 bytes
        for (int i = 0; i < 0x40; i++) {
            rsaKey[0x40 + i] = (byte)(rsaKey[0x40 + i] ^ rsaKey[i]);
        }
        // step 3 : xor bytes 0x0d-0x10 with bytes 0x34-0x38
        for (int i = 0; i < 4; i++) {
            rsaKey[0x0d + i] = (byte)(rsaKey[0x0d + i] ^ rsaKey[0x34 + i]);
        }
        // step 2 : xor first 0x40 bytes with  last 0x40 bytes 
        for (int i = 0; i < 0x40; i++) {
            rsaKey[i] = (byte)(rsaKey[i] ^ rsaKey[0x40 + i]);
        }
        // step 1 : 0x4d-0x50 <-> 0x00-0x04
        for (int i = 0; i < 4; i++) {
            byte temp = rsaKey[0x00 + i];
            rsaKey[0x00 + i] = rsaKey[0x4d + i];
            rsaKey[0x4d + i] = temp;
        }

        Debug.Log($"Unscrambled RSA {rsaKey.Length} : {StringUtils.ByteArrayToString(rsaKey)}");
    }

    public static RsaKeyParameters LoadPublicKey(byte[] modBytes)
    {
        // Используем BouncyCastle BigInteger(1, bytes) чтобы трактовать как положительное число
        var modulus = new Org.BouncyCastle.Math.BigInteger(1, modBytes);
        var exponent = new Org.BouncyCastle.Math.BigInteger(1, new byte[] { 0x01, 0x00, 0x01 }); // 65537

       return  new Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters(false, modulus, exponent);
    }
}
