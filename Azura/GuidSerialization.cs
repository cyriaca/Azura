// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Provides serialization for guids.
    /// </summary>
    public static class GuidSerialization
    {
        /// <summary>
        /// Deserializes a guid.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Deserialize(Stream stream)
        {
            return MemoryMarshal.Read<Guid>(stream.ReadBase128());
        }

        /// <summary>
        /// Serializes a guid.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(this Guid self, Stream stream)
        {
            MemoryMarshal.Write(SerializationInternals.IoBuffer, ref self);
            stream.Write(SerializationInternals.IoBuffer, 0, sizeof(decimal));
        }

        /// <summary>
        /// Deserializes an array of guids.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <returns>Value.</returns>
        public static Guid[] DeserializeArray(Stream stream, int count)
        {
            Guid[] res = new Guid[count];
            stream.ReadSpan<Guid>(res, count, true);
            return res;
        }

        /// <summary>
        /// Serializes an array of guids.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static void SerializeArray(this ReadOnlySpan<Guid> self, Stream stream)
        {
            stream.WriteSpan(self, self.Length, true);
        }
    }
}
