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
using LanguageExt;
using Microservice.DataModel.Core;

namespace Crawler.DataModel
{
    public class CrawlResponseIndexModel : IDataModel
    {
        public Guid Id { get; set; }

        public string Timestamp{get;set;}

        public Option<Guid> CorrelationId { get; set; }
        public Option<Guid> CrawlerId { get; set; }

        public Option<string> Title { get; set; }
        public Option<string> Text { get; set; }
        public Option<List<HyperLink>> Links { get; set; }
        public Option<string> Tables { get; set; }

        public Option<string> ErrorUri { get; set; }

        public Option<string> Raw { get; set; }
    }

    public class HyperLink
    {
        public Option<string> Name {get;set;}
        public Option<string> Uri {get;set;}
    }
}