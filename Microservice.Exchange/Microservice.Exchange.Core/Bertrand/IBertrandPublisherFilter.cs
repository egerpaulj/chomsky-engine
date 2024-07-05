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
using System.Threading.Tasks;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

public interface IBertrandPublisherFilter
{
    string Name { get; }
    TryOptionAsync<bool> IsMatch<TOut>(Option<IPublisher<TOut>> publisher, Option<Message<object>> data);
}

public class BertrandRoutingKeyFilter(string routingKey, string matchingTargetName) : IBertrandPublisherFilter, IBetrandTransformerFilter
{
    private readonly string routingKey = routingKey;
    private readonly string matchingTargetName = matchingTargetName;

    public string Name { get; } = $"Routing key filter: {routingKey}. Match: {matchingTargetName}";

    public TryOptionAsync<bool> IsMatch<TOut>(Option<IPublisher<TOut>> publisher, Option<Message<object>> data)
    {
        return FilterMessage(publisher.Match(p => p.Name, () => string.Empty), data);
    }

    public TryOptionAsync<bool> IsMatch(Option<IBertrandTransformer> transformer, Option<Message<object>> data)
    {
        return FilterMessage(transformer.Match(t => t.Name, () => string.Empty), data);
    }

    private TryOptionAsync<bool> FilterMessage(string name, Option<Message<object>> data)
    {
        return async () =>
        {
            var isRoutingKeyMatch = data.Bind(d => d.RoutingKey).Match(key => key == routingKey, () => false);
            var isNameMatch = name == matchingTargetName;

            return await Task.FromResult(isRoutingKeyMatch && isNameMatch);
        };
    }
}

public class BertrandTypeFilter(string typeName, string matchingTargetName) : IBertrandPublisherFilter, IBetrandTransformerFilter
{
    private readonly string typeName = typeName;
    private readonly string matchingTargetName = matchingTargetName;

    public string Name { get; } = $"Type filter: {typeName}";

    public TryOptionAsync<bool> IsMatch<TOut>(Option<IPublisher<TOut>> publisher, Option<Message<object>> data)
    {
        return FilterMessage(publisher.Match(p => p.Name, () => string.Empty), data);
    }

    public TryOptionAsync<bool> IsMatch(Option<IBertrandTransformer> transformer, Option<Message<object>> data)
    {
        return FilterMessage(transformer.Match(t => t.Name, () => string.Empty), data);
    }

    private TryOptionAsync<bool> FilterMessage(string name, Option<Message<object>> data)
    {
        return async () =>
        {
            var isTypeMatch = data.Bind(d => d.Payload).Match(payload => payload.GetType().FullName == typeName, () => false);
            var isNameMatch = name == matchingTargetName;

            return await Task.FromResult(isTypeMatch && isNameMatch);
        };
    }
}


