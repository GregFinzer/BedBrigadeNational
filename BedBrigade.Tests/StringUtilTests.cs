﻿using BedBrigade.Common;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class StringUtilTests
    {
        [Test]
        public void RestoreHrefWithJavaScript()
        {
            string originalPath = Path.Combine(Environment.CurrentDirectory, "TestFiles", "OriginalHeader.html");
            string originalHeader = File.ReadAllText(originalPath);

            string updatedPath = Path.Combine(Environment.CurrentDirectory, "TestFiles", "UpdatedHeader.html");
            string updatedHeader = File.ReadAllText(updatedPath);

            updatedHeader = StringUtil.RestoreHrefWithJavaScript(originalHeader, updatedHeader); 
            Assert.AreEqual(originalHeader, updatedHeader);
        }
    }
}