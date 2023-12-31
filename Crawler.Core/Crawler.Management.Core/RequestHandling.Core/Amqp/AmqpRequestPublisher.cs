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
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Amqp;

namespace Crawler.Management.Core.RequestHandling.Core.Amqp
{
    public class AmqpRequestPublisher : IRequestPublisher
    {
        private readonly IMessagePublisher _requestPublisher;
        private readonly IMessagePublisher _uriPublisher;
        private bool disposedValue;

        public AmqpRequestPublisher(IAmqpProvider amqpProvider)
        {
            _requestPublisher = amqpProvider.GetPublisher(AmqpRequestProvider.RequestProviderContext).Match(p => p, () => throw new System.Exception("Failed to get AMQP request publisher"), ex => { throw ex; }).Result;
            _uriPublisher = amqpProvider.GetPublisher(AmqpRequestProvider.UriProviderContext).Match(p => p, () => throw new System.Exception("Failed to get AMQP request publisher"), ex => { throw ex; }).Result;
        }


        public TryOptionAsync<Unit> PublishRequest(Option<CrawlRequest> request)
        {
            return _requestPublisher.Publish<CrawlRequest>(request);
        }

        public TryOptionAsync<Unit> PublishUri(Option<string> baseUri, Option<List<DocumentPartLink>> linksList, UriType uriType)
        {
            return linksList.ToTryOptionAsync().Bind<List<DocumentPartLink>, Unit>(links => async () =>
            {
                foreach (var l in links)
                {
                    await _uriPublisher.Publish<CrawlUri>(new CrawlUri
                    {
                        BaseUri = baseUri,
                        Uri = l.Uri,
                        UriTypeId = uriType
                    }).Match(r => r, () => throw new Exception($"Failed to publish uri: {l.Uri}"));
                }

                return Unit.Default;
            });
        }
    }

}