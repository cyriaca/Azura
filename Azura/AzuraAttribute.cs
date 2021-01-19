using System;

namespace Azura
{
    /// <summary>
    /// Marks a type, field, or property as being used for serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field |
                    AttributeTargets.Property)]
    public class AzuraAttribute : Attribute
    {
    }
}
