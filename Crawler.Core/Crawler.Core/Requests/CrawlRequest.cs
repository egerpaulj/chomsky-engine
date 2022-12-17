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
using Crawler.Core.Parser;
using LanguageExt;

namespace Crawler.Core.Requests
{
    public enum CrawlContinuationStrategy
    {
        None,
        All,
        DomainOnly,
        TrackLinksOnly,
        Custom
    }

    public class CrawlRequest
    {
        public Option<Guid> Id { get; set; }
        public Option<Guid> CrawlId { get; set; }
        public Option<Guid> CorrelationCrawlId { get; set; }
        public Option<CrawlContinuationStrategy> ContinuationStrategy { get; set; }
        public Option<LoadPageRequest> LoadPageRequest { get; set; }
        public Option<Document> RequestDocument { get; set; }
        public bool ProvideRaw { get; set; }

        public Option<Exception> Error { get; set; }

        public string GetBriefSummary()
        {
            var crawlId = CrawlId.Match(c => c.ToString("N"), () => string.Empty);
            var correlationID = CorrelationCrawlId.Match(c => c.ToString("N"), () => string.Empty);
            var document = RequestDocument.Match(r => r.GetBriefSummary(), () => string.Empty);

            return $"CRAWL REQUEST:\n Id: {crawlId}, CorrelationId: {correlationID} \n {document}";
        }
    }
}