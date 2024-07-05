//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2024  Paul Eger                                                                                                                                                                     

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
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microservice.Exchange.Core.Bertrand.Tests
{
    [TestClass]
    public class BertrandExchangeTests
    {
        private Mock<IBertrandStateStore> mockBertrandSateStore;
        private List<Mock<IBertrandConsumer>> mockConsumers;
        private List<Mock<IBertrandTransformer>> mockTransformers;
        private List<Mock<IBetrandTransformerFilter>> mockTransformerFilters;
        private List<Mock<IBertrandPublisherFilter>> mockPublisherFilters;
        private List<Mock<IPublisher<object>>> mockPublishers;

        private ILogger<BertrandExchange> logger;

        [TestInitialize]
        public void TestInitialize()
        {
            // Initialize lists of mocked objects
            mockConsumers = [];
            mockTransformers = [];
            mockTransformerFilters = [];
            mockPublisherFilters = [];
            mockPublishers = [];
            mockBertrandSateStore = new Mock<IBertrandStateStore>();
            mockBertrandSateStore.Setup(mock => mock.GetOutstandingMessages()).Returns(async () => await Task.FromResult(new List<Message<object>>()));
            mockBertrandSateStore.Setup(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));
            mockBertrandSateStore.Setup(mock => mock.Delete(It.IsAny<Option<Guid>>())).Returns(async () => await Task.FromResult(Unit.Default));

            // Initialize logger mock
            var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
            })
            .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger<BertrandExchange>>();
        }

        [TestMethod]
        public async Task Start_WithConsumers_ThenConsumersStarted()
        {
            // Arrange
            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockConsumer = new Mock<IBertrandConsumer>();

                mockConsumers.Add(mockConsumer);
                mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Start().Match(r => r, () => throw new Exception("Start failed"), ex => throw ex);


            // Assert
            foreach (var mockConsumer in mockConsumers)
            {
                mockConsumer.Verify(c => c.Start(It.IsAny<IBertrandMessageHandler>()), Times.Once);
            }
        }

        [TestMethod]
        public async Task StartAndConsumerMessage_WithMatchingTransformer_ThenTransformed()
        {
            // Arrange
            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockConsumer = new Mock<IBertrandConsumer>();
                var mockFilter = new Mock<IBetrandTransformerFilter>();
                var mockTransformer = new Mock<IBertrandTransformer>();

                mockConsumers.Add(mockConsumer);
                mockTransformerFilters.Add(mockFilter);
                mockTransformers.Add(mockTransformer);
                mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
                mockFilter.Setup(f => f.IsMatch(It.IsAny<Option<IBertrandTransformer>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));

            }

            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = $"Transformed message: {Guid.NewGuid()}" }));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = new TestMessage { Message = "I am a test" } }).Match(r => r, () => throw new Exception("Failed to handle message"));


            // Assert
            foreach (var transformer in mockTransformers)
            {
                transformer.Verify(t => t.Transform(It.IsAny<Option<Message<object>>>()), Times.Once);
            }
        }

        [TestMethod]
        public async Task StartAndConsumerMessage_StateStored_FinallyStateDeleted()
        {
            // Arrange
            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockConsumer = new Mock<IBertrandConsumer>();
                var mockFilter = new Mock<IBetrandTransformerFilter>();
                var mockTransformer = new Mock<IBertrandTransformer>();

                mockConsumers.Add(mockConsumer);
                mockTransformerFilters.Add(mockFilter);
                mockTransformers.Add(mockTransformer);
                mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
                mockFilter.Setup(f => f.IsMatch(It.IsAny<Option<IBertrandTransformer>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));

            }

            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = $"Transformed message: {Guid.NewGuid()}" }));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = new TestMessage { Message = "I am a test" } }).Match(r => r, () => throw new Exception("Failed to handle message"));

            // Assert
            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Once);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task StartAndConsumerMessage_WithMatchingTransformer_WithMatchinPublisherFilters_WhenTransformed_ThenPublished()
        {
            // Arrange
            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockConsumer = new Mock<IBertrandConsumer>();
                var mockTransformerFilter = new Mock<IBetrandTransformerFilter>();
                var mockTransformer = new Mock<IBertrandTransformer>();
                mockTransformers.Add(mockTransformer);
                mockTransformer.Setup(transformer => transformer.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = "Transformed message" }));
                var mockPublisherFilter = new Mock<IBertrandPublisherFilter>();
                var mockPublisher = new Mock<IPublisher<object>>();
                mockPublisher.Setup(p => p.Name).Returns($"TestPublisher: {i}");

                mockConsumers.Add(mockConsumer);
                mockTransformerFilters.Add(mockTransformerFilter);

                mockPublisherFilters.Add(mockPublisherFilter);
                mockPublishers.Add(mockPublisher);

                mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
                mockTransformerFilter.Setup(f => f.IsMatch(It.IsAny<Option<IBertrandTransformer>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));

                mockPublisherFilter.Setup(f => f.IsMatch(It.IsAny<Option<IPublisher<object>>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));
                mockPublisher.Setup(publisher => publisher.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));
            }

            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = $"Transformed message: {Guid.NewGuid()}" }));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = "Test message" }).Match(r => r, () => throw new Exception("Failed to handle message"));


            // Assert
            foreach (var transformer in mockTransformers)
            {
                transformer.Verify(t => t.Transform(It.IsAny<Option<Message<object>>>()), Times.Once);
            }

            foreach (var publisher in mockPublishers)
            {
                publisher.Verify(t => t.Publish(It.IsAny<Option<Message<object>>>()), Times.Exactly(mockTransformers.Count));
            }

            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Once);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task StartAndConsumerMessage_WithMatchingTransformer_WithoutPublisherFilters_WhenTransformed_ThenPublished()
        {
            // Arrange
            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockConsumer = new Mock<IBertrandConsumer>();
                var mockTransformerFilter = new Mock<IBetrandTransformerFilter>();
                var mockTransformer = new Mock<IBertrandTransformer>();
                mockTransformers.Add(mockTransformer);
                mockTransformer.Setup(transformer => transformer.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = "Transformed message" }));
                var mockPublisher = new Mock<IPublisher<object>>();
                mockPublishers.Add(mockPublisher);
                mockPublisher.Setup(publisher => publisher.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));


                mockConsumers.Add(mockConsumer);
                mockTransformerFilters.Add(mockTransformerFilter);


                mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
                mockTransformerFilter.Setup(f => f.IsMatch(It.IsAny<Option<IBertrandTransformer>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));

            }

            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = $"Transformed message: {Guid.NewGuid()}" }));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = "Test message" }).Match(r => r, () => throw new Exception("Failed to handle message"));


            // Assert
            foreach (var transformer in mockTransformers)
            {
                transformer.Verify(t => t.Transform(It.IsAny<Option<Message<object>>>()), Times.Once);
            }

            foreach (var publisher in mockPublishers)
            {
                publisher.Verify(t => t.Publish(It.IsAny<Option<Message<object>>>()), Times.Exactly(mockTransformers.Count));
            }

            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Once);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task StartAndConsumerMessage_WithoutMatchingTransformer_WithoutPublisherFilters_ThenPublished()
        {
            // Arrange
            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockConsumer = new Mock<IBertrandConsumer>();
                mockConsumers.Add(mockConsumer);
                mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));

                var mockPublisher = new Mock<IPublisher<object>>();
                mockPublishers.Add(mockPublisher);
                mockPublisher.Setup(publisher => publisher.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));
            }

            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = $"Transformed message: {Guid.NewGuid()}" }));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = "Test message" }).Match(r => r, () => throw new Exception("Failed to handle message"));


            // Assert
            foreach (var publisher in mockPublishers)
            {
                publisher.Verify(t => t.Publish(It.IsAny<Option<Message<object>>>()), Times.Once());
            }

            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Once);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task Start_WhenStateExist_StateMessagesProcessed_ThenPublished()
        {
            // Arrange
            mockBertrandSateStore.Setup(mock => mock.GetOutstandingMessages()).Returns(async () => await Task.FromResult(new[] { new Message<object> { Payload = $"Test message from store: {Guid.NewGuid()}" } }));

            foreach (var i in Enumerable.Range(1, 3))
            {
                var mockPublisher = new Mock<IPublisher<object>>();
                mockPublishers.Add(mockPublisher);
                mockPublisher.Setup(publisher => publisher.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));
            }

            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = $"Transformed message: {Guid.NewGuid()}" }));
            }

            var exchange = CreateExchange();

            // Act
            await exchange.Start().Match(r => r, () => throw new Exception("Failed to start exchange"), ex => throw ex);

            // Assert
            foreach (var publisher in mockPublishers)
            {
                publisher.Verify(t => t.Publish(It.IsAny<Option<Message<object>>>()), Times.Once());
            }

            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Never);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_WhenTransformerError_ThenStateMovedToDeadletter()
        {
            // Arrange - Consumer
            var mockConsumer = new Mock<IBertrandConsumer>();
            mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
            mockConsumers.Add(mockConsumer);

            // Arrange - transformer
            var mockTransformerFilter = new Mock<IBetrandTransformerFilter>();
            var mockTransformer = new Mock<IBertrandTransformer>();
            mockTransformers.Add(mockTransformer);
            mockTransformer.Setup(transformer => transformer.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = "Transformed message" }));
            mockTransformerFilters.Add(mockTransformerFilter);

            // Arrange - filter matches transformer
            mockTransformerFilter.Setup(f => f.IsMatch(It.IsAny<Option<IBertrandTransformer>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));

            // Arrange - Publisher
            var mockPublisher = new Mock<IPublisher<object>>();
            mockPublishers.Add(mockPublisher);
            mockPublisher.Setup(publisher => publisher.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));

            // Arrange - Transformer returns empty/error
            foreach (var transformer in mockTransformers)
            {
                transformer.Setup(t => t.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => { await Task.CompletedTask; return null; });
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = "Test message" }).Match(r => r, () => throw new Exception("Failed to handle message"));

            // Assert
            foreach (var publisher in mockPublishers)
            {
                publisher.Verify(t => t.Publish(It.IsAny<Option<Message<object>>>()), Times.Never());
            }

            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Once);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once());
            mockBertrandSateStore.Verify(mock => mock.StoreInDeadletter(It.IsAny<Option<Message<object>>>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_WhenPublisherError_ThenStateMovedToDeadletter()
        {
            // Arrange - Consumer
            var mockConsumer = new Mock<IBertrandConsumer>();
            mockConsumer.Setup(consumer => consumer.Start(It.IsAny<IBertrandMessageHandler>())).Returns(async () => await Task.FromResult(Unit.Default));
            mockConsumers.Add(mockConsumer);

            // Arrange - transformer
            var mockTransformerFilter = new Mock<IBetrandTransformerFilter>();
            var mockTransformer = new Mock<IBertrandTransformer>();
            mockTransformers.Add(mockTransformer);
            mockTransformer.Setup(transformer => transformer.Transform(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(new Message<object> { Payload = "Transformed message" }));
            mockTransformerFilters.Add(mockTransformerFilter);

            // Arrange - filter matches transformer
            mockTransformerFilter.Setup(f => f.IsMatch(It.IsAny<Option<IBertrandTransformer>>(), It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(true));

            // Arrange - Publisher
            var mockPublisher = new Mock<IPublisher<object>>();
            mockPublishers.Add(mockPublisher);
            mockPublisher.Setup(publisher => publisher.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Unit.Default));

            // Arrange - Publisher returns empty/error
            foreach (var publisher in mockPublishers)
            {
                publisher.Setup(p => p.Publish(It.IsAny<Option<Message<object>>>())).Returns(async () => await Task.FromResult(Option<Unit>.None));
            }

            var exchange = CreateExchange();

            // Act
            var result = await exchange.Handle(new Message<object> { Payload = "Test message" }).Match(r => r, () => throw new Exception("Failed to handle message"));

            // Assert
            mockBertrandSateStore.Verify(mock => mock.StoreIncomingMessage(It.IsAny<Option<Message<object>>>()), Times.Once);
            mockBertrandSateStore.Verify(mock => mock.Delete(It.IsAny<Option<Guid>>()), Times.Once());
            mockBertrandSateStore.Verify(mock => mock.StoreInDeadletter(It.IsAny<Option<Message<object>>>()), Times.Once);
        }

        private BertrandExchange CreateExchange()
        {
            return new BertrandExchange(
                            "TestExchange",
                            mockConsumers.ConvertAll(c => c.Object),
                            mockTransformers.ConvertAll(t => t.Object),
                mockTransformerFilters.ConvertAll(tf => tf.Object),
                mockPublisherFilters.ConvertAll(pf => pf.Object),
                mockPublishers.ConvertAll(p => p.Object),
                logger,
                Mock.Of<IBertrandMetrics>(),
                mockBertrandSateStore.Object)
                ;
        }

        public class TestMessage
        {
            public string Message { get; set; }
        }
    }
}
