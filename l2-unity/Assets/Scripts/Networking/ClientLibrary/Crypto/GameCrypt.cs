using AlefCrypto;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using static UnityEngine.Rendering.STP;

namespace L2_login {

    public class GameCrypt {
        private readonly byte[] _inkey = new byte[16];
        private readonly byte[] _outkey = new byte[16];
        private byte[] _outKeyJ = new byte[16];
        private byte[] _outInJ = new byte[16];
        private bool _isEnabled;
        

        public void SetKey(byte[] key) {
            _isEnabled = true;
            key.CopyTo(_inkey, 0);
            key.CopyTo(_outkey, 0);
            key.CopyTo(_outKeyJ, 0);
            key.CopyTo(_outInJ, 0);
            // Array.Copy(key, 0, _outKeyJ, 0, 16);
            Debug.Log("");
            //SetKeyJ(key);
        }

        public void Decrypt(byte[] raw) {
            if (!_isEnabled)
                return;

            uint num1 = 0;
            for (int index = 0; index < raw.Length; ++index) {
                uint num2 = raw[index] & (uint)byte.MaxValue;
                raw[index] = (byte)(num2 ^ _inkey[index & 15] ^ num1);
                num1 = num2;
            }

            uint num3 = ((_inkey[8] & (uint)byte.MaxValue) | (uint)((_inkey[9] << 8) & 65280) | (uint)((_inkey[10] << 16) & 16711680) | (uint)((_inkey[11] << 24) & -16777216)) + (uint)raw.Length;
            _inkey[8] = (byte)(num3 & byte.MaxValue);
            _inkey[9] = (byte)((num3 >> 8) & byte.MaxValue);
            _inkey[10] = (byte)((num3 >> 16) & byte.MaxValue);
            _inkey[11] = (byte)((num3 >> 24) & byte.MaxValue);
        }

        public void Encrypt(byte[] raw) {
            if (!_isEnabled)
                _isEnabled = true;
            else {
                uint num1 = 0;
                for (int index = 0; index < raw.Length; ++index) {
                    num1 = (raw[index] & (uint)byte.MaxValue) ^ _outkey[index & 15] ^ num1;
                    raw[index] = (byte)num1;
                }

                uint num2 = ((_outkey[8] & (uint)byte.MaxValue) | (uint)((_outkey[9] << 8) & 65280) | (uint)((_outkey[10] << 16) & 16711680) | (uint)((_outkey[11] << 24) & -16777216)) + (uint)raw.Length;
                _outkey[8] = (byte)(num2 & byte.MaxValue);
                _outkey[9] = (byte)((num2 >> 8) & byte.MaxValue);
                _outkey[10] = (byte)((num2 >> 16) & byte.MaxValue);
                _outkey[11] = (byte)((num2 >> 24) & byte.MaxValue);
            }
        }

        

        public void GameServerEncrypt(byte[] raw, int offset, int size)
        {

            int temp = 0;
            for (int i = 0; i < size; i++)
            {
                int temp2 = raw[offset + i] & 0xFF;
                temp = temp2 ^ _outKeyJ[i & 15] ^ temp;
                raw[offset + i] = (byte)temp;
            }

            int old = _outKeyJ[8] & 0xff;
            old |= _outKeyJ[9] << 8 & 0xff00;
            old |= _outKeyJ[10] << 0x10 & 0xff0000;
            long lo = _outKeyJ[11] << 0x18 & 0xff000000;
            old |= (int)lo;
            old += size;

            _outKeyJ[8] = (byte)(old & 0xff);
            _outKeyJ[9] = (byte)(old >> 0x08 & 0xff);
            _outKeyJ[10] = (byte)(old >> 0x10 & 0xff);
            _outKeyJ[11] = (byte)(old >> 0x18 & 0xff);
            //Debug.Log($"Key Encrypt!!! >>> : {StringUtils.ByteArrayToString(raw)}");
        }

        public void GameServerDecrypt(byte[] raw, int offset, int size)
        {

            int temp = 0;
            for (int i = 0; i < size; i++)
            {
                int temp2 = raw[offset + i] & 0xFF;
                raw[offset + i] = (byte)(temp2 ^ _outInJ[i & 15] ^ temp);
                temp = temp2;
            }

            int old = _outInJ[8] & 0xff;
            old |= _outInJ[9] << 8 & 0xff00;
            old |= _outInJ[10] << 0x10 & 0xff0000;
            old |= (int)(_outInJ[11] << 0x18 & 0xff000000);

            old += size;

            _outInJ[8] = (byte)(old & 0xff);
            _outInJ[9] = (byte)(old >> 0x08 & 0xff);
            _outInJ[10] = (byte)(old >> 0x10 & 0xff);
            _outInJ[11] = (byte)(old >> 0x18 & 0xff);
            //Debug.Log($"Decrypt Decryp!!! >>> : {StringUtils.ByteArrayToString(raw)}");
        }

       


    }
}
