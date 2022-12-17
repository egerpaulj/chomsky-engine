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
using System.Diagnostics.CodeAnalysis;
using HtmlAgilityPack;

namespace Crawler.Core.Parser.DocumentParts
{
    public class HtmlNodeComparer : IEqualityComparer<HtmlNode>
    {
        public bool Equals(HtmlNode x, HtmlNode y)
        {
            return x?.Name.ToLower() == y?.Name.ToLower()
            && x?.OuterHtml == y?.OuterHtml;
        }

        public int GetHashCode([DisallowNull] HtmlNode obj)
        {
            return obj.OuterHtml.GetHashCode();
        }

        public static IEqualityComparer<HtmlNode> EqualityComparer = new HtmlNodeComparer();
    }
}