using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Selenium.Firefox;
using LanguageExt;
using LanguageExt.UnitsOfMeasure;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

public class WebDriverSelfHealing : IWebDriverService
{
    private WebDriverFirefoxRobust _webDriverFirefoxRobust;
    private ILogger<WebDriverSelfHealing> _logger;
    private IWebDriverMetrics _metrics;
    private ILogger<WebDriverServiceFirefox> _driverLogger;

    private bool _inMaintenance = false;
    private Timer _periodicTimer;

    public WebDriverSelfHealing(
        IWebDriverMetrics metrics,
        ILogger<WebDriverSelfHealing> logger,
        ILoggerFactory loggerFactory
    )
    {
        _metrics = metrics;
        _logger = logger;
        _driverLogger = loggerFactory.CreateLogger<WebDriverServiceFirefox>();
        _webDriverFirefoxRobust = new WebDriverFirefoxRobust(_metrics, _driverLogger);
        _periodicTimer = new Timer(
            x => StartMaintenance(),
            this,
            TimeSpan.Zero,
            TimeSpan.FromHours(2)
        );
    }

    public TryOptionAsync<FileData> Download(Option<DownloadRequest> uri)
    {
        return IsReady().Bind(_ => _webDriverFirefoxRobust.Download(uri));
    }

    public TryOptionAsync<string> LoadPage(Option<LoadPageRequest> request)
    {
        return IsReady().Bind(_ => _webDriverFirefoxRobust.LoadPage(request));
    }

    private void StartMaintenance()
    {
        _inMaintenance = true;
        _webDriverFirefoxRobust?.Dispose();
        _webDriverFirefoxRobust = null;

        foreach (
            var topMemoryProcess in Process
                .GetProcesses()
                .OrderByDescending(p => p.PrivateMemorySize64)
                .Take(10)
        )
        {
            _logger.LogInformation(
                $"Memory usage: {topMemoryProcess.ProcessName}. {topMemoryProcess.PrivateMemorySize64 / (1000 * 1000)} mb"
            );
        }

        foreach (
            var process in Process
                .GetProcesses()
                .Where(p =>
                    p.ProcessName.Contains("firefox") || p.ProcessName.Contains("geckodriver")
                )
        )
        {
            try
            {
                process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop process: " + process.ProcessName);
            }
        }

        _webDriverFirefoxRobust = new WebDriverFirefoxRobust(_metrics, _driverLogger);
        _inMaintenance = false;
    }

    private TryOptionAsync<Unit> IsReady()
    {
        return async () =>
        {
            while (_inMaintenance)
            {
                _logger.LogInformation("Waiting until maintenance mode is finished");
                await Task.Delay(1000);
            }

            return Unit.Default;
        };
    }
}
