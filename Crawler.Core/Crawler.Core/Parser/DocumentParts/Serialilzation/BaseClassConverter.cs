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
using System.Text.Json.Serialization;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Crawler.Core.Parser.DocumentParts.Serialilzation
{
    public class BaseClassConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(DocumentPart));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            //var docType = jo["DocPartType"].Value<Option<DocumentPartType>>().Match(d => d, () => throw new Exception("Unknown Document Part Type"));
            var docPartType = jo["DocPartType"] ?? jo["docPartType"];
            var docTypeArray = docPartType.Value<object>() as JArray;
            var docType = (DocumentPartType) docTypeArray.First.Value<int>();
            
            switch (docType)
            {
                case DocumentPartType.Article:
                    return JsonConvert.DeserializeObject<DocumentPartArticle>(jo.ToString(), this);
                case DocumentPartType.Table:
                    return JsonConvert.DeserializeObject<DocumentPartTable>(jo.ToString(), this);
                case DocumentPartType.AutoDetect:
                    return JsonConvert.DeserializeObject<DocumentPartAutodetect>(jo.ToString(), this);
                case DocumentPartType.File:
                    return JsonConvert.DeserializeObject<DocumentPartFile>(jo.ToString(), this);
                case DocumentPartType.Link:
                    return JsonConvert.DeserializeObject<DocumentPartLink>(jo.ToString(), this);
                case DocumentPartType.Row:
                    return JsonConvert.DeserializeObject<DocumentPartTableRow>(jo.ToString(), this);
                case DocumentPartType.Text:
                    return JsonConvert.DeserializeObject<DocumentPartText>(jo.ToString(), this);
                default:
                    throw new Exception();
            }

            throw new NotImplementedException();
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException(); // won't be called because CanWrite returns false                                                                                                                    
        }


    }
}