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
using Crawler.Core;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel;
using LanguageExt;

namespace Crawler.DataModel
{
    public static class DataModelMapper
    {
        internal static bool ShouldIndexLinks = false;
        public static CrawlResponseModel Map(this CrawlResponse response)
        {
            return new CrawlResponseModel
            {
                CorrelationId = response.CorrelationId,
                CrawlerId = response.CrawlerId,
                Result = response.Result,
                Raw = response.Raw
            };
        }

        public static CrawlResponseModel Map(this CrawlRequest request)
        {
            return new CrawlResponseModel
            {
                CorrelationId = request.CorrelationCrawlId,
                CrawlerId = request.CrawlId,
                Result = request.RequestDocument,
                Error = request.Error.Bind<string>(e => $"{e.Message}\n{e.StackTrace}"),
                ErrorUri = request.LoadPageRequest.Bind<string>(r => r.Uri)
            };
        }

        public static CrawlRequest Map(this CrawlRequestModel model, Option<string> uri, Guid correlationId, Guid crawlId)
        {
            return new CrawlRequest
            {
                ContinuationStrategy = model.ContinuationStrategyDefinition,
                CorrelationCrawlId = correlationId,
                Id = model.Id,
                LoadPageRequest = new LoadPageRequest()
                {
                    Uri = uri,
                    UserActions = model.UiActions,
                    CorrelationId = correlationId
                },
                RequestDocument = new Document
                {
                    RequestDocumentPart = model.DocumentPartDefinition,
                    DownloadContent = model.ShouldDownloadContent
                },
                ProvideRaw = model.ShouldProvideRawSource,
                CrawlId = crawlId
            };
        }

        public static CrawlResponseIndexModel MapToIndex(this CrawlResponse response)
        {
            var resultDocumentPart = response.Result.Bind(r => r.RequestDocumentPart).Match(d => d, () => throw new Exception("No Document part in result"));

            var article = DocumentPartExtensions.GetAllParts<DocumentPartArticle>(resultDocumentPart).FirstOrDefault();
            var title = string.Empty;

            List<HyperLink> links = new List<HyperLink>();
            string tables = string.Empty;
            string text = string.Empty;

            if (article != null)
            {
                title = article.Title.Bind(t => t.Text).Match(t => t, () => string.Empty);

                var articleContentPart = article.Content.MatchUnsafe(c => c, () => null);

                if (articleContentPart != null)
                {
                    links = GetLinks(articleContentPart);
                    tables = GetTables(articleContentPart);
                    text = GetText(articleContentPart);
                }
               
            }

            links.AddRange(GetLinks(resultDocumentPart));
            tables.Append(GetTables(resultDocumentPart));
            text.Append(GetText(resultDocumentPart));

            return new CrawlResponseIndexModel
                {
                    Timestamp = $"{DateTime.UtcNow:yyyy.MM.dd:HH:mm:ss}",
                    CorrelationId = response.CorrelationId,
                    CrawlerId = response.CrawlerId,
                    Title = title,
                    Text = text,
                    Links = links,
                    Tables = tables,
                    Raw = response.Raw
                };
        }

        public static string GetText(DocumentPart resultDocumentPart)
        {
            return DocumentPartExtensions.GetAllParts<DocumentPartText>(resultDocumentPart).SelectMany(t => t.Text.Match(t => t, () => string.Empty) + "\n").ConvertToString();
        }

        public static string GetTables(DocumentPart articleContentPart)
        {
            return DocumentPartExtensions.GetAllParts<DocumentPartTable>(articleContentPart).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();
        }

        private static List<HyperLink> GetLinks(DocumentPart articleContentPart)
        {
            if(!ShouldIndexLinks)
                return new List<HyperLink>();

            var links =  DocumentPartExtensions.GetAllParts<DocumentPartLink>(articleContentPart);
            var fileLinks = DocumentPartExtensions.GetAllParts<DocumentPartFile>(articleContentPart).SelectMany(f => f.DownloadLinks.Match(l => l, () => new List<DocumentPartLink>()));

            links = links.Append(fileLinks);

            return links.Select(l => new HyperLink
            {
                Name = l.Text.Match(t => t, () => string.Empty),
                Uri = l.Uri.Match(u => u, () => string.Empty)
            }).ToList();
        }

        public static CrawlResponseIndexModel MapToIndex(this CrawlRequest request)
        {
            return new CrawlResponseIndexModel
            {
                CrawlerId = request.CrawlId,
                Text = request.Error.Bind<string>(e => $"{e.Message}\n{e.StackTrace}"),
                ErrorUri = request.LoadPageRequest.Bind<string>(r => r.Uri)
            };
        }
    }
}