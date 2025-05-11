using System.Threading.Tasks;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

public class BertrandRoutingKeyFilter(string routingKey, string matchingTargetName)
    : IBertrandPublisherFilter,
        IBetrandTransformerFilter
{
    private readonly string routingKey = routingKey;
    private readonly string matchingTargetName = matchingTargetName;

    public string Name { get; } = $"Routing key filter: {routingKey}. Match: {matchingTargetName}";

    public TryOptionAsync<bool> IsMatch<TOut>(
        Option<IPublisher<TOut>> publisher,
        Option<Message<object>> data
    )
    {
        return FilterMessage(publisher.Match(p => p.Name, () => string.Empty), data);
    }

    public TryOptionAsync<bool> IsMatch(
        Option<IBertrandTransformer> transformer,
        Option<Message<object>> data
    )
    {
        return FilterMessage(transformer.Match(t => t.Name, () => string.Empty), data);
    }

    private TryOptionAsync<bool> FilterMessage(string name, Option<Message<object>> data)
    {
        return async () =>
        {
            var isRoutingKeyMatch = data.Bind(d => d.RoutingKey)
                .Match(key => key == routingKey, () => false);
            var isNameMatch = name == matchingTargetName;

            return await Task.FromResult(isRoutingKeyMatch && isNameMatch);
        };
    }
}
