using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace locr.test
{
    [TestClass]
    public class locrTests
    {

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        [TestMethod]
        public void Can_Analyse_Simple_Text()
        {
            var locr = new lib.locr();
            var str = @"

";
            var result = locr.AnalyseString(str);

            Assert.AreEqual(3, result.Lines);
        }
    }
}
