//      Microservice Cache Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger  
//                                                                                                                                                                  
//      This program is free software: you can redistribute it and/or modify                                                                                                                                          
//      it under the terms of the GNU General Public License as published by                                                                                                                                          
//      the Free Software Foundation, either version 3 of the License, or                                                                                                                                             
//      (at your option) any later version.   
//                                                                                                                                                                        
//      This program is distributed in the hope that it will be useful,                                                                                                                                               
//      but WITHOUT ANY WARRANTY; without even the implied warranty of                                                                                                                                                
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                                                                                                                                                 
//      GNU General Public License for more details.                                                                                                                                                                  
//
//      You should have received a copy of the GNU General Public License                                                                                                                                             
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;

namespace Microservice.DataModel.Core
{
    /// <summary>
    /// Provides an interface for data models. 
    /// </summary>
    public interface IDataModel
    {
        /// <summary>
        /// The Seriliazation JSON Tag, which should be specified for MongoDb DataModels.
        /// E.g. <para>[JsonProperty("_id")]</para>
        /// <para>public Guid Id { get; set; }</para>
        /// </summary>
        const string IdStr = "_id";

        /// <summary>
        /// The Id to uniquely identify each model instance.
        /// </summary>
        public Guid Id { get; set; }
    }
}