using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tetris
{
    [TestFixture]
    class TetrisTests
    {
        private StringBuilder sb;
        private StringWriter writer;

        [SetUp]
        public void SetUp()
        {
            sb = new StringBuilder();
            writer = new StringWriter(sb);
            Console.SetOut(writer);
        }

        [TestCase("Tests\\smallest")]
        [TestCase("Tests\\random-w9-h10-c1000")]
        [TestCase("Tests\\random-w100000-h5-c100000")]
        //[TestCase("Tests\\random-w1000-h1000-c1000000")]
        [TestCase("Tests\\random-w100-h99-c10000")]
        [TestCase("Tests\\cubes-w8-h8-c100")]
        //[TestCase("Tests\\cubes-w1000-h1000-c1000000")]
        [TestCase("Tests\\clever-w9-h10-c200")]
        [TestCase("Tests\\clever-w500-h7-c5000")]
        [TestCase("Tests\\clever-w20-h25-c100000")]
        [TestCase("Tests\\clever-w100-h99-c10000")]
        public void TestFile(string filename)
        {
            Vasilyeva_Veronika.Main(new [] {filename + ".json"});
            var resultLines = sb.ToString().Split(new []{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var expected = File.ReadAllLines(filename + ".txt");
            Assert.AreEqual(expected, resultLines);
        }
    }
}