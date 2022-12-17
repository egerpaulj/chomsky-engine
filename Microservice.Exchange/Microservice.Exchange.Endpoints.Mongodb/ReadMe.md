# Microservice.Exchange.Endpoints.Mongodb

.NET library provides an implementation of Consumer/Publish Endpoint for the Messaging Exchange library.

The library provides the following:
- **DataIn:** Executes an Mongodb query periodically
- **DataOut:** Publishes the data to a Mongodb Document index
- **Deadletter:** Publishes the error data to a Mongodb Document index

## Usage

## Usage

- **ConnectionStrings:** Provide the Connection string to your MongoDb instance
- **DatabaseName:** The name of the database in MongoDb
- **DocumentName:** The document name in the database in MongoDb
- **DocumentFilters:** Only relevant for **DataIn**, the Filters to retrieve data (Always accompanies with a **FieldName** and **FilterValue** and **LogicalOperator**)

**Note:** The following MongoDb filters are available (See MongoDb Documentation for more information on Filters):

- Eq
- Gt
- Gte
- Lt
- Lte
- In
- Regex
- Text

**E.g.**
```
"DocumentFilters": {
            "Eq": {
              "FieldName": "EnrichedData",
              "FilterValue": "Some test data", 
              "LogicalOperator": "Or"
            },
            "Text": {
              "FieldName": "EnrichedData",
              "FilterValue": "test"
            }
}
```

### DataIn

```
"DataIn": {
        "MongodbConsumer": {
          "ConnectionStrings": {
            "MongoDbConnectionString": "mongodb://10.137.0.50:27017"
          },
          "DatabaseName": "TestExchange",
          "DocumentName": "TestDataDocument",
          "IntervalInMs": "1000",
          "DocumentFilters": {
            "Eq": {
              "FieldName": "EnrichedData",
              "FilterValue": "Some test data", 
              "LogicalOperator": "Or"
            },
            "Text": {
              "FieldName": "EnrichedData",
              "FilterValue": "test"
            }

          }
},
"TypeMappings" : {
        "MongodbConsumer": "Microservice.Exchange.Endpoints.Mongodb.MongodbConsumer`1, Microservice.Exchange.Endpoints.Mongodb"
      }      
```

### DataOut

```
"DataOut": {
            "MongodbPublisher": {
              "ConnectionStrings": {
                "MongoDbConnectionString": "mongodb://10.137.0.50:27017"
              },
              "DatabaseName": "TestExchange",
              "DocumentName": "TestDataDocument"
            }
        },
"TypeMappings" : {
          "MongodbPublisher": "Microservice.Exchange.Endpoints.Mongodb.MongodbPublisher`2, Microservice.Exchange.Endpoints.Mongodb"
        }    
```


## License

Copyright (C) 2022  Paul Eger

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