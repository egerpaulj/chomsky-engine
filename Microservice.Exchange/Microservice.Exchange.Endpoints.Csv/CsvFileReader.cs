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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using LanguageExt;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Csv;

public interface ICsvFileReader
{
    TryOptionAsync<Unit> Read(Option<string> filePath, IObserver<Either<CsvData, Exception>> observer, CancellationToken cancelToken);
}

public class CsvFileReader : ICsvFileReader
{
    private readonly ILogger<CsvFileReader> _logger;

    public CsvFileReader(ILogger<CsvFileReader> logger)
    {
        _logger = logger;
    }

    public TryOptionAsync<Unit> Read(Option<string> filePath, IObserver<Either<CsvData, Exception>> observer, CancellationToken cancelToken)
    {
        return filePath.ToTryOptionAsync().Bind<string, Unit>( file => async () =>
        {
            if(observer == null)
                throw new ArgumentNullException("observer should not be null");

            var lineNumber = 1;

            _logger.LogInformation($"Starting to read file: {file}");

            using (StreamReader sr = new StreamReader(file))
            {
                var firstLine = await sr.ReadLineAsync();
                var headers = firstLine?.Split(",");

                lineNumber++;

                while (sr.Peek() >= 0)
                {
                    if (cancelToken.IsCancellationRequested)
                        return await Task.FromResult(Unit.Default);

                    try
                    {
                        var data = await sr.ReadLineAsync();

                        // For compiler warnings - Options are passed on to the CsvDataFactory.
                        if (data == null || headers == null)
                            throw new InvalidDataException("CSV Headers and Data should exist");

                        var csvData = await CsvDataFactory.CreateCsv(headers, data).Match(d => d, () => throw new Exception("Failed to "));
                        observer.OnNext(csvData);

                        _logger.LogInformation($"Processed line: {lineNumber}, file: {file}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to process line: {lineNumber}, file: {file}");
                        observer.OnNext(ex);
                    }

                    lineNumber++;
                }
            }

            return await Task.FromResult(Unit.Default);
        });
    }
}
