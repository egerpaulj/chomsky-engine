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
using Microservice.Exchange.Endpoints.Csv;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microservice.Exchange.Test;

[TestClass]
public class CsvIntegrationTest
{
    private IConfigurationRoot _configuration;

    [TestInitialize]
    public void SetUp()
    {
        Console.WriteLine("**** Missing type name: " + typeof(CsvConsumer).FullName);
        if (Directory.Exists("testData/csv"))
            Directory.Delete("testData/csv", true);

        Directory.CreateDirectory("testData/csv/out");

        _configuration = TestHelper.TestHelper.GetConfiguration($"Csv");
    }

    [TestMethod]
    public async Task CreateExchangeFromConfiguration_CsvRead_ThenJsonFileCreated()
    {
        // ARRANGE - Create a execution host (load IServices and dependencies)
        IHost host = CreateHost();

        var factory = host.Services.GetService<IExchangeFactory>();

        // ACT - Create and start Exchange. From File => Queue
        await factory
            .CreateMessageExchange<CsvData, CsvData>(
                Option<IConfiguration>.Some(_configuration),
                "CsvDataIn"
            )
            .Bind(ex => ex.Start())
            .Match(
                r => r,
                () => throw new Exception("Failed to Create MessageExchange"),
                ex => throw ex
            );

        await Task.Delay(1000);

        var jsonContent = host.Services.GetService<IJsonConverterProvider>();
        var file = Directory.GetFiles("testData/csv/out").FirstOrDefault();

        Assert.IsNotNull(file);
        var result = jsonContent.Deserialize<CsvData>(File.ReadAllText(file));

        Assert.AreEqual("val1", result.Values["heading1"]);
        Assert.AreEqual("12.340", result.Values["heading2"]);
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder(new string[0])
            .SetupLogging()
            .ConfigureServices(
                (hostContext, services) =>
                {
                    services.ConfigureExchange();

                    var configuration = TestHelper.TestHelper.GetConfiguration() as IConfiguration;

                    services.AddTransient<IConfiguration>(s => configuration);
                    services.AddTransient<ICsvFileReader, CsvFileReader>();
                    services.AddTransient<IJsonConverterProvider, CsvJsonConverterProvider>();
                }
            )
            .Build();
    }
}
