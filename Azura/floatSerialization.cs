// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides serialization for 32-bit floating-point values.
/// </summary>
public static class floatSerialization
{
    /// <summary>
    /// Deserializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<float>(stream.ReadBase32());
    }

    /// <summary>
    /// Deserializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="self">Value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(Stream stream, out float self) => self = Deserialize(stream);

    /// <summary>
    /// Serializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(float self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        MemoryMarshal.Write(lcl, ref self);
        stream.Write(lcl, 0, sizeof(float));
    }

    /// <summary>
    /// Serializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this in float self, Stream stream)
    {
        byte[] lcl = SerializationInternals.IoBuffer;
        float self2 = self;
        MemoryMarshal.Write(lcl, ref self2);
        stream.Write(lcl, 0, sizeof(float));
    }

    /// <summary>
    /// Deserializes an array of 32-bit floating-point values.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static float[] DeserializeArray(Stream stream, int count)
    {
        float[] res = new float[count];
        stream.ReadSpan<float>(res, count, false);
        return res;
    }

    /// <summary>
    /// Serializes an array of 32-bit floating-point values.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void SerializeArray(this ReadOnlySpan<float> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, false);
    }
}
