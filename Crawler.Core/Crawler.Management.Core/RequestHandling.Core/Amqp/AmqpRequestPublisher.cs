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
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Amqp;

namespace Crawler.Management.Core.RequestHandling.Core.Amqp
{
    public class AmqpRequestPublisher : IRequestPublisher, IDisposable
    {
        private readonly IMessagePublisher _requestPublisher;
        private bool disposedValue;

        public AmqpRequestPublisher(IAmqpProvider amqpProvider)
        {
            _requestPublisher = amqpProvider.GetPublisher(AmqpRequestProvider.RequestProviderContext).Match(p => p, ()=> throw new System.Exception("Failed to get AMQP request publisher"), ex => {throw ex;}).Result;
        }


        public TryOptionAsync<Unit> PublishRequest(Option<CrawlRequest> request)
        {
            return _requestPublisher.Publish<CrawlRequest>(request);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _requestPublisher.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}