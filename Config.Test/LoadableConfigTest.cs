using Microsoft.VisualStudio.TestTools.UnitTesting;
using Riey.Common.Config;
using System.IO;
using System.Text;

namespace Config.Test
{
    [TestClass]
    public class LoadableConfigTest
    {
        [TestMethod]
        [Timeout(TestTimeout.Infinite)]
        public void LoadConfigTest()
        {
            var testStr = @"""
Width = 140

X = 50

Str = Hi, world!

RootPath = D:\\

""";

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(testStr));
            var testConfig = new TestConfig(ms);

            Assert.AreEqual(140, testConfig.Width);//Check double load correctly
            Assert.AreEqual(100, testConfig.Height);//Check double set default value correctly
            Assert.AreEqual("100", testConfig.ConfigDic["Height"]);//Check double set default string correctly

            Assert.AreEqual(50, testConfig.X);//Check long load correctly
            Assert.AreEqual(100, testConfig.Y);//Check long set default value correctly
            Assert.AreEqual("100", testConfig.ConfigDic["Y"]);//Check long set default string correctly

            Assert.AreEqual("Hi, world!", testConfig.Str);//Check string set default value correctly
            Assert.AreEqual("Hi, world!", testConfig.ConfigDic["Str"]);//Check string set default string correctly

            Assert.AreEqual("D:\\", testConfig.Root.FullName);//Check custom parser load correctly
            Assert.AreEqual(new DirectoryInfo("DLL\\").FullName, testConfig.DllPath.FullName);//Check custom parser set default value correctly
            Assert.AreEqual("DLL\\", testConfig.ConfigDic["DllPath"]);//Check custom writer set default string correctly
        }
    }

    public class TestConfig : LoadableConfig
    {
        [LoadableProperty("100")]
        public double Width { get; private set; }
        [LoadableProperty("100")]
        public double Height { get; private set; }

        [LoadableProperty("100")]
        public long X { get; private set; }
        [LoadableProperty("100")]
        public long Y { get; private set; }

        [LoadableField("Hello, world!", Key = "Str")]
        private string _str;

        public string Str => _str;

        [LoadableProperty("C:\\", Key = "RootPath")]
        public DirectoryInfo Root { get; private set; }

        [LoadableProperty("DLL\\")]
        public DirectoryInfo DllPath { get; private set; }

        protected override void AddParsers(ConfigDic configDic)
        {
            configDic.AddParser(s => new DirectoryInfo(s));
        }

        protected override void AddWriters(ConfigDic configDic)
        {
            configDic.AddWriter<DirectoryInfo>(d => d.FullName);
        }

        public ConfigDic ConfigDic { get; }

        public TestConfig(Stream stream)
        {
            var config = new ConfigDic(true, true);
            config.Load(stream);

            this.Load(config);

            ConfigDic = config;
        }
    }
}
