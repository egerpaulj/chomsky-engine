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
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Crawler.Scheduler.Core
{
    
    public class PeriodUriCrawlJob : IJob
    {
        private ILogger<PeriodUriCrawlJob> _logger;
        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        public PeriodUriCrawlJob(ILogger<PeriodUriCrawlJob> logger, ICrawlerConfigurationService crawlerConfiguration)
        {
            _logger = logger;
            _crawlerConfiguration = crawlerConfiguration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var uri = context.MergedJobDataMap.GetString(UriCollectionJob.JobDataUriKey);
            var id = context.MergedJobDataMap.GetGuid(UriCollectionJob.JobDataIdKey);

            await Schedule(uri, id ).Match(r => r, () => throw new Exception($"Failed to schedule Periodic Uri: {uri}"));
        }

        private TryOptionAsync<Unit> Schedule(string uri, Guid uriId)
        {
            return async () =>
            {
                await _crawlerConfiguration
                .Add(new CrawlUriDataModel
                        {
                            UriId = uriId,
                        })
                .Match(r => {}, () => LogCollectionError(uri), ex => LogCollectionError(uri, ex));
                return Unit.Default;
            };
        }

        private void LogCollectionError(string uri, Exception ex = null)
        {
            var message = $"Failed to schedule Uri: {uri}";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}