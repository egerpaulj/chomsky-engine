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
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Prometheus;

namespace Crawler.Management.Service;

public class UriTransformer<TIn>(
    ILogger<UriTransformer<TIn>> logger,
    ISchedulerRepository schedulerRepository,
    IConfigurationRepository configurationRepository,
    IMongoDbRepository<CrawlResponseModel> responseRepository,
    string name,
    string routingKey
) : IBertrandTransformer
    where TIn : CrawlUri
{
    private static Counter _counter = Prometheus.Metrics.CreateCounter(
        "crawl_uri_found",
        "Processing found uris",
        "context"
    );

    public string Name => name;

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var crawlUri = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));
            var uri = crawlUri.Uri.Match(u => u, () => throw new Exception("Uri is empty"));

            logger.LogInformation($"Procesing Uri: {uri}: {crawlUri.UriTypeId}");
            _counter.WithLabels("processing").Inc();

            var output = new Message<object>();
            output = inputMessage.CopyData(output);
            output.RoutingKey = routingKey;

            var uriExists = await schedulerRepository
                .UriLinkExists(uri.ToLowerInvariant())
                .Match(r => r, () => false);

            if (
                await responseRepository
                    .Get(Builders<BsonDocument>.Filter.Eq("Uri", uri))
                    .Match(r => true, () => false)
            )
            {
                _counter.WithLabels("duplicate_uri_response_exist").Inc();
                logger.LogInformation($"Duplicate Uri (response exists): {uri}");
                output.RoutingKey = "response_exists_duplicate_uri";
            }
            else if (uriExists && crawlUri.UriTypeId != UriType.Onetime)
            {
                _counter.WithLabels("duplicate_uri_found").Inc();
                logger.LogInformation($"Duplicate Uri: {uri}");
                output.RoutingKey = "duplicate_uri";
            }
            else
            {
                logger.LogInformation($"Storing new Uri: {uri}");
                _counter.WithLabels("new_uri_found").Inc();

                var uriDataModel = new UriDataModel
                {
                    UriTypeId = crawlUri.UriTypeId,
                    Uri = uri.ToLowerInvariant(),
                    BaseUri = crawlUri.BaseUri.Match(r => r, () => uri.ToLowerInvariant()),
                };
                var shouldSkip =
                    crawlUri.UriTypeId != UriType.Onetime
                    && await configurationRepository
                        .ShouldSkip(crawlUri.BaseUri, crawlUri.Uri)
                        .Match(
                            r => r,
                            () => false,
                            ex => throw new Exception("Can't determine skip uri")
                        );

                if (shouldSkip)
                {
                    uriDataModel.IsSkipped = true;
                    uriDataModel.IsCompleted = true;
                    _counter.WithLabels("skipped").Inc();
                }

                output.Payload = uriDataModel;

                _counter.WithLabels("new_uri_stored").Inc();
            }

            return output;
        };
    }
}
