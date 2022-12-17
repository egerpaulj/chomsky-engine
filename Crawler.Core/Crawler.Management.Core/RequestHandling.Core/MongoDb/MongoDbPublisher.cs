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
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Mongodb.Repo;

namespace Crawler.Management.Core.RequestHandling.Core.MongoDb
{
    public class MongoPublisher : IResponsePublisher, IFailurePublisher
    {
        
        private readonly MongoDbRepository<CrawlResponseModel> _mongoDbRepository;

        public MongoPublisher(MongoDbRepository<CrawlResponseModel> mongoDbRepository)
        {
            _mongoDbRepository = mongoDbRepository;
        }

        public TryOptionAsync<Unit> PublishResponse(Option<CrawlResponse> request)
        {
            return request.ToTryOptionAsync().
            Bind(response => _mongoDbRepository.AddOrUpdate(response.Map()))
            .Bind<Guid, Unit>(_ => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<Unit> PublishFailure(Option<CrawlRequest> request)
        {
            return request.ToTryOptionAsync().
            Bind(response => _mongoDbRepository.AddOrUpdate(response.Map()))
            .Bind<Guid, Unit>(_ => async () => await Task.FromResult(Unit.Default));
        }
    }
}