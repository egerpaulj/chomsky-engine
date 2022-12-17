# Microservice.Exchange.Endpoints.Elasticsearch

.NET library provides an implementation of Consumer/Publish Endpoint for the Messaging Exchange library.

The library provides the following:
- **DataIn:** Executes an Elasticsearch query periodically
- **DataOut:** Publishes the data to an Elasticsearch index

## Usage

- **ConnectionStrings:** Provide the Connection string to your elastic-search instance
- **Index:** The name of the elastic search index
- **IntervalInMs:** The periodic interval to execute (Leave empty for a single execution)
- **Query:** Only relevant for **DataIn**, the Query to retrieve data

### DataIn

```
"DataIn": {
        "Elastic": {
          "ConnectionStrings": {
            "ElasticsearchConnectionString": "http://10.137.0.50:9200/"
          },
          "Index": "testexchange",
          "IntervalInMs": "1000",
          "Query": "{\"query\": {\"match\": {\"EnrichedData\": {\"query\": \"some test data\"}}}}" 
        } 
      },
"TypeMappings" : {
        "Elastic": "Microservice.Exchange.Endpoints.Elasticsearch.ElasticsearchConsumer`1, Microservice.Exchange.Endpoints.Elasticsearch",
      }
```

### DataOut

```
"DataOut": {
            "Elastic": {
              "ConnectionStrings": {
                "ElasticsearchConnectionString": "http://10.137.0.50:9200/"
              },
              "Index": "testexchange"
            }
        },
"TypeMappings" : {
        "Elastic": "Microservice.Exchange.Endpoints.Elasticsearch.ElasticsearchPublisher`2, Microservice.Exchange.Endpoints.Elasticsearch",
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