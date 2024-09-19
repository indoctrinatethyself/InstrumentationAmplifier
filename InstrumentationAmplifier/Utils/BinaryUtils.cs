using System;
using System.Buffers.Binary;
using System.Numerics;

namespace InstrumentationAmplifier.Utils;

public static class BinaryUtils
{
    public static bool GetBit<T>(T v, int index) where T : IBinaryInteger<T> => (v & (T.One << index)) > T.Zero;

    public static T GetBits<T>(T v, int index, int count) where T : IBinaryInteger<T> => (v >> index) & CreateBitmask<T>(count);

    public static void SetBit<T>(ref T v, int index, bool value) where T : IBinaryInteger<T>
    {
        v &= ~(T.One << index);
        if (value) v |= T.One << index;
    }

    public static void SetBits<T>(ref T v, int index, int count, T value) where T : IBinaryInteger<T>
    {
        T mask = CreateBitmask<T>(count);

        v &= ~(mask << index);
        T b = value & mask;
        v |= b << index;
    }

    public static T CreateBitmask<T>(int bitCount) where T : IBinaryInteger<T> /*, IShiftOperators<T, int, T>*/
    {
        if (bitCount >= GetBitCount<T>()) return ~T.Zero;
        return (T.One << bitCount) - T.One;
    }

    public static int GetBitCount<T>() where T : IBinaryInteger<T> => int.CreateChecked(T.PopCount(T.AllBitsSet));


    public static byte[] GetBytes(byte v)
    {
        return new byte[] { v };
    }

    public static byte[] GetBytes(UInt16 v)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, v);
        return bytes;
    }

    public static byte[] GetBytes24(UInt32 v)
    {
        byte[] b = new byte[3];
        b[0] = (byte)(v >> 16);
        b[1] = (byte)(v >> 8);
        b[2] = (byte)(v >> 0);
        return b;
    }

    public static byte[] GetBytes(UInt32 v)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, v);
        return bytes;
    }

    public static byte[] GetBytes48(UInt64 v)
    {
        byte[] b = new byte[6];
        b[0] = (byte)(v >> 40);
        b[1] = (byte)(v >> 32);
        b[2] = (byte)(v >> 24);
        b[3] = (byte)(v >> 16);
        b[4] = (byte)(v >> 8);
        b[5] = (byte)(v >> 0);
        return b;
    }

    public static byte GetByte(bool b0, bool b1 = false, bool b2 = false, bool b3 = false,
        bool b4 = false, bool b5 = false, bool b6 = false, bool b7 = false) =>
        (byte)((b0 ? 1 : 0) << 0 |
               (b1 ? 1 : 0) << 1 |
               (b2 ? 1 : 0) << 2 |
               (b3 ? 1 : 0) << 3 |
               (b4 ? 1 : 0) << 4 |
               (b5 ? 1 : 0) << 5 |
               (b6 ? 1 : 0) << 6 |
               (b7 ? 1 : 0) << 7);
}