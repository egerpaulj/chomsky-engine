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
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
namespace Crawler.Scheduler.Core
{
    public class UnscheduledUriCrawlJob : IJob
    {
        internal const int IntervalInMinutes = 10;
        private ILogger<UnscheduledUriCrawlJob> _logger;
        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        private readonly IRequestPublisher _requestPublisher;

        public UnscheduledUriCrawlJob(ILogger<UnscheduledUriCrawlJob> logger, ICrawlerConfigurationService crawlerConfiguration, IRequestPublisher requestPublisher)
        {
            _logger = logger;
            _crawlerConfiguration = crawlerConfiguration;
            _requestPublisher = requestPublisher;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await ScheduleUriCrawls().Match(r => r, () => throw new Exception($"Failed to schedule pending Uris"));
        }

        private TryOptionAsync<Unit> ScheduleUriCrawls()
        {
            return _crawlerConfiguration.GetUnscheduledCrawlUriData().Bind(list => Schedule(list));
        }

        private TryOptionAsync<Unit> Schedule(List<CrawlUriDataModel> crawlUriDataModel)
        {
            return async () =>
            {
                await Task.WhenAll(
                    crawlUriDataModel.Select(crawlUri =>
                        _crawlerConfiguration
                            .GetUriData(crawlUri.UriId)
                            .Bind(uriData => _crawlerConfiguration.CreateRequest(uriData.Uri, correlationId: Guid.NewGuid(), crawlUri.Id))
                            .Bind(request => _requestPublisher.PublishRequest(request))
                            .Bind(_ => _crawlerConfiguration.UpdateScheduledTimeUtcNow(crawlUri.Id))
                            .Match(u => { }, () => LogUriError(crawlUri.UriId.ToString()), ex => LogUriError(crawlUri.Id.ToString(), ex))
                    )
                    .ToArray());

                return Unit.Default;
            };

        }

        private void LogUriError(string uri, Exception ex = null)
        {
            var message = $"Failed to schedule UriId: {uri}";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}