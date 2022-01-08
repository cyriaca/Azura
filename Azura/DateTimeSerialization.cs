// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Provides serialization for <see cref="DateTime"/>s.
    /// </summary>
    public static class DateTimeSerialization
    {
        /// <summary>
        /// Deserializes a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DateTime Deserialize(Stream stream)
        {
            if (SerializationInternals._swap)
            {
                ulong value = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<ulong>(stream.ReadBase64()));
                return *(DateTime*)&value;
            }

            return MemoryMarshal.Read<DateTime>(stream.ReadBase64());
        }

        /// <summary>
        /// Deserializes a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="self">Value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deserialize(Stream stream, out DateTime self) => self = Deserialize(stream);

        /// <summary>
        /// Serializes a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Serialize(DateTime self, Stream stream)
        {
            ulong value = *(ulong*)&self;
            if (SerializationInternals._swap) value = BinaryPrimitives.ReverseEndianness(value);
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref value);
            stream.Write(lcl, 0, sizeof(ulong));
        }

        /// <summary>
        /// Serializes a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Serialize(this in DateTime self, Stream stream)
        {
            ulong value;
            fixed (void* p = &self) value = *(ulong*)p;
            if (SerializationInternals._swap) value = BinaryPrimitives.ReverseEndianness(value);
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref value);
            stream.Write(lcl, 0, sizeof(ulong));
        }

        /// <summary>
        /// Deserializes an array of <see cref="DateTime"/>s.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <returns>Value.</returns>
        public static DateTime[] DeserializeArray(Stream stream, int count)
        {
            DateTime[] res = new DateTime[count];
            stream.ReadSpan<DateTime>(res, count, true);
            return res;
        }

        /// <summary>
        /// Serializes an array of <see cref="DateTime"/>s.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static void SerializeArray(this ReadOnlySpan<DateTime> self, Stream stream)
        {
            stream.WriteSpan(self, self.Length, true);
        }
    }
}
