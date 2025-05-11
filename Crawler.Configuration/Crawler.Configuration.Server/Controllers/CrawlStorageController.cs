using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Configuration.Core.Requests;
using Crawler.Core.Requests;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microservice.Core.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Crawler.Configuration.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CrawlStorageController : ControllerBase
    {
        private readonly ILogger<CrawlStorageController> _logger;
        private readonly ICrawlerConfigurationService _configurationService;

        public CrawlStorageController(
            ILogger<CrawlStorageController> logger,
            ICrawlerConfigurationService config
        )
        {
            _logger = logger;
            _configurationService = config;
        }

        [Route("savecrawluridata")]
        public async Task SaveCrawlUriData([FromBody] CrawlUriDataModel dataModel)
        {
            await _configurationService
                .Add(dataModel)
                .Match(
                    r => { },
                    () => throw new Exception("Empty result saving crawl uri data"),
                    e => throw new Exception($"Error Saving Crawl Uri data", e)
                );

            return;
        }

        [Route("saveuridata")]
        public async Task SaveUriData([FromBody] UriDataModel dataModel)
        {
            await _configurationService
                .Add(dataModel)
                .Match(
                    r => { },
                    () => throw new Exception("Empty result saving uri data"),
                    e => throw new Exception($"Error Saving Uri data", e)
                );

            return;
        }

        [Route("updatecompletedtime")]
        public async Task UpdateCompletedTime([FromBody] SingleValue id)
        {
            await _configurationService
                .UpdateCompletedTimeUtcNow(Guid.Parse(id.Value))
                .Match(
                    r => { },
                    () => throw new Exception("Empty result updating completed time on Crawl Uri"),
                    e => throw new Exception($"Error updating completed time on Crawl Uri", e)
                );

            return;
        }

        [Route("updatescheduledtime")]
        public async Task UpdateScheduledTime([FromBody] SingleValue id)
        {
            await _configurationService
                .UpdateScheduledTimeUtcNow(Guid.Parse(id.Value))
                .Match(
                    r => { },
                    () => throw new Exception("Empty result updating scheduled time on Crawl Uri"),
                    e => throw new Exception($"Error updating scheduled time on Crawl Uri", e)
                );

            return;
        }

        [Route("storelinks")]
        public async Task StoreLinks([FromBody] ConfigurationRestStoreLinks request)
        {
            await _configurationService
                .StoreDetectedUrls(request.Links, HttpContext.GetCorrelationId())
                .Match(
                    r => { },
                    () => throw new Exception("Empty result storing links"),
                    e => throw new Exception($"Error storing links", e)
                );

            return;
        }

        private TryOptionAsync<CrawlRequest> GetRequest(
            ConfigurationCrawlRequest request,
            out Uri uri
        )
        {
            uri = request.Uri.Match(
                u => new Uri(u),
                () => throw new Exception("Request Uri is empty")
            );
            var crawlId = request.CrawlId.Match(g => g, () => Guid.Empty);

            return _configurationService.CreateRequest(
                uri.AbsoluteUri,
                HttpContext.GetCorrelationId(),
                crawlId
            );
        }
    }
}
