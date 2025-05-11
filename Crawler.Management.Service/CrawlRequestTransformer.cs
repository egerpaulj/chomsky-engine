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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using Crawler.DataModel;
using Crawler.Stategies.Core;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Management.Service;

public class CrawlRequestTransformer<TIn, TOut>(
    ILogger<CrawlRequestTransformer<TIn, TOut>> logger,
    ICrawlStrategyMapper crawlStrategyMapper,
    string name,
    string routingKey
) : IBertrandTransformer
    where TIn : CrawlRequest
    where TOut : CrawlResponse
{
    private static Counter _counter = Prometheus.Metrics.CreateCounter(
        "crawls",
        "Processing crawls",
        "context"
    );

    public string Name => name;

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var crawlRequest = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));
            var output = new Message<object>();
            output = inputMessage.CopyData(output);
            output.RoutingKey = routingKey;

            logger.LogInformation(
                $"Preparing to Crawl: crawlRequestId: {crawlRequest.Id}, Cont.: {crawlRequest.ContinuationStrategy}"
            );

            var strategy = await crawlStrategyMapper
                .GetCrawlStrategy(crawlRequest)
                .Match(s => s, () => throw new Exception("Strategy missing"), ex => throw ex);
            var contStrategy = await crawlStrategyMapper
                .GetContinuationStrategy(crawlRequest)
                .MatchUnsafe(s => s, () => null, ex => throw ex);
            var contStrategyOpt =
                contStrategy != null
                    ? Option<ICrawlContinuationStrategy>.Some(contStrategy)
                    : Option<ICrawlContinuationStrategy>.None;

            var request = new Request(
                Option<ICrawlStrategy>.Some(strategy),
                contStrategyOpt,
                crawlRequest
            );

            logger.LogInformation($"Starting Crawl: {crawlRequest.Id}");
            _counter.WithLabels($"started").Inc();
            var response = await strategy
                .Crawl(request)
                .Match(
                    r => r,
                    () =>
                    {
                        _counter.WithLabels($"failed").Inc();
                        throw new Exception("Empty result when crawling");
                    },
                    ex =>
                    {
                        _counter.WithLabels($"failed").Inc();
                        throw ex;
                    }
                );
            _counter.WithLabels($"completed").Inc();

            logger.LogInformation($"Completed Crawl: {crawlRequest.Id}");

            output.Payload = response;
            return output;
        };
    }
}
