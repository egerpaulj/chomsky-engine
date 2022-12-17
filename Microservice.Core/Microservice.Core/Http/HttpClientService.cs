//      Microservice Core Libraries for .Net C#                                                                                                                                       
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Microservice.Core.Middlewear;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Timeout;
using System.ComponentModel;
using Microservice.Serialization;

namespace Microservice.Core.Http
{
    public class HttpClientService : IHttpClientService
    {
        private const string HttpContentType = "application/json";
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientService> _logger;
        private readonly IJsonConverterProvider _converterProvider;

        public HttpClientService(HttpClient httpClient, ILogger<HttpClientService> httpClientService, IJsonConverterProvider converterProvider)
        {
            _converterProvider = converterProvider;
            _logger = httpClientService;
            _httpClient = httpClient;
        }

        public TryOptionAsync<T> Get<T>(Option<Guid> correlationId, Option<string> uri)
        {
            return GetParameters(correlationId, uri)
            .Bind<Tuple<Guid, string>, T>(tuple => async () =>
            {
                HttpRequestMessage httpRequestMessage = CreateRequest(tuple.Item1, tuple.Item2, HttpMethod.Get);
                return await GetContent<T>(httpRequestMessage);
            });
        }

        public TryOptionAsync<string> GetStringContent(Option<Guid> correlationId, Option<string> uri)
        {
            return GetParameters(correlationId, uri)
            .Bind<Tuple<Guid, string>, string>(tuple => async () =>
            {
                HttpRequestMessage httpRequestMessage = CreateRequest(tuple.Item1, tuple.Item2, HttpMethod.Get);

                var response = await GetResponse(httpRequestMessage);

                return await response.Content.ReadAsStringAsync();
            });
        }

        public TryOptionAsync<T> Send<R, T>(Option<Guid> correlationId, Option<R> send, Option<string> uri, Option<HttpMethod> method)
        {
            return GetParameters(correlationId, uri)
            .Bind<Tuple<Guid, string>, T>(tuple => async () =>
            {
                HttpRequestMessage httpRequestMessage = CreateRequest(tuple.Item1, tuple.Item2, method.Match(m => m, () => HttpMethod.Get));

                send.Match(s =>
                {
                    var content = typeof(R).IsValueType ? s.ToString() : _converterProvider.Serialize(s);
                    httpRequestMessage.Content = new StringContent(content, Encoding.UTF8, HttpContentType);
                }, () => { });

                return await GetContent<T>(httpRequestMessage);
            });
        }

        public TryOptionAsync<Unit> Send<R>(Option<Guid> correlationId, Option<R> send, Option<string> uri, Option<HttpMethod> method)
        {
            return GetParameters(correlationId, uri)
            .Bind<Tuple<Guid, string>, Unit>(tuple => async () =>
            {
                HttpRequestMessage httpRequestMessage = CreateRequest(tuple.Item1, tuple.Item2, method.Match(m => m, () => HttpMethod.Get));

                send.Match(s =>
                {
                    var content = typeof(R).IsValueType ? s.ToString() : _converterProvider.Serialize(s);
                    httpRequestMessage.Content = new StringContent(content, Encoding.UTF8, HttpContentType);
                }, () => { });

                await Send(httpRequestMessage);

                return Unit.Default;
            });
        }

        private static TryOptionAsync<Tuple<Guid, string>> GetParameters(Option<Guid> correlationId, Option<string> uri)
        {
            return Option<Guid>.Some(correlationId.Match(c => c, () => Guid.NewGuid()))
                        .ToTryOptionAsync()
                        .Bind<Guid, Tuple<Guid, string>>(guid =>
                            uri
                            .ToTryOptionAsync()
                            .Bind<string, Tuple<Guid, string>>(s => async () => await Task.FromResult(Tuple.Create<Guid, string>(guid, s))));
        }

        private static HttpRequestMessage CreateRequest(Guid correlationId, string uri, HttpMethod httpMethod)
        {
            var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);
            httpRequestMessage.Headers.Add(CorrelationIdMiddlware.CorrIdHeaderKey, new[] { correlationId.ToString() });
            return httpRequestMessage;
        }

        private async Task<OptionalResult<T>> GetContent<T>(HttpRequestMessage request)
        {
            HttpResponseMessage response = await GetResponse(request);

            var contentString = await response.Content.ReadAsStringAsync();

            if (typeof(T).IsValueType)
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(contentString);
            }

            var content = _converterProvider.Deserialize<T>(contentString);

            return (T)content;
        }

        private async Task<HttpResponseMessage> GetResponse(HttpRequestMessage request)
        {
            var response = await _httpClient.SendAsync(request);

            if (response.Content == null)
                throw new HttpRequestException("Http response is empty", null, response.StatusCode);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException("Http request not successful", null, response.StatusCode);
            return response;
        }

        private async Task Send(HttpRequestMessage request)
        {
            var retryPolicy = Policy
            .Handle<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .Or<System.AggregateException>()
            .Or<TimeoutException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5),
                    (exception, _) =>
                    {
                        _logger.LogError(exception, "Http Client Timeout/Cancelation Error");
                        _logger.LogWarning($"Timeout or cancellation exception. Retrying request: {request.RequestUri}");
                    });

            var response = await retryPolicy.ExecuteAsync<HttpResponseMessage>(() => _httpClient.SendAsync(request));

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException("Http request not successful", null, response.StatusCode);
        }
    }
}