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
using LanguageExt;
using Microservice.Amqp.Configuration;

namespace Microservice.Amqp
{
    /// <summary>
    /// Provides <see cref="IMessagePublisher"/> and <see cref="IMessageSubscriber"/>.
    /// </summary>
    public interface IAmqpProvider
    {
        TryOptionAsync<IMessagePublisher> GetPublisher(Option<string> contextName);
        TryOptionAsync<IMessageSubscriber<T, R>> GetSubsriber<T, R>(
            Option<string> contextName,
            IMessageHandler<T, R> messageHandler
        );
        TryOptionAsync<IMessageSubscriber<T, R>> GetSubsriber<T, R>(
            Option<string> contextName,
            Option<string> queueName,
            IMessageHandler<T, R> messageHandler
        );
        Option<AmqpContextConfiguration> GetContext(Option<string> contextName);
    }
}
