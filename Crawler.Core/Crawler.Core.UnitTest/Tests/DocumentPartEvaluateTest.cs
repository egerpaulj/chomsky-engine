using System;
using System.Collections.Generic;
using System.Linq;
using Crawler.Core.Management;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Results;
using Crawler.Core.UnitTest.Factories;
using Crawler.Management.Service;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Crawler.Core.UnitTest.Tests;

[TestClass]
public class DocumentPartEvaluateTest
{
    [TestMethod]
    [Ignore]
    public void ParseEvaluateHtmlTest()
    {
        var evaluateHtml = TestCaseFactoryAutoDetect.GetEvaluateHtml();
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(evaluateHtml);
        var baseUri = "https://test.com";

        var documentPart = new DocumentPartArticle(baseUri)
        {
            Timestamp = new DocumentPartText(baseUri)
            {
                Selector = new DocumentPartSelector()
                {
                    Xpath = "//div[contains(@class, 'publishedDate')]",
                },
            },
            Content = new DocumentPartText(baseUri)
            {
                Selector = new DocumentPartSelector()
                {
                    Xpath = "//article[contains(@class, 'article-content')]",
                },
            },
            Title = new DocumentPartText(baseUri)
            {
                Selector = new DocumentPartSelector() { Xpath = "//h1" },
            },
        };
        documentPart.Parse(htmlDocument).Match(m => m, () => throw new System.Exception("Fail"));

        var tables = documentPart.GetAllParts<DocumentPartTable>();
        var rows = documentPart.GetAllParts<DocumentPartTableRow>();
        var links = documentPart.GetAllParts<DocumentPartLink>();

        var crawlResponse = new CrawlResponse
        {
            Result = new Document { RequestDocumentPart = documentPart },
            Uri = "Test uri",
            CrawlerId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
        };
        var transformedStock = StockDataMongoDbTransformer<CrawlResponse>
            .MapToMongodb(crawlResponse)
            .ToList();

        Assert.IsNotNull(tables);
    }
}
