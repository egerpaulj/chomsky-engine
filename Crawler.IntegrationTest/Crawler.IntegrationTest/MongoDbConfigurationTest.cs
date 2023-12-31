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
using Moq;


namespace Crawler.IntegrationTest
{
    [TestClass]
    [TestCategory("IntegrationTest")]
    public class MongoDbConfigurationTest
    {
        
        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task GetConfigurationIntegrationTest()
        {
            var testee = CreateTestee();

            await testee.AddOrUpdate(CreateRequest($"{TestHelper.TestUri}/whatever")).Match(r => r, () => throw new Exception("Failed to store"));

            var res = await testee.GetCrawlRequest($"{TestHelper.TestUri}/?q=somethingElse")
            .Match(r => r, () => throw new System.Exception("Failed"), e => throw e);
            System.Console.WriteLine(res.Id.ToString());
            Assert.IsNotNull(res);
            Assert.AreEqual("test.com", res.Host);
            Assert.AreEqual("*", res.Uri);
            Assert.AreEqual(false, res.ShouldDownloadContent);
            Assert.AreEqual(typeof(DocumentPartText), res.DocumentPartDefinition.GetType());
            var xpath = res.DocumentPartDefinition.Selector.Match(s => s, () => throw new Exception("Missing selector")).Xpath.Match(x => x, () => throw new Exception("Missing xpath"));

            Assert.AreEqual("somexpath", xpath);

            var subparts = res.DocumentPartDefinition.SubParts.Match(p => p, () => throw new Exception("SUb parts empty"));

            Assert.AreEqual(1, subparts.Count);
            Assert.AreEqual(typeof(DocumentPartLink), subparts[0].GetType());
            Assert.AreEqual("linkxpath", subparts[0].Selector.Bind(s => s.Xpath).Match(x => x, () => throw new Exception("Link xpath is empty")));

            Assert.AreEqual(1, res.UiActions.Count);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task DeleteTest()
        {
            var testee = CreateTestee();
            await testee.DeleteAll(TestHelper.TestUri).Match(u => u, () => throw new Exception("Delete all failed"));

            await testee.DeleteAll(TestHelper.TestUri).Match(u => u, () => throw new Exception("Delete all failed"));

            await testee.GetCrawlRequest($"{TestHelper.TestUri}/?q=somethingElse")
            .Match(r => Assert.Fail(), () => { }, e => Assert.Fail(e.Message));

        }


        private static MongoDbConfigurationRepository CreateTestee()
        {
            return new MongoDbConfigurationRepository(
            new MongoDbRepository<CrawlRequestModel>(TestHelper.GetConfiguration(), new MongoDbConfig(), new JsonConverterProvider()));
        }

        private static CrawlRequestModel CreateRequest(string uri)
        {
            return new CrawlRequestModel
            {
                CollectablePattern = "^https://www\\.theguardian\\.com\\D+$",
                UrlSkipList = new List<string>{"localhost", "about:", "about:neterror", "#comments", "page=with:block"},
                IsUrlCollector = true,
                Host = new Uri(uri).Host,
                Uri = "*",
                ContinuationStrategyDefinition = Core.Requests.CrawlContinuationStrategy.DomainOnly,
                DocumentPartDefinition = new DocumentPartArticle(uri)
                {
                    Title = new DocumentPartText(uri)
                    {
                        Selector = new DocumentPartSelector()
                        {
                            Xpath = "//*[@data-gu-name='headline']"
                        }
                    },
                    Content = new DocumentPartText(uri)
                    {
                        Selector = new DocumentPartSelector()
                        {
                            Xpath = "//*[@id='maincontent']"
                        },
                        
                    },
                    SubParts = new List<DocumentPart>()
                    {
                        // Select all images within content
                        new DocumentPartText (uri)
                        {
                            Name = "Heading",
                            Selector = new DocumentPartSelector
                            {
                                Xpath = "//*[@data-gu-name='standfirst']"
                            }
                        },
                    }

                }
            };
        }

        internal class MongoDbConfig : IDatabaseConfiguration
        {
            public string DatabaseName => "Crawler";

            public string DocumentName => "crawl_request";
        }


    }
}
