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
using System.Threading.Tasks;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

public class BertrandPublisher<T> : IPublisher<object>
{
    public string Name { get; }
    private readonly Func<Message<object>, Message<T>> convertFunc;
    private readonly IPublisher<T> publisher;

    public BertrandPublisher(
        string name,
        IPublisher<T> publisher,
        Func<Message<object>, Message<T>> convertFunc = null
    )
    {
        Name = name;
        this.convertFunc = convertFunc;
        this.convertFunc ??= KnownCastConvertion;
        this.publisher = publisher;
    }

    public TryOptionAsync<Unit> Publish(Option<Message<object>> message)
    {
        return message.ToTryOptionAsync().Bind(Convert).Bind(r => publisher.Publish(r));
    }

    public static Message<T> KnownCastConvertion(Message<object> item)
    {
        return new Message<T>
        {
            Id = item.Id,
            CorrelationId = item.CorrelationId,
            Properties = item.Properties,
            RoutingKey = item.RoutingKey,
            Payload = (T)item.Payload.Match(p => p, () => default(T)),
        };
    }

    private TryOptionAsync<Message<T>> Convert(Message<object> payload) =>
        async () => await Task.FromResult(convertFunc(payload));
}
