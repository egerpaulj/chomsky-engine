using System;
using System.Threading.Tasks;
using Crawler.Core.Results;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

public class BertrandUrlFilter<TIn>(string url, string matchingTargetName)
    : IBertrandPublisherFilter,
        IBetrandTransformerFilter
    where TIn : CrawlResponse
{
    public string Name { get; } = $"Url filter: {url}";

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
        return FilterMessage(transformer.Match(p => p.Name, () => string.Empty), data);
    }

    private TryOptionAsync<bool> FilterMessage(string name, Option<Message<object>> data)
    {
        return async () =>
        {
            var isNameMatch = name == matchingTargetName;
            if (!isNameMatch)
                return false;

            return await Task.FromResult(
                data.Bind(d => d.Payload)
                    .Match(
                        p =>
                        {
                            if (p is TIn response)
                            {
                                var incomingUri = response.Uri.Match(u => u, () => string.Empty);
                                return incomingUri
                                    .ToLowerInvariant()
                                    .Contains(url.ToLowerInvariant());
                            }

                            return false;
                        },
                        () => false
                    )
            );
        };
    }
}
