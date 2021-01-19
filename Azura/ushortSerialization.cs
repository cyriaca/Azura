// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? BinaryPrimitives.ReverseEndianness(
                MemoryMarshal.Read<ushort>(stream.ReadBase16()))
            : MemoryMarshal.Read<ushort>(stream.ReadBase16());
    }

    /// <summary>
    /// Serializes an unsigned 16-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ushort self, Stream stream)
    {
        if (SerializationInternals._swap) self = BinaryPrimitives.ReverseEndianness(self);
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(ushort));
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
    public static void SerializeArray(this ReadOnlySpan<ushort> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
