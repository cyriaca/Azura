namespace Azura.TestBase
{
    [Azura]
    public partial record TestPartialRecord
    {
        [Azura] public byte ByteValue { get; init; }
        [Azura] public uint UintValue { get; init; }
        [Azura] public uint UintPleaseRefValue;
        [Azura] public TestStruct StructPleaseRefValue;
        [Azura] public TestEnum EnumValue { get; init; }

        [Azura] public TestEnum EnumValue2;

        [Azura] public readonly TestEnum? EnumValue3;
        [Azura] public TestStruct StructProperty { get; }

        public TestPartialRecord()
        {
        }

        public enum TestEnum
        {
            A,
            B,
            C
        }
    }
}
