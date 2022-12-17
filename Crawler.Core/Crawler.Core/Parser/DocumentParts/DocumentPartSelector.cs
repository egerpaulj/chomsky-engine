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
using System.Text;
using HtmlAgilityPack;

namespace Crawler.Core.Parser.DocumentParts
{
    public class DocumentPartSelector
    {
        public Option<string> Xpath { get; set; }
        public Option<string> ContentSpecificMatch{get;set;}

        public TryOptionAsync<IEnumerable<HtmlNode>> GetNodes(Option<HtmlDocument> document)
        {
            
            return async () =>
            {
                var doc = document.Match(d => d, () => new HtmlDocument());
                

                var matchedNodes = Enumerable.Empty<HtmlNode>();
                var allMatchedNodes = doc.DocumentNode.SelectNodes(Xpath.Match(x => x, () => "//*[not(self::html)]"))?.ToList();
                
                var contentFilter = ContentSpecificMatch.Match(s => s, string.Empty);
                if(!string.IsNullOrEmpty(contentFilter))
                    allMatchedNodes = allMatchedNodes.Where(node => node.InnerText.Contains(contentFilter) ).ToList();

                if(allMatchedNodes == null)
                    return await Task.FromResult(Option<IEnumerable<HtmlNode>>.None);

                var filteredContentBasedNodes = allMatchedNodes
                                                    .Where(n =>
                                                        
                                                        !allMatchedNodes.Any(
                                                                
                                                                n2 => n2.ChildNodes.Contains(n)))
                                                    .ToList();

                matchedNodes = matchedNodes.Append(filteredContentBasedNodes);

                return await Task.FromResult(Option<IEnumerable<HtmlNode>>.Some(matchedNodes.Distinct(HtmlNodeComparer.EqualityComparer)));
            };
        }
    }
}