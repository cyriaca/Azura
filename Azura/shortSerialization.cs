// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides serialization for signed 16-bit integers.
/// </summary>
public static class shortSerialization
{
    /// <summary>
    /// Deserializes a signed 16-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? BinaryPrimitives.ReverseEndianness(
                MemoryMarshal.Read<short>(stream.ReadBase16()))
            : MemoryMarshal.Read<short>(stream.ReadBase16());
    }

    /// <summary>
    /// Serializes a signed 16-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this short self, Stream stream)
    {
        if (SerializationInternals._swap) self = BinaryPrimitives.ReverseEndianness(self);
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(short));
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
    public static void SerializeArray(this ReadOnlySpan<short> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
