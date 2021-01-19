// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
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
    public static double Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<double>(stream.ReadBase64());
    }

    /// <summary>
    /// Serializes a 64-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this double self, Stream stream)
    {
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(double));
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
