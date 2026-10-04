using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Exceptions;
using Crawler.WebDriver.Selenium.Firefox;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

public class WebDriverFirefoxDownloader : IWebDriverDownloader
{
    private readonly ILogger<WebDriverFirefoxDownloader> logger;
    private readonly IWebDriverMetrics webDriverMetrics;
    private readonly IWebDriver webDriver;
    private readonly INetwork network;

    private const string DOWNLOAD_DIR = "/home/user/Downloads";

    public WebDriverFirefoxDownloader(
        IWebDriverMetrics metrics,
        ILogger<WebDriverFirefoxDownloader> logger
    )
        : this(metrics, logger, isHeadless: false) { }

    public WebDriverFirefoxDownloader(
        IWebDriverMetrics metrics,
        ILogger<WebDriverFirefoxDownloader> logger,
        bool isHeadless
    )
    {
        this.logger = logger;
        this.webDriverMetrics = metrics;

        var options = new FirefoxOptions();
        if (isHeadless)
            options.AddArgument("-headless");

        options.AddArgument("--no-sandbox");
        options.Profile = new FirefoxProfile();
        // Inject profiles if necessary
        //     profileDirectory: "/path_to_profile"
        // );

        options.SetPreference(
            "general.useragent.override",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36"
        );

        options.SetPreference("accept.language", "en-US,en;q=0.9");
        options.SetPreference("accept.encoding", "gzip, deflate, br");

        options.AddArgument("-disable-automation"); 
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--start-maximized");

        // Set the download directory and ensure no prompt for download
        options.SetPreference("browser.download.dir", DOWNLOAD_DIR);
        options.SetPreference("browser.download.folderList", 2); // 2 means custom folder
        options.SetPreference("browser.helperApps.neverAsk.saveToDisk", "*/*");
        options.SetPreference("browser.download.useDownloadDir", true);
        options.SetPreference("pdfjs.disabled", true);

        webDriver = new FirefoxDriver(options);
        network = webDriver.Manage().Network;
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

            long fileSize = 0;
            var handler = new NetworkResponseHandler
            {
                ResponseTransformer = r => r,
                ResponseMatcher = r =>
                {
                    fileSize = r.Headers.ContainsKey("Content-Length")
                        ? long.Parse(r.Headers["Content-Length"])
                        : 0;
                    return true;
                },
            };
            try
            {
                network.AddResponseHandler(handler);

                webDriver.Navigate().GoToUrl($"https://{uri.Host}/home");
                var wait = new WebDriverWait(webDriver, timeout: TimeSpan.FromSeconds(60));
                var firefoxDriver = (webDriver as FirefoxDriver);
                wait.PollingInterval = TimeSpan.FromMilliseconds(300);
                if (
                    !wait.Until(_ =>
                        firefoxDriver.ExecuteScript("return document.readyState").Equals("complete")
                    )
                )
                {
                    logger.LogWarning("Document state is not ready. Attempt to download anyway");
                }

                // Navigate to the file URL
                webDriver.Navigate().GoToUrl(uriStr);

                // WebDriverWait cloudFareChallengesWait = new WebDriverWait(
                //     webDriver,
                //     TimeSpan.FromSeconds(30)
                // );

                // wait.Until(d =>
                //     !d.PageSource.Contains("Just a moment")
                //     && !d.PageSource.Contains("cf-challenge")
                // );

                // Define the path to the downloaded file
                string downloadedFilePath = Path.Combine(DOWNLOAD_DIR, fileName);

                // Wait for download completion asynchronously
                await WaitForFileDownloadAsync(downloadedFilePath, fileSize, timeoutInSeconds: 120);

                logger.LogInformation(
                    "File downloaded to: {downloadedFilePath}",
                    downloadedFilePath
                );

                webDriverMetrics.IncDownload(uri.Host);
                var data = Encoding
                    .GetEncoding("iso-8859-1")
                    .GetString(File.ReadAllBytes(downloadedFilePath));

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
            finally
            {
                network.ClearResponseHandlers();
            }
        };
    }

    private async Task WaitForFileDownloadAsync(
        string filePath,
        long expectedFileSize,
        int timeoutInSeconds
    )
    {
        const int pollingInterval = 1000; // 1 second interval to check file size
        int elapsedTime = 0;

        while (elapsedTime < timeoutInSeconds * 1000)
        {
            if (expectedFileSize == 0 && File.Exists(filePath))
            {
                logger.LogInformation("File exists");
                return;
            }

            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == expectedFileSize)
                {
                    logger.LogInformation("Download completed successfully.");
                    return;
                }
            }

            await Task.Delay(pollingInterval); // Asynchronously wait for the next check
            elapsedTime += pollingInterval;
        }

        logger.LogError("Download failed or timed out.");
    }
}
