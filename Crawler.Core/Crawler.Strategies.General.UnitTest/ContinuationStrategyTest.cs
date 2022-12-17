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
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core.Management;
using Crawler.Core.Metrics;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.RequestHandling.Core;
using Crawler.Stategies.Core;
using Crawler.Stategies.Core.UnitTest;
using Crawler.WebDriver.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;


namespace Crawler.Strategies.General.UnitTest
{
    [TestClass]
    public class ContinuationStrategyTest
    {

        string _testcase;

        Mock<ICrawlerConfigurationService> _crawlConfiguration;

        ICrawlContinuationStrategy _continuationStrategyTestee;

        Mock<IWebDriverService> _webDriverMock;

        CrawlRequest _crawlRequest;

        [TestInitialize]
        public void Setup()
        {
            _testcase = GetTestCase();

            _crawlConfiguration = new Mock<ICrawlerConfigurationService>();
            
            _webDriverMock = CrawlerStrategyGenericTest.CreateMockWebDriver(_testcase);

            _crawlRequest = new CrawlRequest()
            {
                LoadPageRequest = new LoadPageRequest
                {
                    Uri = "https://continueTestCase",

                },
                RequestDocument = new Document
                {
                    RequestDocumentPart = new DocumentPartAutodetect()
                    {
                        BaseUri = "https://continueTestCase"
                    },
                    DownloadContent = false,
                }
            };
        }

        [TestMethod]
        public void Crawl_DomainLinks_ContinuationRequestPublished()
        {
            // Arrange
            _continuationStrategyTestee = new CrawlDomainOnlyContinuationStrategy(_crawlConfiguration.Object);
            var testee = new CrawlerStrategyGeneric(_webDriverMock.Object, Mock.Of<IMetricRegister>());
            var request = new Request(testee, Option<ICrawlContinuationStrategy>.Some(_continuationStrategyTestee), _crawlRequest);
            var noLinksStored = 0;
            _crawlConfiguration
            .Setup(m => m.StoreDetectedUrls(It.IsAny<Option<List<DocumentPartLink>>>(), It.IsAny<Option<Guid>>()))
            .Callback<Option<List<DocumentPartLink>>, Option<Guid>>((l, guid) => noLinksStored = l.Match(li => li.Count, () => 0))
            .Returns(Option<Unit>.Some(Unit.Default).ToTryOptionAsync());

            // ACT
            var result = CrawlerStrategyGenericTest.StartCrawl(testee, request);

            //ASSERT
            var documentPartAutodetect = CrawlerStrategyGenericTest.GetDocumentPart<DocumentPartAutodetect>(result);
            Assert.AreEqual(4, noLinksStored);
        }

        [TestMethod]
        public void Crawl_AllLinks_ContinuationRequestPublished()
        {
            // Arrange
            _continuationStrategyTestee = new CrawlAllContinuationStrategy(_crawlConfiguration.Object);
            var testee = new CrawlerStrategyGeneric(_webDriverMock.Object, Mock.Of<IMetricRegister>());
            var request = new Request(testee, Option<ICrawlContinuationStrategy>.Some(_continuationStrategyTestee), _crawlRequest);
            var noLinksStored = 0;
            _crawlConfiguration
            .Setup(m => m.StoreDetectedUrls(It.IsAny<Option<List<DocumentPartLink>>>(), It.IsAny<Option<Guid>>()))
            .Callback<Option<List<DocumentPartLink>>, Option<Guid>>((l, guid) => noLinksStored = l.Match(li => li.Count, () => 0))
            .Returns(Option<Unit>.Some(Unit.Default).ToTryOptionAsync());

            // ACT
            var result = CrawlerStrategyGenericTest.StartCrawl(testee, request);

            //ASSERT
            var documentPartAutodetect = CrawlerStrategyGenericTest.GetDocumentPart<DocumentPartAutodetect>(result);
            Assert.AreEqual(6, noLinksStored);
        }

        private static string GetTestCase()
        {
            var xml = @"<html><head></head>
                        <body>
                            <div>
                                someOthertest1 I am some additional stuff
                            </div>
                            <div>
                                <div> 
                                    <a href=""https://continueTestCase/UriInDomain0"" />
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
    }
}
