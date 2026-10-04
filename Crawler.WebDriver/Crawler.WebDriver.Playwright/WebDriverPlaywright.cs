using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Exceptions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Crawler.WebDriver.Playwright;

public class WebDriverPlaywright : IWebDriverDownloader
{
    private readonly ILogger<WebDriverPlaywright> logger;
    private readonly IWebDriverMetrics webDriverMetrics;
    private readonly bool isHeadless;

    public WebDriverPlaywright(IWebDriverMetrics metrics, ILogger<WebDriverPlaywright> logger)
        : this(metrics, logger, isHeadless: false) { }

    public WebDriverPlaywright(
        IWebDriverMetrics metrics,
        ILogger<WebDriverPlaywright> logger,
        bool isHeadless
    )
    {
        this.logger = logger;
        this.webDriverMetrics = metrics;
        this.isHeadless = isHeadless;
    }

    private async Task<PlaywrightPage> CreateNewPage()
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await playwright.Firefox.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = false,
                // Args = ["--headless=new"]
            }
        );
        var context = await browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                AcceptDownloads = true,
                Locale = "en-US",
                TimezoneId = "Europe/Paris",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                UserAgent =
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["accept-language"] = "en-US,en;q=0.9",
                    ["accept-encoding"] = "gzip, deflate, br",
                },
            }
        );
        return new PlaywrightPage(context.APIRequest, await context.NewPageAsync());
    }

    public TryOptionAsync<FileData> Download(Option<DownloadRequest> downloadRequest)
    {
        return async () =>
        {
            var uriStr = downloadRequest
                .Bind(r => r.Uri)
                .Match(r => r, () => throw new DownloadException("Uri is empty"));
            var correlationId = downloadRequest
                .Bind(r => r.CorrelationId)
                .Match(c => c, () => Guid.NewGuid());

            var uri = new Uri(uriStr);
            var fileName = Path.GetFileName(uri.LocalPath);

            try
            {
                var playwrightPage = await CreateNewPage();

                await playwrightPage.Page.GotoAsync(
                    $"https://{uri.Host}",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 }
                );

                // Check for clearance passed in cookies

                // var pageresponse = await playwrightPage.Page.GotoAsync(
                //     uriStr,
                //     new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 }
                // );

                // if (!pageresponse!.Ok)
                //     throw new Exception("Failed to download. Response is NotOk");

                var response = await playwrightPage.APIRequestContext.GetAsync(
                    uriStr
                // ,
                // new APIRequestContextOptions
                // {
                //     Headers = new Dictionary<string, string>
                //     {
                //         ["Referer"] = uriStr,
                //     },
                // }
                );

                if (!response.Ok)
                    throw new Exception("Failed to download. Response is NotOk");

                var bytes = await response.BodyAsync();

                webDriverMetrics.IncDownload(uri.Host);
                var data = Encoding.GetEncoding("iso-8859-1").GetString(bytes);

                return new FileData()
                {
                    Name = fileName,
                    Uri = uriStr,
                    DataBytes = data,
                };
            }
            catch (Exception ex)
            {
                webDriverMetrics.IncPageLoadFail();
                logger.LogError(ex, "Failed to download: {uri}", uriStr);
                throw;
            }
        };
    }
}
