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
using Crawler.Data.Repository;
using Crawler.DataModel;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Logging;

namespace Crawler.Data.Service;

public class CrawlerResponseCleanerTransformer<TIn>(
    ILogger<IBertrandTransformer> logger,
    ICrawlerResponseShardedRepository readRepository,
    IMongoDbAddOrUpdate<DataPipelineCleanText> writeRepository,
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
                    .Match(mes => mes, () => throw new Exception("Empty message"));

            var uri = crawlerResponseModel.UriText;

            if (await configurationRepository.IsCollectable(uri).Match(r => r, () => false))
            {
                logger.LogWarning("Skipping collectable: {uri}", uri);
                await MarkDataPipelineProcessed(crawlerResponseModel, uri);
                return null;
            }

            var crawlRequest = await configurationRepository
                .GetCrawlRequest(uri)
                .Match(r => r, () => throw new Exception("Missing crawl request for: " + uri));

            if (crawlRequest.UrlSkipList.Any(l => uri.Contains(l)))
            {
                logger.LogWarning("SkipList -> Skipping data cleansing for: {uri}", uri);
                await MarkDataPipelineProcessed(crawlerResponseModel, uri);
                return null;
            }

            var documentPart = crawlerResponseModel
                .Result.Bind(r => r.RequestDocumentPart)
                .Match(r => r, () => new DocumentPartArticle(uri));

            var text = new StringBuilder();
            var articles = documentPart.GetAllParts<DocumentPartArticle>().ToList();

            if (articles != null)
            {
                foreach (var article in articles)
                {
                    var cleanedText = CleanArticle(article, uri, crawlerResponseModel)
                        .Match(r => r, () => new());
                    if (!string.IsNullOrEmpty(cleanedText.CleanedText))
                        await AddCleanedDataToDatabase(cleanedText);
                }
            }

            var files = documentPart.GetAllParts<DocumentPartFile>();

            if (files != null)
            {
                foreach (var file in files)
                {
                    var cleanedText = CleanFile(file, uri, crawlerResponseModel)
                        .Match(r => r, () => new());
                    if (!string.IsNullOrEmpty(cleanedText.CleanedText))
                        await AddCleanedDataToDatabase(cleanedText);
                }
            }

            if (files == null && articles == null)
            {
                logger.LogWarning(
                    "No articles nor files found. Skipping text clean for: {uri}",
                    uri
                );
            }

            //await PublishToWorkerQueue(inputMessage, uri, cleanedText);
            await MarkDataPipelineProcessed(crawlerResponseModel, uri);

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

    private Option<DataPipelineCleanText> CleanFile(
        DocumentPartFile file,
        string uri,
        TIn crawlerResponseModel
    )
    {
        var fileData = file.FileDataList.Match(c => c, () => new());
        var dataList = fileData
            .SelectMany(f => f.DataStr)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (!dataList.Any())
        {
            logger.LogWarning($"File data does not have any text: {uri}");
            return null;
        }

        StringBuilder text = new();

        foreach (var textData in dataList)
        {
            var data = CleanData(textData);
            text.Append(data);
            text.Append(" ");
        }

        return new DataPipelineCleanText
        {
            DataSource = uri,
            DataSourceType = "media",
            CleanedText = text.ToString().Trim(),
            Uri = uri,
            Id = crawlerResponseModel.Id,
        };
    }

    private Option<DataPipelineCleanText> CleanArticle(
        DocumentPartArticle article,
        string uri,
        TIn crawlerResponseModel
    )
    {
        var content = article.Content.Match(c => c, () => new DocumentPartText(uri));
        var documentPartTexts = content.GetAllParts<DocumentPartText>().ToList();

        if (!documentPartTexts.Any())
        {
            logger.LogWarning($"Article does not have any text: {uri}");
            return null;
        }

        StringBuilder text = new();

        foreach (var documentPartText in documentPartTexts)
        {
            var data = documentPartText.Text.Match(r => r, string.Empty);
            data = CleanData(data);

            text.Append(data);
            text.Append(" ");
        }

        return new DataPipelineCleanText
        {
            DataSource = uri,
            DataSourceType = "media",
            CleanedText = text.ToString().Trim(),
            Uri = uri,
            Id = crawlerResponseModel.Id,
        };
    }

    private static string CleanData(string data)
    {
        data = data.ToLowerInvariant();
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

        var dataBuilder = new StringBuilder(data);
        dataBuilder.Replace(".", " . ");
        dataBuilder.Replace("+", " ");
        dataBuilder.Replace("\n", " ");
        dataBuilder.Replace("headlines", "");
        dataBuilder.Replace("ENSocial media", " ");
        dataBuilder.Replace("In-depth investigations from a changing Europe,in your inbox", " ");
        dataBuilder.Replace("Share this page:", " ");
        dataBuilder.Replace("Sign up for mail updates;", " ");
        dataBuilder.Replace("LinkedIn&nbsp;", " ");
        dataBuilder.Replace("WhatsApp&nbsp;", " ");
        dataBuilder.Replace("Facebook&nbsp;", " ");
        dataBuilder.Replace("X&nbsp;", " ");
        dataBuilder.Replace("&nbsp;", " ");
        dataBuilder.Replace("Share this page on Facebook (opens in a new window)", " ");
        dataBuilder.Replace("Share this page on X (opens in a new window)", " ");
        dataBuilder.Replace("Share this page on X (opens in a new window)", " ");
        dataBuilder.Replace("Share this page on LinkedIn (opens in a new window)", " ");
        dataBuilder.Replace("Sign up for mail updates", " ");

        return dataBuilder.ToString();
    }

    private async Task MarkDataPipelineProcessed(TIn crawlerResponseModel, string uri)
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
