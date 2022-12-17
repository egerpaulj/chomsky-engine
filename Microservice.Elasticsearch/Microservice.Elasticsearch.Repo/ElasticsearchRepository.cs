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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microservice.Elasticsearch.Repo
{
    public class ElasticsearchRepository : IElasticsearchRepository
    {
        public const string ConnectionStringKey = "ElasticsearchConnectionString";
        private readonly string _esHosts;
        private readonly string Default = "localhost:9200/";
        private readonly Lazy<ElasticLowLevelClient> _clientLazy;

        private ElasticLowLevelClient _client => _clientLazy.Value;

        private readonly ILogger<ElasticsearchRepository> _logger;
        private readonly IJsonConverterProvider _converterProvider;

        public ElasticsearchRepository(ILogger<ElasticsearchRepository> logger, IConfiguration configuration, IJsonConverterProvider converterProvider)
        {
            _logger = logger;
            _converterProvider = converterProvider;
            _esHosts = configuration.GetConnectionString(ConnectionStringKey) ?? Default;

            _clientLazy = new Lazy<ElasticLowLevelClient>(() =>
                        {
                            var node = new Uri(_esHosts);
                            var config = new ConnectionConfiguration(node);
                            return new ElasticLowLevelClient(config);
                        });

        }

        public TryOptionAsync<Unit> IndexDocument<T>(Option<T> document, Option<string> indexKey) where T : IDataModel
        {
            var doc = document.Match(d => d, () => throw new Exception("Unable to index empty document"));
            var idx = indexKey.Match(i => i, () => throw new Exception("Index key should not be empty"));

            return Serialize<T>(doc)
                    .Bind(json => IndexDocument(doc, json, idx));
        }

        public TryOptionAsync<List<T>> Search<T>(Option<string> index, Option<string> queryJson) where T : class, IDataModel
        {
            return
            index
            .ToTryOptionAsync()
            .SelectMany(
                idx => queryJson.ToTryOptionAsync(), 
                async (idx, query) =>
                {
                    var result = await _client.SearchAsync<StringResponse>(idx, PostData.String(query));

                    if (!result.Success)
                        throw result.OriginalException;

                    var body = Newtonsoft.Json.Linq.JObject.Parse(result.Body);

                    var hits = body["hits"]["hits"].Children().ToList();
                    var searchResults = new List<T>();

                    if (hits.Count == 0)
                        return null;

                    foreach (var hit in hits)
                    {
                        var json = hit["_source"].ToString();

                        try
                        {
                            searchResults.Add(_converterProvider.Deserialize<T>(json));
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Unable to deserialize document found in Elastic search. Type: {typeof(T).Name}");
                            _logger.LogWarning("Skipping elastic search result. Document does not correspond to the requested type.");
                        }

                    }

                    return await Task.FromResult(searchResults);
                }
            );
        }

        public TryOptionAsync<Unit> Delete(Option<string> index)
        {
            return
            index
            .ToTryOptionAsync()
            .Bind<string, Unit>(
                idx =>  
                async () =>
                {
                    var result = await _client.Indices.DeleteAsync<StringResponse>(idx);

                    if (!result.Success)
                        throw result.OriginalException;

                    return await Task.FromResult(Unit.Default);
                }
            );
        }

        private TryOptionAsync<string> Serialize<T>(T document) where T : IDataModel
        {
            return async () =>
            {
                document.Id = document.Id == Guid.Empty ? Guid.NewGuid() : document.Id;
                return await Task.FromResult(_converterProvider.Serialize(document));
            };
        }

        private TryOptionAsync<Unit> IndexDocument<T>(T document, string json, Option<string> indexKey) where T : IDataModel
        {
            return indexKey
                .ToTryOptionAsync()
                .Bind<string, Unit>(key => async () =>
                {
                    var response = await _client.IndexAsync<StringResponse>(key, json);

                    if (response.TryGetServerError(out var serverError) && serverError.Status != 0)
                    {
                        throw new Exception($"Failed to read ES document. Reason: {serverError.Error.Reason} \nStacktrace: {serverError.Error.StackTrace}");
                    }

                    return Unit.Default;
                });
        }
    }
}