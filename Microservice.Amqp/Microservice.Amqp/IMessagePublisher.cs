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

using System;
using LanguageExt;

namespace Microservice.Amqp
{
    /// <summary>
    /// Publish messages to configured Exchange/Queue.
    /// <para>
    /// The Exchange to publish will be determined by requesting a publisher via <see cref="IAmqpProvider"/> </para>
    /// <para>Note: need to provide the context name to get the respective <see cref="IMessagePublisher"/></para>
    /// 
    /// <para>Note: The type of message T; should be designed so that the consumer of the messages are aware. If the types don't match; the messages might be rejected by the Subscribers/Consumers.
    /// </para>
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// <para>
        /// Publishes a <see ref="Message" /> of T.</para>
        /// <para>Note: the properties of the <see ref="Message"/> should be set.
        /// </para>
        /// </summary>
        TryOptionAsync<Unit> Publish<T>(Option<Message<T>> message);
        
        /// <summary>
        /// <para>
        /// Publishes a <see ref="Message" /> of T.
        /// </para>
        /// </summary>
        TryOptionAsync<Unit> Publish<T>(Option<T> message);
        
        /// <summary>
        /// <para>
        /// Publishes a <see ref="Message" /> of T; for a specific Correlation ID.
        /// </para>
        /// </summary>
        TryOptionAsync<Unit> Publish<T>(Option<T> message, Option<Guid> correlationId);
    }
}