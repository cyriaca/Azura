using System;
using System.IO;

namespace Azura
{
    /// <summary>
    /// Common serialization utilities.
    /// </summary>
    public static class SerializationBase
    {
        /// <summary>
        /// Deserializes an array.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <param name="deserialize">Deserializer method.</param>
        /// <returns>Value.</returns>
        public static T[] DeserializeArray<T>(Stream stream, int count, Func<Stream, T> deserialize)
        {
            T[] res = new T[count];
            for (int i = 0; i < count; i++) res[i] = deserialize(stream);
            return res;
        }

        /// <summary>
        /// Serializes an array.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="serialize">Serializer method.</param>
        public static void SerializeArray<T>(ReadOnlySpan<T> self, Stream stream, Action<T, Stream> serialize)
        {
            for (int i = 0; i < self.Length; i++) serialize(self[i], stream);
        }

        /// <summary>
        /// Deserializes an array.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <param name="deserialize">Deserializer method.</param>
        /// <returns>Value.</returns>
        public static T?[] DeserializeArrayValueNullable<T>(Stream stream, int count, Func<Stream, T> deserialize) where T : struct
        {
            T?[] res = new T?[count];
            for (int i = 0; i < count; i++)
            {
                if (byteSerialization.Deserialize(stream) != 0)
                    res[i] = deserialize(stream);
            }

            return res;
        }

        /// <summary>
        /// Serializes an array.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="serialize">Serializer method.</param>
        public static void SerializeArrayValueNullable<T>(ReadOnlySpan<T?> self, Stream stream, Action<T, Stream> serialize) where T : struct
        {
            for (int i = 0; i < self.Length; i++)
            {
                (self[i] != null ? (byte)1 : (byte)0).Serialize(stream);
                if (self[i] != null) serialize(self[i]!.Value, stream);
            }
        }

        /// <summary>
        /// Deserializes an array.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <param name="deserialize">Deserializer method.</param>
        /// <returns>Value.</returns>
        public static T?[] DeserializeArrayNullable<T>(Stream stream, int count, Func<Stream, T> deserialize)
        {
            T?[] res = new T?[count];
            for (int i = 0; i < count; i++)
            {
                if (byteSerialization.Deserialize(stream) != 0)
                    res[i] = deserialize(stream);
            }

            return res;
        }

        /// <summary>
        /// Serializes an array.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="serialize">Serializer method.</param>
        public static void SerializeArrayNullable<T>(ReadOnlySpan<T?> self, Stream stream, Action<T, Stream> serialize)
        {
            for (int i = 0; i < self.Length; i++)
            {
                (self[i] != null ? (byte)1 : (byte)0).Serialize(stream);
                if (self[i] != null) serialize(self[i]!, stream);
            }
        }
    }
}
