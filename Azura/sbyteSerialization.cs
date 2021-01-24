// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte Deserialize(Stream stream)
    {
        return (sbyte)stream.ReadBase8()[0];
    }

    /// <summary>
    /// Deserializes a signed 8-bit integer.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="self">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(Stream stream, out sbyte self) => self = (sbyte)stream.ReadBase8()[0];

    /// <summary>
    /// Serializes a signed 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(sbyte self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        lcl[0] = (byte)self;
        stream.Write(lcl, 0, sizeof(sbyte));
    }

    /// <summary>
    /// Serializes a signed 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this in sbyte self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        lcl[0] = (byte)self;
        stream.Write(lcl, 0, sizeof(sbyte));
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
    public static void SerializeArray(this ReadOnlySpan<sbyte> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
