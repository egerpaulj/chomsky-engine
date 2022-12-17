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
using LanguageExt;

namespace Microservice.Exchange
{
    public class Message<T> : IMessage
    {
        public Option<Guid> Id { get; set; }
        public Option<T> Payload { get; set; }
        public Option<string> RoutingKey { get; set; }
        public Option<Guid> CorrelationId { get; set; }
        public Option<List<KeyValuePair<string, string>>> Properties { get; set; }

        public Message()
        {
        }


        public Message(Option<IMessage> message)
        {
            message.Match(m => 
            {
                Id = m.Id;
                RoutingKey = m.RoutingKey;
                CorrelationId = m.CorrelationId;
                Properties = m.Properties;
                Id = m.Id;
            }, () => 
            {
                Id = Guid.NewGuid();
                CorrelationId = Guid.NewGuid();
            });
        }

        public Message<R> CopyData<R>(Message<R> message)
        {
            message.Id = Id;
            message.RoutingKey = RoutingKey;
            message.CorrelationId = CorrelationId;
            message.Properties = Properties;

            return message;
        }
    }

    public interface IMessage
    {
        public Option<Guid> Id { get; set; }
        public Option<string> RoutingKey { get; set; }
        public Option<Guid> CorrelationId { get; set; }
        public Option<List<KeyValuePair<string, string>>> Properties { get; set; }
    }
}