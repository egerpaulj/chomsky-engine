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
using System.Threading.Tasks;
using LanguageExt;

namespace Microservice.Amqp
{
    /// <summary>
    /// Subscribes to messages in a Queue.
    /// <para>
    /// The Queue name to Subscribe will be determined by requesting a Subscriber via <see cref="IAmqpProvider"/>.
    /// </para>
    /// <para>
    /// Note: need to provide the context name to get the respective <see cref="IMessageSubscriber"/>
    /// </para>
    /// <para>
    /// Note: The type of message T; should be designed so that the consumer of the messages are aware. If the types don't match; the messages might be rejected by the Subscribers/Consumers.
    /// </para>
    /// <para>
    /// Note: The type of expected response R; the receiving message is processed; and return R if successfull.
    /// </para>
    /// </summary>
    public interface IMessageSubscriber<T, R> : IDisposable
    {
        /// <summary>
        /// Gets an <see cref="IObservable"/> of the Queue. Messages are sent to the Observable-
        /// <para>
        /// Note: a Function is provided by the callee; to work on the message. </para>
        /// <para> If the Message was successfully processed, then it is Positively Acknowledged; and is removed from the Queue
        /// </para>
        /// <para> If processing failed, then it is Negatively Acknowledged; and will be sent to the Deadletter Exchange.
        /// </para>
        /// <returns>
        /// An Either => The expected type; if the work was successfull; or the Exception.
        /// </returns>
        /// </summary>
        IObservable<Either<R, Exception>> GetObservable();

        /// <summary>
        /// Gets an <see cref="IObservable"/> of the Queue. Messages are sent to the Observable-
        /// <para>
        /// Note: a Function is provided by the callee; to work on the message. </para>
        /// <para> If the Message was successfully processed, then it is Positively Acknowledged; and is removed from the Queue
        /// </para>
        /// <para> If processing failed, then it is Negatively Acknowledged; and will be sent to the Deadletter Exchange.
        /// </para>
        /// <returns>
        /// An Either => The expected type; if the work was successfull; or the Exception.
        /// </returns>
        /// </summary>
        IObservable<Either<Message<R>, Exception>> GetMessageObservable();

        
         /// <summary>
        /// Start Consuming Messages from the Queue.
        /// </summary>
        void Start();
    }
}