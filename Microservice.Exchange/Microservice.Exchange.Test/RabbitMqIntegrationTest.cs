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
using Microservice.Exchange.Endpoints;
using System.IO;
using Microservice.Exchange.Core;
using Microservice.Exchange.Endpoints.Rabbitmq;
using Microservice.Amqp.Rabbitmq;

namespace Microservice.Exchange.Test
{
    
    [TestClass]
    public class RabbitMqIntegrationTest
    {
        private IConfigurationRoot _configuration;
        private AmqpBootstrapper _amqpBootstrapper;

        [TestInitialize]
        public async Task SetUp()
        {
            if (Directory.Exists("testData/rabbitmq"))
                Directory.Delete("testData/rabbitmq", true);

            _configuration = TestHelper.TestHelper.GetConfiguration($"{TestHelper.TestHelper.GetEnvironment()}.Rabbitmq");

            _amqpBootstrapper = new AmqpBootstrapper(_configuration);
            await _amqpBootstrapper.Bootstrap().Match(r => { }, () => throw new Exception("Failed to bootstrap amqp"));
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _amqpBootstrapper?.Purge().Match(r => { }, () => throw new Exception("Failed to bootstrap amqp"));
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
                        "TestRabbitMqOutExchange")
                    .Bind(ex => ex.Start())
                    .Match(
                            r => r,
                            () => throw new Exception("Failed to Create MessageExchange"),
                            ex => throw ex);

            // ACT - Create and start Exchange. From Queue => File
            await factory
                    .CreateMessageExchange<TestOutputMessage, TestOutputMessage>(
                        Option<IConfiguration>.Some(_configuration),
                        "TestRabbitMqInExchange")
                    .Bind(ex => ex.Start())
                    .Match(
                            r => r,
                            () => throw new Exception("Failed to Create MessageExchange"),
                            ex => throw ex);

            var dataIn = "I am some test data";
            var fileNameAsGuid = Guid.Parse("68bfb27d-c3a6-44d5-9942-402a134f9a28");

            // ACT - Write data to Exchanges Input Consumer path
            File.WriteAllText($"testData/rabbitmq/{fileNameAsGuid}", dataIn);
            File.Move($"testData/rabbitmq/{fileNameAsGuid}", $"testData/rabbitmq/in/{fileNameAsGuid}");

            await Task.Delay(3000);

            // ASSERT - Verify Data is Transformed and Written to output
            var result = await File.ReadAllTextAsync($"testData/rabbitmq/out/{fileNameAsGuid}");

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
                    services.ConfigureRabbitMqEndpoint();

                    var configuration = TestHelper.TestHelper.GetConfiguration() as IConfiguration;

                    services.AddTransient<IConfiguration>(s => configuration);

                })
                .Build();
        }
    }
}
