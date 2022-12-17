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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Core.Metrics;
using HtmlAgilityPack;
using LanguageExt;
using static Crawler.Core.Parser.DocumentPartComparer;

namespace Crawler.Core.Parser.DocumentParts
{
    public class DocumentPartFile : DocumentPart
    {
        public DocumentPartFile()
        {
            DocPartType = DocumentPartType.File;

            Selector = new DocumentPartSelector
            {
                Xpath = ".//*[self::a or self::img]"
            };
        }

        public Option<List<FileData>> FileDataList { get; set; }

        public Option<List<DocumentPartLink>> DownloadLinks { get; set; }

        protected override TryOptionAsync<Unit> ParseDocument(Option<HtmlDocument> document)
        {
            return GetNodes(document)
                .Bind<IEnumerable<HtmlNode>, Unit>(
                    nodes =>
                    {
                        return async () =>
                        {
                            return await MonitorPerformance.MonitorAsync(async () =>
                            {
                                var doc = document.Match(d => d, () => throw new CrawlException("Can't prase an empty document for links", ErrorType.ParseError));
                                var files = nodes.ToList();

                                if (!files.Any())
                                {
                                    AppendAnomaly(AnomalyType.MissingFileLink, "Could not find any anchors");
                                    return await Task.FromResult(Unit.Default);
                                }

                                DownloadLinks = GetLinks(files);

                                return await Task.FromResult(Unit.Default);
                            }, "Document Part File Parser");
                        };
                    });

        }

        private List<DocumentPartLink> GetLinks(List<HtmlNode> nodes)
        {
            return nodes.Select(n =>
            {
                if (n.Name.ToLower() == "img")
                {
                    var uri = ResolveUri(BaseUri, n.Attributes["src"]?.Value);
                    var part = new DocumentPartLink()
                    {
                        Text = DocumentPartText.GetContent(n),
                        Uri = uri,
                        IsParsedSubpart = true
                    };

                    if (uri.IsNone)
                    {
                        part.AppendAnomaly(AnomalyType.MissingImg, $"missing Src in {n.InnerHtml}");
                    }

                    return part;
                }


                var uriHref = ResolveUri(BaseUri, n.Attributes["href"]?.Value);
                var partLink = new DocumentPartLink()
                {

                    Text = DocumentPartText.GetContent(n),
                    Uri = uriHref,
                    IsParsedSubpart = true
                };

                if (uriHref.IsNone)
                    {
                        partLink.AppendAnomaly(AnomalyType.MissingImg, $"missing href in {n.InnerHtml}");
                    }

                return partLink;
            })
            .Distinct(new DocumentPartLinkComparer())
            .ToList();
        }

        public override string GetBriefSummary()
        {
            var downloadLinks = DownloadLinks.Match(h => h, () => new List<DocumentPartLink>()).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();
            var fileData = FileDataList.Match(h => h, () => new List<FileData>()).SelectMany(t => t.Name.Match(n => n, () => string.Empty) + "\n").ConvertToString();
            return $"FILE\n DownloadList:{downloadLinks} \n FileData:{fileData}";
        }
    }
}