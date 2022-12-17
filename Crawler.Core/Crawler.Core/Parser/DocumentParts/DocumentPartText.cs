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
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Metrics;
using HtmlAgilityPack;
using LanguageExt;

namespace Crawler.Core.Parser.DocumentParts
{
    public class DocumentPartText : DocumentPart
    {
        public Option<string> Text { get; set; }

        public DocumentPartText()
        {
            DocPartType = DocumentPartType.Text;
            Selector = new DocumentPartSelector()
            {
                Xpath = "//*"
            };
        }

        public override string ToString()
        {
            return Text.Match(t => t, string.Empty);
        }

        protected override TryOptionAsync<Unit> ParseDocument(Option<HtmlDocument> document)
        {
            return GetNodes(document)
                .Bind<IEnumerable<HtmlNode>, Unit>(
                    nodes =>
                    {
                        return async () =>
                        {
                            return await MonitorPerformance.MonitorAsync(async () => {

                            var text = 
                                nodes
                                .Select(n =>
                                {
                                    return GetContent(n);
                                })
                                .Aggregate(new StringBuilder(),
                                    (builder, val) => builder.AppendLine(val), b => b.ToString());

                            if (!string.IsNullOrEmpty(text))
                            {
                                Text = text;
                            }
                            else
                                AppendAnomaly(AnomalyType.MissingText, "Failed to find Text");
                            
                            return await Task.FromResult(Unit.Default);

                            }, "Document Part Text Parser");
                        };
                    });
        }

        private static string FocusWithoutWhitespaces(string s) => s
        
        ?.Split(Environment.NewLine)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Aggregate(new StringBuilder(),
                        (builder, val) => builder.Append($" {val.Trim()}"), b => b.ToString())
        .Trim();

        internal static string GetContent(HtmlNode n)
        {
            // ToDo Feature Human Readable - if needed
            // ToDo => flag  if human readable results are needed => otherwise for data processing => it does not matter
            // ToDo Same for links and anchors
            // ToDo Poor man's paragraph detector => e.g. Large amounts of text followed by period => then Create a new paragraph
            
            // if(n.Name == "table" || n.Name == "TABLE")
            // {
            //     DocumentPartTable documentPartTable = GetParsedDocumentPartTable(n);
            //     return documentPartTable.GetTextualRepresentationOfTable();
            // }

            // var tableNodes = n.SelectNodes(".//table");
            // if(tableNodes != null)
            // {
            //     foreach(var tableNode in tableNodes)
            //     {
            //         tableNode.InnerHtml = GetParsedDocumentPartTable(tableNode).GetTextualRepresentationOfTable();
            //     }
            // }
            
            if (n.NodeType == HtmlNodeType.Element || n.NodeType == HtmlNodeType.Text)
            {
                return FocusWithoutWhitespaces(n.InnerText);
            }

            return string.Empty;
        }

        private static DocumentPartTable GetParsedDocumentPartTable(HtmlNode n)
        {
            var documentPartTable = new DocumentPartTable();
            var _ = documentPartTable.Parse(CreateDocument(new List<HtmlNode> { n })).Match(t => t, () => Unit.Default).Result;
            return documentPartTable;
        }

        public override string GetBriefSummary()
        {
            var text = Text.Match(t => t, () => string.Empty);
            if (text.Length > 60)
            {
                text = text.Substring(0, 59) + "...";
            }
            return $"{text}";
        }
    }
}