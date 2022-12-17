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

namespace Microservice.Exchange.Test
{
    
    [TestClass]
    public class MongodbIntegrationTest
    {
        private IConfigurationRoot _configuration;

        [TestInitialize]
        public async Task SetUp()
        {
            if (Directory.Exists("testData/mongodb"))
                Directory.Delete("testData/mongodb", true);

            _configuration = TestHelper.TestHelper.GetConfiguration($"{TestHelper.TestHelper.GetEnvironment()}.Mongodb");

            var mongodbConfigMock = new Mock<IDatabaseConfiguration>();
            mongodbConfigMock.Setup(m => m.DatabaseName).Returns("TestExchange");
            mongodbConfigMock.Setup(m => m.DocumentName).Returns("TestDataDocument");

            var mongoRepo = new MongoDbRepository<TestOutputMessage>(_configuration, mongodbConfigMock.Object, new EmptyJsonConverterProvider() );
            await mongoRepo.Delete(Builders<BsonDocument>.Filter.Where(r => true)).Match(r => {}, () => {});

        }

        [TestMethod]
        public async Task CreateExchangeFromConfiguration_WhenFileCopied_ThenTransformedToOutput()
        {
            // ARRANGE - Create a execution host (load IServices and dependencies)
            IHost host = CreateHost();

            var factory = host.Services.GetService<IExchangeFactory>();

            // ACT - Create and start Exchange. From File => Queue
            await factory
                    .CreateMessageExchange<string, TestOutputMessage>(
                        Option<IConfiguration>.Some(_configuration),
                        "TestMongodbOut")
                    .Bind(ex => ex.Start())
                    .Match(
                            r => r,
                            () => throw new Exception("Failed to Create MessageExchange"),
                            ex => throw ex);

            // ACT - Create and start Exchange. From Queue => File
            await factory
                    .CreateMessageExchange<TestOutputMessage, TestOutputMessage>(
                        Option<IConfiguration>.Some(_configuration),
                        "TestMongoDbIn")
                    .Bind(ex => ex.Start())
                    .Match(
                            r => r,
                            () => throw new Exception("Failed to Create MessageExchange"),
                            ex => throw ex);

            var dataIn = $"I am some test data: {DateTime.Now:hh.mm.ss.fff}";
            var fileNameAsGuid = Guid.Parse("68bfb27d-c3a6-44d5-9942-402a134f9a28");

            // ACT - Write data to Exchanges Input Consumer path
            File.WriteAllText($"testData/mongodb/{fileNameAsGuid}", dataIn);
            File.Move($"testData/mongodb/{fileNameAsGuid}", $"testData/mongodb/in/{fileNameAsGuid}");

            await Task.Delay(1500);

            // ASSERT - Verify Data is Transformed and Written to output
            var result = await File.ReadAllTextAsync($"testData/mongodb/out/{fileNameAsGuid}");

            var outputMessage = new EmptyJsonConverterProvider().Deserialize<TestOutputMessage>(result);

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
