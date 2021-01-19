// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.IO;

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
    public static byte Deserialize(Stream stream)
    {
        return stream.ReadBase8()[0];
    }

    /// <summary>
    /// Serializes an unsigned 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this byte self, Stream stream)
    {
        SerializationInternals.IoBuffer[0] = self;
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(byte));
    }
}
