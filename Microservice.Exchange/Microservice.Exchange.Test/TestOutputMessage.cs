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

namespace Microservice.Exchange.Test
{
    public class TestOutputMessage : IDataModel
    {
        public string OriginalData { get; set; }
        public string EnrichedData { get; set; }

        [JsonProperty(IDataModel.IdStr)]
        public Guid Id { get; set; }
        public string Created { get; set; }
        public string Updated { get; set; }
    }
}
