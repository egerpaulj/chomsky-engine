//      Microservice Core Libraries for .Net C#                                                                                                                                       
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

using System;
using System.Net.Http;
using LanguageExt;

namespace Microservice.Core.Http
{
    ///  <summary>
    ///  A REST client service interface.
    ///  </summary>
    public interface IHttpClientService
    {
        ///  <summary>
        ///  Send a request to the target URI, Deserialize the HTTP response to T; and returns T.
        ///  </summary>
        TryOptionAsync<T> Get<T>(Option<Guid> correlationId, Option<string> uri);

        ///  <summary>
        ///  Send a request to the target URI, return HTTP response as a string.
        ///  </summary>
        TryOptionAsync<string> GetStringContent(Option<Guid> correlationId, Option<string> uri);

        ///  <summary>
        ///  Send a request R to the target URI, Deserialize the HTTP response to T; and returns T.
        ///  </summary>
        TryOptionAsync<T> Send<R, T>(Option<Guid> correlationId, Option<R> send, Option<string> uri, Option<HttpMethod> method);

        ///  <summary>
        ///  Send a request R to the target URI.
        ///  </summary>
        TryOptionAsync<Unit> Send<R>(Option<Guid> correlationId, Option<R> send, Option<string> uri, Option<HttpMethod> method);
    }
}