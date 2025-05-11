using System.Threading.Tasks;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

public class BertrandTypeFilter(string typeName, string matchingTargetName)
    : IBertrandPublisherFilter,
        IBetrandTransformerFilter
{
    private readonly string typeName = typeName;
    private readonly string matchingTargetName = matchingTargetName;

    public string Name { get; } = $"Type filter: {typeName}";

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
            var isTypeMatch = data.Bind(d => d.Payload)
                .Match(payload => payload.GetType().FullName == typeName, () => false);
            var isNameMatch = name == matchingTargetName;

            return await Task.FromResult(isTypeMatch && isNameMatch);
        };
    }
}
