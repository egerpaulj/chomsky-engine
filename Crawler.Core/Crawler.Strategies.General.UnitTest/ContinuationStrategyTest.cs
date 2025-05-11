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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core.Management;
using Crawler.Core.Metrics;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.DataModel.Scheduler;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.RequestHandling.Core;
using Crawler.Stategies.Core;
using Crawler.Stategies.Core.UnitTest;
using Crawler.WebDriver.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Crawler.Strategies.General.UnitTest
{
    [TestClass]
    public class ContinuationStrategyTest
    {
        string _testcase;

        Mock<ICrawlerConfigurationService> _crawlConfiguration;

        ICrawlContinuationStrategy _continuationStrategyTestee;

        Mock<IWebDriverService> _webDriverMock;
        Mock<IRequestPublisher> _requestPublisherMock;

        CrawlRequest _crawlRequest;

        [TestInitialize]
        public void Setup()
        {
            _testcase = GetTestCase();

            _crawlConfiguration = new Mock<ICrawlerConfigurationService>();

            _webDriverMock = CrawlerStrategyGenericTest.CreateMockWebDriver(_testcase);
            _requestPublisherMock = new Mock<IRequestPublisher>();
            _requestPublisherMock
                .Setup(mock => mock.PublishRequest(It.IsAny<Option<CrawlRequest>>()))
                .Returns(async () => await Task.FromResult(Unit.Default));
            _requestPublisherMock
                .Setup(mock =>
                    mock.PublishUri(
                        It.IsAny<Option<string>>(),
                        It.IsAny<Option<List<DocumentPartLink>>>(),
                        It.IsAny<UriType>()
                    )
                )
                .Returns(async () => await Task.FromResult(Unit.Default));

            _crawlRequest = new CrawlRequest()
            {
                LoadPageRequest = new LoadPageRequest { Uri = "https://www.continueTestCase" },
                RequestDocument = new Document
                {
                    RequestDocumentPart = new DocumentPartAutodetect(
                        "https://www.continueTestCase"
                    ),
                    DownloadContent = false,
                },
            };
        }

        [TestMethod]
        [Ignore]
        public async Task Crawl_EvaluateStrategyTest()
        {
            var testcase = await GetEvaluateTestCaseAsync();
            _webDriverMock = CrawlerStrategyGenericTest.CreateMockWebDriver(testcase);
            var uri = "https://www.continueTestCase";
            // <a _ngcontent-ng-lseg-c96=\"\" class=\"page-number dots\" title=\"Go to page 6\" href=\"/live-markets/market-data-dashboard/price-explorer?page=6\">
            var crawlRequest = new CrawlRequest()
            {
                LoadPageRequest = new LoadPageRequest { Uri = uri },
                RequestDocument = new Document
                {
                    RequestDocumentPart = new DocumentPartArticle(uri)
                    {
                        Timestamp = new DocumentPartMeta(uri)
                        {
                            Selector = new DocumentPartSelector
                            {
                                Xpath = "//meta[@property='article:published_time']",
                            },
                        },
                        SubParts = new List<DocumentPart>
                        {
                            new DocumentPartLink(uri)
                            {
                                Selector = new DocumentPartSelector
                                {
                                    Xpath = "//a[contains(@class, 'page-number')]",
                                },
                            },
                            new DocumentPartLink(uri)
                            {
                                Selector = new DocumentPartSelector
                                {
                                    Xpath = "//a[contains(@href, 'stock/')]",
                                },
                            },
                            new DocumentPartTable(uri),
                        },
                    },
                    DownloadContent = false,
                },
            };

            List<DocumentPartLink> publishedLinks = [];

            _requestPublisherMock
                .Setup(m =>
                    m.PublishUri(
                        It.IsAny<Option<string>>(),
                        It.IsAny<Option<List<DocumentPartLink>>>(),
                        It.IsAny<UriType>()
                    )
                )
                .Callback<Option<string>, Option<List<DocumentPartLink>>, UriType>(
                    (_, l, _) => publishedLinks = l.Match(r => r, () => [])
                )
                .Returns(Option<Unit>.Some(Unit.Default).ToTryOptionAsync());

            // Arrange
            _continuationStrategyTestee = new CrawlDomainOnlyContinuationStrategy(
                Mock.Of<ILogger<ICrawlContinuationStrategy>>(),
                _requestPublisherMock.Object
            );
            var testee = new CrawlerStrategyGeneric(
                _webDriverMock.Object,
                Mock.Of<IMetricRegister>()
            );
            var request = new Request(
                testee,
                Option<ICrawlContinuationStrategy>.Some(_continuationStrategyTestee),
                crawlRequest
            );
            var noLinksStored = 0;

            // ACT
            var result = CrawlerStrategyGenericTest.StartCrawl(testee, request);

            //ASSERT
            var documentPart = CrawlerStrategyGenericTest.GetDocumentPart<DocumentPartArticle>(
                result
            );
            var tables = documentPart.GetAllParts<DocumentPartTable>();
            var links = documentPart.GetAllParts<DocumentPartLink>();
            Console.WriteLine(documentPart.GetBriefSummary());
            Assert.AreEqual(3, noLinksStored);
        }

        [TestMethod]
        public void Crawl_DomainLinks_ContinuationRequestPublished()
        {
            // Arrange
            _continuationStrategyTestee = new CrawlDomainOnlyContinuationStrategy(
                Mock.Of<ILogger<ICrawlContinuationStrategy>>(),
                _requestPublisherMock.Object
            );
            var testee = new CrawlerStrategyGeneric(
                _webDriverMock.Object,
                Mock.Of<IMetricRegister>()
            );
            var request = new Request(
                testee,
                Option<ICrawlContinuationStrategy>.Some(_continuationStrategyTestee),
                _crawlRequest
            );
            var noLinksStored = 0;

            _requestPublisherMock
                .Setup(m =>
                    m.PublishUri(
                        It.IsAny<Option<string>>(),
                        It.IsAny<Option<List<DocumentPartLink>>>(),
                        It.IsAny<UriType>()
                    )
                )
                .Callback<Option<string>, Option<List<DocumentPartLink>>, UriType>(
                    (_, l, _) => noLinksStored = l.Match(r => r.Count, () => 0)
                )
                .Returns(Option<Unit>.Some(Unit.Default).ToTryOptionAsync());

            // ACT
            var result = CrawlerStrategyGenericTest.StartCrawl(testee, request);

            //ASSERT
            var documentPartAutodetect =
                CrawlerStrategyGenericTest.GetDocumentPart<DocumentPartAutodetect>(result);
            Assert.AreEqual(3, noLinksStored);
        }

        [TestMethod]
        public void Crawl_AllLinks_ContinuationRequestPublished()
        {
            // Arrange
            _continuationStrategyTestee = new CrawlAllContinuationStrategy(
                Mock.Of<ILogger<ICrawlContinuationStrategy>>(),
                _requestPublisherMock.Object
            );
            var testee = new CrawlerStrategyGeneric(
                _webDriverMock.Object,
                Mock.Of<IMetricRegister>()
            );
            var request = new Request(
                testee,
                Option<ICrawlContinuationStrategy>.Some(_continuationStrategyTestee),
                _crawlRequest
            );
            var noLinksStored = 0;
            _requestPublisherMock
                .Setup(m =>
                    m.PublishUri(
                        It.IsAny<Option<string>>(),
                        It.IsAny<Option<List<DocumentPartLink>>>(),
                        It.IsAny<UriType>()
                    )
                )
                .Callback<Option<string>, Option<List<DocumentPartLink>>, UriType>(
                    (_, l, _) => noLinksStored = l.Match(li => li.Count, () => 0)
                )
                .Returns(Option<Unit>.Some(Unit.Default).ToTryOptionAsync());

            // ACT
            var result = CrawlerStrategyGenericTest.StartCrawl(testee, request);

            //ASSERT
            var documentPartAutodetect =
                CrawlerStrategyGenericTest.GetDocumentPart<DocumentPartAutodetect>(result);
            _requestPublisherMock
                .Setup(m =>
                    m.PublishUri(
                        It.IsAny<Option<string>>(),
                        It.IsAny<Option<List<DocumentPartLink>>>(),
                        It.IsAny<UriType>()
                    )
                )
                .Callback<Option<string>, Option<List<DocumentPartLink>>, UriType>(
                    (_, l, _) => noLinksStored = l.Match(li => li.Count, () => 0)
                )
                .Returns(Option<Unit>.Some(Unit.Default).ToTryOptionAsync());

            Assert.AreEqual(6, noLinksStored);
        }

        private static string GetTestCase()
        {
            var xml =
                @"<html><head></head>
                        <body>
                            <div>
                                someOthertest1 I am some additional stuff
                            </div>
                            <div>
                                <div> 
                                    <a href=""https://subdomain.continueTestCase/UriInSubDomainSkip"" />
                                    <a href=""/UriInDomain1"" />
                                    <a href=""https://someotherdomain/UriNotInDomain0"" />
                                    <div>
                                        test1 <p>I am some additional stuff</p>
                                        <div>
                                            <a href=""http://continueTestCase/UriInDomain2"" />
                                            <a href=""UriInDomain3"" />
                                            <a href=""https://someotherdomain/UriNotInDomain1"" />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>";

            return xml;
        }

        private static async Task<string> GetEvaluateTestCaseAsync()
        {
            return await File.ReadAllTextAsync("evaluate.html");
        }
    }
}
