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
using System.Linq;
using System.Threading.Tasks;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.WebDriver.Core;
using LanguageExt;
using Crawler.Core.Parser.Xml;
using HtmlAgilityPack;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Strategy
{
    public abstract class CrawlerStrategyBase : ICrawlStrategy
    {
        protected readonly IWebDriverService Driver;

        // ToDo Do better - inject tracking (Everywhere)
        private Stopwatch _performanceTracker = new Stopwatch();

        protected CrawlerStrategyBase(IWebDriverService driver)
        {
            Driver = driver;
        }

        public TryOptionAsync<CrawlResponse> Crawl(Option<Request> request)
        {
            _performanceTracker.Reset();
            _performanceTracker.Start();

            var requestEither = request.ToEitherAsync(new CrawlException("Request is empty", ErrorType.RequestError));
            var crawlRequestEither = requestEither.Bind(request => request.CrawlRequest.ToEitherAsync(new CrawlException("Requests' CrawlRequest is empty", ErrorType.RequestError)));
            var loadPageEither = crawlRequestEither.Bind(request => request.LoadPageRequest.ToEitherAsync(new CrawlException("Requests' CrawlRequest' Load Page is empty", ErrorType.RequestError)));
            var uriEither = crawlRequestEither.Bind(request => request.LoadPageRequest.Bind(r => r.Uri).ToEitherAsync(new CrawlException("Requests' CrawlRequest' Load Page Uri is empty", ErrorType.RequestError)));
            var requestDocumentEither = crawlRequestEither.Bind(request => request.RequestDocument.ToEitherAsync(new CrawlException("Requests' CrawlRequest's Request Document is empty", ErrorType.RequestError)));
            var requestDocumentPartEither = requestDocumentEither.Bind(request => request.RequestDocumentPart.ToEitherAsync(new CrawlException("Requests' CrawlRequest's Request Document's Document Part is empty", ErrorType.RequestError)));
            var correlationId = request.Bind(r => r.CrawlRequest).Bind(r => r.CorrelationCrawlId).Match(c => c, () => Guid.NewGuid());

            var continuationStrategy = request.Bind(r => r.CrawlContinuationStrategy);

            var generateResponse = new Func<Document, TryOptionAsync<CrawlResponse>>(
                doc => crawlRequestEither
                            .ToTryOption()
                            .Bind(req => GenerateResponse(req, doc)));

            var crawl =
                loadPageEither.ToTryOption()
                .Bind(request => Driver.LoadPage(request))
                .Bind(s => ParsePageSource(s))
                .Bind(xdoc => requestDocumentPartEither.ToTryOption().Bind(docPart => docPart.Parse(xdoc)))
                .Bind(_ => requestDocumentEither.ToTryOption())
                .Bind(doc => DownloadContent(doc, correlationId))
                .SelectMany(doc => uriEither.ToTryOption().Bind(uri => ProcessAnamolies(doc, uri)), (doc, _) => doc)
                .Bind(doc => generateResponse(doc))
                .Bind(r => ApplyContinuationStrategy(continuationStrategy, r));

            return crawl;
        }

        private TryOptionAsync<CrawlResponse> ApplyContinuationStrategy(Option<ICrawlContinuationStrategy> continuationStrategy, Option<CrawlResponse> response)
        {
            return async () =>
            {
                if(continuationStrategy.IsSome)
                {
                    await continuationStrategy.ToTryOptionAsync()
                    .Bind(c => c.Apply(response))
                    .Match(u => u, () => throw new CrawlException("Continuation Error", ErrorType.ContinuationError), e => throw new CrawlException("Continuation Error", ErrorType.ContinuationError, e));
                }
                return await Task.FromResult(response);
            };
        }


        protected abstract TryOptionAsync<Unit> ProcessAnamolies(Option<Document> document, Option<string> uri);

        protected virtual TryOptionAsync<Document> DownloadContent(Document document, Guid correlationId)
        {
            return async () =>
            {
                System.Console.WriteLine($"XML Parsing took: {_performanceTracker.ElapsedMilliseconds}ms");
                _performanceTracker.Restart();
                if (!document.DownloadContent.Match(b => b, () => false))
                    return await Task.FromResult(document);

                var fileParts = document.RequestDocumentPart.Bind<IEnumerable<DocumentPartFile>>(d =>
                {
                    var documentPartFiles = d.GetAllParts<DocumentPartFile>().Distinct();

                    var filesInArticles = d.GetAllParts<DocumentPartArticle>()
                        .Select(f => f.Content.MatchUnsafe(c => c, () => null))
                        .Where(o => o is not null)
                        .SelectMany(dp => dp.GetAllParts<DocumentPartFile>());

                    var filesInTable = d.GetAllParts<DocumentPartTable>()
                        .SelectMany(f =>
                            f.Rows.MatchUnsafe
                                (rlist => rlist.SelectMany(r => r.Columns
                                     .MatchUnsafe(c => c.SelectMany(col =>
                                          col.GetAllParts<DocumentPartFile>()), () => null)), () => null))
                        .Where(o => o is not null);

                    filesInArticles = filesInArticles.Append(filesInTable);

                    return Option<IEnumerable<DocumentPartFile>>.Some(filesInArticles.Append(documentPartFiles));
                }).Match(f => f, Enumerable.Empty<DocumentPartFile>());

                var downloadTasks = fileParts
                    .Select(f => DownloadFiles(f, f.DownloadLinks.Match(f => f, () => new List<DocumentPartLink>()), correlationId))
                    .AsParallel()
                    .Select(data => data.Match(r => r, Unit.Default))
                    .ToList();

                await Task.WhenAll(downloadTasks);

                System.Console.WriteLine($"Downloading Content took: {_performanceTracker.ElapsedMilliseconds}ms");
                _performanceTracker.Restart();

                return await Task.FromResult(document);
            };
        }

        protected TryOptionAsync<CrawlResponse> GenerateResponse(CrawlRequest request, Document document)
        {
            return async () =>
            {
                var response = new CrawlResponse
                {
                    CrawlerId = request.CrawlId,
                    CorrelationId = request.CorrelationCrawlId,
                    // ToDo Change to string
                    Raw = request.ProvideRaw ? document.XmlDocument.ToString() : null,
                    Result = document
                };

                return await Task.FromResult(response);
            };
        }

        private TryOptionAsync<HtmlDocument> ParsePageSource(string source)
        {
            return async () =>
            {
                System.Console.WriteLine($"Load Page and convert to HtmlNode took: {_performanceTracker.ElapsedMilliseconds}ms");
                _performanceTracker.Restart();
                return await Task.FromResult(XmlParser.Parse(source));
            };
        }

        private TryOptionAsync<Unit> DownloadFiles(DocumentPartFile filePart, IEnumerable<DocumentPartLink> documentPartLinks, Guid correlationId)
        {
            return async () =>
            {
                var downloadFuncs = documentPartLinks.Select(link => Driver.Download(new DownloadRequest() { Uri = link.Uri, CorrelationId = correlationId }));

                var fileDataTasks = downloadFuncs
                                    .AsParallel()
                                    .Select(data => data.MatchUnsafe(d => d, null, ex => null))
                                    .ToList();

                var fileData = await Task.WhenAll(fileDataTasks);

                filePart.FileDataList = fileData.Where(f => f != null).ToList();

                return await Task.FromResult(Unit.Default);
            };
        }
    }
}
