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
using System.Threading.Tasks;
using Crawler.Core.Metrics;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.Core.UnitTest;
using Crawler.Core.UnitTest.Factories;
using Crawler.Core.UnitTest.Tests;
using Crawler.WebDriver.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Crawler.Stategies.Core.UnitTest
{
    [TestClass]
    public class CrawlerStrategyGenericTest
    {
        private Mock<IWebDriverService> webDriverMock;

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void Crawl_WhenResultEmpty_ThenException()
        {
            //ARRANGE
            var xml = "<html></html>";
            var crawlRequest = DocumentPartTestHelper.CreateRequestDocumentPartText("http:something", "test1");
            var webDriverMock = CreateMockWebDriver(xml);
            var metricRegisterMock = new Mock<IMetricRegister>();

            var testee = new CrawlerStrategyGeneric(webDriverMock.Object, metricRegisterMock.Object);

            var request = new Request(testee, null, crawlRequest);

            //ACT
            var resultTask = testee.Crawl(request);

            var result = resultTask
            .Match(res => res, () => throw new Exception("Empty result"), e => throw e)
            .Result;

            // ASSERT Nothing => Exception
            var documentPartText = GetDocumentPart<DocumentPartText>(result);

            var text = documentPartText.Text.Match(t => t, () => throw new Exception("Empty Result"));
            Assert.AreEqual(string.Empty, text);
        }

        [TestMethod]
        public void Crawl_WhenContent_ThenFound_ThenResultIsNotEmpty()
        {
            // Arrange
            var testcase = TestCaseFactory.CreateTestCaseContentBasedTextComplex();
            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartText = GetDocumentPart<DocumentPartText>(result);

            var text = documentPartText.Text.Match(t => t, () => throw new Exception("Empty Result"));
            Assert.AreEqual(testcase.ExpectedResult, text);
        }

        

        [TestMethod]
        public void Crawl_WhenNodeName_ThenFound_ThenResultIsNotEmpty()
        {
            //ARRANGE
            var testcase = TestCaseFactory.CreateTestCaseTagBasedSimple();
            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartText = GetDocumentPart<DocumentPartText>(result);

            var text = documentPartText.Text.Match(t => t, () => throw new Exception("Empty Result"));
            Assert.AreEqual(testcase.ExpectedResult, text);
        }

        [TestMethod]
        public void Crawl_WhenAttribute_ThenFound_ThenResultIsNotEmpty()
        {
            //ARRANGE
            var testcase = TestCaseFactory.CreateTestCaseAttributeBasedComplex();
            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartText = GetDocumentPart<DocumentPartText>(result);

            var text = documentPartText.Text.Match(t => t, () => throw new Exception("Empty Result"));
            Assert.AreEqual(testcase.ExpectedResult, text);
        }

        [TestMethod]
        public void Crawl_WhenAnchorWithContent_ThenResultIsNotEmpty()
        {
            //ARRANGE
            var testcase = TestCaseFactoryLink.CreateTestCaseAnchorAndContent();
            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartLink = GetDocumentPart<DocumentPartLink>(result);
            DocumentPartLinkTest.AssertResult(testcase, documentPartLink);
        }

        [TestMethod]
        public void Crawl_WhenAnchorWithContent_ThenContentDownloaded()
        {
            //ARRANGE
            var testcase = TestCaseFactoryFile.CreateTestCaseFile();

            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartFile = GetDocumentPart<DocumentPartFile>(result);
            DocumentPartFileTest.AssertResult(testcase, documentPartFile);
            webDriverMock.Verify(d => d.Download(It.IsAny<Option<DownloadRequest>>()), Times.Exactly(testcase.ExpectedResult.Count));
        }

        [TestMethod]
        public void Crawl_WhenTableWithContent_ThenResultIsNotEmpty()
        {
            //ARRANGE
            var testcase = TestCaseFactoryTable.Create();
            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartTable = GetDocumentPart<DocumentPartTable>(result);
            DocumentPartTableTest.AssertResults(testcase, documentPartTable);
        }

        [TestMethod]
        public void Crawl_WhenArticle_ThenArticleObtained_ThenContentDownloaded()
        {
            //ARRANGE
            var testcase = TestCaseFactoryArticle.CreateTestCase();

            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartArticle = GetDocumentPart<DocumentPartArticle>(result);

            var documentPartFiles = documentPartArticle.Content.Match(c => c.GetAllParts<DocumentPartFile>().ToList(), () => throw new Exception("Missing File"));
            Assert.IsTrue(documentPartFiles.Any());

            webDriverMock.Verify(d => d.Download(It.IsAny<Option<DownloadRequest>>()), Times.Exactly(documentPartFiles.Count));

            DocumentPartArticleTest.AssertResult(testcase, documentPartArticle);
        }

        [TestMethod]
        public void Crawl_WhenAutoDetect_ThenArticleObtained_ThenContentDownloaded()
        {
            //ARRANGE
            var testcase = TestCaseFactoryAutoDetect.CreateArticleTestCase();

            var articleTestCase = new TestCase<ExpectedArticle>();
            articleTestCase.ExpectedResult = new ExpectedArticle
            {
                Title = testcase.ExpectedResult.Title,
                Content = testcase.ExpectedResult.Content
            };

            CrawlerStrategyGeneric testee = CreateTestee(testcase);
            var request = new Request(testee, null, testcase.CrawlRequest);

            // ACT
            var result = StartCrawl(testee, request);

            //ASSERT
            var documentPartAutoDetect = GetDocumentPart<DocumentPartAutodetect>(result);
            var documentPartArticles = documentPartAutoDetect.GetAllParts<DocumentPartArticle>().ToList();

            Assert.AreEqual(1, documentPartArticles.Count);

            var documentPartArticle = documentPartArticles.First();

            var documentPartFiles = documentPartAutoDetect.GetAllParts<DocumentPartFile>().ToList();
            Assert.IsTrue(documentPartFiles.Any());

            webDriverMock.Verify(d => d.Download(It.IsAny<Option<DownloadRequest>>()), Times.Exactly(documentPartFiles.Count));

            DocumentPartAutoDetectTest.AssertResults(testcase, articleTestCase, documentPartAutoDetect);
        }

        public static T GetDocumentPart<T>(Crawler.Core.Results.CrawlResponse result) where T:DocumentPart
        {
            Assert.IsNotNull(result);
            DocumentPart documentPart = GetDocumentPart(result);

            var documentPartLink = (documentPart as T);
            Assert.IsNotNull(documentPartLink);

            return documentPartLink;
        }

        public static DocumentPart GetDocumentPart(Crawler.Core.Results.CrawlResponse result)
        {
            var document = result.Result.Match(res => res, () => throw new Exception("Document Empty"));

            var documentPart = document.RequestDocumentPart.MatchUnsafe(docPart => docPart, () => null);
            Assert.IsNotNull(documentPart);
            return documentPart;
        }

        public static Mock<IWebDriverService> CreateMockWebDriver(string xml)
        {
            var webDriverMock = new Mock<IWebDriverService>();
            webDriverMock.Setup(m => m.LoadPage(It.IsAny<Option<LoadPageRequest>>())).Returns(async () => await Task.FromResult(xml));

            return webDriverMock;
        }

        public static Crawler.Core.Results.CrawlResponse StartCrawl(CrawlerStrategyGeneric testee, Request request)
        {
            var resultOptionAsync = testee.Crawl(request);
            var task = resultOptionAsync.Match(res => res, () => throw new Exception("Empty result"), e => throw e);
            var result = task.Result;
            return result;
        }

        public CrawlerStrategyGeneric CreateTestee<T>(TestCase<T> testcase)
        {
            webDriverMock = CreateMockWebDriver(testcase.Xml);
            var testee = new CrawlerStrategyGeneric(webDriverMock.Object, Mock.Of<IMetricRegister>());
            return testee;
        }

    }
}