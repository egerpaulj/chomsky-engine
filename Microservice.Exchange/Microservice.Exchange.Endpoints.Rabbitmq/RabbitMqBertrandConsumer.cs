//      Microservice Exhange Libraries for .Net C#                                                                                                                                       
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
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Amqp;
using Microservice.Exchange.Core.Bertrand;

namespace Microservice.Exchange.Endpoints.Rabbitmq;

public class RabbitMqBertrandConsumer<T>(IAmqpProvider provider, string contextName, string name) : IBertrandConsumer, IMessageHandler<T, object>
{
    public string Name { get; } = name;
    IMessageSubscriber<T, object> _subscriber;
    private readonly IAmqpProvider _amqpProvider = provider;
    private readonly string _contextName = contextName;
    private IBertrandMessageHandler _messageHandler;

    public TryOptionAsync<Unit> End()
    {
        return async () =>
        {
            _subscriber.Dispose();
            return await Task.FromResult(Unit.Default);
        };
    }

    public async Task<object> HandleMessage(Option<Amqp.Message<T>> message)
    {
        var inData = message.Match(m => m, () => throw new Exception(""));

        var exchangeMessage = new Message<object>()
        {
            Payload = inData.Payload.Match(r => r, () => default),
            Id = inData.Id,
            CorrelationId = inData.CorrelationId,
            RoutingKey = inData.RoutingKey
        };

        return await _messageHandler.Handle(exchangeMessage).Match(r => r, () => throw new Exception("Failed to handle message"), ex => throw ex);
    }

    public TryOptionAsync<Unit> Start(IBertrandMessageHandler messageHandler)
    {
        return async () =>
        {
            _messageHandler = messageHandler;
            _subscriber ??= await _amqpProvider.GetSubsriber(_contextName, this).Match(r => r, () => throw new Exception("Failed to Create Subscriber"), ex => throw ex);

            _subscriber.Start();

            return await Task.FromResult(Unit.Default);
        };
    }
}
