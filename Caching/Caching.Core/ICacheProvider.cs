//      Microservice Cache Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger                                                                                                                                                                     
                                                                                                                                                                                                                   
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

using LanguageExt;

namespace Caching.Core
{
    public interface ICacheProvider
    {
         public const string DateTimeFormat = "hh:MM:dd:mm.ss.fff";

         /// <summary>
         /// Store T in Cache with a unique identifier key.
         /// <para>Expiration duration stores an item for that duration; and then is automatically removed.</para>
         /// </summary>
         TryOptionAsync<Unit> StoreInCache<T>(Option<string> key, T value, double expirationDurationInSeconds = 0);

         /// <summary>
         /// Get T from Cache using a unique identifier key.
         /// </summary>
         TryOptionAsync<T> Get<T>(Option<string> key);
    }
}