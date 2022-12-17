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
using System.Collections.Generic;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Microservice.DataModel.Core;
using Newtonsoft.Json;

namespace Crawler.DataModel
{
    public class CrawlRequestModel : IDataModel
    {
        [JsonProperty("_id")]
        public Guid Id { get; set; }

        public CrawlContinuationStrategy ContinuationStrategyDefinition{get;set;}
        public const string AllUriMatch = "*";
        public string Uri { get; set; }
        public string Host { get; set; }

        public DocumentPart DocumentPartDefinition { get; set; }
        public List<UiAction> UiActions { get; set; }
        public bool ShouldDownloadContent { get; set; }
        public bool ShouldProvideRawSource {get;set;}
        public bool IsUrlCollector {get;set;}

    }
}