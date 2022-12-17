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
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core.Metrics;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.RequestHandling.Core;
using Crawler.Stategies.Core;
using Crawler.WebDriver.Core;
using LanguageExt;

namespace Crawler.Strategies.General
{
    public class CrawlStrategiesMapper : ICrawlStrategyMapper
    {


        private readonly ICrawlStrategy _genericStrategy;

        private readonly ICrawlContinuationStrategy _crawlAllContStrategy;
        private readonly ICrawlContinuationStrategy _crawlDomainOnlyContStrategy;

        private readonly ICrawlContinuationStrategy _crawlTrackLinksContStrategy;

        private readonly Dictionary<string, ICrawlStrategy> _hostToStrategyMapper;
        private readonly Dictionary<string, ICrawlStrategy> _uriToStrategyMapper;

        private readonly Dictionary<string, ICrawlContinuationStrategy> _hostToContStrategyMapper;
        private readonly Dictionary<string, ICrawlContinuationStrategy> _uriToContStrategyMapper;

        public CrawlStrategiesMapper(
            ICrawlerConfigurationService configuration, 
            IWebDriverService webDriver, 
            IMetricRegister metricRegister)
        {
            _crawlAllContStrategy = new CrawlAllContinuationStrategy(configuration);
            _crawlDomainOnlyContStrategy = new CrawlDomainOnlyContinuationStrategy(configuration);
            _crawlTrackLinksContStrategy = new TrackLinksContinuationStrategy(configuration);

            _genericStrategy = new CrawlerStrategyGeneric(webDriver, metricRegister);

            // ToDo Non custom continuation strategy mappings in Database (and allow Factory injection)
            _hostToContStrategyMapper = new Dictionary<string, ICrawlContinuationStrategy>
            {
                {"google.com", _crawlTrackLinksContStrategy}
            };

            _uriToContStrategyMapper = new Dictionary<string, ICrawlContinuationStrategy>
            {
                //{"stock.com/specificData", _someSpecificStrategyForThisParticularUriThatIsDifferentFromGeneralHost}
            };

            _hostToStrategyMapper = new Dictionary<string, ICrawlStrategy>
            {
                {"google.com", _genericStrategy}
            };

            _uriToStrategyMapper = new Dictionary<string, ICrawlStrategy>
            {
                //{"stock.com/specificData", _someSpecificStrategyForThisParticularUriThatIsDifferentFromGeneralHost}
            };
        }

        public TryOptionAsync<ICrawlStrategy> GetCrawlStrategy(Option<CrawlRequest> crawlRequest)
        {
            return crawlRequest
                .Bind( r => r.LoadPageRequest)
                .Bind(l => l.Uri)
                .ToTryOptionAsync()
                .Bind<string, ICrawlStrategy>(u => 
                    async () => await Task.FromResult(Option<ICrawlStrategy>.Some(GetStrategy(u))));
        }


        public TryOptionAsync<ICrawlContinuationStrategy> GetContinuationStrategy(Option<CrawlRequest> crawlRequest)
        {
            return crawlRequest.ToTryOptionAsync().Bind(u => MapRequestToContinuationStrategy(u));
        }

        private TryOptionAsync<ICrawlContinuationStrategy> MapRequestToContinuationStrategy(CrawlRequest crawlRequest)
        {
            return async () =>
            {
                var contStrategy = crawlRequest.ContinuationStrategy.Match(c => c, CrawlContinuationStrategy.None);

                switch (contStrategy)
                {
                    case CrawlContinuationStrategy.None:
                        return await Task.FromResult(Option<ICrawlContinuationStrategy>.None);
                    case CrawlContinuationStrategy.All:
                        return await Task.FromResult(Option<ICrawlContinuationStrategy>.Some(_crawlAllContStrategy));
                    case CrawlContinuationStrategy.DomainOnly:
                        return await Task.FromResult(Option<ICrawlContinuationStrategy>.Some(_crawlDomainOnlyContStrategy));
                    case CrawlContinuationStrategy.TrackLinksOnly:
                        return await Task.FromResult(Option<ICrawlContinuationStrategy>.Some(_crawlTrackLinksContStrategy));
                    case CrawlContinuationStrategy.Custom:
                        return await Task.FromResult(Option<ICrawlContinuationStrategy>.Some(GetCustomContinuationStrategy(crawlRequest.LoadPageRequest.Bind(p => p.Uri))));
                    default:
                        return await Task.FromResult(Option<ICrawlContinuationStrategy>.None);
                }

            };
        }

        private ICrawlContinuationStrategy GetCustomContinuationStrategy(Option<string> uri)
        {
            var u = uri.Match(u => u, () => throw new CrawlStrategyException("Uri is empty"));

            if (_uriToContStrategyMapper.ContainsKey(u))
            {
                return _uriToContStrategyMapper[u];
            }

            var quri = new Uri(u);
            if (_hostToContStrategyMapper.ContainsKey(quri.Host))
            {
                return _hostToContStrategyMapper[quri.Host];
            }

            throw new CrawlStrategyException($"Custom Crawl Continuation Strategy not defined for Uri: {u}");
        }

        private ICrawlStrategy GetStrategy(Option<string> uri)
        {
            var u = uri.Match(u => u, () => throw new CrawlStrategyException("Uri is empty"));

            if (_uriToStrategyMapper.ContainsKey(u))
            {
                return _uriToStrategyMapper[u];
            }

            var quri = new Uri(u);
            if (_hostToStrategyMapper.ContainsKey(quri.Host))
            {
                return _hostToStrategyMapper[quri.Host];
            }

            return _genericStrategy;
        }
    }
}