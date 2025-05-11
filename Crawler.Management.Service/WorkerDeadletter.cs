using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.RequestHandling.Core;
using Microservice.Exchange;
using Microservice.Exchange.Bertrand;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Management.Service;

public class WorkerDeadletter(
    IErrorMessageProcessor errorMessageProcessor,
    IConfiguration configuration,
    IMongRepositoryFactory mongodbFactory
) : BackgroundService
{
    private PeriodicTimer periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(15));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var deadletterStoreConfiguration = new DatabaseConfiguration(
            "bertrand_exchange_crawler_deadletter",
            configuration
        );

        var bertrandStateDeadletterRepository =
            mongodbFactory.CreateRepository<BertrandStateDataModel>(deadletterStoreConfiguration);

        do
        {
            await errorMessageProcessor.ProcessMessages(
                bertrandStateDeadletterRepository,
                stoppingToken
            );
        } while (await periodicTimer.WaitForNextTickAsync(stoppingToken));
    }
}
