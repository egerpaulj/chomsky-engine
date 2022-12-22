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
using System.Collections.ObjectModel;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Serialization;
using Newtonsoft.Json;

namespace Microservice.Exchange.Endpoints.Csv;

public class CsvData : IDataModel
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public Dictionary<string, string> Values { get; set; }

    public CsvData(Dictionary<string, string> values)
    {
        Values = values;
    }

    public CsvData(Option<List<string>> headers, Option<List<string>> values)
    {
        var heads = headers.Match(h => h, () => throw new ArgumentException("headers can't be null"));
        var vals = values.Match(v => v, () => throw new ArgumentException("values can't be null"));

        if (heads.Count() != vals.Count())
            throw new InvalidCsvDataException("values must match headers");

        Values = new Dictionary<string, string>();

        for (var i = 0; i < heads.Count; i++)
        {
            Values.Add(heads[i].Trim(), vals[i].Trim());
        }

        var idKey = Values.Keys.FirstOrDefault(k => k.ToLower() == "id");

        if(idKey == null)
            Values.Add("Id", Guid.NewGuid().ToString());
        else
            if(!Guid.TryParse(Values[idKey], out var Id))
            {
                Id = Guid.NewGuid();
            }
            else
                this.Id = Id;
        
    }
}

public class CsvDataJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableTo(typeof(CsvData));
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        
        var values = serializer.Deserialize<Dictionary<string,string>>(reader);
        return new CsvData(values?? new Dictionary<string, string>());
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var csvData = value as CsvData;
        if(csvData != null)
        {
            serializer.Serialize(writer, csvData.Values);
        }
    }
}

public class CsvJsonConverterProvider : IJsonConverterProvider
{
    public JsonConverter[] GetJsonConverters()
    {
        return new []{new CsvDataJsonConverter()};
    }
}
