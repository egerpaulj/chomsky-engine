using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Configuration.Core.Requests;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microservice.Core.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Crawler.Configuration.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CrawlConfigurationController : ControllerBase
    {
        private readonly ILogger<CrawlConfigurationController> _logger;
        private readonly ICrawlerConfigurationService _configurationService;

        public CrawlConfigurationController(
            ILogger<CrawlConfigurationController> logger,
            ICrawlerConfigurationService config
        )
        {
            _logger = logger;
            _configurationService = config;
        }

        [Route("getcrawlrequest")]
        public async Task<ActionResult<CrawlRequest>> GetCrawlRequest(
            [FromBody] ConfigurationCrawlRequest request
        )
        {
            return await GetRequest(request, out var uri)
                .Match(
                    r => r,
                    () =>
                        throw new Exception(
                            $"Result was empty for CrawlConfiguration CrawlRequest for: {uri.AbsoluteUri}"
                        ),
                    e =>
                        throw new Exception(
                            $"Error obtaining configuration for: {uri.AbsoluteUri}",
                            e
                        )
                );
        }

        [Route("getcollectorcrawlrequest")]
        public async Task<ActionResult<CrawlRequestModel>> GetCollectorCrawlRequest(
            [FromBody] SingleValue uri
        )
        {
            return await _configurationService
                .GetCollectorCrawlRequest(uri.Value)
                .Match(
                    r => r,
                    () =>
                        throw new Exception(
                            $"Result was empty for crawl request model. URI: {uri.Value}"
                        ),
                    e =>
                        throw new Exception(
                            $"Error getting crawl request model. URI: {uri.Value}",
                            e
                        )
                );
        }

        [Route("getuiactions")]
        public async Task<List<UiAction>> GetUiActions([FromBody] ConfigurationCrawlRequest request)
        {
            var uri = request.Uri.Match(
                u => new Uri(u),
                () => throw new Exception("Request Uri is empty")
            );
            var crawlId = request.CrawlId.Match(g => g, () => Guid.Empty);

            return await _configurationService
                .GetUiActions(uri.AbsoluteUri, HttpContext.GetCorrelationId(), crawlId)
                .Match(
                    r => r,
                    () => new List<UiAction>(),
                    e => throw new Exception($"Error obtaining UiActions for: {uri}", e)
                );
        }

        [Route("getdocumentpart")]
        public async Task<DocumentPart> GetDocumentPart(
            [FromBody] ConfigurationCrawlRequest request
        )
        {
            var uri = request.Uri.Match(
                u => new Uri(u),
                () => throw new Exception("Request Uri is empty")
            );
            var crawlId = request.CrawlId.Match(g => g, () => Guid.Empty);

            return await _configurationService
                .GetExpectedDocumentPart(uri.AbsoluteUri, HttpContext.GetCorrelationId(), crawlId)
                .Match(
                    r => r,
                    () => new DocumentPartAutodetect(uri.AbsoluteUri),
                    e => throw new Exception($"Error obtaining UiActions for: {uri}", e)
                );
        }

        [Route("getunscheduledcrawluri")]
        public async Task<List<CrawlUriDataModel>> GetUnscheduledCrawlUri()
        {
            return await _configurationService
                .GetUnscheduledCrawlUriData()
                .Match(
                    r => r,
                    () => new List<CrawlUriDataModel>(),
                    e => throw new Exception($"Error obtaining Unscheduled Crawl Uri", e)
                );
        }

        [Route("getcollectorsourcedata")]
        public async Task<List<UriDataModel>> GetCollectorSourceData()
        {
            return await _configurationService
                .GetCollectorUri()
                .Match(
                    r => r,
                    () => new List<UriDataModel>(),
                    e => throw new Exception($"Error obtaining Collector Source data", e)
                );
        }

        [Route("getperiodicuridata")]
        public async Task<List<UriDataModel>> GetPeriodicUriData()
        {
            return await _configurationService
                .GetPeriodicUri()
                .Match(
                    r => r,
                    () => new List<UriDataModel>(),
                    e => throw new Exception($"Error obtaining Periodic Data Model", e)
                );
        }

        [Route("geturidata")]
        public async Task<UriDataModel> GetUriData([FromBody] SingleValue id)
        {
            return await _configurationService
                .GetUriData(Guid.Parse(id.Value))
                .Match(
                    r => r,
                    () => throw new Exception($"Empty result obtaining Uri Data"),
                    e => throw new Exception($"Error obtaining Uri Data", e)
                );
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
