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
using System.Threading;
using Microservice.Serialization;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.DataTypes.Serialisation;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;


namespace Microservice.Exchange.Endpoints
{
    /// <summary>
    /// File based Endpoint for Input/Output/Deadletter.
    /// </summary>
    public class FileEndpoint<T, R> : IConsumer<T>, IPublisher<R>, IConfigInitializor
    {
        private readonly ILogger<FileEndpoint<T, R>> _logger;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private FileSystemWatcher _inputFileSystemWatcher;
        private DirectoryInfo _inDirectory;
        private DirectoryInfo _outDirectory;
        private DirectoryInfo _errorDirectory;
        private static SemaphoreSlim _ioSemaphore = new SemaphoreSlim(1, 1);

        public string Name => "File";

        public FileEndpoint(ILogger<FileEndpoint<T, R>> logger, IJsonConverterProvider jsonConverterProvider)
        {
            _logger = logger;
            _jsonConverterProvider = jsonConverterProvider;
        }

        public TryOptionAsync<Unit> End()
        {
            return async () =>
            {
                _inputFileSystemWatcher.Dispose();

                return await Task.FromResult(Unit.Default);
            };
        }

        public IObservable<Either<Message<T>, ConsumerException>> GetObservable()
        {
            return
            Observable.Create<FileSystemEventArgs>(o =>
            {
                FileSystemEventHandler eventHandler = (obj, args) => o.OnNext(args);
                _inputFileSystemWatcher.Created += eventHandler;
                _inputFileSystemWatcher.Disposed += (obj, args) => _inputFileSystemWatcher.Created -= eventHandler;

                foreach (var existingFile in _inDirectory.GetFiles())
                    o.OnNext(new FileSystemEventArgs(WatcherChangeTypes.Created, _inDirectory.FullName, existingFile.Name));

                return () => { _inputFileSystemWatcher.Created -= eventHandler; };
            })
            .Select(
                args =>
                {
                    var either = new Either<Message<T>, ConsumerException>();

                    if (!Guid.TryParse(args.Name, out var id))
                        id = Guid.NewGuid();

                    try
                    {
                        var inData = _jsonConverterProvider.Deserialize<T>(File.ReadAllText(args.FullPath, IJsonConverterProvider.TextEncoding));
                        Option<IMessage> imessage = (inData as IMessage) == null ? Option<IMessage>.None : Option<IMessage>.Some(inData as IMessage);
                        either = new Message<T>(imessage)
                        {
                            Payload = inData,
                            Id = id
                        };
                    }
                    catch (Exception e)
                    {
                        var errorMessage = $"Failed to consumer file: {args.FullPath}";
                        _logger.LogError(e, errorMessage);
                        File.Copy(args.FullPath, Path.Combine(_errorDirectory.FullName, $"{args.Name}_error"), overwrite: true);
                        either = new ConsumerException(id, errorMessage, e, this.GetType());
                    }
                    finally
                    {
                        File.Delete(args.FullPath);
                    }

                    return either;
                });
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration
                .ToTryOptionAsync()
                .Bind(InitializeDirectories);
        }

        public TryOptionAsync<Unit> Start()
        {
            // Will start when subscribed to the observable
            return async () => await Task.FromResult(Unit.Default);
        }

        public TryOptionAsync<Unit> Publish(Option<Message<R>> message)
        {
            return async () =>
               {
                   var dataIn = message.Match(r => r, () => throw new Exception("Unable to publish an empty request"));
                   var id = dataIn.Id.Match(i => i, () => Guid.NewGuid());
                   try
                   {
                       var json = dataIn.Payload.Match(p => _jsonConverterProvider.Serialize(p), () => "Empty message");

                       File.WriteAllText(Path.Combine(_outDirectory.FullName, id.ToString()), json, IJsonConverterProvider.TextEncoding);

                       return await Task.FromResult(Unit.Default);
                   }
                   catch (Exception ex)
                   {
                       throw new ConsumerException(id, "Error during reponse publish", ex, GetType());
                   }
               };
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<T>> message)
        {
            return async () =>
               {
                   var dataIn = message.Match(r => r, () => throw new Exception("Unable to publish an empty request"));
                   var id = dataIn.Message.Id.Match(i => i, () => Guid.NewGuid());
                   try
                   {
                       var json = _jsonConverterProvider.Serialize(dataIn);

                       File.WriteAllText(Path.Combine(_errorDirectory.FullName, id.ToString()), json, IJsonConverterProvider.TextEncoding);

                       return await Task.FromResult(Unit.Default);
                   }
                   catch (Exception ex)
                   {
                       throw new ConsumerException(id, "Error during Deadletter publish", ex, GetType());
                   }
               };
        }

        private TryOptionAsync<Unit> InitializeDirectories(IConfiguration configuration)
        {
            return async () =>
            {
                _inDirectory = new DirectoryInfo(configuration.GetValue<string>("InPath") ?? Environment.CurrentDirectory);
                _outDirectory = new DirectoryInfo(configuration.GetValue<string>("OutPath") ?? Environment.CurrentDirectory);
                _errorDirectory = new DirectoryInfo(configuration.GetValue<string>("ErrorPath") ?? Environment.CurrentDirectory);

                await Init();

                _inputFileSystemWatcher = new FileSystemWatcher(_inDirectory.FullName);
                _inputFileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName;
                _inputFileSystemWatcher.EnableRaisingEvents = true;
                _inputFileSystemWatcher.Filter = configuration.GetValue<string>("FileFilter") ?? "*.*";

                return await Task.FromResult(Unit.Default);
            };
        }

        private async Task Init()
        {

            if (AllDirectoriesExists())
            {
                return;
            }

            await _ioSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (AllDirectoriesExists())
                {
                    return;
                }

                CreateIfNotExist(_inDirectory);
                CreateIfNotExist(_outDirectory);
                CreateIfNotExist(_errorDirectory);
            }
            finally
            {
                _ioSemaphore.Release();
            }

        }

        private void CreateIfNotExist(DirectoryInfo info)
        {
            if (!info.Exists)
                info.Create();
        }

        private bool AllDirectoriesExists() => _inDirectory.Exists && _outDirectory.Exists && _errorDirectory.Exists;
    }
}