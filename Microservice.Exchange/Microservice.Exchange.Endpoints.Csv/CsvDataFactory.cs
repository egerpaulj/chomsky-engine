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
using System.Globalization;
using System.Text.RegularExpressions;
using LanguageExt;

namespace Microservice.Exchange.Endpoints.Csv;

public static class CsvDataFactory
{
    private static Regex _regExCsvSplitter = new Regex(@",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
    public static TryOptionAsync<CsvData> CreateCsv<T>(Option<T> data)
    {
        return data
            .ToTryOptionAsync()
            .Bind<T, CsvData>(d =>
                async () =>
                {
                    // Won't happen - ignore compiler warnings.
                    if(d == null)
                        throw new NullReferenceException();

                    var type = d.GetType();

                    var csv = type
                                .GetProperties()
                                .Select(p => 
                                    new { 
                                        p.Name, 
                                        Value = p.GetValue(d, null)?.ToString() ?? string.Empty })
                                .ToList();

                    var headers = csv.Select(item => item.Name).ToList();
                    var values = csv.Select(item => item.Value).ToList();

                    return await Task.FromResult(new CsvData(headers, values));
                });
    }

    public static TryOptionAsync<CsvData> CreateCsv(Option<string[]> headers, Option<string> line)
    {
        return headers
            .SelectMany(h => line, (h, l) => new { h, l })
            .ToTryOptionAsync()
            .SelectMany(args =>
                ExtractData(args.l), (args, data)
                    => new CsvData(args.h.ToList(), data));

    }

    private static TryOptionAsync<List<string>> ExtractData(string line)
    {
        return async () =>
        {
            return await Task.FromResult(_regExCsvSplitter.Split(line ?? string.Empty).ToList());
        };
    }
}
