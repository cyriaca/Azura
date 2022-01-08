// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Provides serialization for <see cref="DateTimeOffset"/>s.
    /// </summary>
    public static class DateTimeOffsetSerialization
    {
        /// <summary>
        /// Deserializes a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTimeOffset Deserialize(Stream stream)
        {
            DateTime dt = DateTimeSerialization.Deserialize(stream);
            short offset = MemoryMarshal.Read<short>(stream.ReadBase16());
            if (SerializationInternals._swap) offset = BinaryPrimitives.ReverseEndianness(offset);
            return new DateTimeOffset(dt, new TimeSpan(offset * TimeSpan.TicksPerMinute));
        }

        /// <summary>
        /// Deserializes a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="self">Value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deserialize(Stream stream, out DateTimeOffset self) => self = Deserialize(stream);

        /// <summary>
        /// Serializes a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(DateTimeOffset self, Stream stream)
        {
            self.DateTime.Serialize(stream);
            short offset = (short)(self.Offset.Ticks / TimeSpan.TicksPerMinute);
            if (SerializationInternals._swap) offset = BinaryPrimitives.ReverseEndianness(offset);
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref offset);
            stream.Write(lcl, 0, sizeof(short));
        }

        /// <summary>
        /// Serializes a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(this in DateTimeOffset self, Stream stream)
        {
            self.DateTime.Serialize(stream);
            short offset = (short)(self.Offset.Ticks / TimeSpan.TicksPerMinute);
            if (SerializationInternals._swap) offset = BinaryPrimitives.ReverseEndianness(offset);
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref offset);
            stream.Write(lcl, 0, sizeof(short));
        }

        /// <summary>
        /// Deserializes an array of <see cref="DateTimeOffset"/>s.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <returns>Value.</returns>
        public static DateTimeOffset[] DeserializeArray(Stream stream, int count)
        {
            DateTimeOffset[] res = new DateTimeOffset[count];
            for (int i = 0; i < res.Length; i++)
                res[i] = Deserialize(stream);
            return res;
        }

        /// <summary>
        /// Serializes an array of <see cref="DateTimeOffset"/>s.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static void SerializeArray(this ReadOnlySpan<DateTimeOffset> self, Stream stream)
        {
            foreach(DateTimeOffset dto in self) dto.Serialize(stream);
        }
    }
}
