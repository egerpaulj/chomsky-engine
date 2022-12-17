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

namespace Microservice.Exchange
{
    /// <summary>
    /// Consumes data from a data source and provides an Observable.
    /// </summary>
    public interface IConsumer<T>
    {
        /// <summary>
        /// Start consuming messages.
        /// </summary>
        TryOptionAsync<Unit> Start();

        /// <summary>
        /// Observable Message or <see cref="ConsumerException" />.
        /// </summary>
        IObservable<Either<Message<T>, ConsumerException>> GetObservable();
        
        /// <summary>
        /// End consuming messages.
        /// </summary>
        TryOptionAsync<Unit> End();
    }
}