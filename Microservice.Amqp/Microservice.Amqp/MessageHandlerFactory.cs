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
    /// Factory to create simple MessageHandlers. This helps avoiding concrete MessageHandler Implmentations for simple lambdas.
    /// </summary>
    public class MessageHandlerFactory
    {
        public static MessageHandler<T, R> Create<T, R>(Func<Message<T>, Task<R>> _handlerLogic) => new(_handlerLogic);
        public static MessageHandler<T, R> Create<T, R>(Func<Message<T>, R> _handlerLogic) => new(async T => await Task.FromResult(_handlerLogic(T)));

        public static MessageHandler<T> Create<T>(Func<Message<T>, Task<T>> _handlerLogic) => new(_handlerLogic);
        public static MessageHandler<T> Create<T>(Func<Message<T>, T> _handlerLogic) => new(async T => await Task.FromResult(_handlerLogic(T)));
    }

    public class MessageHandler<T, R>(Func<Message<T>, Task<R>> handlerLogic) : IMessageHandler<T, R>
    {
        private readonly Func<Message<T>, Task<R>> _handlerLogic = handlerLogic;

        public async Task<R> HandleMessage(Option<Message<T>> message)
        {
            return await _handlerLogic(message.Match(m => m, () => throw new Exception("Message is empty")));
        }
    }

    public class MessageHandler<T>(Func<Message<T>, Task<T>> handlerLogic) : MessageHandler<T, T>(handlerLogic)
    {
    }
}