//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2022  Paul Eger                                                                                                                                                                     

//      This program is free software: you can redistribute it and/or modify                                                                                                                                          
//      it under the terms of the GNU General Public License as published by                                                                                                                                          
//      the Free Software Foundation, either version 3 of the License, or                                                                                                                                             
//      (at your option) any later version.                                                                                                                                                                           

//      This program is distributed in the hope that it will be useful,                                                                                                                                               
//      but WITHOUT ANY WARRANTY; without even the implied warranty of                                                                                                                                                
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                                                                                                                                                 
//      GNU General Public License for more details.                                                                                                                                                                  

//      You should have received a copy of the GNU General Public License                                                                                                                                             
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Linq;
using System.Collections.Generic;
using Crawler.Core.Parser.DocumentParts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Crawler.Core.UnitTest.Factories;

namespace Crawler.Core.UnitTest
{
    [TestClass]
    public class DocumentPartFileTest
    {
        [TestMethod]
        public void ParseFile()
        {
            var testcase = TestCaseFactoryFile.CreateTestCaseFile();
            AssertResult(testcase);
        }

        [TestMethod]
        public void ParseFileWithContentMatch()
        {
            var testcase = TestCaseFactoryFile.CreateTestCaseFileContentSpecific();
            AssertResult(testcase);
        }

        private static void AssertResult(TestCase<List<string>> testcase)
        {
            var result = DocumentPartTestHelper.GetResult<List<string>, DocumentPartFile>(testcase);
            Assert.IsNotNull(result);
            AssertResult(testcase, result);
        }

        public static void AssertResult(TestCase<List<string>> testcase, DocumentPartFile result)
        {
            List<string> linkStrings = GetLinkStrings(result);

            Assert.AreEqual(testcase.ExpectedResult.Count, linkStrings.Count);

            for (int i = 0; i < testcase.ExpectedResult.Count; i++)
            {
                Assert.AreEqual(testcase.ExpectedResult[i], linkStrings[i]);
            }
        }

        public static List<string> GetLinkStrings(DocumentPartFile result)
        {
            return result.DownloadLinks
            .Match(l => l, () => throw new Exception("No Links"))
            .SelectMany(l => DocumentPartLinkTest.GetLinks(l, l.Uri.Match(s => s, () => string.Empty)))
            .ToList();
        }
    }
}