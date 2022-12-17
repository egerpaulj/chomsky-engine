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
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using LanguageExt;

namespace Crawler.Strategies.General
{
    public class TrackLinksContinuationStrategy : ICrawlContinuationStrategy
    {
        private readonly ICrawlerConfigurationService _crawlerConfiguration;
        public TrackLinksContinuationStrategy(ICrawlerConfigurationService crawlerConfiguration)
        {
            _crawlerConfiguration = crawlerConfiguration;
        }
        
        public TryOptionAsync<Unit> Apply(Option<CrawlResponse> response)
        {
            var shouldDownloadContent = response.Bind(r => r.Result).Bind(r => r.DownloadContent).Match(d => d, false);
            var correlationId = response.Bind(r => r.CorrelationId).Match(g =>g, Guid.NewGuid());

            return response
            .Bind(r => r.Result)
            .Bind(r => r.RequestDocumentPart)
            .ToTryOptionAsync()
            .Bind<DocumentPart, List<DocumentPartLink>>(dp => GetDocumentPartLinks(dp))
            .Bind<List<DocumentPartLink>, Unit>(links => _crawlerConfiguration.StoreDetectedUrls(links, correlationId));
        }

        protected virtual IEnumerable<DocumentPartLink> GetRelevantDocumentPartLinks(DocumentPart documentPart)
        {
            return documentPart.GetAllParts<DocumentPartLink>();
        }

        private TryOptionAsync<List<DocumentPartLink>> GetDocumentPartLinks(DocumentPart documentPart)
        {
            return async () =>
            {
                IEnumerable<DocumentPartLink> links = GetRelevantDocumentPartLinks(documentPart);

                return await Task.FromResult(links.ToList());
            };
        }
    }
}