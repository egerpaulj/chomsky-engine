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
using LanguageExt;

namespace Microservice.Exchange
{
    /// <summary>
    /// Transforms data from Type to another. The transformer also can be used to Notify, Transform, Cache; helping to create various Enterprise Integration Patterns.
    /// </summary>
    public interface ITransformer<T, R>
    {
        /// <summary>
        /// Transform data From T to R.
        /// </summary>
        TryOptionAsync<Message<R>> Transform(Option<Message<T>> input);
    }

    /// <summary>
    /// Does nothing, allows the message to pass through the exchange without any interaction with the incoming message.
    /// </summary>
    public class SameTypeTransformer<T, R> : ITransformer<T, R>
    {
        TryOptionAsync<Message<R>> ITransformer<T, R>.Transform(Option<Message<T>> input) => input.ToTryOptionAsync().Map(dataIn => dataIn as Message<R>);
    }
}