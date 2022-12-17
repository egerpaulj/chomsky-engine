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
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Exceptions;
using Crawler.WebDriver.Core.Requests;
using LanguageExt;
using Microservice.Grpc.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Crawler.WebDriver.Grpc.Client
{
    public class GrpcWebDriverService : IWebDriverService
    {
        private readonly ILogger<GrpcWebDriverService> _logger;
        public const string GrpcServerConfigurationKey = "WebDriverServerUri";

        private readonly RpcClient<DriverRequest, DriverResponse> _rpcClient;
        private readonly string _grpcServiceUri;

        public GrpcWebDriverService(IConfiguration configuration, ILogger<GrpcWebDriverService> logger, IGrpcMetrics metrics, IJsonConverterProvider jsonConverterProvider)
        {
            _logger = logger;
            _rpcClient = new RpcClient<DriverRequest, DriverResponse>(logger, metrics, jsonConverterProvider);
            _grpcServiceUri = configuration.GetValue<string>(GrpcServerConfigurationKey);
        }
        
        public TryOptionAsync<FileData> Download(Option<DownloadRequest> request)
        {
            var correlationId = request.Bind(r => r.CorrelationId).Match(c => c, () => Guid.NewGuid());
            var uri = request.Bind(r => r.Uri).Match( u => u, () => throw new DownloadException($"Failed to download. Uri is empty"));

            _logger.LogInformation($"Sending Grpc Request. Download data: {uri}, CorrelationId: {correlationId}");
            return _rpcClient.Execute(new DriverRequest()
            {
                DownloadRequest = request,
            }, _grpcServiceUri, correlationId )
            .Bind<DriverResponse, FileData>(response => async () => await Task.FromResult(response.DownloadRequest.Match(d => d, () => throw new DownloadException($"Failed to download data: {uri}. Data is empty"))));
        }

        public TryOptionAsync<string> LoadPage(Option<LoadPageRequest> request)
        {
            var correlationId = request.Bind(r => r.CorrelationId).Match(c => c, () => Guid.NewGuid());
            var uri = request.Bind(r => r.Uri).Match(u => u, () => throw new PageLoadException("", "Uri is empty"));
            _logger.LogInformation($"Sending Grpc Request. Load Page: {uri}, CorrelationId: {correlationId}");
            return _rpcClient.Execute(new DriverRequest()
            {
                LoadPageRequest = request,
            }, _grpcServiceUri, correlationId )
            .Bind<DriverResponse, string>(response => async () => await Task.FromResult(response.LoadPageRequest.Match(d => d, () => throw new PageLoadException(uri, "Failed to load page. XDocument is empty"))));
        }
    }
}