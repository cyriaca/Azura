using System;
using System.Collections.Generic;
using System.Globalization;
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
            _ms.SetLength(0);
        }

        [Test]
        public void TestSerializationInt()
        {
            const int tInt = 194301;
            tInt.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(intSerialization.Deserialize(_ms), Is.EqualTo(tInt));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationTimeSpan()
        {
            TimeSpan ts = new(103049, 18, 34, 59, 671);
            ts.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TimeSpanSerialization.Deserialize(_ms), Is.EqualTo(ts));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationDateTime()
        {
            DateTime dt = DateTime.ParseExact("Thu 14 Dec 2006 10:49 AM",
                "ddd dd MMM yyyy h:mm tt",
                CultureInfo.InvariantCulture);
            dt.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(DateTimeSerialization.Deserialize(_ms), Is.EqualTo(dt));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationDateTimeOffset()
        {
            DateTimeOffset dto = DateTimeOffset.ParseExact("Thu 14 Dec 2006 10:49 AM +09:00",
                "ddd dd MMM yyyy h:mm tt zzz",
                CultureInfo.InvariantCulture);
            dto.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(DateTimeOffsetSerialization.Deserialize(_ms), Is.EqualTo(dto));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationGuid()
        {
            Guid guid = Guid.ParseExact("811022B7-07F8-43C5-9AD7-5B4FF867C959", "D");
            guid.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(GuidSerialization.Deserialize(_ms), Is.EqualTo(guid));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationString()
        {
            const string tStr = "saxton hale";
            tStr.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(stringSerialization.Deserialize(_ms), Is.EqualTo(tStr));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationClass()
        {
            var tc = new TestClass { IntValue = 4, StringValue = "Sextant" };
            tc.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestClassSerialization.Deserialize(_ms), Is.EqualTo(tc));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationFileScopedNamespace()
        {
            var tcfsn = new TestFileScopedNamespace { IntValue = 4, StringValue = "Sextant" };
            tcfsn.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestFileScopedNamespaceSerialization.Deserialize(_ms), Is.EqualTo(tcfsn));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationRecord()
        {
            var tr = new TestRecord
            {
                ByteValue = 8,
                UintValue = 0x69,
                UintPleaseRefValue = 10,
                StructPleaseRefValue = new TestStruct { Guid = Guid.NewGuid() },
                EnumValue = TestRecord.TestEnum.B,
                EnumValue2 = TestRecord.TestEnum.C,
                EnumValue3 = TestRecord.TestEnum.B
            };
            tr.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestRecordSerialization.Deserialize(_ms), Is.EqualTo(tr));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationRecordStruct()
        {
            var trs = new TestRecordStruct
            {
                ByteValue = 8,
                UintValue = 0x69,
                UintPleaseRefValue = 10,
                StructPleaseRefValue = new TestStruct { Guid = Guid.NewGuid() },
                EnumValue = TestRecordStruct.TestEnum.B,
                EnumValue2 = TestRecordStruct.TestEnum.C,
                EnumValue3 = TestRecordStruct.TestEnum.B
            };
            trs.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestRecordStructSerialization.Deserialize(_ms), Is.EqualTo(trs));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationPartialRecord()
        {
            var tr2 = new TestPartialRecord
            {
                ByteValue = 8,
                UintValue = 0x69,
                UintPleaseRefValue = 10,
                StructPleaseRefValue = new TestStruct { Guid = Guid.NewGuid() },
                EnumValue = TestPartialRecord.TestEnum.B,
                EnumValue2 = TestPartialRecord.TestEnum.C
            };
            tr2.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestPartialRecordSerialization.Deserialize(_ms), Is.EqualTo(tr2));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
            _ms.Position = 0;
            Assert.That(new TestPartialRecord(new AzuraContext(_ms)), Is.EqualTo(tr2));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationPartialRecordStruct()
        {
            var trs2 = new TestPartialRecordStruct
            {
                ByteValue = 8,
                UintValue = 0x69,
                UintPleaseRefValue = 10,
                StructPleaseRefValue = new TestStruct { Guid = Guid.NewGuid() },
                EnumValue = TestPartialRecordStruct.TestEnum.B,
                EnumValue2 = TestPartialRecordStruct.TestEnum.C
            };
            trs2.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestPartialRecordStructSerialization.Deserialize(_ms), Is.EqualTo(trs2));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
            _ms.Position = 0;
            Assert.That(new TestPartialRecordStruct(new AzuraContext(_ms)), Is.EqualTo(trs2));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationStruct()
        {
            var ts = new TestStruct { LongValue = 1010101010101, UshortValue = 66, Guid = Guid.NewGuid() };
            ts.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestStructSerialization.Deserialize(_ms), Is.EqualTo(ts));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationPartialStruct()
        {
            var ts2 = new TestPartialStruct { LongValue = 1010101010101, UshortValue = 66, Guid = Guid.NewGuid() };
            ts2.Serialize(_ms);
            _ms.Position = 0;
            Assert.That(TestPartialStructSerialization.Deserialize(_ms), Is.EqualTo(ts2));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
            _ms.Position = 0;
            Assert.That(new TestPartialStruct(new AzuraContext(_ms)), Is.EqualTo(ts2));
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
        }

        [Test]
        public void TestSerializationArray()
        {
            string[] arr = { "jj", "yy" };
            TestStruct?[] arr2 = { new TestStruct { LongValue = 0 }, null };
            int?[] arr3 = { 3, null };
            HashSet<int> hs = new() { 3, 4, 5 };
            Dictionary<string, int?> yes = new()
            {
                { "street", -99999 },
                { "USAO", 0 },
                { "koko", 1 },
                { "aitsuki nakuru", 2 },
                { "sennzai", null }
            };
            Dictionary<string, TestRecord.TestEnum?> migraine = new()
            {
                { "fucking", TestRecord.TestEnum.A }, { "deviants", TestRecord.TestEnum.B }
            };
            var ta = new TestClassWithArray
            {
                StringArrayValue = arr,
                StructArrayValue2 = arr2,
                IntArrayValue = arr3,
                HashSet = hs,
                Dictionary = yes,
                Dictionary3 = migraine
            };
            ta.Serialize(_ms);
            _ms.Position = 0;
            var resTa = TestClassWithArraySerialization.Deserialize(_ms);
            Assert.That(_ms.Position, Is.EqualTo(_ms.Length));
            Assert.That(resTa.StringArrayValue, Is.EqualTo(arr));
            Assert.That(resTa.StructArrayValue, Is.EqualTo(null));
            Assert.That(resTa.StructArrayValue2, Is.EqualTo(arr2));
            Assert.That(resTa.IntArrayValue, Is.EqualTo(arr3));
            Assert.That(resTa.HashSet, Is.EqualTo(hs));
            Assert.That(resTa.Dictionary, Is.EqualTo(yes));
            Assert.That(resTa.Dictionary3, Is.EqualTo(migraine));
        }
    }
}
