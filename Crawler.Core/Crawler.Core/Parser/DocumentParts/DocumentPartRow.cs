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
using LanguageExt;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Crawler.Core.Parser.DocumentParts
{
    public class DocumentPartTableRow : DocumentPart
    {
        public DocumentPartTableRow()
        {
            this.DocPartType = DocumentPartType.Row;

            this.Selector = new DocumentPartSelector
            {
                Xpath = ".//td"
            };
        }

        public Option<List<DocumentPart>> Columns { get; set; }
        protected override TryOptionAsync<Unit> ParseDocument(Option<HtmlDocument> document)
        {
            return GetNodes(document)
            .Bind<IEnumerable<HtmlNode>, Unit>(
                nodes =>
                {
                    return async () =>
                    {
                        var columns = nodes.ToList();

                        var cols = await columns.SelectAsync(async n =>
                        {
                            var docpart = DetermineDocumentPart(n);
                            await docpart.Parse(CreateDocument(new List<HtmlNode> { n })).Match(u => {}, () => AppendAnomaly(AnomalyType.MissingRow, "Failed to parse row"));
                            return docpart;
                        });

                        Columns = cols.ToList();

                        return await Task.FromResult(Unit.Default);
                    };
                });
        }

        public override string GetBriefSummary()
        {
            var columns = Columns.Match(h => h, () => new List<DocumentPart>()).SelectMany(t => t.GetBriefSummary() + "\n").ConvertToString();
            return $"{columns}";
        }

        // <table>
        // <th></th>
        // <th></th>
        // <tr><td></td><td></td></tr>
        // <tr><td></td><td></td></tr>
        // </table>
    }
}