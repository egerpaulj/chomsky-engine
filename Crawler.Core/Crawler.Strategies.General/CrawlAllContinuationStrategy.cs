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
using Crawler.Configuration.Core;
using Crawler.Core;
using Crawler.Core.Management;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.RequestHandling.Core;
using LanguageExt;

namespace Crawler.Stategies.Core
{
    public class CrawlAllContinuationStrategy : ICrawlContinuationStrategy
    {
        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        public CrawlAllContinuationStrategy(ICrawlerConfigurationService configuration)
        {
            _crawlerConfiguration = configuration;
        }

        public TryOptionAsync<Unit> Apply(Option<CrawlResponse> response)
        {
            var correlationId = response.Bind(r => r.CorrelationId).Match(g =>g, Guid.NewGuid());

            return response
            .Bind(r => r.Result)
            .Bind(r => r.RequestDocumentPart)
            .ToTryOptionAsync()
            .Bind<DocumentPart, List<DocumentPartLink>>(dp => GetDocumentPartLinks(dp))
            .Bind<List<DocumentPartLink>, Unit>(links => StoreUriList(links, correlationId));
        }

        protected virtual IEnumerable<DocumentPartLink> GetRelevantDocumentPartLinks(DocumentPart documentPart)
        {
            var links = documentPart.GetAllParts<DocumentPartLink>().ToList();

            if (documentPart is DocumentPartAutodetect)
            {
                var fileLinks = documentPart.GetAllParts<DocumentPartFile>().SelectMany(a =>
                {
                    return a.DownloadLinks.Match(l => l, () => new List<DocumentPartLink>());
                });
                if (fileLinks.Any())
                    links.AddRange(fileLinks);
            }

            return links;
        }

        private TryOptionAsync<List<DocumentPartLink>> GetDocumentPartLinks(DocumentPart documentPart)
        {
            return async () =>
            {
                IEnumerable<DocumentPartLink> links = GetRelevantDocumentPartLinks(documentPart);

                return await Task.FromResult(links.ToList());
            };
        }


        private TryOptionAsync<Unit> StoreUriList(List<DocumentPartLink> documentPartLinks, Guid correlationId)
        {
            return _crawlerConfiguration.StoreDetectedUrls(documentPartLinks, correlationId);
        }
    }
}