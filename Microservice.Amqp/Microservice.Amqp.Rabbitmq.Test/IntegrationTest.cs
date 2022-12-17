//      Microservice AMQP Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger                                                                                                                                                                     
                                                                                                                                                                                                                   
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
using System.Threading.Tasks;
using Microservice.Amqp.Rabbitmq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reactive.Linq;
using LanguageExt;
using Microservice.Amqp.Rabbitmq.Test;
using Moq;
using Microservice.Serialization;
using Microservice.Amqp;

namespace Amqp.IntegrationTest
{
    [TestClass]
    public class IntegrationTest
    {
        private const int MillisecondsDelay = 300;
        private AmqpProvider _amqpProvider;
        private AmqpBootstrapper _amqpBootstrapper;
        private Task<IMessagePublisher> _publisher;
        public IntegrationTest()
        {
            var configuration = Microservice.TestHelper.TestHelper.GetConfiguration();

            _amqpProvider = new AmqpProvider(configuration, new EmptyJsonConverterProvider(), new RabbitMqConnectionFactory());
            _amqpBootstrapper = new AmqpBootstrapper(configuration);

            _publisher = _amqpProvider.GetPublisher("CrawlRequest").Match(p => p, () => throw new System.Exception("Publisher missing"));
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

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObservable_When100MessagesPublished_Then100MessgesReceived()
        {
            // ARRANGE - Create a subcriber
            var subscriber = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t => t.TestId))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

            // ACT - Publish 100 messages
            var numberOfSentMessages = 100;
            await Publish(numberOfSentMessages);
            var numberOfReceivedMessage = 0;

            // ACT - Subscribe to Message Queue
            subscriber.GetObservable().Subscribe(m => numberOfReceivedMessage++);

            // ACT - Start consuming messages
            subscriber.Start();

            await Task.Delay(MillisecondsDelay);

            // ASSERT
            Assert.AreEqual(numberOfSentMessages, numberOfReceivedMessage);
        }


        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObservableFunc__When100MessagesPublished_WhenMessageIdContains4_ThenThrowError_ThenMessagesAreProcessed_ThenObservableIsNotCold()
        {
            // ARRANGE - Create a subcriber, Error on 4s
            var subscriber = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t =>
                                                            {
                                                                if (t.TestId.Contains("4"))
                                                                    throw new Exception("Expected Test Exception");
                                                                return t.TestId;
                                                            }))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

            var numberOfSentMessages = 100;
            var numberOfReceivedMessage = 0;

            var observable = subscriber
            .GetObservable()
            // ARRANGE - Subscribe, Count messages
            .Subscribe(m => numberOfReceivedMessage++);

            // ACT - Publish 100 messages
            await Publish(numberOfSentMessages);

            // ACT - Start consuming messages
            subscriber.Start();

            await Task.Delay(MillisecondsDelay);
            Assert.AreEqual(numberOfSentMessages, numberOfReceivedMessage);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObservableAsync__When100MessagesPublished_WhenMessageIdContains4_ThenThrowError_ThenMessagesAreProcessed_ThenObservableIsNotCold()
        {
            // ARRANGE - Create a subcriber with a handler that errors on 4
            var subscriber = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t =>
                                                            {
                                                                if (t.TestId.Contains("4"))
                                                                    throw new Exception("Expected Test Exception");
                                                                return t.TestId;
                                                            }))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);
            var numberOfSentMessages = 100;
            var numberOfReceivedMessage = 0;

            // ARRANGE - Get an observervable with a worker func that returns the string message - throw an error if message contains 4
            var observable = subscriber
            .GetObservable()
            // ARRANGE - Subscribe and count messages
            .Subscribe(m =>numberOfReceivedMessage++);

            // ACT - Publish 100 messages
            await Publish(numberOfSentMessages);

            // ACT - Start consuming messages
            subscriber.Start();

            await Task.Delay(MillisecondsDelay);
            Assert.AreEqual(numberOfSentMessages, numberOfReceivedMessage);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObservableFunc__WhenMultipleSubscribers_ThenLoadSharedBetweenSubscribers()
        {
            // ARRANGE - Create "same" subcriber twice
            var subscriber1 = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t => t.TestId))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

            var subscriber2 = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t => t.TestId))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);


            var numberOfSentMessages = 100;
            var numberOfReceivedMessage = 0;
            var numberOfReceivedMessage2 = 0;

            // ARRANGE - count and log first subscriber messages
            var observable = subscriber1
            .GetObservable()
            .Subscribe(m => numberOfReceivedMessage++);

            // ARRANGE - count subsriber's messages
            var observable2 = subscriber2
            .GetObservable()
            .Subscribe(m => numberOfReceivedMessage2++);

            // ACT - Publish 100 messages
            await Publish(numberOfSentMessages);

            // ACT - Start consuming messages
            subscriber1.Start();
            subscriber2.Start();

            await Task.Delay(MillisecondsDelay);
            Assert.AreEqual(numberOfSentMessages, numberOfReceivedMessage + numberOfReceivedMessage2);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObservable_When100MessagesPublished_WhenMultipleObservables_ThenDoubleTheMessgesReceived()
        {
            // ARRANGE - Create a subcriber
            var subscriber = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t => t.TestId))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

            

            // ACT - Publish 100 messages
            var numberOfSentMessages = 100;
            await Publish(numberOfSentMessages);
            var numberOfReceivedMessage = 0;

            // ACT - Subscribe to Message Queue
            subscriber.GetObservable().Subscribe(m => numberOfReceivedMessage++);
            subscriber.GetObservable().Subscribe(m => numberOfReceivedMessage++);

            // ACT - Start consuming messages
            subscriber.Start();

            await Task.Delay(MillisecondsDelay * 3);

            // ASSERT
            Assert.AreEqual(numberOfSentMessages * 2, numberOfReceivedMessage);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObservable_When10MessagesPublished_WhenMessageReceived_WhenSecondObservable_WhenPublishedAgain_ThenMessagesReceived()
        {
            // ARRANGE - Create a subcriber
            var subscriber = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t => t.TestId))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

            // ACT - Publish messages
            var numberOfSentMessages = 5;
            await Publish(numberOfSentMessages);
            var numberOfReceivedMessage = 0;

            // ACT - Subscribe to Message Queue
            subscriber.GetObservable().Subscribe(m => numberOfReceivedMessage++);
            
            // ACT - Start consuming messages
            subscriber.Start();
            await Task.Delay(MillisecondsDelay);

            // ASSERT
            Assert.AreEqual(numberOfSentMessages, numberOfReceivedMessage);

            // ACT - Subscribe to second observable
            subscriber.GetObservable().Subscribe(m => numberOfReceivedMessage++, ex => Console.WriteLine(ex.Message));

            // ACT - publish messages
            await Publish(numberOfSentMessages);
            await Task.Delay(MillisecondsDelay);

            // ASSERT
            Assert.AreEqual((numberOfSentMessages * 2) + numberOfSentMessages, numberOfReceivedMessage);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task TestObserverNack_ThenSentToDeadletterQueue()
        {
            // ARRANGE - Create a subcriber with a handler that errors
            var subscriber = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequest",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t =>
                                                            {
                                                                return ThrowException();
                                                            }))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);
            var numberOfSentMessages = 10;
            var numberOfReceivedMessage = 0;

            // ARRANGE - Get an observervable with a worker func that returns the string message - throw an error if message contains 4
            var observable = subscriber
            .GetObservable()
            // ARRANGE - Subscribe and count messages
            .Subscribe(m =>
            {
                // ARRANGE - Only count successfull messages. Either =>  Left:Message, Right:Exception
                if(m.IsLeft)
                    numberOfReceivedMessage++;
            });

            // ACT - Publish 100 messages
            await Publish(numberOfSentMessages);

            // ACT - Start consuming messages
            subscriber.Start();

            // ACT - Get Deadletter Messages
            var subscriberDeadletter = await _amqpProvider.GetSubsriber<TestRequestMessage, string>(
                                                            "CrawlRequestDeadletter",
                                                            MessageHandlerFactory.Create<TestRequestMessage, string>(t => t.TestId))
                                                        .Match(p => p, () => throw new System.Exception("Subscriber missing"), ex => throw ex);

            var numberOfDeadletterMessage = 0;
            subscriberDeadletter.GetObservable().Subscribe(t => numberOfDeadletterMessage++);
            subscriberDeadletter.Start();

            await Task.Delay(MillisecondsDelay);
            Assert.AreEqual(0, numberOfReceivedMessage);
            Assert.AreEqual(numberOfSentMessages, numberOfDeadletterMessage);
        }

        private static string ThrowException()
        {
            throw new Exception("Expected Test Exception");
        }

        private async Task<string> HandleMessage(TestRequestMessage message)
        {
            var contentText = message.TestId;

            if (contentText.Contains("4"))
                throw new Exception("Expected Test Error");

            return await Task.FromResult(contentText);
        }

        public async Task Publish(int numberOfMessage)
        {
            var publisher = await _publisher;
            for (int i = 1; i <= numberOfMessage; i++)
            {
                await publisher.Publish<TestRequestMessage>(new TestRequestMessage
                {
                    TestId = $"TestCase: {i}  -  {Guid.NewGuid().ToString()}"
                }).Match(p => p, () => throw new Exception("publish failed"));
            }
        }
    }
}