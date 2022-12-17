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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microservice.Core.Middlewear
{
    /// <summary>
    /// Middlewear ensures, ALL Requests/Responses are associated with a CorrelationId.
    /// The middlewear should be used with services/clients with similar CorrelationId middlewear/clients (i.e. with the HTTP header CorrIdMicroservice). Otherwise BadHttpRequestException is thrown.
    /// I.e. If No CorrelationId is sent via the HTTP header, then requests are rejected.
    /// Note: Proprietery clients would have to include a valid GUID in the HTTP header; Key: CorrIdMicroservice, Value: GUID.
    /// </summary>
    public class CorrelationIdMiddlware
    {
        internal const string CorrIdHeaderKey = "CorrIdMicroservice";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddlware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context?.Request == null && context?.Response == null)
                return;

            var requestCorrId = GetRequestCorrId(context);
            var responseCorrId = GetResponseCorrId(context);

            if (requestCorrId == null)
            {
                if (context?.Request != null)
                {
                    var newId = Guid.NewGuid().ToString();
                    context.Request.Headers?.Add(CorrIdHeaderKey, newId);
                    requestCorrId = newId;
                }
            }

            if (responseCorrId == null)
            {
                if (context?.Response != null)
                {
                    // If sending a response without a correlation id; the original request should have a correlation id
                    // If this is a request to another service - then the client should be given Correlation id
                    // No correlation ID, then microservice is outside 
                    if (requestCorrId == null && responseCorrId == null)
                        throw new BadHttpRequestException($"Invalid/missing Correlation ID: {CorrIdHeaderKey}", StatusCodes.Status400BadRequest);

                    context.Response.Headers?.Add(CorrIdHeaderKey, requestCorrId);
                }
            }

            await _next(context);
        }

        internal static string GetRequestCorrId(HttpContext context)
        {
            var val = context?.Request?.Headers?.FirstOrDefault(h => h.Key == CorrIdHeaderKey);

            if (val == null)
                return null;

            return val.Value.Value;
        }

        private static string GetResponseCorrId(HttpContext context)
        {
            var val = context?.Response?.Headers?.FirstOrDefault(h => h.Key == CorrIdHeaderKey);

            if (val == null)
                return null;

            return val.Value.Value;
        }
    }
}