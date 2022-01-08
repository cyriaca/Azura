// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Provides serialization for GUIDs.
    /// </summary>
    public static class GuidSerialization
    {
        /// <summary>
        /// Deserializes a GUID.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Deserialize(Stream stream)
        {
            int a = intSerialization.Deserialize(stream);
            short b = shortSerialization.Deserialize(stream);
            short c = shortSerialization.Deserialize(stream);
            Span<byte> d = stream.ReadBase64();
            return new Guid(a, b, c, d[0], d[1], d[2], d[3], d[4], d[5], d[6], d[7]);
        }

        /// <summary>
        /// Deserializes a GUID.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="self">Value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deserialize(Stream stream, out Guid self) => self = Deserialize(stream);

        /// <summary>
        /// Serializes a GUID.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(Guid self, Stream stream)
        {
            byte[] lcl = SerializationInternals.IoBuffer;
            MemoryMarshal.Write(lcl, ref self);
            if (SerializationInternals._swap)
            {
                byte t = lcl[0];
                lcl[0] = lcl[3];
                lcl[3] = t;
                t = lcl[1];
                lcl[1] = lcl[2];
                lcl[2] = t;
                t = lcl[4];
                lcl[4] = lcl[5];
                lcl[5] = t;
                t = lcl[7];
                lcl[7] = lcl[6];
                lcl[6] = t;
            }
            stream.Write(lcl, 0, sizeof(decimal));
        }

        /// <summary>
        /// Serializes a GUID.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(this in Guid self, Stream stream)
        {
            byte[] lcl = SerializationInternals.IoBuffer;
            var self2 = self;
            MemoryMarshal.Write(lcl, ref self2);
            if (SerializationInternals._swap)
            {
                byte t = lcl[0];
                lcl[0] = lcl[3];
                lcl[3] = t;
                t = lcl[1];
                lcl[1] = lcl[2];
                lcl[2] = t;
                t = lcl[4];
                lcl[4] = lcl[5];
                lcl[5] = t;
                t = lcl[7];
                lcl[7] = lcl[6];
                lcl[6] = t;
            }
            stream.Write(lcl, 0, sizeof(decimal));
        }

        /// <summary>
        /// Deserializes an array of GUIDs.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <returns>Value.</returns>
        public static Guid[] DeserializeArray(Stream stream, int count)
        {
            Guid[] res = new Guid[count];
            for (int i = 0; i < res.Length; i++)
                res[i] = Deserialize(stream);
            return res;
        }

        /// <summary>
        /// Serializes an array of GUIDs.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        public static void SerializeArray(this ReadOnlySpan<Guid> self, Stream stream)
        {
            foreach(Guid guid in self) guid.Serialize(stream);
        }
    }
}
