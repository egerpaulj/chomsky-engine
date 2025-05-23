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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using HtmlAgilityPack;
using LanguageExt;

namespace Crawler.Core.Parser.DocumentParts
{
    public enum AnomalyType
    {
        None,
        Missing,
        MissingText,
        MissingTable,
        MissingRow,
        MissingHeader,
        MissingImg,
        MissingAnchor,
        MissingFileLink,
        MissingTitle,
        MissingContent,
        TooManyTablesFound,
        DynamicParseIssue,
        Critical,
    }

    public class Anomaly
    {
        public Option<AnomalyType> AnomalyType { get; set; }
        public Option<DocumentPartType> DocumentPartType { get; set; }
        public Option<string> Decription { get; set; }

        public Anomaly(
            Option<AnomalyType> anomalyType,
            Option<DocumentPartType> documentPartType,
            Option<string> decription
        )
        {
            AnomalyType = anomalyType;
            DocumentPartType = documentPartType;
            Decription = decription;
        }

        public Anomaly() { }
    }

    public enum DocumentPartType
    {
        Text = 1,
        Link = 2, // ToDo Button Links <button onclick="document.location=''"> or onclick="javascriptFunc()" => then need to parse Javascript for function() => then URI [or continuation strategy]
        File = 3,
        Table = 4,
        Row = 5,
        Article = 6,
        AutoDetect = 7,
        Stream = 8,
        Form = 9,
        Meta = 10,
    }

    [JsonConverter(typeof(BaseClassConverter))]
    public abstract class DocumentPart
    {
        public Option<string> Name { get; set; }
        public Option<string> BaseUri { get; set; }
        public Option<string> Raw { get; set; }
        public Option<List<string>> StyleList { get; set; }
        public Option<List<DocumentPart>> SubParts { get; set; }
        public Option<List<Anomaly>> Anomalies { get; set; }
        public Option<DocumentPartType> DocPartType { get; set; }

        public Option<DocumentPartSelector> Selector { get; set; }

        public DocumentPart(Option<string> baseUri)
        {
            Anomalies = new List<Anomaly>();
            Selector = new DocumentPartSelector();

            // if(baseUri.IsNone)
            //     throw new ArgumentException("Base uri missing");

            BaseUri = baseUri;
        }

        public TryOptionAsync<Unit> Parse(Option<HtmlDocument> document)
        {
            return ParseDocument(document).Bind(_ => ParseSubParts(document));
        }

        protected TryOptionAsync<IEnumerable<HtmlNode>> GetNodes(Option<HtmlDocument> document) =>
            Selector.ToTryOptionAsync().Bind(s => s.GetNodes(document));

        protected TryOptionAsync<Unit> Parse<T>(
            Option<T> documentPart,
            Option<HtmlDocument> document
        )
            where T : DocumentPart
        {
            return documentPart
                .ToTryOptionAsync()
                .Bind(d =>
                {
                    return d
                        .Selector.ToTryOptionAsync()
                        .Bind(s => s.GetNodes(document))
                        .Bind(e => d.Parse(CreateDocument(e)));
                });
        }

        protected abstract TryOptionAsync<Unit> ParseDocument(Option<HtmlDocument> document);

        internal bool IsParsedSubpart { get; set; }

        public void AppendAnomaly(AnomalyType anomalyType, Option<string> description)
        {
            Anomalies = Anomalies.Bind<List<Anomaly>>(a =>
            {
                var selectorText = Selector
                    .Bind<string>(s => s.ToString())
                    .Match(s => s, string.Empty);

                a.Add(
                    new Anomaly(
                        anomalyType,
                        DocPartType,
                        $"{description}, Selector: {selectorText}"
                    )
                );
                return a;
            });
        }

        public abstract string GetBriefSummary();

        protected DocumentPart DetermineDocumentPart(HtmlNode element)
        {
            DocumentPart docPart = null;

            var anchors = element.SelectNodes(".//a")?.ToList();
            var images = element.SelectNodes(".//img")?.ToList();
            var tables = element.SelectNodes(".//table")?.ToList();

            if (
                (anchors != null && anchors.Any() && images != null && images.Any())
                || (
                    ((anchors != null && anchors.Any()) || (images != null && images.Any()))
                    && (tables != null && tables.Any())
                )
            )
            {
                docPart = CreateDefaultArticle();
            }
            else if (images != null && images.Any())
                docPart = new DocumentPartFile(BaseUri);
            else if (anchors != null && anchors.Any())
                docPart = new DocumentPartLink(BaseUri);

            if (element.Name.ToLower().Equals("table"))
                docPart = new DocumentPartTable(BaseUri);

            if (docPart == null)
                docPart = new DocumentPartText(BaseUri);

            return docPart;
        }

        protected DocumentPart CreateDefaultArticle()
        {
            return new DocumentPartArticle(this.BaseUri)
            {
                BaseUri = this.BaseUri,
                Title = new DocumentPartText(BaseUri)
                {
                    Selector = new DocumentPartSelector { Xpath = "//title" },
                },
                Content = new DocumentPartText(BaseUri)
                {
                    Selector = new DocumentPartSelector() { Xpath = "//body" },
                    SubParts = new List<DocumentPart>
                    {
                        new DocumentPartLink(BaseUri),
                        new DocumentPartFile(BaseUri),
                    },
                },
            };
        }

        private TryOptionAsync<Unit> ParseSubParts(Option<HtmlDocument> document)
        {
            if (SubParts.IsNone)
                return async () => await Task.FromResult(Unit.Default);

            return SubParts
                .ToTryOptionAsync()
                .Bind<List<DocumentPart>, Unit>(subParts =>
                {
                    return async () =>
                    {
                        await Parallel
                            .ForEach(
                                subParts.Where(part => !part.IsParsedSubpart),
                                p => p.Parse(document).Match(t => t, () => Unit.Default)
                            )
                            .AsTask();

                        return await Task.FromResult(Unit.Default);
                    };
                });
        }

        protected static Option<string> ResolveUri(Option<string> baseUri, Option<string> target)
        {
            return baseUri.Bind<string>(b =>
            {
                return target
                    .Bind<string>(t =>
                    {
                        var targetUriCleaned = t.Replace("\\", "").Replace("\"", "");
                        var validUri = Uri.TryCreate(
                            targetUriCleaned,
                            UriKind.RelativeOrAbsolute,
                            out var targetUri
                        );
                        var baseUri = new Uri(b);

                        if (!validUri || !targetUri.IsAbsoluteUri)
                        {
                            targetUri = new Uri(baseUri, targetUriCleaned);
                        }
                        return targetUri.AbsoluteUri;
                    })
                    .MatchUnsafe(res => res, () => null);
            });
        }

        protected static HtmlDocument CreateDocument(IEnumerable<HtmlNode> nodes)
        {
            return GetHtmlDocument(nodes);
        }

        private static HtmlDocument GetHtmlDocument(IEnumerable<HtmlNode> nodes)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.DocumentNode.AppendChild(
                new HtmlNode(HtmlNodeType.Element, htmlDocument, 0)
            );
            htmlDocument.DocumentNode.FirstChild.Name = "html";
            foreach (var node in nodes.Where(n => n.NodeType != HtmlNodeType.Comment))
            {
                htmlDocument.DocumentNode.FirstChild.AppendChild(node);
            }
            return htmlDocument;
        }
    }
}
