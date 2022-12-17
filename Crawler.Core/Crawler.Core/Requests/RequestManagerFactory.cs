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
using Crawler.Core.Cache;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Requests
{
    public interface IRequestManagerFactory : IDisposable
    {
        IRequestManager GetRequestManager(Option<string> uri);
    }

    public class RequestManagerFactory : IRequestManagerFactory
    {
        private bool disposedValue;
        private Dictionary<string, IRequestManager> _requestManagers = new Dictionary<string, IRequestManager>();

        private readonly ILoggerFactory _loggerFactory;
        private readonly ICache _cache;

        private readonly object _syncObject = new object();

        public RequestManagerFactory(ILoggerFactory loggerFactory, ICache cache)
        {
            _loggerFactory = loggerFactory;
            _cache = cache;
        }

        public IRequestManager GetRequestManager(Option<string> uri)
        {
            var throttleUri = new Uri(uri.Match(u => u, () => throw new CrawlException("Throttle Uri is empty", ErrorType.ThrottleError)));
            var host = throttleUri.Host?? "unknown";
            lock (_syncObject)
            {
                if (!_requestManagers.TryGetValue(throttleUri.Host, out var requestManager))
                {
                    var reqManager = new RequestManager(_loggerFactory.CreateLogger<RequestManager>(), _cache, host);
                    _requestManagers.Add(host, reqManager);

                    return reqManager;
                }

                return requestManager;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                _requestManagers = null;
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RequestManagerFactory()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}