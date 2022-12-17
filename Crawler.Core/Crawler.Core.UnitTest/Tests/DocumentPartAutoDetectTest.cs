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
using static Crawler.Core.Parser.DocumentPartComparer;

namespace Crawler.Core.UnitTest.Tests
{
    [TestClass]
    public class DocumentPartAutoDetectTest
    {
        [TestMethod]
        public void ParseAutodetectArticle()
        {
            var testcase = TestCaseFactoryAutoDetect.CreateArticleTestCase();
            DocumentPartAutoDetectTest.AssertResult(testcase);
        }

        [TestMethod]
        public void ParseBrokenHtml()
        {
            var testcase = TestCaseFactoryAutoDetect.CreateBrokenHtmlTestCase();

            var result = DocumentPartTestHelper.GetResult<ExpectedArticle, DocumentPartAutodetect>(testcase);


            var articles = result.GetAllParts<DocumentPartArticle>().ToList();
            Assert.AreEqual(1, articles.Count);

            var files = result.GetAllParts<DocumentPartFile>().ToList();
            //Assert.AreEqual(120, files.Count);

            var multipleLinks = files.Where(f => f.DownloadLinks.Match(d => d, () => new List<DocumentPartLink>()).Count > 1).ToList();

            var links = files.SelectMany(f =>
            {
                return f.DownloadLinks.Match(d => d, () => new List<DocumentPartLink>());
            }).ToList();

            //var distinctLinks = links.Distinct(new DocumentPartLinkComparer()).ToList();

            //Assert.AreEqual(distinctLinks.Count, links.Count);

            Assert.AreEqual(137, links.Count);

        }

        public static void AssertResult(TestCase<ExpectedArticle> testcase)
        {
            var articleTestCase = new TestCase<ExpectedArticle>();
            articleTestCase.ExpectedResult = new ExpectedArticle
            {
                Title = testcase.ExpectedResult.Title,
                Content = testcase.ExpectedResult.Content
            };

            var result = DocumentPartTestHelper.GetResult<ExpectedArticle, DocumentPartAutodetect>(testcase);
            AssertResults(testcase, articleTestCase, result);

        }

        public static void AssertResults(TestCase<ExpectedArticle> testcase, TestCase<ExpectedArticle> articleTestCase, DocumentPartAutodetect result)
        {
            Assert.IsNotNull(result);

            var articles = result.GetAllParts<DocumentPartArticle>().ToList();
            Assert.AreEqual(1, articles.Count);
            AssertResult(articleTestCase, articles.First());

            var tables = result.GetAllParts<DocumentPartTable>().ToList();
            Assert.AreEqual(1, tables.Count);

            var expectedTable = testcase.ExpectedResult.Table;
            DocumentPartTableTest.AssertTable(expectedTable, tables.First());

            var files = result.GetAllParts<DocumentPartFile>().ToList();
            var links = files.SelectMany(f =>
            {
                return f.DownloadLinks.Match(d => d, () => new List<DocumentPartLink>());
            }).ToList();

            var expectedLinks = testcase.ExpectedResult.Links;
            DocumentPartArticleTest.AssertIteratively(expectedLinks, links.Select(l => l.Uri.Match(u => u, () => throw new System.Exception("missing uri"))).ToList());


            var expectedImages = testcase.ExpectedResult.Images;
        }

        public static void AssertResult(TestCase<ExpectedArticle> testcase, DocumentPartArticle result)
        {
            var titleParts = result.Title.Select(t => t.GetAllParts<DocumentPartText>()).Match(s => s, () => throw new System.Exception("Title empty")).ToList();
            var contentParts = result.Content.Select(c => c.GetAllParts<DocumentPartText>()).Match(s => s, () => throw new System.Exception("Content empty")).ToList();
            Assert.AreEqual(1, titleParts.Count);
            Assert.AreEqual(1, contentParts.Count);


            Assert.AreEqual(testcase.ExpectedResult.Title, titleParts.First().Text.Match(t => t, () => throw new System.Exception("Title missing")));
            Assert.AreEqual(testcase.ExpectedResult.Content, contentParts.First().Text.Match(t => t, () => throw new System.Exception("Content missing")));
        }
    }
}