using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using YeongHun.Common.Config;
using System.Text.RegularExpressions;

namespace Config.Test
{
    [TestClass]
    public class ConfigTest
    {
        private struct Point
        {
            public int X { get; }
            public int Y { get; }
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override bool Equals(object obj)
            {
                if (obj is Point p)
                    return X == p.X && Y == p.Y;
                return false;
            }

            public override int GetHashCode()
            {
                return X.GetHashCode() ^ Y.GetHashCode();
            }
        }
        private struct Resolution
        {
            public int Width { get; }
            public int Height { get; }
            public Resolution(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public override bool Equals(object obj)
            {
                if (obj is Resolution res)
                    return Width == res.Width && Height == res.Height;
                return false;
            }

            public override int GetHashCode()
            {
                return Width.GetHashCode() ^ Height.GetHashCode();
            }
        }
        [TestMethod]
        [Timeout(TestTimeout.Infinite)]
        public void LoadTest()
        {
            string testStr =@"""

NoTag = AAA

[Graphic]
Resolution = 1920 * 1080
Point1 = (100, 20)
Point2 = (20, 100)

[String]

Hello = Hello world!
""";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(testStr));
            var dic = new ConfigDic();
            dic.Load(ms);
            
            dic.AddParser(str =>
            {
                var match = Regex.Match(str, @"(?<Width>\d+) \* (?<Height>\d+)");
                if (!match.Success)
                    throw new InvalidCastException();
                else
                    return new Resolution(int.Parse(match.Groups["Width"].Value), int.Parse(match.Groups["Height"].Value));
            });

            dic.AddParser(str =>
            {
                var match = Regex.Match(str, @"\((?<X>\d+), ?(?<Y>\d+)\)");
                if (!match.Success)
                    throw new InvalidCastException();
                else
                    return new Point(int.Parse(match.Groups["X"].Value), int.Parse(match.Groups["Y"].Value));
            });

            Assert.AreEqual(new Resolution(1920, 1080), dic.GetValue<Resolution>("Graphic", "Resolution"));
            Assert.AreEqual(new Point(100, 20), dic.GetValue<Point>("Graphic", "Point1"));
            Assert.AreEqual(new Point(20, 100), dic.GetValue<Point>("Graphic", "Point2"));
            Assert.AreEqual("Hello world!", dic.GetValue<string>("String", "Hello"));

            Assert.AreEqual("AAA", dic.GetValue<string>("NoTag"));
        }
    }
}
