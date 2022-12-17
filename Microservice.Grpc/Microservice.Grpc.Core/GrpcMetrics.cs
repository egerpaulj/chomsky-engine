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

using Prometheus;

namespace Microservice.Grpc.Core
{
    
    public class GrpcMetrics : IGrpcMetrics
    {
        private readonly Counter _grpcCounter;

        private const string Context ="context";

        
        public GrpcMetrics()
        {
            _grpcCounter = Metrics.CreateCounter("grpc_counters", "Grpc related counters", "context");
        }

        public void IncClientError(string name)
        {
            _grpcCounter.WithLabels("client_error").Inc();
        }

        public void IncError(string name)
        {
            _grpcCounter.WithLabels("error").Inc();
        }

        public void IncReceived(string name)
        {
            _grpcCounter.WithLabels("request_received").Inc();
        }

        public void IncReplyReceived(string name)
        {
            _grpcCounter.WithLabels("reply_received").Inc();
        }

        public void IncSent(string name)
        {
            _grpcCounter.WithLabels("request_sent").Inc();
        }

        public void IncServerSuccess(string name)
        {
            _grpcCounter.WithLabels("request_server_success").Inc();
        }
    }
}