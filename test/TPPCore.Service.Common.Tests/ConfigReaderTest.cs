using Xunit;

namespace TPPCore.Service.Common.Tests
{
    public class ObjectGraph
    {
        public MyObject1 root;
    }
    public class MyObject1
    {
        public string field1 { get; set; }
        public double field2 { get; set; }
        public MyObject2 field3 { get; set; }
    }

    public class MyObject2
    {
        public string field1 { get; set; }
        public bool field2 { get; set; }
    }

    public class SimpleMap
    {
        public string a;
        public int b;
        public bool c;
        public double d;
        public string e;
        public string f;
        public int[] g;
    }

    public class NestedMap
    {
        public string key00;
        public string key01;
        public Key02 key02;

        public class Key02
        {
            public string key10;
            public string key11;
            public Key12 key12;

            public class Key12
            {
                public string key20;
                public string key21;
                public string key22;
            }
        }
    }

    public class ConfigReaderTest
    {
        [Fact]
        public void TestSimpleMap()
        {
            var configReader = new ConfigReader();
            configReader.LoadString(@"
{
    ""a"": ""hello"",
    ""b"": 123,
    ""c"": true,
    ""d"": 123.456,
    ""e"": ""hello world"",
    ""f"": ""123"",
    ""g"": [1, 2, 3]
}
            ");

            Assert.Equal("hello", configReader.GetCheckedValue<string, SimpleMap>("a"));
            Assert.Equal(123, configReader.GetCheckedValue<int, SimpleMap>("b"));
            Assert.True(configReader.GetCheckedValue<bool, SimpleMap>("c"));
            Assert.Equal(123.456, configReader.GetCheckedValue<double, SimpleMap>("d"));
            Assert.Equal("hello world", configReader.GetCheckedValue<string, SimpleMap>("e"));
            Assert.Equal("123", configReader.GetCheckedValue<string, SimpleMap>("f"));
            Assert.Equal(new[] {1, 2, 3}, configReader.GetCheckedValue<int[], SimpleMap>("g"));

            Assert.Equal("hi", configReader.GetCheckedValueOrDefault<string, SimpleMap>(new[] {"no exist"}, "hi"));

            Assert.Throws<ConfigKeyNotFoundException>(() => configReader.GetCheckedValue<string, SimpleMap>("no exist"));
            Assert.Throws<ConfigException>(() => configReader.GetCheckedValue<int, SimpleMap>("a"));
        }

        [Fact]
        public void TestNestedMap()
        {
            var configReader = new ConfigReader();
            configReader.LoadString(@"
{
    ""key00"": ""value0.0"",
    ""key01"": ""value0.1"",
    ""key02"": {
        ""key10"": ""value1.0"",
        ""key11"": ""value1.1"",
        ""key12"": {
            ""key20"": ""value2.0"",
            ""key21"": ""value2.1"",
            ""key22"": ""value2.2""
        }
    }
}");

            Assert.Equal("value2.2", configReader.GetCheckedValue<string, NestedMap>("key02", "key12", "key22"));
        }

        [Fact]
        public void TestObjectGraph()
        {
            var configReader = new ConfigReader();

            configReader.LoadString(@"
{
    ""root"": {
        ""field1"": ""hi"",
        ""field2"": 123.456,
        ""field3"": {
            ""field1"": ""hello"",
            ""field2"": true
        }
    }
}");

            var result = configReader.GetCheckedValue<MyObject1, ObjectGraph>(new[] {"root"});

            Assert.IsType<MyObject1>(result);
            Assert.Equal("hi", result.field1);
            Assert.Equal(123.456, result.field2);
            Assert.Equal("hello", result.field3.field1);
            Assert.True(result.field3.field2);
        }
    }
}
