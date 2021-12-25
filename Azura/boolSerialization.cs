// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides serialization for booleans.
/// </summary>
public static class boolSerialization
{
    /// <summary>
    /// Deserializes a boolean.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Deserialize(Stream stream)
    {
        return stream.ReadBase8()[0] != 0;
    }

    /// <summary>
    /// Deserializes a boolean.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="self">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(Stream stream, out bool self) => self = stream.ReadBase8()[0] != 0;

    /// <summary>
    /// Serializes a boolean.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(bool self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        lcl[0] = self ? (byte)1 : (byte)0;
        stream.Write(lcl, 0, sizeof(byte));
    }

    /// <summary>
    /// Serializes a boolean.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this in bool self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        lcl[0] = self ? (byte)1 : (byte)0;
        stream.Write(lcl, 0, sizeof(byte));
    }

    /// <summary>
    /// Deserializes an array of booleans.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static bool[] DeserializeArray(Stream stream, int count)
    {
        bool[] res = new bool[count];
        stream.ReadSpan<bool>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of booleans.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void SerializeArray(this ReadOnlySpan<bool> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
