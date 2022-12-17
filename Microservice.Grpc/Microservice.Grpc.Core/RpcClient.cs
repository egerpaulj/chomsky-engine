//      Microservice Grpc Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger                                                                                                                                                                     

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
using Grpc.Net.Client;
using LanguageExt;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microservice.Grpc.Core
{
    /// <summary>
    /// Rpc Client interface. Allows executing an RPC Request T, the returns the Response R.
    /// </summary>
    public interface IRpcClient<T, R>
    {
        /// <summary>
        /// Sends Request T to the server, the returns the Response R from the server.
        /// </summary>
        TryOptionAsync<R> Execute(T request, string serverAddress, Guid correlationId);
    }

    public class RpcClient<T, R> : IRpcClient<T, R>
    {
        private readonly ILogger _logger;
        private readonly IGrpcMetrics _metrics;
        private readonly string _name;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        public RpcClient(ILogger logger, IGrpcMetrics metrics, IJsonConverterProvider jsonConverterProvider)
        {
            _jsonConverterProvider = jsonConverterProvider;
            _metrics = metrics;
            _logger = logger;
            _name = $"{typeof(T).Name}<->{typeof(R).Name}";
        }

        public TryOptionAsync<R> Execute(T request, string serverAddress, Guid correlationId)
        {
            return async () =>
            {
                try
                {
                    using var channel = GrpcChannel.ForAddress(serverAddress);
                    var client = new RpcClient(channel);

                    _metrics.IncSent(_name);
                    _logger.LogInformation($"Sending request to Grpc server. ServerName: {serverAddress}, CorrelationId: {correlationId}, Req-Res: {_name}");

                    var reply = await client.ExecuteAsync(new RpcRequest
                    {
                        Correlationid = correlationId.ToString(),
                        Request = _jsonConverterProvider.Serialize(request),
                    });

                    if (!string.IsNullOrEmpty(reply.Errors))
                    {
                        throw new GrpcException(correlationId.ToString(), _name, reply.Errors);
                    }

                    _metrics.IncReplyReceived(_name);
                    _logger.LogInformation($"Received response from Grpc server. ServerName: {serverAddress}, CorrelationId: {correlationId}, Req-Res: {_name}");
                    return _jsonConverterProvider.Deserialize<R>(reply.Reponse);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Grpc Error: ServerName: {serverAddress}, CorrelationId: {correlationId}, Req-Res: {_name}", e);
                    _metrics.IncClientError(_name);
                    throw;
                }
            };
        }
    }
}