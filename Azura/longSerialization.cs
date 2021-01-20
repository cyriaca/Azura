// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? BinaryPrimitives.ReverseEndianness(
                MemoryMarshal.Read<long>(stream.ReadBase64()))
            : MemoryMarshal.Read<long>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes a signed 64-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(long self, Stream stream)
    {
        if (SerializationInternals._swap) self = BinaryPrimitives.ReverseEndianness(self);
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(long));
    }

    /// <summary>
    /// Serializes a signed 64-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ref long self, Stream stream)
    {
        long v = SerializationInternals._swap ? BinaryPrimitives.ReverseEndianness(self) : self;
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref v);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(long));
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
    public static void SerializeArray(this ReadOnlySpan<long> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
