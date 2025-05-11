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
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Requests;
using Grpc.Core;
using LanguageExt;
using Microservice.Grpc.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Crawler.WebDriver.Grpc.Server
{
    public class WebDriverService : RpcServiceBase<DriverRequest, DriverResponse>
    {
        private readonly IWebDriverService _webDriverService;

        public WebDriverService(
            ILogger<Rpc.RpcBase> logger,
            IGrpcMetrics metrics,
            IWebDriverService webDriverService,
            IJsonConverterProvider jsonConverterProvider
        )
            : base(logger, metrics, jsonConverterProvider)
        {
            _webDriverService = webDriverService;
        }

        protected override TryOptionAsync<DriverResponse> Execute(Option<DriverRequest> request)
        {
            var dlRequest = request.Bind(r => r.DownloadRequest);
            var pageLoadRequest = request.Bind(r => r.LoadPageRequest);

            if (pageLoadRequest.IsSome)
                return pageLoadRequest
                    .ToTryOptionAsync()
                    .Bind<LoadPageRequest, DriverResponse>(r => LoadPage(r));

            if (dlRequest.IsSome)
                return dlRequest
                    .ToTryOptionAsync()
                    .Bind<DownloadRequest, DriverResponse>(r => Download(r));

            throw new Exception("Load Page and Download Page requests are empty");
        }

        private TryOptionAsync<DriverResponse> LoadPage(LoadPageRequest request)
        {
            return _webDriverService
                .LoadPage(request)
                .Bind<string, DriverResponse>(r =>
                    async () =>
                    {
                        return await Task.FromResult(new DriverResponse { LoadPageRequest = r });
                    }
                );
        }

        private TryOptionAsync<DriverResponse> Download(DownloadRequest request)
        {
            return _webDriverService
                .Download(request)
                .Bind<FileData, DriverResponse>(r =>
                    async () =>
                    {
                        return await Task.FromResult(new DriverResponse { DownloadRequest = r });
                    }
                );
        }
    }
}
