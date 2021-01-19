// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides serialization for characters.
/// </summary>
public static class charSerialization
{
    /// <summary>
    /// Deserializes a character.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char Deserialize(Stream stream)
    {
        return SerializationInternals._swap
            ? (char)BinaryPrimitives.ReverseEndianness(
                MemoryMarshal.Read<char>(stream.ReadBase16()))
            : MemoryMarshal.Read<char>(stream.ReadBase16());
    }

    /// <summary>
    /// Serializes a character.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this char self, Stream stream)
    {
        if (SerializationInternals._swap) self = (char)BinaryPrimitives.ReverseEndianness(self);
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(char));
    }

    /// <summary>
    /// Deserializes an array of characters.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static char[] DeserializeArray(Stream stream, int count)
    {
        char[] res = new char[count];
        stream.ReadSpan<char>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of characters.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void SerializeArray(this ReadOnlySpan<char> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
