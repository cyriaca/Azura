// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.IO;

#pragma warning disable 8714

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
    public static T[] DeserializeArray<T>(Stream stream, int count, Serialization<T>.Deserialize deserialize)
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
    public static void SerializeArray<T>(Span<T> self, Stream stream, Serialization<T>.Serialize serialize)
    {
        for (int i = 0; i < self.Length; i++) serialize(in self[i], stream);
    }

    /// <summary>
    /// Deserializes an array.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static T?[] DeserializeArrayValueNullable<T>(Stream stream, int count,
        Serialization<T>.Deserialize deserialize)
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
    public static void SerializeArrayValueNullable<T>(Span<T?> self, Stream stream,
        Serialization<T>.Serialize serialize) where T : struct
    {
        foreach (var t in self)
        {
            bool hasValue = t != null;
            hasValue.Serialize(stream);
            if (t != null)
            {
                var x = t.Value;
                serialize(in x, stream);
            }
        }
    }

    /// <summary>
    /// Deserializes an array.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static T?[] DeserializeArrayNullable<T>(Stream stream, int count, Serialization<T>.Deserialize deserialize)
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
    public static void SerializeArrayNullable<T>(Span<T?> self, Stream stream,
        Serialization<T>.Serialize serialize)
    {
        for (int i = 0; i < self.Length; i++)
        {
            var t = self[i];
            bool hasValue = t != null;
            hasValue.Serialize(stream);
            if (t != null)
            {
                serialize(in t!, stream);
            }
        }
    }

    #endregion

    #region Lists

    /// <summary>
    /// Deserializes a list.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static List<T> DeserializeList<T>(Stream stream, int count, Serialization<T>.Deserialize deserialize)
    {
        List<T> res = new();
        for (int i = 0; i < count; i++) res.Add(deserialize(stream));
        return res;
    }

    /// <summary>
    /// Serializes a list.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    /// <param name="serialize">Serializer method.</param>
    public static void SerializeList<T>(List<T> self, Stream stream, Serialization<T>.Serialize serialize)
    {
        foreach (var t in self)
        {
            var x = t;
            serialize(in x, stream);
        }
    }

    /// <summary>
    /// Deserializes a list.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static List<T?> DeserializeListValueNullable<T>(Stream stream, int count,
        Serialization<T>.Deserialize deserialize)
        where T : struct
    {
        List<T?> res = new();
        for (int i = 0; i < count; i++)
        {
            res.Add(byteSerialization.Deserialize(stream) != 0 ? deserialize(stream) : null);
        }

        return res;
    }

    /// <summary>
    /// Serializes a list.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    /// <param name="serialize">Serializer method.</param>
    public static void SerializeListValueNullable<T>(List<T?> self, Stream stream,
        Serialization<T>.Serialize serialize) where T : struct
    {
        foreach (var t in self)
        {
            bool hasValue = t != null;
            hasValue.Serialize(stream);
            if (t != null)
            {
                var x = t.Value;
                serialize(in x, stream);
            }
        }
    }

    /// <summary>
    /// Deserializes a list.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static List<T?> DeserializeListNullable<T>(Stream stream, int count,
        Serialization<T>.Deserialize deserialize)
    {
        List<T?> res = new();
        for (int i = 0; i < count; i++)
            res.Add(byteSerialization.Deserialize(stream) != 0 ? deserialize(stream) : default);

        return res;
    }

    /// <summary>
    /// Serializes a list.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    /// <param name="serialize">Serializer method.</param>
    public static void SerializeListNullable<T>(List<T?> self, Stream stream, Serialization<T>.Serialize serialize)
    {
        foreach (var t in self)
        {
            bool hasValue = t != null;
            hasValue.Serialize(stream);
            if (t != null)
            {
                var x = t;
                serialize(in x, stream);
            }
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
    public static HashSet<T> DeserializeHashSet<T>(Stream stream, int count, Serialization<T>.Deserialize deserialize)
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
    public static void SerializeHashSet<T>(HashSet<T> self, Stream stream, Serialization<T>.Serialize serialize)
    {
        foreach (var t in self)
        {
            var x = t;
            serialize(in x, stream);
        }
    }

    /// <summary>
    /// Deserializes a hash set.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static HashSet<T?> DeserializeHashSetValueNullable<T>(Stream stream, int count,
        Serialization<T>.Deserialize deserialize) where T : struct
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
        Serialization<T>.Serialize serialize) where T : struct
    {
        foreach (var t in self)
        {
            bool hasValue = t != null;
            hasValue.Serialize(stream);
            if (t != null)
            {
                var x = t!.Value;
                serialize(in x, stream);
            }
        }
    }

    /// <summary>
    /// Deserializes a hash set.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserialize">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static HashSet<T?> DeserializeHashSetNullable<T>(Stream stream, int count,
        Serialization<T>.Deserialize deserialize)
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
    public static void SerializeHashSetNullable<T>(HashSet<T?> self, Stream stream,
        Serialization<T>.Serialize serialize)
    {
        foreach (var t in self)
        {
            bool hasValue = t != null;
            hasValue.Serialize(stream);
            if (t != null)
            {
                var x = t;
                serialize(in x, stream);
            }
        }
    }

    #endregion

    #region Dictionaries

    /// <summary>
    /// Deserializes a dictionary.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserializeKey">Deserializer method.</param>
    /// <param name="deserializeValue">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static Dictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(Stream stream, int count,
        Serialization<TKey>.Deserialize deserializeKey, Serialization<TValue>.Deserialize deserializeValue)
    {
        Dictionary<TKey, TValue> res = new();
        for (int i = 0; i < count; i++) res.Add(deserializeKey(stream), deserializeValue(stream));
        return res;
    }

    /// <summary>
    /// Serializes a dictionary.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    /// <param name="serializeKey">Serializer method.</param>
    /// <param name="serializeValue">Serializer method.</param>
    public static void SerializeDictionary<TKey, TValue>(Dictionary<TKey, TValue> self, Stream stream,
        Serialization<TKey>.Serialize serializeKey, Serialization<TValue>.Serialize serializeValue)
    {
        foreach (var t in self)
        {
            var x = t.Key;
            serializeKey(in x, stream);
            var y = t.Value;
            serializeValue(in y, stream);
        }
    }

    /// <summary>
    /// Deserializes a dictionary.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserializeKey">Deserializer method.</param>
    /// <param name="deserializeValue">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static Dictionary<TKey, TValue?> DeserializeDictionaryValueNullable<TKey, TValue>(Stream stream,
        int count, Serialization<TKey>.Deserialize deserializeKey, Serialization<TValue>.Deserialize deserializeValue)
        where TValue : struct
    {
        Dictionary<TKey, TValue?> res = new();
        for (int i = 0; i < count; i++)
        {
            var key = deserializeKey(stream);
            res.Add(key, byteSerialization.Deserialize(stream) != 0 ? deserializeValue(stream) : default(TValue?));
        }

        return res;
    }

    /// <summary>
    /// Serializes a dictionary.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    /// <param name="serializeKey">Serializer method.</param>
    /// <param name="serializeValue">Serializer method.</param>
    public static void SerializeDictionaryValueNullable<TKey, TValue>(Dictionary<TKey, TValue?> self, Stream stream,
        Serialization<TKey>.Serialize serializeKey, Serialization<TValue>.Serialize serializeValue)
        where TValue : struct
    {
        foreach (var t in self)
        {
            var x = t.Key;
            serializeKey(in x, stream);
            bool hasValue = t.Value != null;
            hasValue.Serialize(stream);
            if (t.Value != null)
            {
                var y = t.Value.Value;
                serializeValue(in y, stream);
            }
        }
    }

    /// <summary>
    /// Deserializes a dictionary.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="count">Element count.</param>
    /// <param name="deserializeKey">Deserializer method.</param>
    /// <param name="deserializeValue">Deserializer method.</param>
    /// <returns>Value.</returns>
    public static Dictionary<TKey, TValue?> DeserializeDictionaryNullable<TKey, TValue>(Stream stream, int count,
        Serialization<TKey>.Deserialize deserializeKey, Serialization<TValue>.Deserialize deserializeValue)
    {
        Dictionary<TKey, TValue?> res = new();
        for (int i = 0; i < count; i++)
        {
            var key = deserializeKey(stream);
            res.Add(key, byteSerialization.Deserialize(stream) != 0 ? deserializeValue(stream) : default);
        }

        return res;
    }

    /// <summary>
    /// Serializes a dictionary.
    /// </summary>
    /// <param name="self">Value.</param>
    /// <param name="stream">Stream to write to.</param>
    /// <param name="serializeKey">Serializer method.</param>
    /// <param name="serializeValue">Serializer method.</param>
    public static void SerializeDictionaryNullable<TKey, TValue>(Dictionary<TKey, TValue?> self, Stream stream,
        Serialization<TKey>.Serialize serializeKey, Serialization<TValue>.Serialize serializeValue)
    {
        foreach (var t in self)
        {
            var x = t.Key;
            serializeKey(in x, stream);
            bool hasValue = t.Value != null;
            hasValue.Serialize(stream);
            if (t.Value != null)
            {
                var y = t.Value;
                serializeValue(in y, stream);
            }
        }
    }

    #endregion
}
