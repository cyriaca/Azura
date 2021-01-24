using System;
using System.Collections.Generic;
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
            var tr = new TestRecord
            {
                ByteValue = 8,
                UintValue = 0x69,
                UintPleaseRefValue = 10,
                StructPleaseRefValue = new TestStruct {Guid = Guid.NewGuid()},
                EnumValue = TestRecord.TestEnum.B,
                EnumValue2 = TestRecord.TestEnum.C,
                EnumValue3 = TestRecord.TestEnum.B
            };
            tr.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tr, TestRecordSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Record (partial)
            var tr2 = new TestPartialRecord
            {
                ByteValue = 8,
                UintValue = 0x69,
                UintPleaseRefValue = 10,
                StructPleaseRefValue = new TestStruct {Guid = Guid.NewGuid()},
                EnumValue = TestPartialRecord.TestEnum.B,
                EnumValue2 = TestPartialRecord.TestEnum.C,
                EnumValue3 = TestPartialRecord.TestEnum.B
            };
            tr2.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(tr2, TestPartialRecordSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Struct
            var ts = new TestStruct {LongValue = 1010101010101, UshortValue = 66, Guid = Guid.NewGuid()};
            ts.Serialize(_ms);
            _ms.Position = 0;
            Assert.AreEqual(ts, TestStructSerialization.Deserialize(_ms));
            Assert.AreEqual(_ms.Length, _ms.Position);
            _ms.SetLength(0);

            // Class with array
            string[] arr = {"jj", "yy"};
            TestStruct?[] arr2 = {new TestStruct {LongValue = 0}, null};
            int?[] arr3 = {3, null};
            HashSet<int> hs = new() {3, 4, 5};
            Dictionary<string, int?> korone = new() {{"one", 1}, {"i'm die", 2}, {"thank you forever", null}};
            Dictionary<string, TestRecord.TestEnum?> migraine = new()
            {
                {"fucking", TestRecord.TestEnum.A}, {"deviants", TestRecord.TestEnum.B}
            };
            var ta = new TestClassWithArray
            {
                StringArrayValue = arr,
                StructArrayValue2 = arr2,
                IntArrayValue = arr3,
                HashSet = hs,
                Dictionary = korone,
                Dictionary3 = migraine
            };
            ta.Serialize(_ms);
            _ms.Position = 0;
            var resTa = TestClassWithArraySerialization.Deserialize(_ms);
            Assert.AreEqual(_ms.Length, _ms.Position);
            Assert.AreEqual(arr, resTa.StringArrayValue);
            Assert.AreEqual(null, resTa.StructArrayValue);
            Assert.AreEqual(arr2, resTa.StructArrayValue2);
            Assert.AreEqual(arr3, resTa.IntArrayValue);
            Assert.AreEqual(hs, resTa.HashSet);
            Assert.AreEqual(korone, resTa.Dictionary);
            Assert.AreEqual(migraine, resTa.Dictionary3);
            _ms.SetLength(0);
        }
    }
}
