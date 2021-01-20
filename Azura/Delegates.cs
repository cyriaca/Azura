// ReSharper disable CheckNamespace

using System.IO;

/// <summary>
/// Provides delegates for serialization.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public class Serialization<T>
{
    /// <summary>
    /// Deserialization delegate.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    public delegate T Deserialize(Stream stream);

    /// <summary>
    /// Serialization delegate.
    /// </summary>
    /// <param name="t">Data type.</param>
    /// <param name="stream">Target stream.</param>
    public delegate void Serialize(ref T t, Stream stream);
}
