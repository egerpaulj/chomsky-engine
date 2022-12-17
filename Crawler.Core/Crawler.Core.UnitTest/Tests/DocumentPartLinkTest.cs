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

namespace Crawler.Core.UnitTest
{
    [TestClass]
    public class DocumentPartLinkTest
    {
        [TestMethod]
        public void ParseAnchorOnlyTest()
        {
            var testcase = TestCaseFactoryLink.CreateTestCaseAnchorOnly();
            
            AssertResult(testcase);
        }

        [TestMethod]
        public void ParseAnchorFullUriTest()
        {
            var testcase = TestCaseFactoryLink.CreateTestCaseAnchorOnlyFullUri();
            
            AssertResult(testcase);
        }

        [TestMethod]
        public void ParseAnchorAndContentTest()
        {
            var testcase = TestCaseFactoryLink.CreateTestCaseAnchorAndContent();
            
            AssertResult(testcase);
        }

        private static void AssertResult(TestCase<List<string>> testcase)
        {
            DocumentPartLink result = DocumentPartTestHelper.GetResult<List<string>, DocumentPartLink>(testcase);
            Assert.IsNotNull(result);
            AssertResult(testcase, result);
        }
 
        public static void AssertResult(TestCase<List<string>> testcase, DocumentPartLink result)
        {
            var uriResult = result.Uri.Match(t => t, () => throw new Exception("Empty result"));
            List<string> links = GetLinks(result, uriResult);

            Assert.AreEqual(testcase.ExpectedResult.Count, links.Count);

            for (int i = 0; i < links.Count; i++)
            {
                Assert.AreEqual(testcase.ExpectedResult[i], links[i]);
            }
        }

        public static List<string> GetLinks(DocumentPartLink result, string uriResult)
        {
            if(result.SubParts.IsNone)
                return new List<string>{uriResult};
                
            var links = result.SubParts
            .Match(s => s, () => throw new Exception()).OfType<DocumentPartLink>()
            .Select(p => p.Uri.Match(s => s, () => string.Empty))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

            links.Insert(0, uriResult);
            return links;
        }
    }
}