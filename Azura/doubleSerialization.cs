// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides serialization for 64-bit floating-point values.
/// </summary>
public static class doubleSerialization
{
    /// <summary>
    /// Deserializes a 64-bit floating-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<double>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes a 64-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(double self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        MemoryMarshal.Write(lcl, ref self);
        stream.Write(lcl, 0, sizeof(double));
    }

    /// <summary>
    /// Serializes a 64-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ref double self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        MemoryMarshal.Write(lcl, ref self);
        stream.Write(lcl, 0, sizeof(double));
    }

    /// <summary>
    /// Deserializes an array of 64-bit floating-point values.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static double[] DeserializeArray(Stream stream, int count)
    {
        double[] res = new double[count];
        stream.ReadSpan<double>(res, count, false);
        return res;
    }

    /// <summary>
    /// Serializes an array of 64-bit floating-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void SerializeArray(this ReadOnlySpan<double> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, false);
    }
}
