using System;
using System.Collections.Generic;
using System.IO;

namespace Azura
{
    /// <summary>
    /// Common serialization utilities.
    /// </summary>
    public static class SerializationBase
    {
        #region Arrays

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
        public static T?[] DeserializeArrayValueNullable<T>(Stream stream, int count, Func<Stream, T> deserialize)
            where T : struct
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
        public static void SerializeArrayValueNullable<T>(ReadOnlySpan<T?> self, Stream stream,
            Action<T, Stream> serialize) where T : struct
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

        #endregion

        #region HashSets

        /// <summary>
        /// Deserializes a hash set.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <param name="deserialize">Deserializer method.</param>
        /// <returns>Value.</returns>
        public static HashSet<T> DeserializeHashSet<T>(Stream stream, int count, Func<Stream, T> deserialize)
        {
            HashSet<T> res = new();
            for (int i = 0; i < count; i++) res.Add(deserialize(stream));
            return res;
        }

        /// <summary>
        /// Serializes a hash set.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="serialize">Serializer method.</param>
        public static void SerializeHashSet<T>(HashSet<T> self, Stream stream, Action<T, Stream> serialize)
        {
            foreach (var t in self) serialize(t, stream);
        }

        /// <summary>
        /// Deserializes a hash set.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <param name="deserialize">Deserializer method.</param>
        /// <returns>Value.</returns>
        public static HashSet<T?> DeserializeHashSetValueNullable<T>(Stream stream, int count,
            Func<Stream, T> deserialize) where T : struct
        {
            HashSet<T?> res = new();
            for (int i = 0; i < count; i++)
                res.Add(byteSerialization.Deserialize(stream) != 0 ? deserialize(stream) : default(T?));
            return res;
        }

        /// <summary>
        /// Serializes a hash set.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="serialize">Serializer method.</param>
        public static void SerializeHashSetValueNullable<T>(HashSet<T?> self, Stream stream,
            Action<T, Stream> serialize) where T : struct
        {
            foreach (var t in self)
            {
                (t != null ? (byte)1 : (byte)0).Serialize(stream);
                if (t != null) serialize(t!.Value, stream);
            }
        }

        /// <summary>
        /// Deserializes a hash set.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="count">Element count.</param>
        /// <param name="deserialize">Deserializer method.</param>
        /// <returns>Value.</returns>
        public static HashSet<T?> DeserializeHashSetNullable<T>(Stream stream, int count, Func<Stream, T> deserialize)
        {
            HashSet<T?> res = new();
            for (int i = 0; i < count; i++)
                res.Add(byteSerialization.Deserialize(stream) != 0 ? deserialize(stream) : default);
            return res;
        }

        /// <summary>
        /// Serializes a hash set.
        /// </summary>
        /// <param name="self">Value.</param>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="serialize">Serializer method.</param>
        public static void SerializeHashSetNullable<T>(HashSet<T?> self, Stream stream, Action<T, Stream> serialize)
        {
            foreach (var t in self)
            {
                (t != null ? (byte)1 : (byte)0).Serialize(stream);
                if (t != null) serialize(t!, stream);
            }
        }

        #endregion
    }
}
