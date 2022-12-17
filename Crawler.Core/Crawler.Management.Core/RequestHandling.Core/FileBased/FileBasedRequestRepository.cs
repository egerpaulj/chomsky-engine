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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Serialization;
using Newtonsoft.Json;

namespace Crawler.Management.Core.RequestHandling.Core.FileBased
{
    public class FileBasedRequestRepository : IRequestProvider, IResponsePublisher, IFailurePublisher, IRequestPublisher
    {
        private readonly int PollIntervalMs;
        private readonly DirectoryInfo _workingDirectory;
        private readonly DirectoryInfo _inDirectory;
        private readonly DirectoryInfo _outDirectory;
        private readonly DirectoryInfo _errorDirectory;
        private readonly SemaphoreSlim _ioSemaphore = new SemaphoreSlim(1, 1);
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private FileSystemWatcher _inputFileSystemWatcher;

        public FileBasedRequestRepository(IJsonConverterProvider jsonConverterProvider) : this(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "RequestRepository")), 5000, jsonConverterProvider)
        {

        }

        public FileBasedRequestRepository(Option<DirectoryInfo> workingDirectory, int pollIntervalInMs, IJsonConverterProvider jsonConverterProvider)
        {
            _workingDirectory = workingDirectory.Match(d => d, () => new DirectoryInfo(Environment.CurrentDirectory));

            _inDirectory = new DirectoryInfo(Path.Combine(_workingDirectory.FullName, "in"));
            _outDirectory = new DirectoryInfo(Path.Combine(_workingDirectory.FullName, "out"));
            _errorDirectory = new DirectoryInfo(Path.Combine(_workingDirectory.FullName, "error"));
            _jsonConverterProvider = jsonConverterProvider;

            PollIntervalMs = pollIntervalInMs;

            var res = Init().Match(r => r, () => throw new Exception("Failed to initialize folders in File repository")).Result;

            _inputFileSystemWatcher = new FileSystemWatcher(_inDirectory.FullName);
            _inputFileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime|NotifyFilters.FileName;
            _inputFileSystemWatcher.EnableRaisingEvents = true;
            _inputFileSystemWatcher.Filter = "*.*";
        }

        public FileBasedRequestRepository(Option<DirectoryInfo> workingDirectory, IJsonConverterProvider jsonConverterProvider)
        : this(workingDirectory, 100, jsonConverterProvider)
        {
        }

        public IObservable<Either<CrawlRequest, CrawlRequestException>> GetObservable(Option<CancellationToken> token, Func<CrawlRequest, Task<LanguageExt.Unit>> crawlTask)
        {
            return 
            Observable.Create<FileSystemEventArgs>(o =>
            {
                FileSystemEventHandler eventHandler = (obj, args) => o.OnNext(args);
                _inputFileSystemWatcher.Created += eventHandler;

                return () => { _inputFileSystemWatcher.Created -= eventHandler; };
            })
            .Select(
                args =>
                {
                    if (token.Match(t => t, () => new CancellationToken()).IsCancellationRequested)
                        throw new OperationCanceledException();

                    var either = new Either<CrawlRequest, CrawlRequestException>();

                    try
                    {
                        
                        var request = JsonConvert.DeserializeObject<CrawlRequest>(File.ReadAllText(args.FullPath, IJsonConverterProvider.TextEncoding), _jsonConverterProvider.GetJsonConverters());
                        File.Move(args.FullPath, Path.Combine(_workingDirectory.FullName, args.Name), overwrite: true);
                        either = request;
                        return either;
                    }catch(Exception e)
                    {
                        // ToDo Log 
                        File.Move(args.FullPath, Path.Combine(_errorDirectory.FullName, $"args.Name_error"), overwrite: true);
                        either = new CrawlRequestException(e);
                        return either;
                    }


                }
            ).SelectMany(eitherRes => Observable.FromAsync<Either<CrawlRequest, CrawlRequestException>>(async () =>
            {
                return await eitherRes.MatchAsync(ex => eitherRes, async req =>
                {
                    var crawlEither = new Either<CrawlRequest, CrawlRequestException>();
                    try{
                        await crawlTask(req);
                        crawlEither = req;
                    }
                    catch (Exception e)
                    {
                        crawlEither = new CrawlRequestException(req, e);
                    }
                    return crawlEither;
                });
            }));
        }

        public TryOptionAsync<Unit> PublishFailure(Option<CrawlRequest> request)
        {
            return Init().Bind<Unit, Unit>(u => async () =>
            {
                var crawlrequest = request.Match(r => r, () => throw new CrawlException("Unable to publish an empty request", ErrorType.PublishError));
                try
                {
                    var json = JsonConvert.SerializeObject(crawlrequest);

                    File.WriteAllText(Path.Combine(_errorDirectory.FullName, crawlrequest.CrawlId.Match(id => id.ToString(), () => "unknown")), json, IJsonConverterProvider.TextEncoding);

                    return await Task.FromResult(Unit.Default);
                }
                catch (Exception ex)
                {
                    throw new CrawlException("Error during failure publish", ErrorType.PublishError, ex);
                }
            });
        }

        public TryOptionAsync<Unit> PublishResponse(Option<CrawlResponse> response)
        {
            return Init().Bind<Unit, Unit>(u => async () =>
               {
                   var crawlresponse = response.Match(r => r, () => throw new Exception("Unable to publish an empty response"));
                   var document = crawlresponse.Result.Match(r => r, () => new Document());

                   try
                   {
                       var json = JsonConvert.SerializeObject(crawlresponse);

                       File.WriteAllText(Path.Combine(_outDirectory.FullName, crawlresponse.CrawlerId.Match(id => id.ToString(), () => "unknown")), json, IJsonConverterProvider.TextEncoding);

                       var requestMatch = _workingDirectory.GetFiles().FirstOrDefault(f => f.Name.Contains(crawlresponse.CrawlerId.Match(i => i.ToString(), () => "unknown")));

                       if (requestMatch != null && requestMatch.Exists)
                           requestMatch.Delete();


                       System.Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: Finished Processing request");
                       System.Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: RESPONSE: \n {document.GetBriefSummary()}");

                       return await Task.FromResult(Unit.Default);
                   }
                   catch (Exception ex)
                   {
                       throw new CrawlException("Error during reponse publish", ErrorType.PublishError, ex);
                   }
               });
        }

        public TryOptionAsync<Unit> PublishRequest(Option<CrawlRequest> request)
        {
            return Init().Bind<Unit, Unit>(u => async () =>
               {
                   var crawlRequest = request.Match(r => r, () => throw new Exception("Unable to publish an empty request"));
                   try
                   {
                       var json = JsonConvert.SerializeObject(crawlRequest);

                       File.WriteAllText(Path.Combine(_inDirectory.FullName, crawlRequest.CrawlId.Match(id => id.ToString(), () => "unknown")), json, IJsonConverterProvider.TextEncoding);

                       var requestMatch = _workingDirectory.GetFiles().FirstOrDefault(f => f.Name.Contains(crawlRequest.CrawlId.Match(i => i.ToString(), () => "unknown")));

                       if (requestMatch != null && requestMatch.Exists)
                           requestMatch.Delete();

                       return await Task.FromResult(Unit.Default);
                   }
                   catch (Exception ex)
                   {
                       throw new CrawlException("Error during reponse publish", ErrorType.PublishError, ex);
                   }
               });
        }

        private TryOptionAsync<Unit> Init()
        {
            return async () =>
            {
                if (AllDirectoriesExists())
                {
                    return await Task.FromResult(Unit.Default);
                }

                await _ioSemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (AllDirectoriesExists())
                    {
                        return await Task.FromResult(Unit.Default);
                    }

                    CreateIfNotExist(_inDirectory);
                    CreateIfNotExist(_outDirectory);
                    CreateIfNotExist(_errorDirectory);
                }
                finally
                {
                    _ioSemaphore.Release();
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        private void CreateIfNotExist(DirectoryInfo info)
        {
            if (!info.Exists)
                info.Create();
        }

        private bool AllDirectoriesExists() => _workingDirectory.Exists && _inDirectory.Exists && _outDirectory.Exists && _errorDirectory.Exists;
    }
}