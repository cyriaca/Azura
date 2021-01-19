// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.IO;
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
    public static float Deserialize(Stream stream)
    {
        return MemoryMarshal.Read<float>(stream.ReadBase32());
    }

    /// <summary>
    /// Serializes a 32-bit floating-point value.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this float self, Stream stream)
    {
        MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(long));
    }
}