//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2024  Paul Eger

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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Results;
using Crawler.DataModel;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;

public class CrawlResponseMongoDbTransformer<TIn>(string name) : IBertrandTransformer
    where TIn : CrawlResponse
{
    public string Name => name;

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var message = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));

            var output = new Message<object>();
            output = inputMessage.CopyData(output);
            output.RoutingKey = name;

            output.Payload = MapToMongodb(message);

            return await Task.FromResult(output);
        };
    }

    private static CrawlResponseModel MapToMongodb(CrawlResponse response)
    {
        return new CrawlResponseModel
        {
            CorrelationId = response.CorrelationId,
            CrawlerId = response.CrawlerId,
            Raw = response.Raw,
            Result = response.Result,
            Uri = response.Uri,
            IsIndexed = response.ShouldIndex,
        };
    }
}
