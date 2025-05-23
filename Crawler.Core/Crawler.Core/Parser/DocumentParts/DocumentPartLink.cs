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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Core.Metrics;
using HtmlAgilityPack;
using LanguageExt;

namespace Crawler.Core.Parser.DocumentParts
{
    [DebuggerDisplay("{Uri}")]
    public class DocumentPartLink : DocumentPart
    {
        public Option<string> Text { get; set; }

        public Option<string> Uri { get; set; }

        public DocumentPartLink(Option<string> baseUri)
            : base(baseUri)
        {
            DocPartType = DocumentPartType.Link;

            Selector = new DocumentPartSelector { Xpath = "//a" };
        }

        public override string ToString()
        {
            return Text.Match(t => t, string.Empty);
        }

        protected override TryOptionAsync<Unit> ParseDocument(Option<HtmlDocument> document)
        {
            return GetNodes(document)
                .Bind<IEnumerable<HtmlNode>, Unit>(nodes =>
                {
                    return async () =>
                    {
                        return await MonitorPerformance.MonitorAsync(
                            async () =>
                            {
                                var anchors = nodes
                                    .Where(n => n.Attributes["href"] != null)
                                    .ToList();

                                if (!anchors.Any())
                                {
                                    AppendAnomaly(
                                        AnomalyType.MissingFileLink,
                                        "Could not find any anchors"
                                    );
                                    return await Task.FromResult(Unit.Default);
                                }

                                var first = anchors.FirstOrDefault();
                                if (first != null)
                                {
                                    Text = DocumentPartText.GetContent(first);
                                    Uri = ResolveUri(BaseUri, first.Attributes["href"].Value);
                                    SubParts = anchors
                                        .Where(a => a != first)
                                        .Select(n =>
                                        {
                                            var subpartUri = new Uri(
                                                ResolveUri(BaseUri, n.Attributes["href"].Value)
                                                    .Match(
                                                        r => r,
                                                        () =>
                                                            BaseUri.Match(
                                                                br => br,
                                                                () => string.Empty
                                                            )
                                                    )
                                            );

                                            var documentPartLink = new DocumentPartLink(
                                                subpartUri.Host
                                            )
                                            {
                                                IsParsedSubpart = true,
                                            };
                                            documentPartLink.Text = DocumentPartText.GetContent(n);
                                            documentPartLink.Uri = subpartUri.AbsoluteUri;

                                            return documentPartLink as DocumentPart;
                                        })
                                        .ToList();
                                }

                                return await Task.FromResult(Unit.Default);
                            },
                            "Document Part Link Parser"
                        );
                    };
                });
        }

        public override string GetBriefSummary()
        {
            return $"LINK: Uri: {Uri.Match(u => u, () => string.Empty)}. Text: {Text.Match(t => t, () => string.Empty)}";
        }
    }
}
