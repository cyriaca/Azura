using System;

namespace Azura.TestBase;

[Azura]
public class TestFileScopedNamespace : IEquatable<TestFileScopedNamespace>
{
    [Azura] public int IntValue { get; init; }
    [Azura] public string? StringValue { get; init; }

    public bool Equals(TestFileScopedNamespace? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return IntValue == other.IntValue && StringValue == other.StringValue;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((TestClass)obj);
    }

    public override int GetHashCode() => HashCode.Combine(IntValue, StringValue);
}
