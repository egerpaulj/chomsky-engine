//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2022  Paul Eger

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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Parser.File;
using Crawler.Core.Parser.Xml;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.WebDriver.Core;
using HtmlAgilityPack;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Strategy
{
    public abstract class CrawlerStrategyBase : ICrawlStrategy
    {
        protected readonly IWebDriverService Driver;
        private readonly IDataExtractor dataExtractor;

        // ToDo Do better - inject tracking (Everywhere)
        private Stopwatch _performanceTracker = new Stopwatch();

        protected CrawlerStrategyBase(IWebDriverService driver, IDataExtractor dataExtractor)
        {
            Driver = driver;
            this.dataExtractor = dataExtractor;
        }

        public TryOptionAsync<CrawlResponse> Crawl(Option<Request> request)
        {
            _performanceTracker.Reset();
            _performanceTracker.Start();

            var requestEither = request.ToEitherAsync(
                new CrawlException("Request is empty", ErrorType.RequestError)
            );
            var crawlRequestEither = requestEither.Bind(request =>
                request.CrawlRequest.ToEitherAsync(
                    new CrawlException("Requests' CrawlRequest is empty", ErrorType.RequestError)
                )
            );
            var loadPageEither = crawlRequestEither.Bind(request =>
                request.LoadPageRequest.ToEitherAsync(
                    new CrawlException(
                        "Requests' CrawlRequest' Load Page is empty",
                        ErrorType.RequestError
                    )
                )
            );
            var uriEither = crawlRequestEither.Bind(request =>
                request
                    .LoadPageRequest.Bind(r => r.Uri)
                    .ToEitherAsync(
                        new CrawlException(
                            "Requests' CrawlRequest' Load Page Uri is empty",
                            ErrorType.RequestError
                        )
                    )
            );
            var requestDocumentEither = crawlRequestEither.Bind(request =>
                request.RequestDocument.ToEitherAsync(
                    new CrawlException(
                        "Requests' CrawlRequest's Request Document is empty",
                        ErrorType.RequestError
                    )
                )
            );
            var requestDocumentPartEither = requestDocumentEither.Bind(request =>
                request.RequestDocumentPart.ToEitherAsync(
                    new CrawlException(
                        "Requests' CrawlRequest's Request Document's Document Part is empty",
                        ErrorType.RequestError
                    )
                )
            );
            var correlationId = request
                .Bind(r => r.CrawlRequest)
                .Bind(r => r.CorrelationCrawlId)
                .Match(c => c, () => Guid.NewGuid());

            var continuationStrategy = request.Bind(r => r.CrawlContinuationStrategy);

            var generateResponse = new Func<Document, string, TryOptionAsync<CrawlResponse>>(
                (doc, src) =>
                    crawlRequestEither.ToTryOption().Bind(req => GenerateResponse(req, doc, src))
            );

            string pageSource = string.Empty;

            var crawl = loadPageEither
                .ToTryOption()
                .Bind(request => Driver.LoadPage(request))
                .Bind(s =>
                {
                    pageSource = s;
                    return ParsePageSource(s);
                })
                .Bind(xdoc =>
                    requestDocumentPartEither.ToTryOption().Bind(docPart => docPart.Parse(xdoc))
                )
                .Bind(_ => requestDocumentEither.ToTryOption())
                .Bind(doc => DownloadContent(doc, correlationId))
                .SelectMany(
                    doc => uriEither.ToTryOption().Bind(uri => ProcessAnamolies(doc, uri)),
                    (doc, _) => doc
                )
                .Bind(doc => generateResponse(doc, pageSource))
                .Bind(r => ApplyContinuationStrategy(continuationStrategy, r));

            return crawl;
        }

        private TryOptionAsync<CrawlResponse> ApplyContinuationStrategy(
            Option<ICrawlContinuationStrategy> continuationStrategy,
            Option<CrawlResponse> response
        )
        {
            return async () =>
            {
                if (continuationStrategy.IsSome)
                {
                    await continuationStrategy
                        .ToTryOptionAsync()
                        .Bind(c => c.Apply(response))
                        .Match(
                            u => u,
                            () =>
                                throw new CrawlException(
                                    "Continuation Error",
                                    ErrorType.ContinuationError
                                ),
                            e =>
                                throw new CrawlException(
                                    "Continuation Error",
                                    ErrorType.ContinuationError,
                                    e
                                )
                        );
                }
                return await Task.FromResult(response);
            };
        }

        protected abstract TryOptionAsync<Unit> ProcessAnamolies(
            Option<Document> document,
            Option<string> uri
        );

        protected virtual TryOptionAsync<Document> DownloadContent(
            Document document,
            Guid correlationId
        )
        {
            return async () =>
            {
                System.Console.WriteLine(
                    $"XML Parsing took: {_performanceTracker.ElapsedMilliseconds}ms"
                );
                _performanceTracker.Restart();
                if (!document.DownloadContent.Match(b => b, () => false))
                    return await Task.FromResult(document);

                var fileParts = document
                    .RequestDocumentPart.Bind<IEnumerable<DocumentPartFile>>(d =>
                    {
                        var documentPartFiles = d.GetAllParts<DocumentPartFile>().Distinct();

                        var filesInArticles = d.GetAllParts<DocumentPartArticle>()
                            .Select(f => f.Content.MatchUnsafe(c => c, () => null))
                            .Where(o => o is not null)
                            .SelectMany(dp => dp.GetAllParts<DocumentPartFile>());

                        var filesInTable = d.GetAllParts<DocumentPartTable>()
                            .SelectMany(f =>
                                f.Rows.MatchUnsafe(
                                    rlist =>
                                        rlist.SelectMany(r =>
                                            r.Columns.MatchUnsafe(
                                                c =>
                                                    c.SelectMany(col =>
                                                        col.GetAllParts<DocumentPartFile>()
                                                    ),
                                                () => null
                                            )
                                        ),
                                    () => null
                                )
                            )
                            .Where(o => o is not null);

                        filesInArticles = filesInArticles.Append(filesInTable);

                        return Option<IEnumerable<DocumentPartFile>>.Some(
                            filesInArticles.Append(documentPartFiles)
                        );
                    })
                    .Match(f => f, Enumerable.Empty<DocumentPartFile>());

                var downloadTasks = fileParts
                    .Select(f =>
                        DownloadFiles(
                            f,
                            f.DownloadLinks.Match(f => f, () => new List<DocumentPartLink>()),
                            document.SkipList,
                            correlationId
                        )
                    )
                    .AsParallel()
                    .Select(data => data.Match(r => r, Unit.Default))
                    .ToList();

                await Task.WhenAll(downloadTasks);

                System.Console.WriteLine(
                    $"Downloading Content took: {_performanceTracker.ElapsedMilliseconds}ms"
                );
                _performanceTracker.Restart();

                return await Task.FromResult(document);
            };
        }

        protected TryOptionAsync<CrawlResponse> GenerateResponse(
            CrawlRequest request,
            Document document,
            string source
        )
        {
            return async () =>
            {
                var response = new CrawlResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Uri = request.LoadPageRequest.Bind(r => r.Uri),
                    CrawlerId = request.CrawlId,
                    CorrelationId = request.CorrelationCrawlId,
                    Raw = request.ProvideRaw ? source : null,
                    Result = document,
                    ShouldIndex = request.ShouldIndex,
                };

                return await Task.FromResult(response);
            };
        }

        private TryOptionAsync<HtmlDocument> ParsePageSource(string source)
        {
            return async () =>
            {
                System.Console.WriteLine(
                    $"Load Page and convert to HtmlNode took: {_performanceTracker.ElapsedMilliseconds}ms"
                );
                _performanceTracker.Restart();
                return await Task.FromResult(XmlParser.Parse(source));
            };
        }

        private TryOptionAsync<Unit> DownloadFiles(
            DocumentPartFile filePart,
            IEnumerable<DocumentPartLink> documentPartLinks,
            IEnumerable<string> skipList,
            Guid correlationId
        )
        {
            return async () =>
            {
                if (documentPartLinks.Count() == 0)
                    return Unit.Default;

                var fileDataList = new List<FileData>();

                foreach (var link in documentPartLinks)
                {
                    if (
                        link.Uri.Match(l => skipList.Any(s => l.ToLower().Contains(s)), () => false)
                    )
                    {
                        fileDataList.Add(new FileData { Error = "Matched skip list" });
                        continue;
                    }
                    var fileData = await Driver
                        .Download(
                            new DownloadRequest() { Uri = link.Uri, CorrelationId = correlationId }
                        )
                        .Match(
                            r => r,
                            () => new FileData { Error = "Failed" },
                            ex => new FileData { Error = ex.Message }
                        );

                    await Task.Delay(new Random().Next(1000, 3000));

                    if (fileData.DataBytes.IsSome)
                    {
                        await ExtractTextOrDefault(fileData);
                    }

                    fileDataList.Add(fileData);
                }

                filePart.FileDataList = fileDataList;

                return await Task.FromResult(Unit.Default);
            };
        }

        private async Task<FileData> ExtractTextOrDefault(FileData fileData)
        {
            var data = fileData.DataBytes.Match(
                r => r,
                () => throw new Exception("File data is empty")
            );
            var dataBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(data);

            var fileExtension = fileData.Name.Match(n => new FileInfo(n).Extension, () => "");

            fileExtension = string.IsNullOrEmpty(fileExtension)
                ? fileData.Uri.Match(
                    u => u.ToLowerInvariant().Contains("pdf") ? ".pdf" : ".unknown",
                    () => ".unknown"
                )
                : fileExtension;

            switch (fileExtension)
            {
                case ".pdf":
                    fileData.DataStr = await dataExtractor
                        .ExtractFromPdf(dataBytes)
                        .Match(r => r, data);
                    fileData.DataBytes = null;
                    return fileData;
                case ".docx":
                    fileData.DataStr = await dataExtractor
                        .ExtractFromDocx(dataBytes)
                        .Match(r => r, data);
                    fileData.DataBytes = null;
                    return fileData;
                default:
                    return fileData;
            }
        }
    }
}
