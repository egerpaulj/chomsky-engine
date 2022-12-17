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
using Crawler.RequestHandling.Core;
using LanguageExt;
using Crawler.DataModel;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Crawler.Scheduler.Core
{
    public class UriCollectionJob : IJob
    {
        internal const string JobDataUriKey = "Uri";
        internal const string JobDataIdKey = "Id";
        private readonly ICrawlerConfigurationService _crawlerConfiguration;
        private readonly IRequestPublisher _requestPublisher;
        private readonly ILogger<UriCollectionJob> _logger;

        public UriCollectionJob(ILogger<UriCollectionJob> logger, ICrawlerConfigurationService crawlerConfiguration, IRequestPublisher requestPublisher)
        {
            _crawlerConfiguration = crawlerConfiguration;
            _requestPublisher = requestPublisher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var uri = context.MergedJobDataMap.GetString(JobDataUriKey);
            var id = context.MergedJobDataMap.GetGuid(JobDataIdKey);
            await Schedule(uri, id ).Match(r => r, () => throw new Exception($"Failed to schedule Url Collection for Uri: {uri}"));
        }

        private TryOptionAsync<Unit> Schedule(string uri, Guid id)
        {
            return async () =>
            {
                await _crawlerConfiguration
                            .GetCollectorCrawlRequest(uri)
                            .Bind(request => _requestPublisher.PublishRequest(request.Map(uri, correlationId: Guid.NewGuid(), crawlId: id)))
                            .Match(u => { }, () => LogCollectionError(uri), ex => LogCollectionError(uri, ex));

                return Unit.Default;
            };
        }

        private void LogCollectionError(string uri, Exception ex = null)
        {
            var message = $"Failed to schedule URI Collection: {uri}";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}