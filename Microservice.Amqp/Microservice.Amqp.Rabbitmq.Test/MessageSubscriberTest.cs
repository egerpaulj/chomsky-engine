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

using System.Collections.Generic;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microservice.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client.Events;

namespace Microservice.Amqp.Rabbitmq.Test
{
    [TestClass]
    public class MessageSubscriberTest
    {
        private Mock<IMessageHandler<TestRequestMessage, TestRequestMessage>> _messageHandlerMock;
        private RabbitMqSubscriberConfig _configuration;
        
        public MessageSubscriberTest()
        {
            _messageHandlerMock = new Mock<IMessageHandler<TestRequestMessage, TestRequestMessage>>();

            _configuration = new RabbitMqSubscriberConfig
             {
                Host = "TestHost",
                Password = "TestPassword",
                Port = 7788,
                Username = "TestUserName",
                VirtHost = "TestVirtualHost",
                QueueName = "TestQueue",
            };
            
        }
        
        [TestMethod]
        public void Start_ThenConnectToAmqp_ThenConsumeMessages()
        {
            var connectionFactoryMock = TestHelper.GetConnectionFactoryMock(out var connection, out var model);

            var testee = new MessageSubscriber<TestRequestMessage, TestRequestMessage>
            (
                _configuration,
                Mock.Of<IJsonConverterProvider>(),
                connectionFactoryMock.Object,
                _messageHandlerMock.Object
            );

            testee.Start();
            
            model.Verify(m => m.BasicConsume(
                _configuration.QueueName, 
                false, 
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string,object>>(),
                It.IsAny<AsyncEventingBasicConsumer>()),
                Times.Once);
        }
    }
}