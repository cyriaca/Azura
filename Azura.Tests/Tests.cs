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
            _ms.SetLength(0);
            string tStr = "saxton hale";
            tStr.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tStr, stringSerialization.Deserialize(_ms));
            _ms.SetLength(0);

            // Class
            var tc = new TestClass {IntValue = 4, StringValue = "Sextant"};
            tc.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tc, TestClassSerialization.Deserialize(_ms));
            _ms.SetLength(0);

            // Record
            var tr = new TestRecord {ByteValue = 8, UintValue = 0x69};
            tr.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tr, TestRecordSerialization.Deserialize(_ms));
            _ms.SetLength(0);

            // Struct
            var ts = new TestStruct {LongValue = 1010101010101, UshortValue = 66};
            ts.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(ts, TestStructSerialization.Deserialize(_ms));
            _ms.SetLength(0);

            // Class with array
            string[] arr = {"jj", "yy"};
            var ta = new TestClassWithArray {StringArrayValue = arr};
            ta.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(arr, TestClassWithArraySerialization.Deserialize(_ms).StringArrayValue);
            _ms.SetLength(0);
        }
    }
}
