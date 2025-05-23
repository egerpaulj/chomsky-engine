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

using System.Configuration;
using System.Security;
using LanguageExt;
using Microsoft.Extensions.Configuration;

namespace Microservice.Amqp.Rabbitmq.Configuration
{
    /// <summary>
    /// The configuration for the RabbitMQ messaging system.
    /// </summary>
    public class RabbitmqConfig
    {
        public string VirtHost { get; set; }

        public string Host { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public int Port { get; set; }
    }
}
