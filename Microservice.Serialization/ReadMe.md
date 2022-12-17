# Microservice.Serialization

A .NET library which provides a generic JSON converter interface-

A custom **IJsonConverterProvider** can be used to provide custom serialization/deserialization logic to Newtonsoft; **only if needed** (e.g. if abstract base classes are part of a composite class structure).

This is done by implementing one or several **JsonConverter**s

```
public interface IJsonConverterProvider
{
    JsonConverter[] GetJsonConverters();
}
```

## Interface Usage

Implement the interface **IJsonConverterProvider** and provide the custom converters.

E.g. a custom **BaseClassConverter**

```
public class JsonConverterProvider : IJsonConverterProvider
{
    private readonly JsonConverter[] _converters = new []{new BaseClassConverter()};
    public JsonConverter[] GetJsonConverters() => _converters;
}
```

E.g. **BaseClassConverter** instructs JSON how to Deserilize a **BusinessObjectAbstractClass** and Concrete implementations **BusinessObjectConcreteA** and **BusinessObjectConcreteB**

Note: in the example below, each business object's enum **DocPartType** is used determine the type of deserialization necessary

```
public class BaseClassConverter : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(BusinessObjectAbstractClass));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        var docPartType = jo["DocPartType"] ?? jo["docPartType"];
        var docTypeArray = docPartType.Value<object>() as JArray;
        var docType = (BusinessObjectType) docTypeArray.First.Value<int>();
        
        switch (docType)
        {
            case BusinessObjectEnum.ConcreteA:
                return JsonConvert.DeserializeObject<BusinessObjectConcreteA>(jo.ToString(), this);
            case BusinessObjectEnum.ConcreteB:
                return JsonConvert.DeserializeObject<BusinessObjectConcreteB>(jo.ToString(), this);
            default:
                throw new Exception("BusinessObjectAbstraction Deserialization error");
        }
        throw new NotImplementedException();
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException(); // won't be called because CanWrite returnsfalse                                                                                                                    
    }
}
```

### Extension Methods

The Extension methods of **IJsonConverterProvider** provides 
- string Serialize(object o)
- T Deserialize<T>(string valueStr)

**Note:** the extension methods will ensure that JsonConverters are used to Serialize/Deserialize.

## License

Copyright (C) 2021  Paul Eger

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
