using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Results;
using Crawler.Microservice.Core;
using Microservice.Amqp.Rabbitmq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reactive.Linq;
using System.Linq;
using LanguageExt;
using System.IO;
using Newtonsoft.Json;
using Crawler.Core.Requests;
using Microservice.TestHelper;

namespace Crawler.IntegrationTest
{
    [TestClass]
    public class AmqpTest
    {
        private AmqpProvider _amqpProvider;
        private AmqpBootstrapper _amqpBootstrapper;

        public AmqpTest()
        {
            var configuration = TestHelper.GetConfiguration();

            _amqpProvider = new AmqpProvider(configuration, new JsonConverterProvider(), new RabbitMqConnectionFactory());
            _amqpBootstrapper = new AmqpBootstrapper(configuration);
        }

        //[TestMethod]
        public void Bootstrap()
        {

        }

        [TestInitialize]
        public async Task Init()
        {
            await _amqpBootstrapper.Bootstrap().Match(a => a, () => Unit.Default);
            
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _amqpBootstrapper.Purge().Match(r => r, () => Unit.Default);
        }

        public async Task Publish(int numberOfMessage)
        {
            var responsePublisher = await _amqpProvider.GetPublisher("CrawlResponse").Match(p => p, () => throw new System.Exception("Publisher missing"));

            for (int i = 1; i <= numberOfMessage; i++)
                await responsePublisher.Publish<CrawlResponse>(new CrawlResponse
                {
                    CorrelationId = Guid.NewGuid(),
                    CrawlerId = Guid.NewGuid(),
                    Result = new Document
                    {
                        RequestDocumentPart = new DocumentPartArticle("")
                        {
                            Title = new DocumentPartText("")
                            {
                                Text = "SOme test title"
                            },
                            Content = new DocumentPartText("")
                            {
                                Text = $"Some Content{i}",
                                SubParts = new List<DocumentPart>
                            {
                                new DocumentPartLink(""){
                                    Text = "I am a link to nowhere",
                                    Uri = "Http://nowhereman"
                                }
                            }
                            }
                        }
                    }

                }).Match(p => p, () => throw new Exception("publish failed"));
        }

        [TestMethod]
        public async Task PublishRequestToQueue()
        {
            var environment = TestHelper.GetEnvironment();
            var test = await File.ReadAllTextAsync($"Resources/56bc3065-fc3c-4af6-acc0-dda71f70c35f_{environment}.json");

            var request = JsonConvert.DeserializeObject<CrawlRequest>(test, new JsonConverterProvider().GetJsonConverters());

             var publisher = await _amqpProvider.GetPublisher("CrawlRequest").Match(p => p, () => throw new System.Exception("Publisher missing"));

             await publisher.Publish<CrawlRequest>(request).Match(r => r, () => throw new Exception("Empty publish result"), ex => throw ex);
        }

        // [TestMethod]
        // public async Task TestSubscribe()
        // {
        //     var responseSubscriber = await _amqpProvider.GetSubsriber<CrawlResponse>("CrawlResponse").Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

        //     var numberOfMessages = 100;
        //     await Publish(numberOfMessages);
        //     var numberOfReceivedMessage = 0;

        //     responseSubscriber.GetAutoAckObservable().Subscribe(m =>
        //    {
        //        numberOfReceivedMessage++;
        //        Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {m.Context.Match(c => c, () => string.Empty)}");
        //        Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {m.CorrelationId}");
        //        Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {m.MessageType}");
        //        var reqDoc = m.Payload.Bind(p => p.Result).Bind(r => r.RequestDocumentPart).Match(d => d, () => throw new Exception("wtf"));

        //        var docPartArticles = DocumentPartExtensions.GetAllParts<DocumentPartArticle>(reqDoc).FirstOrDefault();
        //        var content = docPartArticles.Content.Match(c => c, () => throw new Exception("Content should not be null")) as DocumentPartText;
        //        var contentText = content.Text.Match(t => t, () => string.Empty);

        //        Console.WriteLine($"Message received: {contentText}");
        //    },
        //     e =>
        //     {
        //         Console.WriteLine($"Observable error. Turning cold due to: {e.Message}");
        //     });

        //     await Task.Delay(300);
        //     Assert.AreEqual(numberOfMessages, numberOfReceivedMessage);

        // }

        // [TestMethod]
        // public async Task TestSubscribeWhenErrorThenCold()
        // {
        //     var responseSubscriber = await _amqpProvider.GetSubsriber<CrawlResponse>("CrawlResponse").Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

        //     var numberOfMessages = 100;
        //     await Publish(numberOfMessages);
        //     var numberOfReceivedMessage = 0;

        //     responseSubscriber.GetAutoAckObservable().Subscribe(m =>
        //    {
        //        numberOfReceivedMessage++;
        //        Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {m.Context.Match(c => c, () => string.Empty)}");
        //        Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {m.CorrelationId}");
        //        Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {m.MessageType}");
        //        var reqDoc = m.Payload.Bind(p => p.Result).Bind(r => r.RequestDocumentPart).Match(d => d, () => throw new Exception("wtf"));

        //        var docPartArticles = DocumentPartExtensions.GetAllParts<DocumentPartArticle>(reqDoc).FirstOrDefault();
        //        var content = docPartArticles.Content.Match(c => c, () => throw new Exception("Content should not be null")) as DocumentPartText;
        //        var contentText = content.Text.Match(t => t, () => string.Empty);
        //        if (contentText.Contains("4"))
        //            throw new Exception("Test");

        //        Console.WriteLine($"Message received: {contentText}");
        //    },
        //     e =>
        //     {
        //         Console.WriteLine($"Observable error. Turning cold due to: {e.Message}");
        //     });

        //     await Task.Delay(300);
        //     Assert.AreEqual(4, numberOfReceivedMessage);

        // }

        // [TestMethod]
        // public async Task TestSubscribeWithWorker()
        // {
        //     var numberOfMessages = 100;
        //     await Publish(numberOfMessages);
        //     var numberOfReceivedMessage = 0;
            
        //     var responseSubscriber = await _amqpProvider.GetSubsriber<CrawlResponse>("CrawlResponse").Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

        //     var observable = responseSubscriber.GetObservable<string>(m =>
        //     {
        //         var reqDoc = m.Payload.Bind(p => p.Result).Bind(r => r.RequestDocumentPart).Match(d => d, () => throw new Exception("wtf"));

        //         var docPartArticles = DocumentPartExtensions.GetAllParts<DocumentPartArticle>(reqDoc).FirstOrDefault();
        //         var content = docPartArticles.Content.Match(c => c, () => throw new Exception("Content should not be null")) as DocumentPartText;
        //         var contentText = content.Text.Match(t => t, () => string.Empty);

        //         if (contentText.Contains("4"))
        //             throw new Exception("Test");

        //         return contentText;
        //     })
        //     .Subscribe(m =>
        //    {
        //        numberOfReceivedMessage++;
        //        m.Match(ex => Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Error: {ex.Message}"),
        //        result => Console.WriteLine($"{DateTime.Now:HH-mm-dd-ss.fff} - Message received: {result}"),
        //        () => throw new Exception("Nothing Happened"));
        //        Console.WriteLine();
        //    },
        //     e =>
        //     {
        //         Console.WriteLine($"Observable error. Turning cold due to: {e.Message}");
        //     });

        //     await Task.Delay(300);
        //     Assert.AreEqual(numberOfMessages, numberOfReceivedMessage);
        // }
    }
}