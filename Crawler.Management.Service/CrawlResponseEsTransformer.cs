//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2024  Paul Eger

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
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Results;
using Crawler.DataModel;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;

public class CrawlResponseEsTransformer<TIn>(string name) : IBertrandTransformer
    where TIn : CrawlResponse
{
    public string Name => name;

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var message = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));
            var output = new Message<object>();
            output.RoutingKey = "Filtered out";

            if (!message.ShouldIndex)
                return output;

            output = inputMessage.CopyData(output);
            output.RoutingKey = name;

            output.Payload = MapToEs(message);

            return await Task.FromResult(output);
        };
    }

    private static CrawlEsResponseModel MapToEs(CrawlResponse response)
    {
        var documentPart = response
            .Result.Bind(r => r.RequestDocumentPart)
            .Match(d => d, () => throw new Exception("Empty result"));

        if (documentPart is DocumentPartArticle article1)
        {
            return CreateResponseModel(response, article1);
        }

        var article = documentPart.GetAllParts<DocumentPartArticle>().FirstOrDefault();
        if (article == null)
        {
            return new CrawlEsResponseModel
            {
                Content = GetText(documentPart),
                CorrelationId = response.CorrelationId.Match(c => c.ToString(), () => string.Empty),
                CrawlerId = response.CrawlerId.Match(c => c.ToString(), () => string.Empty),
                Uri = response.Uri,
                Timestamp = response.Timestamp.Match(
                    t => t.ToString(DateStrFormat),
                    () => DateTime.UtcNow.ToString(DateStrFormat)
                ),
            };
        }

        return CreateResponseModel(response, article);
    }

    private const string DateStrFormat = "yyyy-MM-dd'T'HH:mm:ss.fff";

    private static CrawlEsResponseModel CreateResponseModel(
        CrawlResponse response,
        DocumentPartArticle article
    )
    {
        var title = article.Title.Bind(t => t.Text).Match(r => r, () => string.Empty);
        var contentDocPart = article.Content.Match(
            c => c,
            () => throw new Exception("Empty content")
        );
        var content = GetText(contentDocPart);
        var heading = GetText(article.GetAllParts("Heading").FirstOrDefault());

        if (string.IsNullOrEmpty(content))
            throw new Exception("Content empty - avoid indexing");

        return new CrawlEsResponseModel
        {
            Heading = heading,
            Title = title,
            Content = content,
            CorrelationId = response.CorrelationId.Match(c => c.ToString(), () => string.Empty),
            CrawlerId = response.CrawlerId.Match(c => c.ToString(), () => string.Empty),
            Uri = response.Uri,
            Timestamp = response.Timestamp.Match(
                t => t.ToString(DateStrFormat),
                () => DateTime.UtcNow.ToString(DateStrFormat)
            ),
        };
    }

    private static string GetText(DocumentPart docPart)
    {
        string content = string.Empty;
        if (docPart == null)
            return content;

        if (docPart is DocumentPartText)
        {
            content = ((DocumentPartText)docPart).Text.Match(t => t, () => string.Empty);
        }
        else
        {
            content = docPart
                .GetAllParts<DocumentPartText>()
                .Select(p => p.Text.Match(t => t.Trim(), () => string.Empty))
                .Aggregate(
                    new StringBuilder(),
                    (sb, val) =>
                    {
                        if (!string.IsNullOrEmpty(val))
                            sb.AppendLine(val);

                        return sb;
                    },
                    sb => sb.ToString()
                );
        }

        return content;
    }
}
