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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Amqp;

namespace Crawler.Management.Core.RequestHandling.Core.Amqp
{
    public class AmqpRequestProvider : IRequestProvider, IDisposable
    {
        internal const string RequestProviderContext = "CrawlRequest";
        internal const string UriProviderContext = "CrawlUri";

        private IMessageSubscriber<CrawlRequest, CrawlRequest> _subscriber;
        private bool disposedValue;
        private readonly IAmqpProvider _amqpProvider;

        public AmqpRequestProvider(IAmqpProvider amqpProvider)
        {
            _amqpProvider = amqpProvider;
        }

        public IObservable<Either<CrawlRequest, CrawlRequestException>> GetObservable(
            Option<CancellationToken> token,
            Func<CrawlRequest, Task<Unit>> crawlTask
        )
        {
            var handler = MessageHandlerFactory.Create<CrawlRequest, CrawlRequest>(async request =>
            {
                var crawlRequest = request.Payload.Match(
                    p => p,
                    () => throw new Exception("Empty payload")
                );
                await crawlTask(crawlRequest);
                return crawlRequest;
            });

            if (_subscriber == null)
                _subscriber = _amqpProvider
                    .GetSubsriber<CrawlRequest, CrawlRequest>(RequestProviderContext, handler)
                    .Match(
                        s => s,
                        () => throw new Exception("Failed to get AMQP Request subscriber"),
                        ex =>
                        {
                            throw ex;
                        }
                    )
                    .Result;

            _subscriber.Start();

            return _subscriber
                .GetObservable()
                .Select(res => res.Map<CrawlRequestException>(ex => new CrawlRequestException(ex)));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _subscriber?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
