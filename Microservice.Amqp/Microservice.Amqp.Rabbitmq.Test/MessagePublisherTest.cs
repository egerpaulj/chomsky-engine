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
using Microservice.Amqp.Rabbitmq.Configuration;
using Microservice.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;

namespace Microservice.Amqp.Rabbitmq.Test
{
    [TestClass]
    public class MessagePublisherTest
    {
        [TestMethod]
        public async Task Publish_ThenModelBasicPublishCalled()
        {
            // ARRANGE
            var connectionFactoryMock = TestHelper.GetConnectionFactoryMock(out var connection, out var model);
            
            var rabbitmqConfig = new RabbitMqPublisherConfig()
            {
                Context = "Test",
                Exchange = "TestExchange",
                Host = "TestHost",
                Password = "TestPassword",
                Port = 7788,
                RoutingKey = "TestRoutingKey",
                Username = "TestUserName",
                VirtHost = "TestVirtualHost"
                
            };

            model.Setup(m => m.CreateBasicProperties()).Returns(Mock.Of<IBasicProperties>());
            
            var testee = new MessagePublisher(rabbitmqConfig, connectionFactoryMock.Object, new EmptyJsonConverterProvider());
            
            // ACT
            await testee.Publish<TestRequestMessage>(new TestRequestMessage{TestId = "Test Id"}).Match(r => {}, () => {});


            // ASSERT
            var expectedExchange = rabbitmqConfig.Exchange.Match(r => r, () => string.Empty);
            var expectedRoutingKey = rabbitmqConfig.RoutingKey.Match(r => r, () => string.Empty);

            model.Verify(m => m.BasicPublish
                (expectedExchange,
                expectedRoutingKey, 
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(), 
                It.IsAny<ReadOnlyMemory<byte>>()
                ), 
                Times.Once);

        }
    }
}