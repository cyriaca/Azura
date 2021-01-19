// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? BinaryPrimitives.ReverseEndianness(
                MemoryMarshal.Read<ulong>(stream.ReadBase64()))
            : MemoryMarshal.Read<ulong>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes an unsigned 64-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ulong self, Stream stream)
    {
        if (SerializationInternals._swap) self = BinaryPrimitives.ReverseEndianness(self);
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(ulong));
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
    public static void SerializeArray(this ReadOnlySpan<ulong> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
