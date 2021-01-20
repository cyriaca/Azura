// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.IO;
using System.Runtime.CompilerServices;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Deserialize(Stream stream)
    {
        int count = intSerialization.Deserialize(stream);
        byte[] buf = Shared.Rent(count);
        try
        {
            stream.ReadArray(buf, 0, count);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this string self, Stream stream) => Serialize(ref self, stream);

    /// <summary>
    /// Serializes a string.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(ref string self, Stream stream)
    {
        int count = Encoding.UTF8.GetByteCount(self);
        intSerialization.Serialize(count, stream);
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
}
