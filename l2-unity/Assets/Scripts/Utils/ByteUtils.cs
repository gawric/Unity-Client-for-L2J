using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ByteUtils
{

    public static  int fromByteArray(byte[] bytes)
    {
        return bytes[0] << 24 | (bytes[1] & 0xFF) << 16 | (bytes[2] & 0xFF) << 8 | (bytes[3] & 0xFF);
    }

    public static double[] byte2Double(byte[] inData, bool byteSwap)
    {
        int j = 0, upper, lower;
        int length = inData.Length / 8;
        double[] outData = new double[length];
        if (!byteSwap)
            for (int i = 0; i < length; i++)
            {
                j = i * 8;
                upper = (((inData[j] & 0xff) << 24)
                    + ((inData[j + 1] & 0xff) << 16)
                    + ((inData[j + 2] & 0xff) << 8) + ((inData[j + 3] & 0xff) << 0));
                lower = (((inData[j + 4] & 0xff) << 24)
                    + ((inData[j + 5] & 0xff) << 16)
                    + ((inData[j + 6] & 0xff) << 8) + ((inData[j + 7] & 0xff) << 0));
                //outData[i] = Double.longBitsToDouble((((long)upper) << 32)
                outData[i] = (((long)upper) << 32);
            }
        else
            for (int i = 0; i < length; i++)
            {
                j = i * 8;
                upper = (((inData[j + 7] & 0xff) << 24)
                    + ((inData[j + 6] & 0xff) << 16)
                    + ((inData[j + 5] & 0xff) << 8) + ((inData[j + 4] & 0xff) << 0));
                lower = (((inData[j + 3] & 0xff) << 24)
                    + ((inData[j + 2] & 0xff) << 16)
                    + ((inData[j + 1] & 0xff) << 8) + ((inData[j] & 0xff) << 0));
                outData[i] = (((long)upper) << 32);
            }

        return outData;
    }

    public static short fromByteArrayShort(byte[] bytes)
    {
        return (short)(((bytes[0] & 0xFF) << 8) | (bytes[1] & 0xFF));
    }

    public static byte[] shortToBytes(short value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] toByteArray(int value)
    {
        return new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value };
    }

    public static byte[] toByteArray(short value)
    {
            return new byte[] { (byte)(value & 0x00FF), (byte)((value & 0xFF00) >> 8) };
    }


 

        public static int CombineFlags(params int[] flags)
        {
            if (flags == null) throw new ArgumentNullException(nameof(flags));
            int mask = 0;
            foreach (var f in flags)
            {
                if (f <= 0 || (f & (f - 1)) != 0)
                    throw new ArgumentException($"Значение {f} не является степенью двойки.", nameof(flags));
                mask |= f;
            }
            return mask;
        }

    
        public static long CombineFlags(params long[] flags)
        {
            if (flags == null) throw new ArgumentNullException(nameof(flags));
            long mask = 0;
            foreach (var f in flags)
            {
                if (f <= 0 || (f & (f - 1)) != 0)
                    throw new ArgumentException($"Значение {f} не является степенью двойки.", nameof(flags));
                mask |= f;
            }
            return mask;
        }


    public static int[] SplitMask(int mask)
    {
        if (mask == 0) return Array.Empty<int>();
        var list = new List<int>();
        while (mask != 0)
        {
            int bit = mask & -mask; 
            list.Add(bit);
            mask &= mask - 1; 
        }
        return list.ToArray();
    }


    public static long[] SplitMask(long mask)
    {
        if (mask == 0L) return Array.Empty<long>();
        var list = new List<long>();
        while (mask != 0L)
        {
            long bit = mask & -mask;
            list.Add(bit);
            mask &= mask - 1;
        }
        return list.ToArray();
    }


}
