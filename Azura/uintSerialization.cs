// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.Buffers.Binary;
using System.IO;
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
    public static uint Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? BinaryPrimitives.ReverseEndianness(
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
        if (SerializationInternals._swap) self = BinaryPrimitives.ReverseEndianness(self);
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(uint));
    }
}
