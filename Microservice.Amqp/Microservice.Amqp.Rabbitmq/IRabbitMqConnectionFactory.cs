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

using Microservice.Amqp.Rabbitmq.Configuration;
using RabbitMQ.Client;

namespace Microservice.Amqp.Rabbitmq
{
    public interface IRabbitMqConnectionFactory
    {
        IConnectionFactory CreateConnectionFactory(RabbitmqConfig configuration);
    }

    public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
    {
        public IConnectionFactory CreateConnectionFactory(RabbitmqConfig config)
        {
            return new ConnectionFactory
            {
                HostName = config.Host,
                VirtualHost = config.VirtHost,
                UserName = config.Username,
                Password = config.Password,
                DispatchConsumersAsync = true,
            };
        }
    }
}
