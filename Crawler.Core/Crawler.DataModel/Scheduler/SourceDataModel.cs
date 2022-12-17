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
using Microservice.DataModel.Core;
using Newtonsoft.Json;

namespace Crawler.DataModel.Scheduler
{
    public enum SourceType
    {
        Custom = 1,
        Collector = 2
    }

    public class SourceDataModel : IDataModel
    {
        [JsonProperty("_id")]
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Uri { get; set; }

        public SourceType SourceTypeId { get; set; }

        public string CronPeriod { get; set; }



    }
}