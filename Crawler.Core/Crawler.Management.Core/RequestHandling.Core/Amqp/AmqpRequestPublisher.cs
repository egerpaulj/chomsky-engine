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
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Amqp;
using Microservice.Amqp.Configuration;

namespace Crawler.Management.Core.RequestHandling.Core.Amqp
{
    public class AmqpRequestPublisher : IRequestPublisher
    {
        private readonly IMessagePublisher requestPublisher;
        private readonly IMessagePublisher uriPublisher;
        private readonly IAmqpProvider amqpProvider;
        private readonly IAmqpBootstrapper amqpBootstrapper;
        private bool disposedValue;

        public AmqpRequestPublisher(IAmqpProvider amqpProvider, IAmqpBootstrapper amqpBootstrapper)
        {
            requestPublisher = amqpProvider
                .GetPublisher(AmqpRequestProvider.RequestProviderContext)
                .Match(
                    p => p,
                    () => throw new System.Exception("Failed to get AMQP request publisher"),
                    ex =>
                    {
                        throw ex;
                    }
                )
                .Result;
            uriPublisher = amqpProvider
                .GetPublisher(AmqpRequestProvider.UriProviderContext)
                .Match(
                    p => p,
                    () => throw new System.Exception("Failed to get AMQP request publisher"),
                    ex =>
                    {
                        throw ex;
                    }
                )
                .Result;
            this.amqpProvider = amqpProvider;
            this.amqpBootstrapper = amqpBootstrapper;
        }

        public TryOptionAsync<Unit> PublishRequest(Option<CrawlRequest> request)
        {
            var req = request.Match(r => r, () => throw new Exception("Request can't be empty"));
            var uriStr = req
                .LoadPageRequest.Bind(p => p.Uri)
                .Match(u => u, () => throw new Exception("Uri is empty"));

            var host = uriStr;
            try
            {
                host = new Uri(uriStr).Host;
            }
            catch (Exception) { }

            return PrepareForPublish(host, req)
                .Bind(message => requestPublisher.Publish<CrawlRequest>(message));
        }

        private TryOptionAsync<Message<CrawlRequest>> PrepareForPublish(
            string host,
            CrawlRequest crawlRequest
        )
        {
            return amqpProvider
                .GetContext(AmqpRequestProvider.RequestProviderContext)
                .ToTryOptionAsync()
                .Bind<AmqpContextConfiguration, Message<CrawlRequest>>(c =>
                    async () =>
                    {
                        await amqpBootstrapper
                            .CreateQueue(host, c.Exchange, host)
                            .Match(
                                r => r,
                                () => throw new Exception("Failed to create queue: " + host),
                                ex => throw ex
                            );
                        return new Message<CrawlRequest>
                        {
                            Context = AmqpRequestProvider.RequestProviderContext,
                            CorrelationId = crawlRequest.CorrelationCrawlId,
                            Id = Guid.NewGuid(),
                            RoutingKey = crawlRequest.IsAdhocRequest ? "request*" : host,
                            Payload = crawlRequest,
                        };
                    }
                );
        }

        public TryOptionAsync<Unit> PublishUri(
            Option<string> baseUri,
            Option<List<DocumentPartLink>> linksList,
            UriType uriType
        )
        {
            return linksList
                .ToTryOptionAsync()
                .Bind<List<DocumentPartLink>, Unit>(links =>
                    async () =>
                    {
                        foreach (var l in links)
                        {
                            await uriPublisher
                                .Publish<CrawlUri>(
                                    new CrawlUri
                                    {
                                        BaseUri = baseUri,
                                        Uri = l.Uri,
                                        UriTypeId = uriType,
                                    }
                                )
                                .Match(
                                    r => r,
                                    () => throw new Exception($"Failed to publish uri: {l.Uri}")
                                );
                        }

                        return Unit.Default;
                    }
                );
        }
    }
}
