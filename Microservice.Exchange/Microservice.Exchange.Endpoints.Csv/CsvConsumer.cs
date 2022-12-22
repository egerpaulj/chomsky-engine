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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Csv;

public class CsvConsumer : IConsumer<CsvData>, IConfigInitializor
{
    private readonly ILogger<CsvConsumer> _logger;
    private readonly ICsvFileReader _csvFileReader;
    private IObserver<Either<CsvData, Exception>>? _observer;
    private IObservable<Either<CsvData, Exception>> _observable;
    private CancellationTokenSource? _cancellationTokenSource;
    private string? _filePath;


    public CsvConsumer(ILogger<CsvConsumer> logger, ICsvFileReader csvFileReader)
    {

        _logger = logger;
        _csvFileReader = csvFileReader;

        _observable = Observable.Create<Either<CsvData, Exception>>(observer =>
            {
                _observer = observer;
                return Disposable.Empty;
            });
    }

    public TryOptionAsync<Unit> End()
    {
        return async () => 
        {
             _logger.LogInformation($"Stopping CSV Consumer: {_filePath}");
             _cancellationTokenSource?.Cancel();
             return await Task.FromResult(Unit.Default);
        };
    }

    public IObservable<Either<Message<CsvData>, ConsumerException>> GetObservable()
    {
        return _observable.Select(result => 
        {
            return result
            .Map(ex => new ConsumerException(Guid.NewGuid(), ex.Message, ex, this.GetType()))
            .MapLeft(data => new Message<CsvData>()
            {
                Id = Guid.NewGuid(),
                Payload = data
            });
        });
    }

    public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
    {
        return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                _filePath = config.GetValue<string>("FilePath");

                return await Task.FromResult(Unit.Default);
            });
    }

    public TryOptionAsync<Unit> Start()
    {
        var cancelPreviousRun = new TryOptionAsync<Unit>(async () =>
        {
            _logger.LogInformation($"Starting CSV Consumer: {_filePath}");
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            return await Task.FromResult(Unit.Default);

        });
        
        #pragma warning disable CS8602 // null
        return cancelPreviousRun
                .Bind(_ => _csvFileReader.Read(_filePath, _observer, _cancellationTokenSource.Token));
        #pragma warning disable CS8602 

    }
}
