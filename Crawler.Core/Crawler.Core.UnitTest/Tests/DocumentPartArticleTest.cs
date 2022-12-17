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
using System.Collections.Generic;
using System.Linq;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.UnitTest.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Crawler.Core.UnitTest.Tests
{
    [TestClass]
    public class DocumentPartArticleTest
    {
        [TestMethod]
        public void ParseArticle()
        {
            var testcase = TestCaseFactoryArticle.CreateTestCase();
            AssertResult(testcase);
        }

        public static void AssertResult(TestCase<ExpectedArticle> testcase)
        {
            var result = DocumentPartTestHelper.GetResult<ExpectedArticle, DocumentPartArticle>(testcase);
            Assert.IsNotNull(result);
            AssertResult(testcase, result);
        }

        public static void AssertResult(TestCase<ExpectedArticle> testcase, DocumentPartArticle result)
        {
            var titleParts = result.Title.Select(t => t.GetAllParts<DocumentPartText>()).Match(s => s, () => throw new System.Exception("Title empty")).ToList();
            var contentParts = result.Content.Select(c => c.GetAllParts<DocumentPartText>()).Match(s => s, () => throw new System.Exception("Content empty")).ToList();
            var links = result.Content.Select(c => c.GetAllParts<DocumentPartLink>()).Match(s => s, () => throw new System.Exception("Links empty"));
            var images = result.Content.Select(c => c.GetAllParts<DocumentPartFile>()).Match(s => s, () => throw new System.Exception("Images empty")).ToList();
            
            var tables = result.Content.Select(c => c.GetAllParts<DocumentPartTable>()).Match(s => s, () => throw new System.Exception("Table empty")).ToList();

            Assert.AreEqual(1, titleParts.Count);
            Assert.AreEqual(1, contentParts.Count);
            Assert.AreEqual(1, tables.Count);

            DocumentPartTableTest.AssertResults(TestCaseFactoryTable.Create(), tables.First());

            Assert.AreEqual(testcase.ExpectedResult.Title, titleParts.First().Text.Match(t => t, () => throw new System.Exception("Title missing")));
            Assert.AreEqual(testcase.ExpectedResult.Content, contentParts.First().Text.Match(t => t, () => throw new System.Exception("Content missing")));

            AssertIteratively(testcase.ExpectedResult.Links, links.Select(l => l.Uri.Match(u => u, () => throw new System.Exception("missing uri"))).ToList());

            var downloadLinks = images.Bind(i => i.DownloadLinks).Bind(l => l.Select(link => link.Uri.Match(s => s, () => throw new System.Exception("uri empty")))).ToList();

            AssertIteratively(testcase.ExpectedResult.Images, downloadLinks);
        }

        public static void AssertIteratively<T>(List<T> expectedList, List<T> actualList)
        {
            Assert.AreEqual(expectedList.Count, actualList.Count);

            for (int i = 0; i < expectedList.Count; i++)
            {
                Assert.AreEqual(expectedList[i], actualList[i]);
            }
        }
    }
}