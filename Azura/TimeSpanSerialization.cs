// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Provides serialization for timespans.
    /// </summary>
    public static class TimeSpanSerialization
    {
        /// <summary>
        /// Deserializes a timespan.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Value.</returns>
        public static unsafe TimeSpan Deserialize(Stream stream)
        {
            if (SerializationInternals._swap)
            {
                long value = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(stream.ReadBase64()));
                return *(TimeSpan*)value;
            }

            return MemoryMarshal.Read<TimeSpan>(stream.ReadBase64());
        }

        /// <summary>
        /// Serializes a timespan.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static unsafe void Serialize(TimeSpan self, Stream stream)
        {
            long value = *(long*)&self;
            if (SerializationInternals._swap) value = BinaryPrimitives.ReverseEndianness(value);
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref value);
            stream.Write(lcl, 0, sizeof(long));
        }

        /// <summary>
        /// Serializes a timespan.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static unsafe void Serialize(this ref TimeSpan self, Stream stream)
        {
            long value;
            fixed (void* p = &self) value = *(long*)p;
            if (SerializationInternals._swap) value = BinaryPrimitives.ReverseEndianness(value);
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref value);
            stream.Write(lcl, 0, sizeof(long));
        }

        /// <summary>
        /// Deserializes an array of timespans.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <returns>Value.</returns>
        public static TimeSpan[] DeserializeArray(Stream stream, int count)
        {
            TimeSpan[] res = new TimeSpan[count];
            stream.ReadSpan<TimeSpan>(res, count, true);
            return res;
        }

        /// <summary>
        /// Serializes an array of timespans.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static void SerializeArray(this ReadOnlySpan<TimeSpan> self, Stream stream)
        {
            stream.WriteSpan(self, self.Length, true);
        }
    }
}
