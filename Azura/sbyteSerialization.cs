// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.IO;

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
    public static sbyte Deserialize(Stream stream)
    {
        return (sbyte)stream.ReadBase8()[0];
    }

    /// <summary>
    /// Serializes a signed 8-bit integer.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this sbyte self, Stream stream)
    {
        SerializationInternals.IoBuffer[0] = (byte)self;
        stream.Write(SerializationInternals.IoBuffer, 0, sizeof(sbyte));
    }
}
