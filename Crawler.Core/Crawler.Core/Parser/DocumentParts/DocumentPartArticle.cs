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
using System.Threading.Tasks;
using System.Linq;
using LanguageExt;
using HtmlAgilityPack;
using Crawler.Core.Metrics;

namespace Crawler.Core.Parser.DocumentParts
{
    public class DocumentPartArticle : DocumentPart
    {
        public Option<DocumentPartText> Title { get; set; }
        public Option<DocumentPart> Content { get; set; }

        public DocumentPartArticle()
        {
            DocPartType = DocumentPartType.Article;
        }


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
                                var t = await Title.SelectAsync(t => t.Parse(document).Match(u => { }, () => AppendAnomaly(AnomalyType.MissingTitle, "Failed to parse title")));
                                
                                
                                var contentSelector = Content.Bind(con => con.Selector);
                                var contentNodes = await contentSelector.ToTryOptionAsync().Bind<DocumentPartSelector, IEnumerable<HtmlNode>>( s => s.GetNodes(document)).Match(n =>n, () => new List<HtmlNode>());

                                var contentDocument = CreateDocument(contentNodes);
                                var c = await Content.SelectAsync(t => t.Parse(contentDocument).Match(u => { }, () => AppendAnomaly(AnomalyType.MissingContent, "Failed to parse content")));
                                
                                return await Task.FromResult(Unit.Default);

                            }, "Document Part Article Parse");
                    };

                });
        }

        public override string GetBriefSummary()
        {
            var title = Title.Bind(t => t.Text).Match(t => t, string.Empty);
            var contentPart = Content.Match(c => c, () => new DocumentPartText());
            var content = contentPart.GetBriefSummary();

            var links = DocumentPartExtensions.GetAllParts<DocumentPartLink>(contentPart).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();
            var files = DocumentPartExtensions.GetAllParts<DocumentPartFile>(contentPart).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();
            return $"ARTICLE: \n Title: {title} \n Content:{content} \nLinks: {links} \nFiles: {files}";
        }
    }
}