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
using LanguageExt;
using Microservice.Serialization;

namespace Microservice.Exchange.Endpoints
{
    /// <summary>
    /// Publishes data to the console via the configured logger.
    /// </summary>
    public class ConsolePublisher<T, R> : IPublisher<R>, IDeadletterPublisher<T, R>
    {
        private readonly IJsonConverterProvider _jsonConverterProvider;

        public ConsolePublisher(IJsonConverterProvider jsonConverterProvider)
        {
            _jsonConverterProvider = jsonConverterProvider;
        }

        public string Name => "Console";

        public TryOptionAsync<Unit> Publish(Option<Message<R>> message)
        {
            return message.ToTryOptionAsync().Map(message => 
            {
                Console.WriteLine($"## Console publisher: Publishing Message. Id: {message.Id}");
                Console.WriteLine($"## Console publisher: Publishing Message. CorrelationId: {message.CorrelationId}");
                Console.WriteLine(_jsonConverterProvider.Serialize(message));

                return Unit.Default;
            });
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<T>> message)
        {
            return message.ToTryOptionAsync().Map(message => 
            {
                Console.WriteLine($"## Console publisher: Publishing Error Message. Id: {message.Message.Id}");
                Console.WriteLine($"## Console publisher: Publishing Error Message. CorrelationId: {message.Message.CorrelationId}");
                Console.WriteLine(_jsonConverterProvider.Serialize(message));
                
                return Unit.Default;
            });
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<R>> message)
        {
            return message.ToTryOptionAsync().Map(message => 
            {
                Console.WriteLine($"## Console publisher: Publishing Error Message. Id: {message.Message.Id}");
                Console.WriteLine($"## Console publisher: Publishing Error Message. CorrelationId: {message.Message.CorrelationId}");
                Console.WriteLine(_jsonConverterProvider.Serialize(message));
                
                return Unit.Default;
            });
        }

        public TryOptionAsync<Unit> PublishError(Option<string> message)
        {
            return message.ToTryOptionAsync().Map(message => 
            {
                Console.WriteLine($"## Console publisher: Publishing Error Message. ");
                Console.WriteLine(message);
                
                return Unit.Default;
            });
        }
    }
}