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
using Crawler.Core.Parser.DocumentParts;

namespace Crawler.Core.Parser
{
    public class DocumentPartComparer
    {

        public class DocumentPartLinkComparer : IEqualityComparer<DocumentPartLink>
        {
            public bool Equals(DocumentPartLink x, DocumentPartLink y)
            {
                var xuri = x.Uri.MatchUnsafe(u => u, ()=> null);
                var yuri = y.Uri.MatchUnsafe(u => u, ()=> null);

                var xtext = x.Text.MatchUnsafe(u => u, ()=> null);
                var ytext = y.Text.MatchUnsafe(u => u, ()=> null);

                if(xuri == null && xtext == null)
                    return false;

                if(yuri == null && ytext == null)
                    return false;

                return xuri == yuri && xtext == ytext;
            }

            public int GetHashCode([DisallowNull] DocumentPartLink obj)
            {
                return obj.Uri.Match(u => u, () => obj.GetHashCode().ToString()).GetHashCode();
            }
        }
    }

    
}