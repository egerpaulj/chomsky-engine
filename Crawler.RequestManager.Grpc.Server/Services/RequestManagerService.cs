using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Core;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Requests;
using LanguageExt;
using Microservice.Grpc.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Crawler.RequestManager.Grpc.Server
{
    public class RequestManagerService : RpcServiceBase<DriverRequest, DriverResponse>
    {
        private IRequestManagerFactory _requestManagerFactory;
        private IWebDriverService _webDriverService;

        public RequestManagerService(
            ILogger<Rpc.RpcBase> logger, 
            IGrpcMetrics metrics, 
            IRequestManagerFactory requestManagerFactory, 
            IWebDriverService webDriverService, 
            IJsonConverterProvider jsonConverterProvider) 
        : base(logger, metrics, jsonConverterProvider)
        {
            _webDriverService = webDriverService;
            _requestManagerFactory = requestManagerFactory;
        }
        
        protected override TryOptionAsync<DriverResponse> Execute(Option<DriverRequest> request)
        {
            var dlRequest = request.Bind(r => r.DownloadRequest);
            var pageLoadRequest = request.Bind(r => r.LoadPageRequest);

            if (pageLoadRequest.IsSome)
                return pageLoadRequest.ToTryOptionAsync().Bind<LoadPageRequest, DriverResponse>(r => LoadPage(r));

            if (dlRequest.IsSome)
                return dlRequest.ToTryOptionAsync().Bind<DownloadRequest, DriverResponse>(r => Download(r));

            throw new Exception("Load Page and Download Page requests are empty");
        }

        private TryOptionAsync<DriverResponse> LoadPage(LoadPageRequest request)
        {
            var requestManager = _requestManagerFactory.GetRequestManager(request.Uri);

            return requestManager.ThrottleRequest(() => _webDriverService.LoadPage(request))
                    .Bind<string, DriverResponse>(r => async () => { return await Task.FromResult(new DriverResponse { LoadPageRequest = r }); });
        }

        private TryOptionAsync<DriverResponse> Download(DownloadRequest request)
        {
            var requestManager = _requestManagerFactory.GetRequestManager(request.Uri);

            return requestManager.ThrottleDownload(() => _webDriverService.Download(request)
                   .Bind<FileData, DriverResponse>(r => async () => { return await Task.FromResult(new DriverResponse { DownloadRequest = r }); }));
        }


    }
}
