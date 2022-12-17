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
    public class DocumentPartTable : DocumentPart
    {
        public DocumentPartTable()
        {
            DocPartType = DocumentPartType.Table;
            Selector = new DocumentPartSelector
            {
                Xpath = "//table"
            };
        }

        public Option<List<DocumentPart>> Headers { get; set; }
        public Option<List<DocumentPartTableRow>> Rows { get; set; }

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

                                var tableElements = nodes.ToList();

                                var tableElement = tableElements.First();

                                Headers = (await tableElement.ChildNodes.Where(n => n.Name == "th").SelectAsync(async n =>
                                {
                                    var docpart = DetermineDocumentPart(n);
                                    await docpart.Parse(CreateDocument(new List<HtmlNode> { n })).Match(u => { }, () => AppendAnomaly(AnomalyType.MissingHeader, "Failed to parse Header"));
                                    return docpart;
                                }))
                                .ToList();

                                

                                Rows = (await tableElement.ChildNodes.Where(n => n.Name == "tr").SelectAsync(async n =>
                                {
                                    var docpart = new DocumentPartTableRow()
                                    {
                                        BaseUri = BaseUri
                                    };
                                    await docpart.Parse(CreateDocument(new List<HtmlNode> { n })).Match(u => { }, () => AppendAnomaly(AnomalyType.MissingRow, "Failed to parse row"));
                                    return docpart;
                                }))
                                .ToList();

                                SubParts = (await tableElements.Where(t => t != tableElement).SelectAsync(async n =>
                               {
                                   var documentPartTable = new DocumentPartTable()
                                   {
                                       BaseUri = BaseUri
                                   };
                                   
                                   await documentPartTable.Parse(CreateDocument(new List<HtmlNode> { n })).Match(u => { }, () => AppendAnomaly(AnomalyType.MissingTable, "Failed to parse Additionally matched table"));
                                   documentPartTable.IsParsedSubpart = true;
                                   return documentPartTable as DocumentPart;
                               })).ToList();

                               return await Task.FromResult(Unit.Default);

                            }, "Document Part Table Parser");

                            
                        };
                    });
        }

        public override string GetBriefSummary()
        {
            var rows = Rows.Match(r => r, () => new List<DocumentPartTableRow>()).SelectMany(t => t.GetBriefSummary()).ConvertToString();
            var headers = Headers.Match(h => h, () => new List<DocumentPart>()).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();
            return $"TABLE: \n Headers: \n{headers} \n Rows: \n{rows}";
        }

        public string GetTextualRepresentationOfTable()
        {
            // ToDO - Build a textual table with indents or other to display data in text format. 
            return $"\n*******Table******{GetBriefSummary()}\n**********End of Table****************";
        }
    }
}