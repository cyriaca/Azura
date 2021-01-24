// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides serialization for 128-bit fixed-point values.
/// </summary>
public static class decimalSerialization
{
    /// <summary>
    /// Deserializes a 128-bit fixed-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<decimal>(stream.ReadBase128());
    }

    /// <summary>
    /// Deserializes a 128-bit fixed-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="self">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(Stream stream, out decimal self) => self = Deserialize(stream);

    /// <summary>
    /// Serializes a 128-bit fixed-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(decimal self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        MemoryMarshal.Write(lcl, ref self);
        stream.Write(lcl, 0, sizeof(decimal));
    }

    /// <summary>
    /// Serializes a 128-bit fixed-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this in decimal self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        decimal self2 = self;
        MemoryMarshal.Write(lcl, ref self2);
        stream.Write(lcl, 0, sizeof(decimal));
    }

    /// <summary>
    /// Deserializes an array of 128-bit fixed-point values.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static decimal[] DeserializeArray(Stream stream, int count)
    {
        decimal[] res = new decimal[count];
        stream.ReadSpan<decimal>(res, count, false);
        return res;
    }

    /// <summary>
    /// Serializes an array of 128-bit fixed-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void SerializeArray(this ReadOnlySpan<decimal> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, false);
    }
}
