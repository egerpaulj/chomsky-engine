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
using Microservice.Amqp.Configuration;

namespace Microservice.Amqp
{
    /// <summary>
    /// Helper interface to Create/Delete Exchanges/Queues.
    /// </summary>
    public interface IAmqpBootstrapper
    {
        /// <summary>
        /// Create all Exchanges and Queues defined in the <see cref="AmqpConfiguration"/>.
        /// The Create exchanges and queues will be linked with the defined RoutingKey.
        /// </summary>
        TryOptionAsync<Unit> Bootstrap();

        /// <summary>
        /// Deletes all Exchanges and Queues defined in the <see cref="AmqpConfiguration"/>
        /// </summary>
        TryOptionAsync<Unit> Purge();
    }
}