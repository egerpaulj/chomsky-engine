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
using Caching.Core;
using LanguageExt;

namespace Crawler.Core.Cache
{
    public class CrawlerCache : ICache
    {
        private readonly ICacheProvider _cacheProvider;

        private const string CrawlKey = "Crawl_{0}";

        private const string LastRequestKey = "LastRequest_{0}";
        private const string ActiveDownloadKey = "ActiveDownload_{0}";

        private const double LastRequestExpiryInSeconds = 15;
        
        public CrawlerCache(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public TryOptionAsync<DateTime> GetLastRequestTime(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind( u => _cacheProvider.Get<DateTime>(string.Format(LastRequestKey, u)));
        }

        public TryOptionAsync<Unit> StoreLastRequest(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind(u => _cacheProvider.StoreInCache(string.Format(LastRequestKey, u), DateTime.UtcNow, LastRequestExpiryInSeconds));
        }

        public TryOptionAsync<Unit> StoreCrawlEnded(Option<Crawl> crawl)
        {
            return crawl
            .Bind(c => c.CrawlRequest)
            .Bind(c => c.CrawlId)
            .ToTryOptionAsync()
                .Bind(id => 
                    _cacheProvider.StoreInCache(string.Format(CrawlKey, id.ToString()), $"Crawl InProgress: {GetDateTime}") );
        }

        public TryOptionAsync<Unit> UpdateCrawlCompleted(Option<Guid> crawlId)
        {
            return crawlId
            .ToTryOptionAsync()
            .Bind(id => 
                    _cacheProvider.StoreInCache(string.Format(CrawlKey, id.ToString()), $"Crawl Ended: {GetDateTime}") );
        }

        public TryOptionAsync<bool> IsActiveDownload(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind(u => 
                    _cacheProvider.Get<bool>(string.Format(ActiveDownloadKey, u)) );
        }

        public TryOptionAsync<Unit> SetActiveDownload(Option<string> uri, bool downloadState)
        {
            return uri
            .ToTryOptionAsync()
            .Bind( 
                u => _cacheProvider.StoreInCache(string.Format(ActiveDownloadKey, u), downloadState));
        }

        private static string GetDateTime => DateTime.UtcNow.ToString(ICacheProvider.DateTimeFormat);
    }
}