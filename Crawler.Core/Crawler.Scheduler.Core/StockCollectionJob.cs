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
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Crawler.Scheduler.Core
{
    public class StockCollectionJob(
        IConfigurationRepository configurationRepository,
        IRequestPublisher requestPublisher,
        ILogger<UriCollectionJob> logger
    ) : IJob
    {
        private static Counter counter = Metrics.CreateCounter(
            $"job_stock_collection",
            "stock uris",
            "context"
        );

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation($"Running Stock Collection job");
            await Schedule()
                .Match(_ => { }, () => throw new Exception($"Failed to schedule stock collection"));
        }

        private TryOptionAsync<Unit> Schedule()
        {
            return async () =>
            {
                var corrId = Guid.NewGuid();

                foreach (var page in Enumerable.Range(1, 1742))
                {
                    var uri =
                        $"https://www.londonstockexchange.com/live-markets/market-data-dashboard/price-explorer?page={page}";
                    Console.WriteLine($"Processing: {uri}");

                    await configurationRepository
                        .GetCrawlRequest(uri)
                        .Bind(request =>
                            requestPublisher.PublishRequest(
                                request.Map(
                                    uri,
                                    correlationId: corrId,
                                    crawlId: Guid.NewGuid(),
                                    isAdhoc: true
                                )
                            )
                        )
                        .Match(_ => { }, () => LogStockError(uri), ex => LogStockError(uri, ex));counter.WithLabels($"failed").Inc();

                    counter.WithLabels($"published").Inc();
                }

                return Unit.Default;
            };
        }

        private void LogStockError(string uri, Exception ex = null)
        {
            counter.WithLabels($"failed").Inc();
            var message = $"Failed to schedule stock uri: {uri}";
            if (ex == null)
                logger.LogError(message);
            else
                logger.LogError(ex, message);
        }
    }
}
