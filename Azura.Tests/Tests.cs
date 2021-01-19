using System.IO;
using Azura.TestBase;
using NUnit.Framework;

namespace Azura.Tests
{
    public class Tests
    {
        private readonly MemoryStream _ms = new();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestSerialization()
        {
            _ms.SetLength(0);

            // Basic
            int tInt = 194301;
            tInt.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tInt, intSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);
            string tStr = "saxton hale";
            tStr.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tStr, stringSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Class
            var tc = new TestClass {IntValue = 4, StringValue = "Sextant"};
            tc.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tc, TestClassSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Record
            var tr = new TestRecord {ByteValue = 8, UintValue = 0x69};
            tr.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tr, TestRecordSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Struct
            var ts = new TestStruct {LongValue = 1010101010101, UshortValue = 66};
            ts.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(ts, TestStructSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Class with array
            string[] arr = {"jj", "yy"};
            TestStruct?[] arr2 = {new TestStruct {LongValue = 0}, null};
            int?[] arr3 = {3, null};
            var ta = new TestClassWithArray {StringArrayValue = arr, StructArrayValue2 = arr2, IntArrayValue = arr3};
            ta.Serialize(_ms);
            _ms.Position = 0;
            var resTa = TestClassWithArraySerialization.Deserialize(_ms);
            Assert.AreEqual(_ms.Length, _ms.Position);
            Assert.AreEqual(arr, resTa.StringArrayValue);
            Assert.AreEqual(null, resTa.StructArrayValue);
            Assert.AreEqual(arr2, resTa.StructArrayValue2);
            Assert.AreEqual(arr3, resTa.IntArrayValue);
            _ms.SetLength(0);
        }
    }
}
