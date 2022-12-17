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
using System.Threading.Tasks;
using Grpc.Core;
using LanguageExt;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microservice.Grpc.Core
{
    public abstract class RpcServiceBase<T, R> : Rpc.RpcBase
    {
        private readonly ILogger<Rpc.RpcBase> _logger;
        private readonly IGrpcMetrics _metrics;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private readonly string _name;

        public RpcServiceBase(ILogger<Rpc.RpcBase> logger, IGrpcMetrics metrics, IJsonConverterProvider jsonConverterProvider)
        {
            _metrics = metrics;
            _jsonConverterProvider = jsonConverterProvider;
            _logger = logger;
            _name = this.GetType().Name;
        }


        /// <summary>
        /// Executes Deserializes the request's JSON, executes the concrete implementation, and returns the response (Serialized within RpcReply)
        /// </summary>
        public override async Task<RpcReply> Execute(RpcRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received Grpc request on {_name}, CorrelationId: {request.Correlationid}");
            _metrics.IncReceived(_name);

            var reply = new RpcReply();
            reply.Correlationid = request.Correlationid;
            try
            {
                var genericRequest = _jsonConverterProvider.Deserialize<T>(request.Request);

                var result = await Execute(genericRequest).Match(x => x, () => throw new Exception("Grpc Service Error - Nothing returned"), ex => throw ex);

                if (result is not Unit)
                {
                    reply.Reponse = _jsonConverterProvider.Serialize(result);
                }

                _logger.LogInformation($"Successfully processed Grpc request on {_name}, CorrelationId: {request.Correlationid}");
                _metrics.IncServerSuccess(_name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Grpc Error on {_name}, CorrelationId: {request.Correlationid}");
                _metrics.IncError(_name);
                reply.Errors = $"{ex.Message}:{ex.StackTrace}";
            }

            return reply;
        }

        /// <summary>
        /// Services should implement this to Execute the RPC operation on T and return the result R.
        /// </summary>
        protected abstract TryOptionAsync<R> Execute(Option<T> request);
    }
}
