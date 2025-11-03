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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.DataModel;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Logging;

namespace Crawler.Data.Service;

public class CrawlerResponseCleanerTransformer<TIn>(
    ILogger<IBertrandTransformer> logger,
    IMongoDbRepository<CrawlResponseModel> readRepository,
    IMongoDbRepository<DataPipelineCleanText> writeRepository,
    IPublisher<DataPipelineCleanText> workerQueuePublisher,
    IConfigurationRepository configurationRepository
) : IBertrandTransformer
    where TIn : CrawlResponseModel
{
    public string Name => CrawlerResponseCleanerTransformerName;
    public const string CrawlerResponseCleanerTransformerName =
        "crawler-response-cleaner-transformer";

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var crawlerResponseModel = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));

            var uri = crawlerResponseModel.UriText;

            if (await configurationRepository.IsCollectable(uri).Match(r => r, () => false))
            {
                logger.LogWarning("Skipping collectable: {uri}", uri);
                await UpdateReadDataWithTimestamp(crawlerResponseModel, uri);
                return null;
            }

            var crawlRequest = await configurationRepository
                .GetCrawlRequest(uri)
                .Match(r => r, () => throw new Exception("Missing crawl request for: " + uri));

            if (crawlRequest.UrlSkipList.Any(l => uri.Contains(l)))
            {
                logger.LogWarning("Skipping: {uri}", uri);
                return null;
            }

            var documentPart = crawlerResponseModel
                .Result.Bind(r => r.RequestDocumentPart)
                .Match(r => r, () => new DocumentPartArticle(uri));

            var text = new StringBuilder();
            var article = documentPart.GetAllParts<DocumentPartArticle>().ToList().FirstOrDefault();

            if (article == null)
            {
                logger.LogWarning("Failed to process message without article: {uri}", uri);
                await UpdateReadDataWithTimestamp(crawlerResponseModel, uri);
                return null;
            }

            var content = article.Content.Match(c => c, () => new DocumentPartText(uri));
            var documentPartTexts = content.GetAllParts<DocumentPartText>().ToList();

            if (!documentPartTexts.Any())
            {
                logger.LogWarning($"Response does not have any text: {uri}");
                await UpdateReadDataWithTimestamp(crawlerResponseModel, uri);
                return null;
            }

            foreach (var documentPartText in documentPartTexts)
            {
                var data = documentPartText.Text.Match(r => r, string.Empty);
                data = data.Replace(".", " . ");
                data = data.ToLowerInvariant();
                data = data.Replace("headlines", "");

                data = Regex.Replace(
                    input: data,
                    pattern: @"this article is more than \d+ years old",
                    replacement: string.Empty
                );

                data = Regex.Replace(
                    input: data,
                    pattern: @"this article is more than \d+ months old",
                    replacement: string.Empty
                );

                data = Regex.Replace(
                    input: data,
                    pattern: @"this article is more than \d+ days old",
                    replacement: string.Empty
                );

                text.Append(data);
                text.Append(" ");
            }

            var cleanedText = new DataPipelineCleanText
            {
                DataSource = uri,
                DataSourceType = "media",
                CleanedText = text.ToString().Trim(),
                Uri = uri,
                Id = inputMessage.Id.Match(i => i, Guid.NewGuid()),
            };

            await AddCleanedDataToDatabase(cleanedText);
            //await PublishToWorkerQueue(inputMessage, uri, cleanedText);
            await UpdateReadDataWithTimestamp(crawlerResponseModel, uri);

            return null;
        };
    }

    private async Task AddCleanedDataToDatabase(DataPipelineCleanText cleanedText)
    {
        await writeRepository
            .AddOrUpdate(cleanedText)
            .Match(
                r => r,
                () => throw new Exception("Failed to store cleaned text"),
                ex => throw ex
            );
    }

    private async Task PublishToWorkerQueue(
        Message<object> inputMessage,
        string uri,
        DataPipelineCleanText cleanedText
    )
    {
        await workerQueuePublisher
            .Publish(
                new Message<DataPipelineCleanText>
                {
                    CorrelationId = inputMessage.CorrelationId,
                    Id = inputMessage.Id,
                    RoutingKey = "*",
                    Payload = cleanedText,
                }
            )
            .Match(
                r => { },
                () => logger.LogError("Failed to publish cleaned text for: {Uri}", uri),
                ex => logger.LogError(ex, "Failed to publish cleaned text for: {Uri}", uri)
            );
    }

    private async Task UpdateReadDataWithTimestamp(TIn crawlerResponseModel, string uri)
    {
        var DateStrFormat = "yyyy-MM-dd-HH:mm:ss.fff";
        crawlerResponseModel.DataPipelineUpdated = DateTime.UtcNow.ToString(DateStrFormat);

        await readRepository
            .AddOrUpdate(crawlerResponseModel)
            .Match(
                r => { },
                () => logger.LogWarning("Failed to update DataPipelineUpdated: {Uri}", uri),
                ex => logger.LogError(ex, "Failed to update DataPipelineUpdated: {Uri}", uri)
            );
    }
}
