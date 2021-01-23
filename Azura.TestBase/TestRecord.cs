namespace Azura.TestBase
{
    [Azura]
    public record TestRecord
    {
        [Azura] public byte ByteValue { get; init; }
        [Azura] public uint UintValue { get; init; }
        [Azura] public uint UintPleaseRefValue;
        [Azura] public TestStruct StructPleaseRefValue;
        [Azura]
        public TestEnum EnumValue { get; init; }

        [Azura] public TestEnum EnumValue2;

        public enum TestEnum
        {
            A,
            B,
            C
        }
    }
}
