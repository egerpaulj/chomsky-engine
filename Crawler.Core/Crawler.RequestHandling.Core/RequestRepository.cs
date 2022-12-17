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
using System.Threading;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using LanguageExt;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Crawler.RequestHandling.Core
{
    public class RequestRepository : IRequestRepository
    {
        private readonly IRequestProvider _requestProvider;
        private readonly IResponsePublisher _responsePublisher;
        private readonly IFailurePublisher _failurePublisher;

        public RequestRepository(IRequestProvider requestProvider, IResponsePublisher responsePublisher, IFailurePublisher failurePublisher)
        {
            _requestProvider = requestProvider;
            _responsePublisher = responsePublisher;
            _failurePublisher = failurePublisher;
        }

        public IObservable<Either<CrawlRequest, CrawlRequestException>> GetRequestObservable(CancellationToken token, Func<CrawlRequest, Task<LanguageExt.Unit>> crawlerTask) => _requestProvider.GetObservable(token, crawlerTask);

        public TryOptionAsync<LanguageExt.Unit> PublishFailure(Option<CrawlRequest> request) => _failurePublisher.PublishFailure(request);

        public TryOptionAsync<LanguageExt.Unit> PublishResponse(Option<CrawlResponse> response) => _responsePublisher.PublishResponse(response);
    }
}