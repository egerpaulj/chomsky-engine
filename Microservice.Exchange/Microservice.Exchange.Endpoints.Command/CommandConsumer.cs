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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Exchange.Core.Polling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Command
{
    public class CommandConsumer : IConsumer<CommandData>, IConfigInitializor
    {
        private IObserver<Either<Message<CommandData>, ConsumerException>> _observer;
        private IObservable<Either<Message<CommandData>, ConsumerException>> _observable;
        private readonly ILogger<CommandConsumer> _logger;
        private PollingConsumer<CommandData> _pollingConsumer;
        private string _command;
        private string _arguments;
        private string _workingDirectory;

        public CommandConsumer(ILogger<CommandConsumer> logger)
        {
            _logger = logger;

            _observable = Observable.Create<Either<Message<CommandData>, ConsumerException>>(observer =>
            {
                _observer = observer;
                return Disposable.Empty;
            });
        }

        public TryOptionAsync<Unit> End()
        {
            return _pollingConsumer.End();
        }

        public IObservable<Either<Message<CommandData>, ConsumerException>> GetObservable()
        {
            return _observable;
        }

        public TryOptionAsync<Unit> Start()
        {
            return _pollingConsumer.Start(_observer);
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                _command = config.GetValue<string>("Command");
                _arguments = config.GetValue<string>("Arguments");
                _workingDirectory = config.GetValue<string>("WorkingDirectory");
                _workingDirectory = _workingDirectory ?? Environment.CurrentDirectory;

                _pollingConsumer = new PollingConsumer<CommandData>(_logger, config, () => RunCommand(_command, _workingDirectory, _arguments));

                return await Task.FromResult(Unit.Default);
            });
        }

        internal static TryOptionAsync<List<CommandData>> RunCommand(string command, string workingDirectory, string arguments = null)
        {
            return async () => 
            {
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.CreateNoWindow = true;
                processStartInfo.UseShellExecute = false;
                processStartInfo.FileName = command;
                processStartInfo.Arguments= arguments;
                processStartInfo.WorkingDirectory = workingDirectory;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardOutput = true;
                
                
                var process = Process.Start(processStartInfo);
                await process.WaitForExitAsync();

                if(process.ExitCode != 0)
                {
                    throw new Exception($"Error executing command: {command}, arguments: {arguments}, \nStdError: {await process.StandardError.ReadToEndAsync()}, \nStdOut: {await process.StandardOutput.ReadToEndAsync()}");
                }
                
                return await Task.FromResult(new List<CommandData>{new CommandData()
                {
                    Command = command,
                    Arguments = arguments,
                    StdError = await process.StandardError.ReadToEndAsync(),
                    StdOut = await process.StandardOutput.ReadToEndAsync(),
                    Id = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid()
                }});
            };
        }
    }
}