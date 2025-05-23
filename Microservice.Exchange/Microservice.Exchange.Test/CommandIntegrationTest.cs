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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Core.Middlewear;
using Microservice.Exchange.Core;
using Microservice.Exchange.Endpoints.Command;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microservice.Exchange.Test
{
    [TestClass]
    public class CommandIntegrationTest
    {
        [TestMethod]
        public void TestDsl()
        {
            var example =
                "DataIn:rabbitmq?host=10.137.0.1&port=5672&queue=queuename&username=guestsPAssword=guest,DataOut:console";
            // var completeExample = "from:rabbitmq?host=10.137.0.1&port=5672&queue=queuename&username=guest&password=guest,filter:filterName, transform:tranformername, to:console";

            var components = example.Split(",");

            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

            configuration["Amqp:Contexts:DslExchange:Exchange"] = "Exchange";
            configuration["Amqp:Contexts:TestSomeother:Exchange"] = "ExchangeAnother";
            _ = configuration.GetSection("Amqp").GetSection("Contexts").GetChildren();

            foreach (var component in components)
            {
                var uri = new Uri(component);
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine($"Direction: {uri.Scheme}");
                Console.WriteLine($"Target component: {uri.AbsolutePath}");
                var compConfigKey = $"MessageExchanges:dsl:{uri.Scheme}:{uri.AbsolutePath}";

                Console.ForegroundColor = color;
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var query = uri.Query[1..];
                    var parameters = query.Split("&");

                    if (parameters.Length != 0)
                        foreach (var q in parameters)
                        {
                            var kvp = q.Split("=");
                            Console.WriteLine($"Configuration Key: {kvp[0]}, Value: {kvp[1]}");
                            var paramConfigKey = $"{compConfigKey}:{kvp[0]}";
                            configuration[paramConfigKey] = kvp[1];
                        }
                }
            }
        }

        private IConfigurationRoot _configuration;

        [TestInitialize]
        public void SetUp()
        {
            if (Directory.Exists("testData/command"))
                Directory.Delete("testData/command", true);

            Directory.CreateDirectory("testData/command/out");

            _configuration = TestHelper.TestHelper.GetConfiguration(
                $"{TestHelper.TestHelper.GetEnvironment()}.Command"
            );
        }

        [TestMethod]
        public async Task CreateExchangeFromConfiguration_WhenFileCopied_ThenTransformedToOutput()
        {
            // ARRANGE - Create a execution host (load IServices and dependencies)
            IHost host = CreateHost();

            var factory = host.Services.GetService<IExchangeFactory>();

            // ACT - Create and start Exchange. From File => Queue
            await factory
                .CreateMessageExchange<CommandData, CommandData>(
                    Option<IConfiguration>.Some(_configuration),
                    "CommandAsDatIn"
                )
                .Bind(ex => ex.Start())
                .Match(
                    r => r,
                    () => throw new Exception("Failed to Create MessageExchange"),
                    ex => throw ex
                );

            await Task.Delay(1000);

            // ACT - Create and start Exchange. From Queue => File
            await factory
                .CreateMessageExchange<CommandData, CommandData>(
                    Option<IConfiguration>.Some(_configuration),
                    "CommandAsDatOut"
                )
                .Bind(ex => ex.Start())
                .Match(
                    r => r,
                    () => throw new Exception("Failed to Create MessageExchange"),
                    ex => throw ex
                );

            await Task.Delay(2000);

            // ASSERT - Verify Data is Transformed and Written to output
            var outputfile = Directory.GetFiles($"testData/command/out/").FirstOrDefault();
            Assert.IsNotNull(outputfile);

            var result = await File.ReadAllTextAsync(outputfile);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("appsettings.Development.json"));

            host.Dispose();
        }

        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder([])
                .SetupLogging()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.ConfigureExchange();

                        var configuration =
                            TestHelper.TestHelper.GetConfiguration() as IConfiguration;

                        services.AddTransient<IConfiguration>(s => configuration);
                    }
                )
                .Build();
        }
    }
}
