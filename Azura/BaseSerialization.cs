// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Buffers.ArrayPool<byte>;
using static System.Buffers.Binary.BinaryPrimitives;
using static SerializationInternals;

internal static class SerializationInternals
{
    internal static readonly bool _swap = !BitConverter.IsLittleEndian;
    [ThreadStatic] private static byte[]? _buffer;
    internal static byte[] IoBuffer => _buffer ??= new byte[sizeof(decimal)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> ReadBase8(this Stream stream)
    {
        int tot = 0;
        do
        {
            int read = stream.Read(IoBuffer, tot, sizeof(byte) - tot);
            if (read == 0)
                throw new EndOfStreamException(
                    $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ushort) - tot:X} left");
            tot += read;
        } while (tot < sizeof(byte));

        return IoBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> ReadBase16(this Stream stream)
    {
        int tot = 0;
        do
        {
            int read = stream.Read(IoBuffer, tot, sizeof(ushort) - tot);
            if (read == 0)
                throw new EndOfStreamException(
                    $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ushort) - tot:X} left");
            tot += read;
        } while (tot < sizeof(ushort));

        return IoBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> ReadBase32(this Stream stream)
    {
        int tot = 0;
        do
        {
            int read = stream.Read(IoBuffer, tot, sizeof(uint) - tot);
            if (read == 0)
                throw new EndOfStreamException(
                    $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(uint) - tot:X} left");
            tot += read;
        } while (tot < sizeof(uint));

        return IoBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> ReadBase64(this Stream stream)
    {
        int tot = 0;
        do
        {
            int read = stream.Read(IoBuffer, tot, sizeof(ulong) - tot);
            if (read == 0)
                throw new EndOfStreamException(
                    $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ulong) - tot:X} left");
            tot += read;
        } while (tot < sizeof(ulong));

        return IoBuffer;
    }

    /// <summary>
    /// Read span.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="target">Target buffer.</param>
    /// <param name="count">Number of elements.</param>
    /// <param name="enableSwap">Enable element endianness swapping.</param>
    /// <typeparam name="T">Type of elements.</typeparam>
    /// <exception cref="ApplicationException">If failed to read required number of bytes.</exception>
    public static unsafe void ReadSpan<T>(this Stream stream, Span<T> target, int count, bool enableSwap)
        where T : unmanaged
    {
        if (count == 0) return;
        var mainTarget = MemoryMarshal.Cast<T, byte>(target);
        int order = sizeof(T);
        int mainLen = count * order;
        byte[]? buf = Shared.Rent(4096);
        var span = buf.AsSpan();
        try
        {
            int left = mainLen, tot = 0;
            do
            {
                int read = stream.Read(buf, 0, Math.Min(4096, left));
                if (read == 0)
                    throw new ApplicationException(
                        $"Failed to read required number of bytes! 0x{tot:X} read, 0x{left:X} left");
                span.Slice(0, read).CopyTo(mainTarget.Slice(tot));
                left -= read;
                tot += read;
            } while (left > 0);

            if (order != 1 && enableSwap && _swap)
                fixed (byte* p = &mainTarget.GetPinnableReference())
                {
                    switch (order)
                    {
                        case 2:
                            short* tmp2 = (short*)p;
                            for (int i = 0; i < mainLen; i += 2)
                            {
                                *tmp2 = ReverseEndianness(*tmp2);
                                tmp2++;
                            }

                            break;
                        case 4:
                            int* tmp4 = (int*)p;
                            for (int i = 0; i < mainLen; i += 4)
                            {
                                *tmp4 = ReverseEndianness(*tmp4);
                                tmp4++;
                            }

                            break;
                        case 8:
                            long* tmp8 = (long*)p;
                            for (int i = 0; i < mainLen; i += 8)
                            {
                                *tmp8 = ReverseEndianness(*tmp8);
                                tmp8++;
                            }

                            break;
                        default:
                            int half = order / 2;
                            for (int i = 0; i < mainLen; i += order)
                            for (int j = 0; j < half; j++)
                            {
                                int fir = i + j;
                                int sec = i + order - 1 - j;
                                byte tmp = p[fir];
                                p[fir] = p[sec];
                                p[sec] = tmp;
                            }

                            break;
                    }
                }
        }
        finally
        {
            Shared.Return(buf);
        }
    }

    /// <summary>
    /// Write span.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="count">Number of elements.</param>
    /// <param name="enableSwap">Enable element endianness swapping.</param>
    /// <typeparam name="T">Type of elements.</typeparam>
    public static unsafe void WriteSpan<T>(this Stream stream, ReadOnlySpan<T> source, int count, bool enableSwap)
        where T : unmanaged
    {
        if (count == 0)
            return;
        var mainTarget = MemoryMarshal.Cast<T, byte>(source);
        byte[]? buf = Shared.Rent(4096);
        var span = buf.AsSpan();
        int order = sizeof(T);
        int left = count * order;
        int tot = 0;
        try
        {
            if (order == 1 || !enableSwap || !_swap)
            {
                while (left > 0)
                {
                    int noSwapCur = Math.Min(left, 4096);
                    mainTarget.Slice(tot, noSwapCur).CopyTo(buf);
                    stream.Write(buf, 0, noSwapCur);
                    left -= noSwapCur;
                    tot += noSwapCur;
                }

                return;
            }

            int maxCount = 4096 / order * order;
            fixed (byte* p = &span.GetPinnableReference())
            {
                while (left != 0)
                {
                    int cur = Math.Min(left, maxCount);
                    mainTarget.Slice(tot, cur).CopyTo(buf);
                    switch (order)
                    {
                        case 2:
                            short* tmp2 = (short*)p;
                            for (int i = 0; i < cur; i += 2)
                            {
                                *tmp2 = ReverseEndianness(*tmp2);
                                tmp2++;
                            }

                            break;
                        case 4:
                            int* tmp4 = (int*)p;
                            for (int i = 0; i < cur; i += 4)
                            {
                                *tmp4 = ReverseEndianness(*tmp4);
                                tmp4++;
                            }

                            break;
                        case 8:
                            long* tmp8 = (long*)p;
                            for (int i = 0; i < cur; i += 8)
                            {
                                *tmp8 = ReverseEndianness(*tmp8);
                                tmp8++;
                            }

                            break;
                        default:
                            int half = order / 2;
                            for (int i = 0; i < cur; i += order)
                            for (int j = 0; j < half; j++)
                            {
                                int fir = i + j;
                                int sec = i + order - 1 - j;
                                byte tmp = p[fir];
                                p[fir] = p[sec];
                                p[sec] = tmp;
                            }

                            break;
                    }

                    stream.Write(buf, 0, cur);
                    left -= cur;
                    tot += cur;
                }
            }
        }
        finally
        {
            Shared.Return(buf);
        }
    }
}

/// <summary>
/// Provides serialization for unsigned 8-bit integers.
/// </summary>
public static class byteSerialization
{
    /// <summary>
    /// Deserializes an unsigned 8-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static byte Deserialize(Stream stream)
    {
        return stream.ReadBase8()[0];
    }

    /// <summary>
    /// Serializes an unsigned 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this byte self, Stream stream)
    {
        IoBuffer[0] = self;
        stream.Write(IoBuffer, 0, sizeof(byte));
    }

    /// <summary>
    /// Deserializes an array of unsigned 8-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static byte[] DeserializeArray(Stream stream, int count)
    {
        byte[] res = new byte[count];
        stream.ReadSpan<byte>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of unsigned 8-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<byte> self, Stream stream)
    {
        stream.WriteSpan<byte>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of unsigned 8-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<byte> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for signed 8-bit integers.
/// </summary>
public static class sbyteSerialization
{
    /// <summary>
    /// Deserializes a signed 8-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static sbyte Deserialize(Stream stream)
    {
        return (sbyte)stream.ReadBase8()[0];
    }

    /// <summary>
    /// Serializes a signed 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this sbyte self, Stream stream)
    {
        IoBuffer[0] = (byte)self;
        stream.Write(IoBuffer, 0, sizeof(sbyte));
    }

    /// <summary>
    /// Deserializes an array of signed 8-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static sbyte[] DeserializeArray(Stream stream, int count)
    {
        sbyte[] res = new sbyte[count];
        stream.ReadSpan<sbyte>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of signed 8-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<sbyte> self, Stream stream)
    {
        stream.WriteSpan<sbyte>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of signed 8-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<sbyte> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for unsigned 16-bit integers.
/// </summary>
public static class ushortSerialization
{
    /// <summary>
    /// Deserializes an unsigned 16-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static ushort Deserialize(Stream stream)
    {
        return _swap
            ? ReverseEndianness(
                MemoryMarshal.Read<ushort>(stream.ReadBase16()))
            : MemoryMarshal.Read<ushort>(stream.ReadBase16());
    }

    /// <summary>
    /// Serializes an unsigned 16-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ushort self, Stream stream)
    {
        if (_swap) self = ReverseEndianness(self);
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(ushort));
    }

    /// <summary>
    /// Deserializes an array of unsigned 16-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static ushort[] DeserializeArray(Stream stream, int count)
    {
        ushort[] res = new ushort[count];
        stream.ReadSpan<ushort>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of unsigned 16-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<ushort> self, Stream stream)
    {
        stream.WriteSpan<ushort>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of unsigned 16-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<ushort> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for signed 16-bit integers.
/// </summary>
public static class shortForSerialization
{
    /// <summary>
    /// Deserializes a signed 16-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static short Deserialize(Stream stream)
    {
        return _swap
            ? ReverseEndianness(
                MemoryMarshal.Read<short>(stream.ReadBase16()))
            : MemoryMarshal.Read<short>(stream.ReadBase16());
    }

    /// <summary>
    /// Serializes a signed 16-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this short self, Stream stream)
    {
        if (_swap) self = ReverseEndianness(self);
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(short));
    }

    /// <summary>
    /// Deserializes an array of signed 16-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static short[] DeserializeArray(Stream stream, int count)
    {
        short[] res = new short[count];
        stream.ReadSpan<short>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of signed 16-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<short> self, Stream stream)
    {
        stream.WriteSpan<short>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of signed 16-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<short> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for unsigned 32-bit integers.
/// </summary>
public static class uintSerialization
{
    /// <summary>
    /// Deserializes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static uint Deserialize(Stream stream)
    {
        return _swap
            ? ReverseEndianness(
                MemoryMarshal.Read<uint>(stream.ReadBase32()))
            : MemoryMarshal.Read<uint>(stream.ReadBase32());
    }

    /// <summary>
    /// Serializes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this uint self, Stream stream)
    {
        if (_swap) self = ReverseEndianness(self);
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(uint));
    }

    /// <summary>
    /// Deserializes an array of unsigned 32-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static uint[] DeserializeArray(Stream stream, int count)
    {
        uint[] res = new uint[count];
        stream.ReadSpan<uint>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of unsigned 32-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<uint> self, Stream stream)
    {
        stream.WriteSpan<uint>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of unsigned 32-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<uint> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for signed 32-bit integers.
/// </summary>
public static class intSerialization
{
    /// <summary>
    /// Deserializes a signed 32-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static int Deserialize(Stream stream)
    {
        return _swap
            ? ReverseEndianness(
                MemoryMarshal.Read<int>(stream.ReadBase32()))
            : MemoryMarshal.Read<int>(stream.ReadBase32());
    }

    /// <summary>
    /// Serializes a signed 32-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this int self, Stream stream)
    {
        if (_swap) self = ReverseEndianness(self);
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(int));
    }

    /// <summary>
    /// Deserializes an array of signed 32-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static int[] DeserializeArray(Stream stream, int count)
    {
        int[] res = new int[count];
        stream.ReadSpan<int>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of signed 32-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<int> self, Stream stream)
    {
        stream.WriteSpan<int>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of signed 32-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<int> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for unsigned 64-bit integers.
/// </summary>
public static class ulongSerialization
{
    /// <summary>
    /// Deserializes an unsigned 64-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static ulong Deserialize(Stream stream)
    {
        return _swap
            ? ReverseEndianness(
                MemoryMarshal.Read<ulong>(stream.ReadBase64()))
            : MemoryMarshal.Read<ulong>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes an unsigned 64-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ulong self, Stream stream)
    {
        if (_swap) self = ReverseEndianness(self);
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(ulong));
    }

    /// <summary>
    /// Deserializes an array of unsigned 64-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static ulong[] DeserializeArray(Stream stream, int count)
    {
        ulong[] res = new ulong[count];
        stream.ReadSpan<ulong>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of unsigned 64-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<ulong> self, Stream stream)
    {
        stream.WriteSpan<ulong>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of unsigned 64-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<ulong> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for signed 64-bit integers.
/// </summary>
public static class longSerialization
{
    /// <summary>
    /// Deserializes a signed 64-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static long Deserialize(Stream stream)
    {
        return _swap
            ? ReverseEndianness(
                MemoryMarshal.Read<long>(stream.ReadBase64()))
            : MemoryMarshal.Read<long>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes a signed 64-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this long self, Stream stream)
    {
        if (_swap) self = ReverseEndianness(self);
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(long));
    }

    /// <summary>
    /// Deserializes an array of signed 64-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static long[] DeserializeArray(Stream stream, int count)
    {
        long[] res = new long[count];
        stream.ReadSpan<long>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of signed 64-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<long> self, Stream stream)
    {
        stream.WriteSpan<long>(self, self.Length, true);
    }

    /// <summary>
    /// Serializes an array of signed 64-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<long> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}

/// <summary>
/// Provides serialization for 32-bit floating-point values.
/// </summary>
public static class floatSerialization
{
    /// <summary>
    /// Deserializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static float Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<float>(stream.ReadBase32());
    }

    /// <summary>
    /// Serializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this float self, Stream stream)
    {
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(long));
    }

    /// <summary>
    /// Deserializes an array of 32-bit floating-point values.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static float[] DeserializeArray(Stream stream, int count)
    {
        float[] res = new float[count];
        stream.ReadSpan<float>(res, count, false);
        return res;
    }

    /// <summary>
    /// Serializes an array of 32-bit floating-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<float> self, Stream stream)
    {
        stream.WriteSpan<float>(self, self.Length, false);
    }

    /// <summary>
    /// Serializes an array of 32-bit floating-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<float> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, false);
    }
}


/// <summary>
/// Provides serialization for 64-bit floating-point values.
/// </summary>
public static class doubleSerialization
{
    /// <summary>
    /// Deserializes a 64-bit floating-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static double Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<double>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes a 64-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this double self, Stream stream)
    {
        MemoryMarshal.Write(IoBuffer, ref self);
        stream.Write(IoBuffer, 0, sizeof(double));
    }

    /// <summary>
    /// Deserializes an array of 64-bit floating-point values.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static double[] DeserializeArray(Stream stream, int count)
    {
        double[] res = new double[count];
        stream.ReadSpan<double>(res, count, false);
        return res;
    }

    /// <summary>
    /// Serializes an array of 64-bit floating-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<double> self, Stream stream)
    {
        stream.WriteSpan<double>(self, self.Length, false);
    }

    /// <summary>
    /// Serializes an array of 64-bit floating-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<double> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, false);
    }
}

/// <summary>
/// Provides serialization for strings.
/// </summary>
public static class stringSerialization
{
    /// <summary>
    /// Deserializes a string.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static string Deserialize(Stream stream)
    {
        int count = intSerialization.Deserialize(stream);
        byte[] buf = Shared.Rent(count);
        try
        {
            stream.ReadSpan<byte>(buf, count, false);
            return Encoding.UTF8.GetString(buf, 0, count);
        }
        finally
        {
            Shared.Return(buf);
        }
    }

    /// <summary>
    /// Serializes a string.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this string self, Stream stream)
    {
        int count = Encoding.UTF8.GetByteCount(self);
        count.Serialize(stream);
        byte[] buf = Shared.Rent(count);
        try
        {
            Encoding.UTF8.GetBytes(self, 0, self.Length, buf, 0);
            stream.Write(buf, 0, count);
        }
        finally
        {
            Shared.Return(buf);
        }
    }

    /// <summary>
    /// Deserializes an array of strings.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static string[] DeserializeArray(Stream stream, int count)
    {
        string[] res = new string[count];
        for (int i = 0; i < count; i++) res[i] = Deserialize(stream);
        return res;
    }

    /// <summary>
    /// Serializes an array of strings.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this Span<string> self, Stream stream)
    {
        for (int i = 0; i < self.Length; i++) self[i].Serialize(stream);
    }

    /// <summary>
    /// Serializes an array of strings.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<string> self, Stream stream)
    {
        for (int i = 0; i < self.Length; i++) self[i].Serialize(stream);
    }
}
