// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Deserialize(Stream stream)
    {
        return stream.ReadBase8()[0];
    }

    /// <summary>
    /// Serializes an unsigned 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(byte self, Stream stream)
    {
        SerializationInternals.IoBuffer[0] = self;
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(byte));
    }

    /// <summary>
    /// Serializes an unsigned 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ref byte self, Stream stream)
    {
        SerializationInternals.IoBuffer[0] = self;
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(byte));
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
    public static void SerializeArray(this ReadOnlySpan<byte> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
