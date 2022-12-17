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

namespace Microservice.Grpc.Core
{
    public interface IGrpcMetrics
    {
        /// <summary>
        /// Increments the Message received counter.
        /// </summary>
        void IncReceived(string name);
        
        /// <summary>
        /// Increments the message successfully processed in Grpc Server; counter.
        /// </summary>
        void IncServerSuccess(string name);

        /// <summary>
        /// Increments Grpc server errors.
        /// </summary>
        void IncError(string name);
        
        /// <summary>
        /// Increments Grpc client errors.
        /// </summary>
        void IncClientError(string name);
        
        /// <summary>
        /// Increments the Message sent by client counter.
        /// </summary>
        void IncSent(string name);

        /// <summary>
        /// Increments the reply  received by client counter.
        /// </summary>
        void IncReplyReceived(string name);

    }
}