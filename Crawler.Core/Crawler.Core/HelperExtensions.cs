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
using System.Linq;
using System.Threading.Tasks;

namespace Crawler.Core
{
    public static class HelperExtensions
    {
        public static string ConvertToString(this IEnumerable<char> chars)
        {
            return string.Concat(chars);
        }

        public static async Task<IEnumerable<T>> SelectAsync<I, T>(this IEnumerable<I> enumerable, Func<I, Task<T>> selectFunc)
        {
            if(!enumerable.Any())
                return await Task.FromResult(Enumerable.Empty<T>());

            var items =  new List<T>();

            foreach(var item in enumerable)
            {
                items.Add(await selectFunc(item));
            }

            return await Task.FromResult(items);
        }
    }
}