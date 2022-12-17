using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Command
{
    public class CommandPublisher : IPublisher<CommandData>, IConfigInitializor
    {
        public string Name => "Command";

        public string _command;
        public string _arguments;
        public string _workingDirectory;

        private readonly ILogger<CommandPublisher> _logger;

        public CommandPublisher(ILogger<CommandPublisher> logger)
        {
            _logger = logger;
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                _command = config.GetValue<string>("Command");
                _arguments = config.GetValue<string>("Arguments");
                _workingDirectory = config.GetValue<string>("WorkingDirectory");
                _workingDirectory = _workingDirectory ?? Environment.CurrentDirectory;

                return await Task.FromResult(Unit.Default);
            });
        }

        public TryOptionAsync<Unit> Publish(Option<Message<CommandData>> message)
        {
            return message
                .Bind(m => m.Payload)
                .ToTryOptionAsync()
                .Bind(m => CommandConsumer.RunCommand(_command, _workingDirectory, GetArguments(m.StdOut)))
                .Bind<List<CommandData>, Unit>(r => async () =>
                {
                    var result = r.FirstOrDefault();

                    if (result != null)
                    {
                        _logger.LogInformation($"Finished executing command: {_command}. With arguments: {result.Arguments}");
                        _logger.LogInformation($"Command output: {result.StdOut}. Std Error: {result.StdError}");
                    }

                    return await Task.FromResult(Unit.Default);
                });
        }

        private string GetArguments(string output) => string.IsNullOrEmpty(_arguments) ? output : string.Format(_arguments, output, CultureInfo.InvariantCulture);
    }
}