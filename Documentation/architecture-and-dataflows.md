# Chomsky Engine Components and Data Flows

This document describes the production crawler topology started by
[`Jenkinsfile`](/home/user/src/chomsky-engine-pri/Jenkinsfile), the infrastructure
containers started from `Infrastructure/docker`, and the message-exchange flows
defined under `Microservice.Exchange`.

Detailed Bertrand exchange documentation:

- [`Crawler-Exchange`](/home/user/src/chomsky-engine-pri/Documentation/Exchanges/crawler-exchange.md)
- [`Data-Pipeline-Exchange`](/home/user/src/chomsky-engine-pri/Documentation/Exchanges/data-pipeline-exchange.md)
- [`Data-Pipeline-Cleaner-Exchange`](/home/user/src/chomsky-engine-pri/Documentation/Exchanges/data-pipeline-cleaner-exchange.md)
- [`Data-Pipeline-Adhoc-Exchange`](/home/user/src/chomsky-engine-pri/Documentation/Exchanges/data-pipeline-adhoc-exchange.md)
- [`MongoDB use cases`](/home/user/src/chomsky-engine-pri/Documentation/mongodb-use-cases.md)

The component view follows the current Jenkins pipeline. `Crawler.Configuration`
is shown because the running services reference `config_server`, although the
current Jenkins start stage does not start that container.

## Component diagram

```mermaid
flowchart TB
    Jenkins["Jenkins pipeline"]
    Registry["Docker Registry<br/>registry:5000"]
    Docker["Docker Engine<br/>shared socket"]

    subgraph Crawler["Crawler services on development_network"]
        Request["Request Manager<br/>request_server<br/>HTTPS/gRPC :443"]
        WebDriver["WebDriver Server<br/>webdriver_server<br/>gRPC :443"]
        Management["Management Service<br/>management_service"]
        Scheduler["Scheduler<br/>scheduler"]
        Data["Data Service<br/>data_service"]
        Configuration["Configuration Server<br/>config_server<br/>HTTPS :443"]
    end

    subgraph Infrastructure["Infrastructure/docker"]
        Mongo["MongoDB<br/>mongodb:27017"]
        Redis["Redis<br/>redis:6379"]
        Rabbit["RabbitMQ<br/>rabbitmq:5672"]
        Elastic["Elasticsearch<br/>es:9200"]
        Nginx["Nginx content host<br/>nginx:80"]
        Prom["Prometheus"]
    end

    Jenkins -->|"build, publish, run"| Docker
    Docker -->|"pull/start images"| Registry
    Docker --> Crawler

    Management -->|"gRPC IWebDriverService"| WebDriver
    Data -->|"gRPC IWebDriverService"| Request
    Request -->|"gRPC IWebDriverService"| WebDriver
    Management -->|"configuration HTTP"| Configuration
    Scheduler -->|"configuration repository"| Mongo
    Configuration -->|"configuration repository"| Mongo
    Management -->|"repositories"| Mongo
    Data -->|"repositories"| Mongo
    Management -->|"cache"| Redis
    Data -->|"cache"| Redis
    Request -->|"cache"| Redis
    Management -->|"AMQP publish/consume"| Rabbit
    Data -->|"AMQP publish/consume"| Rabbit
    Scheduler -->|"AMQP publish"| Rabbit
    Management -->|"data repositories and Serilog"| Elastic
    Data -->|"data repositories and Serilog"| Elastic
    Request -->|"Serilog"| Elastic
    WebDriver -->|"Serilog"| Elastic
    Configuration -->|"Serilog"| Elastic
    WebDriver -->|"browser/content source"| Nginx
    Crawler -.->|"metrics scraped"| Prom
```

### Components

| Component | Started by Jenkins | Injected or configured dependencies | Primary role |
| --- | --- | --- | --- |
| Request Manager | Yes (`request_server`) | Redis; WebDriver gRPC client; Elasticsearch logging | Receives HTTPS/gRPC crawl requests and coordinates request execution |
| WebDriver Server | Yes (`webdriver_server`) | Firefox/WebDriver implementation; Elasticsearch logging | Downloads and renders target pages through the gRPC API |
| Management Service | Yes (`management_service`) | MongoDB repositories; Redis cache; RabbitMQ; Elasticsearch repository/logging; WebDriver gRPC client | Manages crawl requests, configuration, publishing, and dead-letter handling |
| Scheduler | Yes (`scheduler`) | MongoDB repositories; RabbitMQ publisher; configuration repository | Creates scheduled and URI crawl work |
| Data Service | Yes (`data_service`) | MongoDB repositories; Redis cache; RabbitMQ; Elasticsearch repositories/logging; Request Manager gRPC client | Consumes crawl work and persists/cleans crawl data |
| Configuration Server | Not in current Jenkins start stage | MongoDB repositories; Elasticsearch logging | Serves crawler configuration over HTTPS |
| MongoDB | Infrastructure script | Persistent `/home/user/docker/volumes/mongodb` volume | Crawler requests, schedules, configuration, and document data |
| Redis | Infrastructure script | Persistent `/home/user/docker/volumes/redis` volume | Shared crawler cache |
| RabbitMQ | Infrastructure script | `crawler` virtual host; port 5672 | Crawl request, URI, response, legacy, and clean-data messages |
| Elasticsearch | Infrastructure script | Persistent `/home/user/docker/volumes/es` volume | Search/data repository and centralized Serilog sink |
| Docker Registry | Infrastructure script | Persistent registry volume | Stores the images built by Jenkins |

## Exchange processing model

Every Exchange flow has the same logical middle section:

```mermaid
flowchart LR
    In["DataIn consumer"] --> Transform["Optional Transformer"]
    Transform --> Filter["Publisher filters"]
    Filter --> Out["Matched DataOut publisher(s)"]
    In -. "failure" .-> Dead["Deadletter publisher"]
    Transform -. "failure" .-> Dead
    Out -. "failure" .-> Dead
```

The flow diagrams below show the concrete endpoint combinations configured in
`Microservice.Exchange/Microservice.Exchange.Test` and the Bertrand integration
tests. Dead-letter output is omitted from the success path and shown as a
dashed error edge.

## Flow 1: File to console and file

Configured by `appsettings.Development.json` and `appsettings.Simple.json`.

```mermaid
flowchart LR
    FilesIn["testData/in<br/>or testData/simple/in"] --> Consumer["FileConsumer"]
    Consumer --> Transformer["TestDataTransformer"]
    Transformer --> Console["ConsolePublisher"]
    Transformer --> FilesOut["FilePublisher<br/>testData/out or testData/simple/out"]
    Consumer -. "read/transform/publish error" .-> Dead["Console deadletter"]
```

## Flow 2: CSV to JSON file

Configured by `appsettings.Csv.json`.

```mermaid
flowchart LR
    Csv["test.csv"] --> Consumer["CsvConsumer"]
    Consumer --> File["FilePublisher<br/>testData/csv/out"]
    Consumer -. "read/publish error" .-> Dead["Console deadletter"]
```

## Flow 3: Command to file, then file to command

Configured by `appsettings.Development.Command.json`.

```mermaid
flowchart LR
    CommandIn["/bin/bash<br/>list appsettings files"] --> Ex1["CommandAsDatIn<br/>CommandConsumer"]
    Ex1 --> Shared["testData/command/in"]
    Shared --> Ex2["CommandAsDatOut<br/>File consumer"]
    Ex2 --> CommandOut["/bin/bash<br/>write testData/command/out/testoutput"]
    Ex1 -. "error" .-> Dead1["Console deadletter"]
    Ex2 -. "error" .-> Dead2["Console deadletter"]
```

## Flow 4: File to RabbitMQ and RabbitMQ to file

Configured by `appsettings.Development.Rabbitmq.json`.

```mermaid
flowchart LR
    Input["testData/rabbitmq/in"] --> Publish["TestRabbitMqOutExchange<br/>File consumer"]
    Publish --> Transform["TestDataTransformer"]
    Transform --> RabbitOut["RabbitMq publisher"]
    RabbitOut --> Broker["RabbitMQ<br/>exchange: exchangetest<br/>queue: request_queue<br/>routing key: request*"]
    Broker --> RabbitIn["TestRabbitMqInExchange<br/>RabbitMq consumer"]
    RabbitIn --> Output["testData/rabbitmq/out"]
    Publish -. "error" .-> Dead1["Console deadletter"]
    RabbitIn -. "error" .-> Dead2["Console deadletter"]
```

## Flow 5: File to MongoDB and MongoDB to file

Configured by `appsettings.Development.Mongodb.json`.

```mermaid
flowchart LR
    Input["testData/mongodb/in"] --> Publish["TestMongodbOut<br/>File consumer"]
    Publish --> Transform["TestDataTransformer"]
    Transform --> MongoOut["MongoDB publisher<br/>test.TestDataDocument"]
    MongoOut --> Mongo["MongoDB"]
    Mongo --> MongoIn["TestMongoDbIn<br/>MongoDB consumer<br/>filter: EnrichedData"]
    MongoIn --> Output["testData/mongodb/out"]
    Publish -. "error" .-> Dead1["Console deadletter"]
    MongoIn -. "error" .-> Dead2["Console deadletter"]
```

## Flow 6: File to Elasticsearch and Elasticsearch to file

Configured by `appsettings.Development.Elasticsearch.json`.

```mermaid
flowchart LR
    Input["testData/elasticsearch/in"] --> Publish["ElasticOut<br/>File consumer"]
    Publish --> Transform["TestEsDataTransformer"]
    Transform --> EsOut["Elasticsearch publisher<br/>index: testexchange"]
    EsOut --> Elastic["Elasticsearch"]
    Elastic --> EsIn["ElasticIn<br/>Elasticsearch consumer<br/>match EnrichedData"]
    EsIn --> Output["testData/elasticsearch/out"]
    Publish -. "error" .-> Dead1["Console deadletter"]
    EsIn -. "error" .-> Dead2["Console deadletter"]
```

## Flow 7: Bertrand RabbitMQ to Elasticsearch with MongoDB state

The Bertrand integration tests compose a RabbitMQ consumer, transformer, routing
filters, and Elasticsearch publisher. MongoDB stores exchange and dead-letter
state so processing can resume after failures.

```mermaid
flowchart LR
    RabbitIn["RabbitMQ consumer<br/>testexchange_input<br/>bertrand_exchange_test"]
    RabbitIn --> Transformer["Bertrand transformer"]
    Transformer --> Filter["Routing-key/type filters"]
    Filter --> RabbitOut["RabbitMQ publisher<br/>testexchange<br/>bertrand_exchange_test_output"]
    Filter --> ElasticOut["Elasticsearch publisher"]
    ElasticOut --> Elastic["Elasticsearch"]
    State["MongoDB Bertrand state store"] -. "checkpoint/deadletter state" .- Transformer
    Transformer -. "transform error" .-> Dead["MongoDB deadletter state"]
```

## Flow summary

| Flow | Input | Transformer | Output | Error handling |
| --- | --- | --- | --- | --- |
| File to console/file | File directory | `TestDataTransformer` | Console and file | Console deadletter |
| CSV to JSON file | `test.csv` | None | JSON file directory | Console deadletter |
| Command round trip | Shell command, then file | None | File, then shell command | Console deadletter per exchange |
| RabbitMQ round trip | File, then RabbitMQ | `TestDataTransformer` on outbound leg | RabbitMQ, then file | Console deadletter per exchange |
| MongoDB round trip | File, then MongoDB | `TestDataTransformer` on outbound leg | MongoDB, then file | Console deadletter per exchange |
| Elasticsearch round trip | File, then Elasticsearch | `TestEsDataTransformer` on outbound leg | Elasticsearch, then file | Console deadletter per exchange |
| Bertrand RabbitMQ to Elasticsearch | RabbitMQ | Bertrand transformer and filters | RabbitMQ and Elasticsearch | MongoDB state/deadletter store |
