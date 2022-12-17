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

namespace Crawler.Core
{
    public enum ErrorType
    {
        UnknownError,
        MissingConfiguration,
        ParseError,
        WebRequestError,
        FileDownloadError,
        CssDownloadError,
        JsDownloadError,
        PageLoadError,
        
        CacheError,
        NetworkError,
        PublishError,
        RequestError,
        ContinuationError,
        ThrottleError,
        ConfigurationException


    }

    public class CrawlException : Exception
    {
        public CrawlException(string message, ErrorType error) : base(message)
        {
            Error = error;
        }

        public CrawlException(string message, ErrorType error, Exception innerException) : base(message, innerException)
        {
            Error = error;
        }

        public ErrorType Error{get; private set;}

    }
}