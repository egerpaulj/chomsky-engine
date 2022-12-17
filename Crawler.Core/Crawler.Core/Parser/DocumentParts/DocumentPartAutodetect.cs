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
using LanguageExt;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;
using Crawler.Core.Metrics;

namespace Crawler.Core.Parser.DocumentParts
{
    public class DocumentPartAutodetect : DocumentPart
    {
        public DocumentPartAutodetect()
        {
            DocPartType = DocumentPartType.AutoDetect;
            Selector = new DocumentPartSelector()
            {
                Xpath = ".//*[not(self::html)]"
            };
        }

        protected override TryOptionAsync<Unit> ParseDocument(Option<HtmlDocument> document)
        {
            return GetNodes(document)
            .Bind<IEnumerable<HtmlNode>, Unit>(
                nodes =>
                {
                    return async () =>
                    {
                        return await MonitorPerformance.MonitorAsync( async() =>
                            {
                                var subparts = new List<DocumentPart>();

                                var documentPartArticle = new DocumentPartArticle
                                {
                                    IsParsedSubpart = true,
                                    BaseUri = this.BaseUri,
                                    Title = new DocumentPartText
                                    {
                                        BaseUri = this.BaseUri,
                                        Selector = new DocumentPartSelector
                                        {
                                            Xpath = "//title"
                                        }
                                    },
                                    Content = new DocumentPartText
                                    {
                                        BaseUri = this.BaseUri,
                                        Selector = new DocumentPartSelector()
                                        {
                                            Xpath = "//body"
                                        },
                                    }
                                };

                                var result = await documentPartArticle.Parse(document)
                                    .Match(u => { }, () => documentPartArticle.AppendAnomaly(AnomalyType.MissingContent, "Failed to parse content"));

                                subparts.Add(documentPartArticle);

                                var doc = document.Match(doc => doc, () => throw new CrawlException("Document is empty", ErrorType.ParseError));

                                var tableNodes = doc.DocumentNode.SelectNodes("//table");
                                if (tableNodes != null)
                                {
                                    var tableParts = tableNodes.Select(t =>
                                    {
                                        var documentPart = new DocumentPartTable()
                                        {
                                            BaseUri = BaseUri,
                                            IsParsedSubpart = true
                                        };

                                        var res = documentPart.Parse(CreateDocument(new List<HtmlNode> { t }))
                                        .Match(u => { }, () => documentPart.AppendAnomaly(AnomalyType.MissingContent, "Failed to parse content"))
                                        .Result;

                                        return documentPart;
                                    });

                                    subparts.AddRange(tableParts);

                                }
                                

                                // ToDo Html Agility pack error - Xpath Depth limit?
                                // var linkNodes = doc.DocumentNode.SelectNodes(".//*[self::img or self::a]")?.ToList() ?? new List<HtmlNode>();
                                // linkNodes = linkNodes.Distinct(new HtmlNodeComparer()).ToList();
                                
                                var linkNodes = doc.DocumentNode
                                .Descendants()
                                .Where(n => n.Name == "img" || n.Name == "a")
                                .ToList();

                                if (linkNodes != null)
                                {

                                    var linkParts = await linkNodes.SelectAsync(t =>
                                    {
                                        var documentPart = new DocumentPartFile()
                                        {
                                            BaseUri = BaseUri,
                                            IsParsedSubpart = true
                                        };

                                        var res =  documentPart.Parse(CreateDocument(new List<HtmlNode> { t }))
                                        .Match(u => { }, () => documentPart.AppendAnomaly(AnomalyType.MissingContent, "Failed to parse content"));

                                        return Task.FromResult(documentPart);
                                    });

                                    subparts.AddRange(linkParts);
                                }


                                SubParts = subparts;

                                return await Task.FromResult(Unit.Default);

                            }, "Document Part Autodetect Parser");
                    };
                });
        }

        public override string GetBriefSummary()
        {
            var autoDetectText = SubParts.Match(t => t, () => new List<DocumentPart>()).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();

            return $"AUTO_DETECT: {autoDetectText}";
        }
    }
}