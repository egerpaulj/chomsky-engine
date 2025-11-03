//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2025  Paul Eger

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
using Crawler.Core.Parser.DocumentParts;
using Crawler.Data.Repository;
using Crawler.DataModel;
using DnsClient.Internal;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Logging;

namespace Crawler.Data.Service;

public class CrawlerResponseTransformer<TIn>(
    ILogger<IBertrandTransformer> logger,
    IMongoDbRepository<CrawlResponseModel> defaultRepository,
    ICrawlerResponseShardedRepository crawlerResponseShardedRepository
) : IBertrandTransformer
    where TIn : CrawlResponseModel
{
    public string Name => CrawlerResponseTransformerName;
    public const string CrawlerResponseTransformerName = "crawler-response-transformer";

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var message = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));

            var uriText = message.Uri.Match(u => u, string.Empty);

            if (string.IsNullOrEmpty(uriText))
                return Option<Message<object>>.None;

            logger.LogInformation($"Article found for: {uriText}");

            message.UriText = uriText;
            message.DataPipelineUpdated = null;

            var output = new Message<object>();
            output = inputMessage.CopyDataInto(output);
            output.RoutingKey = CrawlerResponseTransformerName;

            output.Payload = message;

            await crawlerResponseShardedRepository
                .AddOrUpdate(message)
                .Match(r => r, () => throw new Exception("Failed to update document"));

            await defaultRepository
                .Delete(message.Id)
                .Match(m => m, () => throw new Exception("Failed to delete"));

            return output;
        };
    }
}
