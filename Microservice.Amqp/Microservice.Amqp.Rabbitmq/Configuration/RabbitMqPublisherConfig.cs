//      Microservice AMQP Libraries for .Net C#                                                                                                                                       
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

using LanguageExt;

namespace Microservice.Amqp.Rabbitmq.Configuration
{
    /// <summary>
    /// Configuration specific to publishing messages.
    /// </summary>
    public class RabbitMqPublisherConfig : RabbitmqConfig
    {
        /// <summary>
        /// Tells the RabbitMq exchange; how to route the message. I.e. which Target Queue(s) should the message be published to.
        /// </summary>
        public Option<string> RoutingKey { get; set; }

        /// <summary>
        /// The Target RabbitMQ Exchange name; where messages will published.
        /// </summary>
        public Option<string> Exchange { get; set; }

        /// <summary>
        /// The AMQP context. The context defines the integration context; explicit in the <see cref="Configuration.AmqpConfiguration"/>.
        /// </summary>
        public Option<string> Context { get; set; }

    }
}