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
using Crawler.Core.Cache;
using Crawler.Core.Management;
using Crawler.Core.Metrics;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.Management.Core.RequestHandling.Core.FileBased;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Crawler.Core.UnitTest.Factories;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Threading.Tasks;
using LanguageExt;
using Crawler.Stategies.Core.UnitTest;
//using Crawler.WebDriver.Grpc.Client;
using Microservice.Grpc.Core;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;
using Crawler.Core.Parser.Xml;
using System.Threading;
using Caching.Redis;
using Crawler.Core.Requests;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using Crawler.Strategies.General;
using Crawler.Stategies.Core;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;

namespace Crawler.Management.Core.UnitTest
{
    [TestClass]
    public class CrawlerManagerTest
    {
        private CrawlerManager _testee;
        private IConfigurationRoot _appConfig;
        private ILoggerFactory _loggerFactory;

        private Mock<IMetricRegister> _metricRegisterMock;
        private Mock<IGrpcMetrics> _grpcMetricsMock;
        private IRequestRepository _requestRepository;

        private Mock<ICache> _cacheMock;
        public CrawlerManagerTest()
        {
            if (Directory.Exists("Requests"))
                Directory.Delete("Requests", true);

            var testRepository = new FileBasedRequestRepository(new DirectoryInfo("Requests"), new JsonConverterProvider());
            _requestRepository = new RequestRepository(testRepository, testRepository, testRepository);

            var xml = TestCaseFactoryAutoDetect.CreateArticleTestCase().Xml;
            var webDriverMock = CrawlerStrategyGenericTest.CreateMockWebDriver(xml);


            _metricRegisterMock = new Mock<IMetricRegister>();
            _grpcMetricsMock = new Mock<IGrpcMetrics>();

            _appConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .Build();

            _loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddSimpleConsole();
            });
            var crawlConfiguration = new CrawlerConfigurationGeneric(webDriverMock.Object, _metricRegisterMock.Object
            , Mock.Of<ILogger<CrawlerConfigurationGeneric>>());

            _cacheMock = new Mock<ICache>();
            _cacheMock.Setup(c => c.UpdateCrawlCompleted(It.IsAny<Option<Guid>>())).Returns(async () => await Task.FromResult(Unit.Default));

            _cacheMock.Setup(m => m.StoreCrawlEnded(It.IsAny<Option<Crawl>>())).Returns(async () =>
            {
                return await Task.FromResult(Unit.Default);
            });

            var strategyMapperMock = new Mock<ICrawlStrategyMapper>();
            strategyMapperMock.Setup(m => m.GetCrawlStrategy(It.IsAny<Option<CrawlRequest>>())).Returns(async () => await Task.FromResult(Option<ICrawlStrategy>.Some(new CrawlerStrategyGeneric(webDriverMock.Object, Mock.Of<IMetricRegister>()))));
            strategyMapperMock.Setup(m => m.GetContinuationStrategy(It.IsAny<Option<CrawlRequest>>())).Returns(async () => await Task.FromResult(Option<ICrawlContinuationStrategy>.None));

            _testee = new CrawlerManager(_loggerFactory.CreateLogger<CrawlerManager>(), crawlConfiguration, _cacheMock.Object, _metricRegisterMock.Object, _requestRepository, strategyMapperMock.Object);
        }

        //[TestMethod]
        public void CreateRequestFile()
        {
            var testCase = TestCaseFactoryAutoDetect.CreateArticleTestCase();

            var json = JsonConvert.SerializeObject(testCase.CrawlRequest);

            var testFilePath = Path.Combine("/home/user/vscode/csharp/crawler/TestData/Requests", "crawlRequest.json");
            Console.WriteLine("Creating file at: " + testFilePath);

            File.WriteAllText(testFilePath, json);
        }

        

        //[TestMethod]
        public void DeserializeTest()
        {
            var res = JsonConvert.DeserializeObject<CrawlRequest>(File.ReadAllText("Resources/56bc3065-fc3c-4af6-acc0-dda71f70c35f.json"), new BaseClassConverter());
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void StartCrawlTest()
        {
            

            var t = _testee.Start().Match(a => a, () => throw new Exception("Failed to start crawl"));

            // ACT
            File.Copy("Resources/56bc3065-fc3c-4af6-acc0-dda71f70c35e.json", "Requests/in/56bc3065-fc3c-4af6-acc0-dda71f70c35e.json");
            System.Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Request Submitted");
            Task.Delay(800).Wait();
            _testee.Stop();

            var resultFiles = Directory.GetFiles("Requests/out").Length;

            Assert.AreEqual(1, resultFiles);
        }

        [TestMethod]
        public void TestXmlParsing()
        {
            var xml = File.ReadAllText("Resources/test.html");
            var htmlDocument = XmlParser.Parse(xml);
        }
    }
}
