/*
 * Retrieved from:
 * https://github.com/cyriaca/Cyriaca.MoonSharp/blob/main/net.cyriaca.moonsharp/SpookyV2/SpookyV2.cs
 * @660ff78
 *
 * Changes:
 * -Namespace
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Azura.Generator;

/// <summary>
/// Managed implementation of SpookyHash v2.
/// </summary>
/// <remarks>
/// Based on public domain implementation from
/// http://www.burtleburtle.net/bob/hash/spooky.html
/// </remarks>
public unsafe struct SpookyHash
{
    private const bool AllowUnalignedReads = false;
    private const int NumVars = 12;
    private const ulong Const = 0xdeadbeefdeadbeefUL;
    private const int BlockSize = NumVars * sizeof(ulong);
    private const int BufSize = 2 * BlockSize;

#pragma warning disable 649
    private fixed ulong _data[2 * NumVars];
    private fixed ulong _state[NumVars];
#pragma warning restore 649
    private int _length;
    private byte _remainder;


    /// <summary>
    /// Initializes instance using two ulong seed values.
    /// </summary>
    /// <param name="seed1">First seed part.</param>
    /// <param name="seed2">Second seed part.</param>
    public SpookyHash(ulong seed1, ulong seed2)
    {
        _length = 0;
        _remainder = 0;
        _state[0] = seed1;
        _state[1] = seed2;
    }

    #region Lifecycle

    /// <summary>
    /// Initializes instance using two ulong seed values.
    /// </summary>
    /// <param name="seed1">First seed part.</param>
    /// <param name="seed2">Second seed part.</param>
    public void Init(ulong seed1, ulong seed2)
    {
        _length = 0;
        _remainder = 0;
        _state[0] = seed1;
        _state[1] = seed2;
    }

    /// <summary>
    /// Outputs final hash.
    /// </summary>
    /// <param name="hash">Hash.</param>
    public void Final(out Sh128 hash)
    {
        hash = default;
        if (_length < BufSize)
        {
            hash.A = _state[0];
            hash.B = _state[1];
            fixed (ulong* data = _data) Short((byte*)data, _length, ref hash);
            return;
        }

        fixed (ulong* dat = _data)
        {
            ulong* data = dat;
            byte remainder = _remainder;

            ulong h0 = _state[0];
            ulong h1 = _state[1];
            ulong h2 = _state[2];
            ulong h3 = _state[3];
            ulong h4 = _state[4];
            ulong h5 = _state[5];
            ulong h6 = _state[6];
            ulong h7 = _state[7];
            ulong h8 = _state[8];
            ulong h9 = _state[9];
            ulong h10 = _state[10];
            ulong h11 = _state[11];

            if (remainder >= BlockSize)
            {
                Mix(data, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
                    ref h11);
                data += NumVars;
                remainder -= BlockSize;
            }

            MemSetSlow(&((byte*)data)[remainder], 0, BlockSize - remainder);

            ((byte*)data)[BlockSize - 1] = remainder;

            End(data, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
                ref h11);

            hash.A = h0;
            hash.B = h1;
        }
    }

    #endregion

    #region Unmanaged

    /// <summary>
    /// Updates this hash instance with an unmanaged value.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <typeparam name="T">Value type.</typeparam>
    public void Update<T>(ref T value) where T : unmanaged
    {
        fixed (void* v = &value) Update(v, sizeof(T));
    }

    /// <summary>
    /// Computes hash of an unmanaged value.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <typeparam name="T">Value type.</typeparam>
    /// <returns>Hash.</returns>
    public static Sh128 Compute<T>(ref T value) where T : unmanaged
    {
        Sh128 res = default;
        fixed (void* v = &value) Hash128(v, sizeof(T), ref res);
        return res;
    }

    /// <summary>
    /// Updates this hash instance with an unmanaged array.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <typeparam name="T">Value type.</typeparam>
    public void Update<T>(T[] value) where T : unmanaged
    {
        fixed (void* v = value) Update(v, value.Length * sizeof(T));
    }

    /// <summary>
    /// Computes hash of an unmanaged array.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <returns>Hash.</returns>
    public static Sh128 Compute<T>(T[] value) where T : unmanaged
    {
        Sh128 res = default;
        fixed (T* p = value) Hash128(p, value.Length * sizeof(T), ref res);

        return res;
    }

    #endregion

    #region Strings

    /// <summary>
    /// Updates this hash instance with a string.
    /// </summary>
    /// <param name="value"></param>
    public void Update(string value)
    {
        int l = _utf8.GetMaxByteCount(value.Length);
        IntPtr p = Marshal.AllocHGlobal(l);
        try
        {
            int al;
            fixed (char* c = value) al = _utf8.GetBytes(c, value.Length, (byte*)p.ToPointer(), l);
            Update(p.ToPointer(), al);
        }
        finally
        {
            Marshal.FreeHGlobal(p);
        }
    }

    /// <summary>
    /// Computes hash of a string.
    /// </summary>
    /// <param name="value">String.</param>
    /// <returns>Hash.</returns>
    public static Sh128 Compute(string value)
    {
        int l = _utf8.GetMaxByteCount(value.Length);
        IntPtr p = Marshal.AllocHGlobal(l);
        try
        {
            int al;
            fixed (char* c = value) al = _utf8.GetBytes(c, value.Length, (byte*)p.ToPointer(), l);
            Sh128 r = default;
            Hash128(p.ToPointer(), al, ref r);
            return r;
        }
        finally
        {
            Marshal.FreeHGlobal(p);
        }
    }

    private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

    #endregion

    #region Raw buffers

    /// <summary>
    /// Updates this hash instance with a hash byte array.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <returns>Hash.</returns>
    public void Update(byte[] value)
    {
        fixed(byte* p = value) Update(p, value.Length);
    }

    /// <summary>
    /// Computes hash of a byte array.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <returns>Hash.</returns>
    public static Sh128 Compute(byte[] value)
    {
        Sh128 res = default;
        fixed (byte* p = value) Hash128(p, value.Length, ref res);

        return res;
    }

    /// <summary>
    /// Update this hash instance with a binary buffer.
    /// </summary>
    /// <param name="message">Message to append.</param>
    /// <param name="length">Message length.</param>
    public void Update(void* message, int length)
    {
        ulong h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11;
        int newLength = length + _remainder;
        byte remainder;
        U u = default;
        ulong* end;
        if (newLength < BufSize)
        {
            fixed (ulong* data = _data) Buffer.MemoryCopy(message, &((byte*)data)[_remainder], length, length);
            _length = length + _length;
            _remainder = (byte)newLength;
            return;
        }

        if (_length < BufSize)
        {
            h0 = h3 = h6 = h9 = _state[0];
            h1 = h4 = h7 = h10 = _state[1];
            h2 = h5 = h8 = h11 = Const;
        }
        else
        {
            h0 = _state[0];
            h1 = _state[1];
            h2 = _state[2];
            h3 = _state[3];
            h4 = _state[4];
            h5 = _state[5];
            h6 = _state[6];
            h7 = _state[7];
            h8 = _state[8];
            h9 = _state[9];
            h10 = _state[10];
            h11 = _state[11];
        }

        _length = length + _length;

        if (_remainder != 0)
        {
            byte prefix = (byte)(BufSize - _remainder);
            fixed (ulong* data = _data)
            {
                Buffer.MemoryCopy(message, &((byte*)data)[_remainder], prefix, prefix);
                u.P64 = data;
                Mix(u.P64, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
                    ref h11);
                Mix(&u.P64[NumVars], ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9,
                    ref h10, ref h11);
            }

            u.P8 = (byte*)message + prefix;
            length -= prefix;
        }
        else
        {
            u.P8 = (byte*)message;
        }

        end = u.P64 + length / BlockSize * NumVars;
        remainder = (byte)(length - (int)((byte*)end - u.P8));
        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
        if (AllowUnalignedReads || (u.I & 0x7) == 0)
            while (u.P64 < end)
            {
                Mix(u.P64, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
                    ref h11);
                u.P64 += NumVars;
            }
        else
            while (u.P64 < end)
            {
                fixed (ulong* data = _data)
                {
                    Buffer.MemoryCopy(u.P8, data, BlockSize, BlockSize);
                    Mix(data, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9,
                        ref h10, ref h11);
                    u.P64 += NumVars;
                }
            }

        _remainder = remainder;
        fixed (ulong* data = _data) Buffer.MemoryCopy(end, data, remainder, remainder);

        _state[0] = h0;
        _state[1] = h1;
        _state[2] = h2;
        _state[3] = h3;
        _state[4] = h4;
        _state[5] = h5;
        _state[6] = h6;
        _state[7] = h7;
        _state[8] = h8;
        _state[9] = h9;
        _state[10] = h10;
        _state[11] = h11;
    }

    /// <summary>
    /// Computes hash of a binary buffer.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="length">Message length.</param>
    /// <returns>Hash.</returns>
    public static Sh128 Compute(void* message, int length)
    {
        Sh128 res = default;
        Hash128(message, length, ref res);
        return res;
    }

    /// <summary>
    /// Computes hash of a buffer.
    /// </summary>
    /// <param name="message">Input.</param>
    /// <param name="length">Input length.</param>
    /// <param name="result">Hash (may have initial state).</param>
    public static void Hash128(void* message, int length, ref Sh128 result)
    {
        if (length < BufSize)
        {
            Short(message, length, ref result);
            return;
        }

        ulong h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11;
        ulong* buf = stackalloc ulong[NumVars];
        ulong* end;
        U u = default;
        int remainder;

        h0 = h3 = h6 = h9 = result.A;
        h1 = h4 = h7 = h10 = result.B;
        h2 = h5 = h8 = h11 = Const;

        u.P8 = (byte*)message;
        end = u.P64 + length / BlockSize * NumVars;

        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
        if (AllowUnalignedReads || (u.I & 0x7) == 0)
            while (u.P64 < end)
            {
                Mix(u.P64, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
                    ref h11);
                u.P64 += NumVars;
            }
        else
            while (u.P64 < end)
            {
                Buffer.MemoryCopy(u.P64, buf, BlockSize, BlockSize);
                Mix(buf, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
                    ref h11);
                u.P64 += NumVars;
            }

        remainder = length - (int)((byte*)end - (byte*)message);
        Buffer.MemoryCopy(end, buf, remainder, remainder);
        MemSetSlow((byte*)buf + remainder, 0, BlockSize - remainder);
        ((byte*)buf)[BlockSize - 1] = (byte)remainder;

        End(buf, ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10, ref h11);
        result.A = h0;
        result.B = h1;
    }

    #endregion

    #region Internals

    private static void Short(void* message, int length, ref Sh128 result)
    {
        if (length > BufSize)
            throw new ArgumentException($"Short form can't be called with buffer longer than {BufSize}");

        ulong* buf = stackalloc ulong[2 * NumVars];
        U u = default;
        u.P8 = (byte*)message;

        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
        if (!AllowUnalignedReads && (u.I & 0x7) != 0)
        {
            Buffer.MemoryCopy(message, buf, BufSize, length);
            u.P64 = buf;
        }

        int remainder = length % 32;
        ulong a = result.A;
        ulong b = result.B;
        ulong c = Const;
        ulong d = Const;
        if (length > 15)
        {
            ulong* end = u.P64 + length / 32 * 4;

            for (; u.P64 < end; u.P64 += 4)
            {
                c += u.P64[0];
                d += u.P64[1];
                ShortMix(ref a, ref b, ref c, ref d);
                a += u.P64[2];
                b += u.P64[3];
            }

            if (remainder >= 16)
            {
                c += u.P64[0];
                d += u.P64[1];
                ShortMix(ref a, ref b, ref c, ref d);
                u.P64 += 2;
                remainder -= 16;
            }
        }

        d += (ulong)length << 56;
        switch (remainder)
        {
            case 15:
                d += (ulong)u.P8[14] << 48;
                goto C14;
            case 14:
                C14:
                d += (ulong)u.P8[13] << 40;
                goto C13;
            case 13:
                C13:
                d += (ulong)u.P8[12] << 32;
                goto C12;
            case 12:
                C12:
                d += u.P32[2];
                c += u.P64[0];
                break;
            case 11:
                d += (ulong)u.P8[10] << 16;
                goto C10;
            case 10:
                C10:
                d += (ulong)u.P8[9] << 8;
                goto C9;
            case 9:
                C9:
                d += u.P8[8];
                goto C8;
            case 8:
                C8:
                c += u.P64[0];
                break;
            case 7:
                c += (ulong)u.P8[6] << 48;
                goto C6;
            case 6:
                C6:
                c += (ulong)u.P8[5] << 40;
                goto C5;
            case 5:
                C5:
                c += (ulong)u.P8[4] << 32;
                goto C4;
            case 4:
                C4:
                c += u.P32[0];
                break;
            case 3:
                c += (ulong)u.P8[2] << 16;
                goto C2;
            case 2:
                C2:
                c += (ulong)u.P8[1] << 8;
                goto C1;
            case 1:
                C1:
                c += u.P8[0];
                break;
            case 0:
                c += Const;
                d += Const;
                break;
        }

        ShortEnd(ref a, ref b, ref c, ref d);
        result.A = a;
        result.B = b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShortMix(ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3)
    {
        h2 = Rot64(h2, 50);
        h2 += h3;
        h0 ^= h2;
        h3 = Rot64(h3, 52);
        h3 += h0;
        h1 ^= h3;
        h0 = Rot64(h0, 30);
        h0 += h1;
        h2 ^= h0;
        h1 = Rot64(h1, 41);
        h1 += h2;
        h3 ^= h1;
        h2 = Rot64(h2, 54);
        h2 += h3;
        h0 ^= h2;
        h3 = Rot64(h3, 48);
        h3 += h0;
        h1 ^= h3;
        h0 = Rot64(h0, 38);
        h0 += h1;
        h2 ^= h0;
        h1 = Rot64(h1, 37);
        h1 += h2;
        h3 ^= h1;
        h2 = Rot64(h2, 62);
        h2 += h3;
        h0 ^= h2;
        h3 = Rot64(h3, 34);
        h3 += h0;
        h1 ^= h3;
        h0 = Rot64(h0, 5);
        h0 += h1;
        h2 ^= h0;
        h1 = Rot64(h1, 36);
        h1 += h2;
        h3 ^= h1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShortEnd(ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3)
    {
        h3 ^= h2;
        h2 = Rot64(h2, 15);
        h3 += h2;
        h0 ^= h3;
        h3 = Rot64(h3, 52);
        h0 += h3;
        h1 ^= h0;
        h0 = Rot64(h0, 26);
        h1 += h0;
        h2 ^= h1;
        h1 = Rot64(h1, 51);
        h2 += h1;
        h3 ^= h2;
        h2 = Rot64(h2, 28);
        h3 += h2;
        h0 ^= h3;
        h3 = Rot64(h3, 9);
        h0 += h3;
        h1 ^= h0;
        h0 = Rot64(h0, 47);
        h1 += h0;
        h2 ^= h1;
        h1 = Rot64(h1, 54);
        h2 += h1;
        h3 ^= h2;
        h2 = Rot64(h2, 32);
        h3 += h2;
        h0 ^= h3;
        h3 = Rot64(h3, 25);
        h0 += h3;
        h1 ^= h0;
        h0 = Rot64(h0, 63);
        h1 += h0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Mix(ulong* data,
        ref ulong s0, ref ulong s1, ref ulong s2, ref ulong s3,
        ref ulong s4, ref ulong s5, ref ulong s6, ref ulong s7,
        ref ulong s8, ref ulong s9, ref ulong s10, ref ulong s11)
    {
        s0 += data[0];
        s2 ^= s10;
        s11 ^= s0;
        s0 = Rot64(s0, 11);
        s11 += s1;
        s1 += data[1];
        s3 ^= s11;
        s0 ^= s1;
        s1 = Rot64(s1, 32);
        s0 += s2;
        s2 += data[2];
        s4 ^= s0;
        s1 ^= s2;
        s2 = Rot64(s2, 43);
        s1 += s3;
        s3 += data[3];
        s5 ^= s1;
        s2 ^= s3;
        s3 = Rot64(s3, 31);
        s2 += s4;
        s4 += data[4];
        s6 ^= s2;
        s3 ^= s4;
        s4 = Rot64(s4, 17);
        s3 += s5;
        s5 += data[5];
        s7 ^= s3;
        s4 ^= s5;
        s5 = Rot64(s5, 28);
        s4 += s6;
        s6 += data[6];
        s8 ^= s4;
        s5 ^= s6;
        s6 = Rot64(s6, 39);
        s5 += s7;
        s7 += data[7];
        s9 ^= s5;
        s6 ^= s7;
        s7 = Rot64(s7, 57);
        s6 += s8;
        s8 += data[8];
        s10 ^= s6;
        s7 ^= s8;
        s8 = Rot64(s8, 55);
        s7 += s9;
        s9 += data[9];
        s11 ^= s7;
        s8 ^= s9;
        s9 = Rot64(s9, 54);
        s8 += s10;
        s10 += data[10];
        s0 ^= s8;
        s9 ^= s10;
        s10 = Rot64(s10, 22);
        s9 += s11;
        s11 += data[11];
        s1 ^= s9;
        s10 ^= s11;
        s11 = Rot64(s11, 46);
        s10 += s0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void End(ulong* data,
        ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3,
        ref ulong h4, ref ulong h5, ref ulong h6, ref ulong h7,
        ref ulong h8, ref ulong h9, ref ulong h10, ref ulong h11
    )
    {
        h0 += data[0];
        h1 += data[1];
        h2 += data[2];
        h3 += data[3];
        h4 += data[4];
        h5 += data[5];
        h6 += data[6];
        h7 += data[7];
        h8 += data[8];
        h9 += data[9];
        h10 += data[10];
        h11 += data[11];
        EndPartial(ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
            ref h11);
        EndPartial(ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
            ref h11);
        EndPartial(ref h0, ref h1, ref h2, ref h3, ref h4, ref h5, ref h6, ref h7, ref h8, ref h9, ref h10,
            ref h11);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EndPartial(
        ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3,
        ref ulong h4, ref ulong h5, ref ulong h6, ref ulong h7,
        ref ulong h8, ref ulong h9, ref ulong h10, ref ulong h11)
    {
        h11 += h1;
        h2 ^= h11;
        h1 = Rot64(h1, 44);
        h0 += h2;
        h3 ^= h0;
        h2 = Rot64(h2, 15);
        h1 += h3;
        h4 ^= h1;
        h3 = Rot64(h3, 34);
        h2 += h4;
        h5 ^= h2;
        h4 = Rot64(h4, 21);
        h3 += h5;
        h6 ^= h3;
        h5 = Rot64(h5, 38);
        h4 += h6;
        h7 ^= h4;
        h6 = Rot64(h6, 33);
        h5 += h7;
        h8 ^= h5;
        h7 = Rot64(h7, 10);
        h6 += h8;
        h9 ^= h6;
        h8 = Rot64(h8, 13);
        h7 += h9;
        h10 ^= h7;
        h9 = Rot64(h9, 38);
        h8 += h10;
        h11 ^= h8;
        h10 = Rot64(h10, 53);
        h9 += h11;
        h0 ^= h9;
        h11 = Rot64(h11, 42);
        h10 += h0;
        h1 ^= h10;
        h0 = Rot64(h0, 54);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Rot64(ulong x, int k) => x << k | x >> 64 - k;

    private static void MemSetSlow(byte* p, byte v, int c)
    {
        for (int i = 0; i < c; i++)
            p[i] = v;
    }

    #endregion

    #region Types

    [StructLayout(LayoutKind.Explicit)]
    private struct U
    {
        [FieldOffset(0)] public byte* P8;
        [FieldOffset(0)] public uint* P32;
        [FieldOffset(0)] public ulong* P64;
        [FieldOffset(0)] public int I;
    }

    #endregion
}

#region Types

/// <summary>
/// Represents a 128-bit hash.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct Sh128 : IEquatable<Sh128>
{
    /// <summary>
    /// Full value.
    /// </summary>
    [FieldOffset(0)] public fixed byte Value[16];

    /// <summary>
    /// First 64 bits.
    /// </summary>
    [FieldOffset(0)] public ulong A;

    /// <summary>
    /// Second 64 bits.
    /// </summary>
    [FieldOffset(8)] public ulong B;

    /// <inheritdoc />
    public override string ToString()
    {
        char* r = stackalloc char[32];
        Repr(r, 32);
        return new string(r, 0, 32);
    }

    /// <summary>
    /// Gets representation
    /// </summary>
    /// <param name="r">Target buffer.</param>
    /// <param name="count">Element count.</param>
    /// <exception cref="ArgumentException">Thrown if count &lt; 32.</exception>
    public void Repr(char* r, int count)
    {
        if (count < 32) throw new ArgumentException($"Buffer length {count} too small, must be >= 32");

        char* c = stackalloc char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
        };
        for (int i = 0; i < 16; i++)
        {
            byte vv = Value[i];
            r[i * 2] = c[vv >> 4];
            r[i * 2 + 1] = c[vv & 0xf];
        }
    }

    private static byte Dehex(char c)
    {
        if (c > 'f') throw new ArgumentException();
        if (c >= 'a') return (byte)(c - 'a' + 10);
        if (c > '9') throw new ArgumentException();
        if (c >= '0') return (byte)(c - '0');
        throw new ArgumentException();
    }

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    /// <param name="str">Input.</param>
    /// <returns>Result.</returns>
    public static implicit operator Sh128(string str)
    {
        if (str.Length != 32) throw new ArgumentException("Expected length 32 string");
        Sh128 res = default;
        for (int i = 0; i < 16; i++) res.Value[i] = (byte)((Dehex(str[i * 2]) << 4) | Dehex(str[i * 2 + 1]));
        return res;
    }

    /// <inheritdoc />
    public bool Equals(Sh128 other) => A == other.A && B == other.B;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Sh128 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return (A.GetHashCode() * 397) ^ B.GetHashCode();
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }
    }
}

#endregion
