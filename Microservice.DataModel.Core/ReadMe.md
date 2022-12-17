# Microservices DataModel

.NET Library to encapsulate Data Models used by **Microservice** repository libraries. Enforces identifcation using a unique GUID.

```
public interface IDataModel
{
    const string IdStr = "_id";
    public Guid Id { get; set; }
}
```

## IdStr

For MongoDb the serialization should serialize the Id property as _id. 

E.g.

```
     [JsonProperty("_id")]
     public Guid Id { get; set; }
```

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