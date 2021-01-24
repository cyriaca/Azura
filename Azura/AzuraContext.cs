using System.IO;

namespace Azura
{
    /// <summary>
    /// Represents a deserializer context.
    /// </summary>
    /// <remarks>
    /// Only provided to ensure a unique constructor is used.
    /// </remarks>
    public struct AzuraContext
    {
        /// <summary>
        /// Stream used by the context.
        /// </summary>
        public Stream Stream;

        /// <summary>
        /// Creates a new instance of <see cref="AzuraContext"/>.
        /// </summary>
        /// <param name="stream">Stream used by the context.</param>
        public AzuraContext(Stream stream)
        {
            Stream = stream;
        }
    }
}
