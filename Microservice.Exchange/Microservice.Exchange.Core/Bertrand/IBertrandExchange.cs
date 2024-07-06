//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2024  Paul Eger                                                                                                                                                                     

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
using System.Collections.Generic;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

/// <summary>
/// Message Exchange that consumes from several data sources, processes the data, transforms and publishes to several data sources.
/// </summary>
public interface IBertrandExchange
{
    /// <summary>
    /// Start processing messages.
    /// </summary>
    TryOptionAsync<Unit> Start();

    /// <summary>
    /// Stop processing messages.
    /// </summary>
    TryOptionAsync<Unit> End();

    string ExchangeName { get; }

    IReadOnlyList<IBertrandConsumer> GetConsumers();
    IReadOnlyList<IBertrandTransformer> GetTransformers();
    IReadOnlyList<IPublisher<object>> GetPublishers();
    IReadOnlyList<IBetrandTransformerFilter> GetTransformerFilters();
    IReadOnlyList<IBertrandPublisherFilter> GetPublisherFilters();
}
