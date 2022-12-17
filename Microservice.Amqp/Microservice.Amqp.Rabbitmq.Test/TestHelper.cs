using System;
using System.IO;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using RabbitMQ.Client;

namespace Microservice.Amqp.Rabbitmq.Test
{
    public class TestHelper
    {
        internal static Mock<IRabbitMqConnectionFactory> GetConnectionFactoryMock(out Mock<IConnection> connection, out Mock<IModel> model)
        {
            var rabbitMqFactoryMock = new Mock<IRabbitMqConnectionFactory>();

            model = new Mock<IModel>();
            var connectionFactoryMock = new Mock<IConnectionFactory>();
            connection = new Mock<IConnection>();

            connectionFactoryMock.Setup(m => m.CreateConnection()).Returns(connection.Object);
            connection.Setup(m => m.CreateModel()).Returns(model.Object);

            rabbitMqFactoryMock.Setup (m => m.CreateConnectionFactory(It.IsAny<RabbitmqConfig>())).Returns(connectionFactoryMock.Object);

            return rabbitMqFactoryMock;
        }
    }
}