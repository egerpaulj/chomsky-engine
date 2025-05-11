using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.Microservice.Core;
using Crawler.WebDriver.Selenium.UserActions;
using Microservice.Mongodb.Repo;
using Microservice.TestHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace Crawler.IntegrationTest
{
    [TestClass]
    [TestCategory("IntegrationTest")]
    public class MongoDbConfigurationTest
    {
        [TestCleanup]
        public async Task CleanUp()
        {
            var repo = CreateRepository();
            await repo.Delete(Builders<BsonDocument>.Filter.Empty)
                .Match(r => r, () => throw new Exception("Delete failed"), ex => throw ex);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task ReadSelector()
        {
            var testee = CreateTestee();
            await testee
                .AddOrUpdate(CreateRequest($"{TestHelper.TestUri}/whatever", isUrlCollector: false))
                .Match(r => r, () => throw new Exception("Failed to store"));

            var result = await testee
                .GetCrawlRequest("https://test.com/test")
                .Match(r => r, () => throw new Exception("Empty result"), e => throw e);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task UiActionTest()
        {
            var testee = CreateTestee();

            var request = new CrawlRequestModel
            {
                Id = Guid.Parse("56fbd8f0-5a25-43ca-ab76-b3b844f243c1"),
                Uri = "*",
                Host = "www.test.com",
                ContinuationStrategyDefinition = CrawlContinuationStrategy.TrackLinksOnly,
                IsUrlCollector = true,
                UiActions =
                [
                    new UiAction { XPath = "//footer", Type = UiAction.ActionType.Scroll },
                ],
                CollectablePattern = "^https://www\\.test\\.com\\D+$",
                DocumentPartDefinition = new DocumentPartAutodetect("https://www.test.com"),
                UrlSkipList =
                [
                    "localhost",
                    "about:",
                    "about:neterror",
                    "#comments",
                    "page=with:block",
                ],
            };

            await testee
                .AddOrUpdate(request)
                .Match(r => r, () => throw new Exception("Failed to store"));

            var result = await testee
                .GetCollectorCrawlRequest("https://www.test.com")
                .Match(r => r, () => throw new Exception("Failed to get"), ex => throw ex);
            Assert.AreEqual(request.Id, result.Id);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task GetConfigurationIntegrationTest()
        {
            var testee = CreateTestee();

            await testee
                .AddOrUpdate(CreateRequest($"{TestHelper.TestUri}/whatever", isUrlCollector: false))
                .Match(r => r, () => throw new Exception("Failed to store"));

            var res = await testee
                .GetCrawlRequest($"{TestHelper.TestUri}/?q=somethingElse")
                .Match(r => r, () => throw new System.Exception("Failed"), e => throw e);
            System.Console.WriteLine(res.Id.ToString());
            Assert.IsNotNull(res);
            Assert.AreEqual("test.com", res.Host);
            Assert.AreEqual("*", res.Uri);
            Assert.AreEqual(false, res.ShouldDownloadContent);
            Assert.AreEqual(typeof(DocumentPartArticle), res.DocumentPartDefinition.GetType());
            var xpath = res
                .DocumentPartDefinition.Selector.Match(
                    s => s,
                    () => throw new Exception("Missing selector")
                )
                .Xpath.Match(x => x, () => throw new Exception("Missing xpath"));

            Assert.AreEqual("somexpath", xpath);

            var subparts = res.DocumentPartDefinition.SubParts.Match(
                p => p,
                () => throw new Exception("SUb parts empty")
            );

            Assert.AreEqual(1, subparts.Count);
            Assert.AreEqual(typeof(DocumentPartLink), subparts[0].GetType());
            Assert.AreEqual(
                "linkxpath",
                subparts[0]
                    .Selector.Bind(s => s.Xpath)
                    .Match(x => x, () => throw new Exception("Link xpath is empty"))
            );

            Assert.AreEqual(1, res.UiActions.Count);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task DeleteTest()
        {
            var testee = CreateTestee();
            await testee
                .DeleteAll(TestHelper.TestUri)
                .Match(u => u, () => throw new Exception("Delete all failed"));

            await StoreConfigurationIntegrationTest();
            await testee
                .DeleteAll(TestHelper.TestUri)
                .Match(u => u, () => throw new Exception("Delete all failed"));

            await testee
                .GetCrawlRequest($"{TestHelper.TestUri}/?q=somethingElse")
                .Match(r => Assert.Fail(), () => { }, e => Assert.Fail(e.Message));
        }

        public static async Task StoreConfigurationIntegrationTest()
        {
            var testee = CreateTestee();
            await DeleteAll();
            var res = await testee
                .AddOrUpdate(CreateRequest("https://www.test.com"))
                .Match(r => r, () => throw new System.Exception("Failed"), e => throw e);

            Assert.IsNotNull(testee);
        }

        public static async Task DeleteAll()
        {
            var testee = CreateTestee();
            await testee.DeleteAll(TestHelper.TestUri).Match(r => { }, () => { });
        }

        private static MongoDbConfigurationRepository CreateTestee()
        {
            return new MongoDbConfigurationRepository(CreateRepository());
        }

        private static MongoDbRepository<CrawlRequestModel> CreateRepository()
        {
            var configuration = TestHelper.GetConfiguration();
            return new MongoDbRepository<CrawlRequestModel>(
                configuration,
                new DatabaseConfiguration("crawl_request", "test"),
                new JsonConverterProvider()
            );
        }

        private static CrawlRequestModel CreateRequest(string uri, bool isUrlCollector = true)
        {
            return new CrawlRequestModel
            {
                CollectablePattern = "^https://www\\.test\\.com\\D+$",
                UrlSkipList =
                [
                    "localhost",
                    "about:",
                    "about:neterror",
                    "#comments",
                    "page=with:block",
                ],
                IsUrlCollector = isUrlCollector,
                Host = new Uri(uri).Host,
                Uri = "*",
                ContinuationStrategyDefinition = Core.Requests.CrawlContinuationStrategy.DomainOnly,
                UiActions = [new UiAction()],
                DocumentPartDefinition = new DocumentPartArticle(uri)
                {
                    Selector = new DocumentPartSelector() { Xpath = "somexpath" },
                    Title = new DocumentPartText(uri)
                    {
                        Selector = new DocumentPartSelector()
                        {
                            Xpath = "//*[@data-gu-name='headline']",
                        },
                    },
                    Content = new DocumentPartText(uri)
                    {
                        Selector = new DocumentPartSelector() { Xpath = "//*[@id='maincontent']" },
                    },
                    SubParts = new List<DocumentPart>()
                    {
                        // Select all images within content
                        new DocumentPartLink(uri)
                        {
                            Name = "Heading",
                            Selector = new DocumentPartSelector { Xpath = "linkxpath" },
                        },
                    },
                },
            };
        }
    }
}
