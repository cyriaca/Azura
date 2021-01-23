using System.Collections.Generic;

namespace Azura.TestBase
{
    [Azura]
    public class TestClassWithArray
    {
        [Azura] public string[]? StringArrayValue { get; set; }
        [Azura] public TestStruct?[]? StructArrayValue { get; set; }
        [Azura] public TestStruct?[]? StructArrayValue2 { get; set; }
        [Azura] public int?[]? IntArrayValue { get; set; }
        [Azura] public int[]? IntArrayValue2 { get; set; }
        [Azura] public HashSet<int>? HashSet { get; set; }
        [Azura] public Dictionary<string, int?>? Dictionary { get; set; }
        [Azura] public Dictionary<string, TestClass?>? Dictionary2 { get; set; }
        [Azura] public Dictionary<string, TestRecord.TestEnum?>? Dictionary3 { get; set; }
    }
}
