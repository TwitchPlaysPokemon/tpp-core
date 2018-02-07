using System;
using Xunit;

namespace TPPCore.Service.Common.Tests
{
    class MyObject1
    {
        public string Field1 { get; set; }
        public double Field2 { get; set; }
        public MyObject2 Field3 { get; set; }
    }

    class MyObject2
    {
        public string Field1 { get; set; }
        public bool Field2 { get; set; }
    }

    public class ConfigReaderTest
    {
        [Fact]
        public void TestSimpleMap()
        {
            var configReader = new ConfigReader();
            configReader.LoadString(@"
a: hello
b: 123
c: true
d: 123.456
e: 'hello world'
f: '123'
g: [1, 2, 3]
            ");

            Assert.Equal("hello", configReader.GetCheckedValue<string>("a"));
            Assert.Equal(123, configReader.GetCheckedValue<int>("b"));
            Assert.True(configReader.GetCheckedValue<bool>("c"));
            Assert.Equal(123.456, configReader.GetCheckedValue<double>("d"));
            Assert.Equal("hello world", configReader.GetCheckedValue<string>("e"));
            Assert.Equal("123", configReader.GetCheckedValue<string>("f"));
            Assert.Equal(new[] {1, 2, 3}, configReader.GetCheckedValue<int[]>("g"));

            Assert.Equal("hi", configReader.GetCheckedValueOrDefault<string>(new[] {"no exist"}, "hi"));

            Assert.Throws<ConfigKeyNotFoundException>(() => configReader.GetCheckedValue<string>("no exist"));
            Assert.Throws<ConfigException>(() => configReader.GetCheckedValue<int>("a"));
        }

        [Fact]
        public void TestNestedMap()
        {
            var configReader = new ConfigReader();
            configReader.LoadString(@"
key0.0: value0.0
key0.1: value0.1
key0.2:
    key1.0: value1.0
    key1.1: value1.1
    key1.2:
        key2.0: value2.0
        key2.1: value2.1
        key2.2: value2.2
            ");

            Assert.Equal("value2.2", configReader.GetCheckedValue<string>("key0.2", "key1.2", "key2.2"));
        }

        [Fact]
        public void TestObjectGraph()
        {
            var configReader = new ConfigReader();
            configReader.LoadString(@"
root:
    field1: hi
    field2: 123.456
    field3:
        field1: hello
        field2: yes
            ");

            var result = configReader.GetCheckedValue<MyObject1>(new[] {"root"});

            Assert.IsType<MyObject1>(result);
            Assert.Equal("hi", result.Field1);
            Assert.Equal(123.456, result.Field2);
            Assert.Equal("hello", result.Field3.Field1);
            Assert.True(result.Field3.Field2);
        }
    }
}
