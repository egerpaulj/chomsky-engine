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
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Amqp;

namespace Crawler.Management.Core.RequestHandling.Core.Amqp
{
    public class AmqpPublisher : IResponsePublisher, IFailurePublisher
    {
        private const string FailurePublisherContext = "CrawlFailure";
        private const string ResponsePublisherContext = "CrawlResponse";

        private readonly IMessagePublisher _failurePublisher;
        private readonly IMessagePublisher _responsePublisher;

        public AmqpPublisher(IAmqpProvider amqpProvider)
        {
            _failurePublisher = amqpProvider.GetPublisher(FailurePublisherContext).Match(p => p, ()=> throw new System.Exception("Failed to get AMQP failure publisher"), ex => {throw ex;}).Result;
            _responsePublisher = amqpProvider.GetPublisher(ResponsePublisherContext).Match(p => p, ()=> throw new System.Exception("Failed to get AMQP response publisher"), ex => {throw ex;}).Result;
        }

        public TryOptionAsync<Unit> PublishFailure(Option<CrawlRequest> request)
        {
            return _failurePublisher.Publish<CrawlRequest>(request);
        }

        public TryOptionAsync<Unit> PublishResponse(Option<CrawlResponse> request)
        {
            return _responsePublisher.Publish<CrawlResponse>(request);
        }
    }
}