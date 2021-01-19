// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Text;
using static System.Buffers.ArrayPool<byte>;


/// <summary>
/// Provides serialization for strings.
/// </summary>
public static class stringSerialization
{
    /// <summary>
    /// Deserializes a string.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Value.</returns>
    public static string Deserialize(Stream stream)
    {
        int count = intSerialization.Deserialize(stream);
        byte[] buf = Shared.Rent(count);
        try
        {
            stream.ReadSpan<byte>(buf, count, false);
            return Encoding.UTF8.GetString(buf, 0, count);
        }
        finally
        {
            Shared.Return(buf);
        }
    }

    /// <summary>
    /// Serializes a string.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this string self, Stream stream)
    {
        int count = Encoding.UTF8.GetByteCount(self);
        count.Serialize(stream);
        byte[] buf = Shared.Rent(count);
        try
        {
            Encoding.UTF8.GetBytes(self, 0, self.Length, buf, 0);
            stream.Write(buf, 0, count);
        }
        finally
        {
            Shared.Return(buf);
        }
    }

    /// <summary>
    /// Deserializes an array of unsigned 32-bit integers.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <returns>Value.</returns>
    public static uint[] DeserializeArray(Stream stream, int count)
    {
        uint[] res = new uint[count];
        stream.ReadSpan<uint>(res, count, true);
        return res;
    }

    /// <summary>
    /// Serializes an array of unsigned 32-bit integers.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void Serialize(this ReadOnlySpan<uint> self, Stream stream)
    {
        stream.WriteSpan(self, self.Length, true);
    }
}
