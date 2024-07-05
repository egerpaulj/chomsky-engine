using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Exchange.Core.Polling;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Command;

public class CommandBertranConsumer : BertrandPollingConsumerBase
{
    public CommandBertranConsumer(
        string name,
        IPollingConsumerFactory pollingConsumerFactory,
        ILogger<CommandBertranConsumer> logger,
        IJsonConverterProvider jsonConverterProvider,
        string command,
        string workingDirectory,
        int intervalMs,
        string arguments = null
        )
        : base(name, logger)
    {
        var runCommand = CommandConsumer.RunCommand(
            command, workingDirectory, arguments
            );

        var commandWithObjectResults = runCommand.Bind<List<CommandData>, List<object>>(data => async () => await Task.FromResult(data.Select(d => d as object).ToList()));
        PollingConsumer = pollingConsumerFactory.Create<object>(
            () => commandWithObjectResults, intervalMs);
    }

    protected override IPollingConsumer<object> PollingConsumer { get; }
}