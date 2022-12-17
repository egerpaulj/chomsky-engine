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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Crawler.Core.Parser.DocumentParts
{
    public static class DocumentPartExtensions
    {
        public static IEnumerable<T> GetAllParts<T>(this DocumentPart documentPart) where T : DocumentPart
        {
            int iterationCount = 0;
            var partsEnumerable = Enumerable.Empty<T>();
            if(documentPart is T)
                partsEnumerable = partsEnumerable.Append(documentPart as T);

            var allParts = RecusrsiveGetSubParts<T>(documentPart, ref partsEnumerable, ref iterationCount);

            return allParts;
        }

        public static IEnumerable<Anomaly> GetAnomalies(this DocumentPart documentPart)
        {
            int iterationCount = 0;
            var partsEnumerable = Enumerable.Empty<DocumentPart>();
            
            partsEnumerable = partsEnumerable.Append(documentPart);

            var allParts = RecusrsiveGetSubParts<DocumentPart>(documentPart, ref partsEnumerable, ref iterationCount);

            return allParts.SelectMany(d => d.Anomalies.Match(a => a, Enumerable.Empty<Anomaly>()));
        }

        private static IEnumerable<T> RecusrsiveGetSubParts<T>(DocumentPart documentPart, ref IEnumerable<T> partsEnumerable, ref int recursionLevels)
        where T : DocumentPart
        {
            ControlRecursionLevel(ref recursionLevels);

            var subParts = documentPart.SubParts.Match(p => p, () => Enumerable.Empty<DocumentPart>());

            if (subParts.Any())
            {
                var matchedSubParts = subParts.OfType<T>();
                
                partsEnumerable = partsEnumerable.Append(matchedSubParts);

                foreach (var s in subParts)
                {
                    RecusrsiveGetSubParts<T>(s, ref partsEnumerable, ref recursionLevels);
                }
            }

            return partsEnumerable;
        }

        private static void ControlRecursionLevel(ref int recursionLevels)
        {
            recursionLevels++;
            if (recursionLevels > 5000)
                throw new InvalidOperationException("The Document Definition is too complex. Maximum depth is 5000 elements");
        }
    }
}