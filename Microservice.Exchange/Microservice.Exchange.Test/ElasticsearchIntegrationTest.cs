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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microservice.Core.Middlewear;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using LanguageExt;
using System.Threading.Tasks;
using System;
using System.IO;
using Microservice.Exchange.Core;
using Microservice.Exchange.Endpoints.Mongodb;
using Microservice.Mongodb.Repo;
using Moq;
using MongoDB.Driver;
using MongoDB.Bson;
using Microservice.Elasticsearch.Repo;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Test
{
    
    [TestClass]
    public class ElasticsearchIntegrationTest
    {
        private IConfigurationRoot _configuration;

        [TestInitialize]
        public async Task SetUp()
        {
            if (Directory.Exists("testData/elasticsearch"))
                Directory.Delete("testData/elasticsearch", true);

            _configuration = TestHelper.TestHelper.GetConfiguration($"{TestHelper.TestHelper.GetEnvironment()}.Elasticsearch");
            await DeleteTestIndex();

        }

        [TestCleanup]
        public async Task CleanUp()
        {
            await DeleteTestIndex();
        }

        private async Task DeleteTestIndex()
        {
            var elasticsearchRepo = new ElasticsearchRepository(Mock.Of<ILogger<ElasticsearchRepository>>(), _configuration, new EmptyJsonConverterProvider());
            await elasticsearchRepo.Delete("testexchange").Match(r => { }, () => { });
        }

        [TestMethod]
        public async Task CreateExchangeFromConfiguration_WhenFileCopied_ThenTransformedToOutput()
        {
            // ARRANGE - Create a execution host (load IServices and dependencies)
            IHost host = CreateHost();

            var factory = host.Services.GetService<IExchangeFactory>();

            // ACT - Create and start Exchange. From File => Queue
            await factory
                    .CreateMessageExchange<string, TestEsOutputMessage>(
                        Option<IConfiguration>.Some(_configuration),
                        "ElasticOut")
                    .Bind(ex => ex.Start())
                    .Match(
                            r => r,
                            () => throw new Exception("Failed to Create MessageExchange"),
                            ex => throw ex);

            // ACT - Create and start Exchange. From Queue => File
            await factory
                    .CreateMessageExchange<TestEsOutputMessage, TestEsOutputMessage>(
                        Option<IConfiguration>.Some(_configuration),
                        "ElasticIn")
                    .Bind(ex => ex.Start())
                    .Match(
                            r => r,
                            () => throw new Exception("Failed to Create MessageExchange"),
                            ex => throw ex);

            var dataIn = $"I am some test data: {DateTime.Now:hh.mm.ss.fff}";
            var fileNameAsGuid = Guid.Parse("68bfb27d-c3a6-44d5-9942-402a134f9a28");

            // ACT - Write data to Exchanges Input Consumer path
            File.WriteAllText($"testData/elasticsearch/{fileNameAsGuid}", dataIn);
            File.Move($"testData/elasticsearch/{fileNameAsGuid}", $"testData/elasticsearch/in/{fileNameAsGuid}");

            await Task.Delay(3000);

            // ASSERT - Verify Data is Transformed and Written to output
            var result = await File.ReadAllTextAsync($"testData/elasticsearch/out/{fileNameAsGuid}");

            var outputMessage = new EmptyJsonConverterProvider().Deserialize<TestEsOutputMessage>(result);

            Assert.AreEqual(dataIn, outputMessage.OriginalData);
            Assert.AreEqual(TestDataTransformer.TestData, outputMessage.EnrichedData);

            host.Dispose();
        }

        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder(new string[0])
                .SetupLogging()
                .ConfigureServices((hostContext, services) =>
                {
                    services.ConfigureExchange();

                    var configuration = TestHelper.TestHelper.GetConfiguration() as IConfiguration;

                    services.AddTransient<IConfiguration>(s => configuration);

                })
                .Build();
        }
    }
}
