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


using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microservice.Elasticsearch.Repo;
using Moq;
using Microsoft.Extensions.Configuration;
using Microservice.Serialization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microservice.Elasticsearch.Test
{
    [TestClass]
    public class IntegrationTest
    {
        private const string TestIndexKey = "integrationtestindex";
        private ElasticsearchRepository _testee;

        private ElasticLowLevelClient _client;

        [TestInitialize]
        public async Task Setup()
        {

            var configuration = TestHelper.TestHelper.GetConfiguration();

            _testee = new ElasticsearchRepository(Mock.Of<ILogger<ElasticsearchRepository>>(), configuration, Mock.Of<IJsonConverterProvider>());

            var node = new Uri(configuration.GetConnectionString(ElasticsearchRepository.ConnectionStringKey));
            var config = new ConnectionConfiguration(node);
            _client = new ElasticLowLevelClient(config);


            await _client.Indices.DeleteAsync<StringResponse>(TestIndexKey);
        }

        [TestMethod]
        public async Task IndexObject_ThenObjectIndexed()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            var testData = new TestDataModel()
            {
                Id = id,
                Data = $"Some test information: {id}"
            };

            Console.WriteLine(testData.Id);

            // ACT
            await _testee
                .IndexDocument<TestDataModel>(testData, TestIndexKey)
                .Match(r => r, () => throw new Exception("Failed to index document"));

            await Task.Delay(1200);

            // ASSERT
            var query = @"{""query"": {
                    ""match"": {
                        ""Data"": {
                            ""query"": ""test""
                        }
                    }
                }}";

            var resultList = await _testee.Search<TestDataModel>(TestIndexKey, query).Match(r => r, () => throw new Exception("Empty"), ex => throw ex);
            var resultData = resultList.First();
            Assert.AreEqual(testData.Data, resultData.Data);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _client.Indices.Delete<StringResponse>(TestIndexKey);
        }
    }
}
