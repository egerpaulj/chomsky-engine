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
using Crawler.DataModel;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Elasticsearch.Repo;

namespace Crawler.Management.Core.RequestHandling.Core.Elasticsearch
{
    public class ElasticsearchPublisher : IResponsePublisher, IFailurePublisher
    {
        private readonly string _indexPrefix = "crawler_responses-";
        private readonly string _errorIndexPrefix = "crawler_errors-";
        
        private string _indexKey => $"{_indexPrefix}{DateTime.UtcNow:yyyy.MM.dd}";
        private string _errorIndexKey => $"{_errorIndexPrefix}{DateTime.UtcNow:yyyy.MM.dd}";
        private readonly IElasticsearchRepository _elasticsearchRepo;

        public ElasticsearchPublisher(IElasticsearchRepository elasticsearchRepo)
        {
            _elasticsearchRepo = elasticsearchRepo;
        }

        public TryOptionAsync<Unit> PublishFailure(Option<CrawlRequest> request)
        {
            return request
            .ToTryOptionAsync()
            .Bind(
                r => _elasticsearchRepo.IndexDocument<CrawlResponseIndexModel>(
                    r.MapToIndex(), _errorIndexKey));
        }

        public TryOptionAsync<Unit> PublishResponse(Option<CrawlResponse> request)
        {
            return request
            .ToTryOptionAsync()
            .Bind(
                r => _elasticsearchRepo.IndexDocument<CrawlResponseIndexModel>(
                    r.MapToIndex(), _indexKey));
        }
    }
}