// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? BinaryPrimitives.ReverseEndianness(
                MemoryMarshal.Read<uint>(stream.ReadBase32()))
            : MemoryMarshal.Read<uint>(stream.ReadBase32());
    }

    /// <summary>
    /// Deserializes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="self">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(Stream stream, out uint self) => self = Deserialize(stream);

    /// <summary>
    /// Serializes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(uint self, Stream stream)
    {
        if (SerializationInternals._swap) self = BinaryPrimitives.ReverseEndianness(self);
        byte[] lcl = SerializationInternals.IoBuffer;
        MemoryMarshal.Write(lcl, ref self);
        stream.Write(lcl, 0, sizeof(uint));
    }

    /// <summary>
    /// Serializes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this in uint self, Stream stream)
    {
        uint v = SerializationInternals._swap ? BinaryPrimitives.ReverseEndianness(self) : self;
        byte[] lcl = SerializationInternals.IoBuffer;
        MemoryMarshal.Write(lcl, ref v);
        stream.Write(lcl, 0, sizeof(uint));
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
    public static void SerializeArray(this ReadOnlySpan<uint> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
