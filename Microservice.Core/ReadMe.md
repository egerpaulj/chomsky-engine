# Microservice.Core 

.NET library provides re-usable Microservice components:
- Logging (seri Log)
- Metrics (Prometheus)
- Generic Http Client (Factory, Injection, Resiliency)
- CorrelattionId
- Https
- Request duration logging

**Note:** Contains git submodule 

```
git clone --recurse-submodules https://github.com/egerpaulj/Microservice.Amqp.git
```

OR

```
git clone https://github.com/egerpaulj/Microservice.Amqp.git
git submodule init
git submodule update
```

## Abstract

Whilst implementing Microservice designs; several common design/implementation decisions should be provided as re-usable libraries.

This allows quick boostrapping of services; so that the engineers can focus on the implementations; i.e. implement "business logic". 

### Some examples are listed below:

E.g. 
- **Custom Request Duration Histogram Middlewear**"
- **Host Prometheus metrics**

```
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app
    .UseRouting()
    .UseCustomSerilogRequestLogging()
    .SetupMetrics()
    .UseMiddleware<RequestDurationMetricsMiddlewear>()
    .ConfigureGrpcService<WebDriverService>();
}
```

E.g. 
- **Logging** 
- **HTTPS Kestrel**
```
Host.CreateDefaultBuilder(args)
    .SetupLogging()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder
        .UseKestrelHttps()
        .UseStartup<Startup>();
    });
```

E.g. 
- **HttpClient** 
```
Host.CreateDefaultBuilder(args)
    .SetupLogging()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
        services.SetupHttpClient();
        ...
    });
```

## Logging

Serilog is a highly efficient logging library; with many target sinks. The following are popular sinks:
- Console
- File
- Elastic-search
- MongoDb

See https://serilog.net

Microservice.Core library provides bootstrap logic for logging:
- Set up dependency injection for **ILogger<T>**
- Load appsettings and configure **Logger<T>**


### Logging Usage

Simply call **SetupLogging()** when bootstrapping the **IHostBuilder**.

E.g. 

```
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .SetupLogging();
```

**Note:** The configuration target sinks can be provided via the **appsettings**.

E.g. Configure 3 sinks: **Console**, **File** and **Elastic-Search**

Note: See below **Using** and **WriteTo**

```
"Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Sinks.Elasticsearch"
    ],
    "Enrich": ["FromLogContext"],
    "Minimumlevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo" : [
      { "Name": "Console" },
      { 
        "Name": "File",
        "Args": {
          "path": "application.log"
        }
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://10.137.0.50:9200/",
          "autoRegisterTemplate": true,
          "overwriteTemplate": true,
          "autoRegisterTemplateVersion": "ESv7",
          "registerTemplateFailure": "IndexAnyway",
          "numberOfShards": "1",
          "numberOfReplicas": "1",
          "indexFormat": "crawler-logs-{0:yyyy.MM.dd}",
          "emitEventFailure": "WriteToSelfLog"
          
        }
      }
    ]
  }
```

E.g. Any type **T** can be used with **ILogger<T>**; the logger will be injected and configured; to log to all sinks defined in the **appsettings**.

```
public class Foo
{
  public Foo(ILogger<Foo> logger)
  {}
}
```

### E.g. Logs in Elastic Search

  ![Screenshot: Kibana Querying Elastic Search](/Documentation/EsLogsGeneral.png)

### E.g. Logs queried from Elastic Search in Grafana

  ![Screenshot: Grafana Querying Elastic Search](/Documentation/LogsInGrafana.png)

## Metrics

Metrics (infrastructure, business, performance etc.) can provide important insights. Additionally would enable visualizations, alerting, monitoring of the hosted services.

Prometheus is a time series database designed for this purpose. Metrics hosted can be "scraped" by **Prometheus** and later visualized/processed in **Grafana**.

  ![Metrics of a distrubuted crawler](/Documentation/GeneralMetrics.png)

See https://prometheus.io

### Metrics Usage 

Any Prometheus metric can be defined and recorded within the service/libraries used. 

E.g.

- Guages
- Counters
- Histograms
- Summary 

The metrics data can be exposed using **SetupMetrics()**. Additionally this would also record system-resource-metrics; i.e. resources consumed by the service.

Note: ensure **SetupMetrics()** is called after **UseRouting()**

E.g.

```
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app
    .UseRouting()
    .SetupMetrics();
}
```

  ![Prometheus configured to scrape metrics from services/workers](/Documentation/PrometheusScraping.png)

### Metrics on Request Duration

The library provides a Prometheus Histogram (with label: **request_duration_in_ms**); which contains the "request duration" in milliseconds. 

These can be used to keep track of anomalies (E.g. abnormally long durations, large amounts of requests during off peak time-ranges. etc. )

To enable request duration metrics, call **.UseMiddleware<RequestDurationMetricsMiddlewear>()** during bootstrap.

```
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app
    .UseRouting()
    .UseMiddleware<RequestDurationMetricsMiddlewear>();
}
```

  ![Request duration multiple services: REST and GRPC](/Documentation/requestDuration.png)

### Metrics insights

The example below shows how a single request, results in several requests to microservices. It also shows the CPU spikes during requests.

  ![Time-based Insights of collected metrics](/Documentation/insights.png)

**Hint:** alarms/triggers can be setup based on thresholds etc.

**Hint:** prometheus can be queried to create a normal distrubution from the data; in a given time period. The distrubution can be used to spot anomalies in metrics collected afterwards; then raise alarms or execute triggers as needed.

## HTTPS

It is good practice to communicate via HTTPS; since the packet's data is not readable to third-parties; i.e. if a compromised system starts to inspect packets.

**Note:** Additionally, protocols like Grpc would not work without HTTPS. 

### HTTPS Usage

The following code will configure kestrel to host HTTPS: **UseKestrelHttps()**; 

```
Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder
        .UseKestrelHttps()
        .UseStartup<Startup>();
    });
```


The path to the certificate and password should be defined in the configuration file (e.g. appsettings.Development.json):
```
{
  "Port" : "443",
  "CertPath" : "/opt/certs/certificate.pfx",
  "PassPath" : "/opt/certs/exportphrase"
}
```

Additionally, the **UseKestrelHttps()** deletes the certificate and password files (this is handy for security reasons; especially if a CI/CD pipeline lacks a vault to store/retrieve secrets); and useful if the service is not exposed outside the cluster (See ISTIO for additional security options for PODs)


Note: once the containers are generated; it is convenient to forget the private certificate. **Once started, the certificate/password is deleted by default.**

Note: if the certificate should remain in the configured path; then set the following in appsettings
```
"KeepCertificate" : true
```

### HTTPS Generate certificates

The following commands can be used to generate the necessary certificates:

#### 1) Generate security phrases 

The following will generate secure passwords

```
openssl rand -base64 32 > passphrase
openssl rand -base64 32 > exportphrase
```

#### 2) Generate keys 

The following will generate a private key and public key; using the generated "passphrase" in the previous step

```
openssl req -x509  -newkey rsa:4096 -keyout privatekey.pem -out publickey.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=config_server"
```

#### 3) Generate a certificate 

Based on the private and public keys generated previously; the following will generate a certificate using the exportphrase; which is the password for the generated certificate: **certificate.pfx**

```
openssl pkcs12 -export -out certificate.pfx -inkey privatekey.pem -in publickey.pem -passin file:passphrase -passout file:exportphrase
```

#### 4) Use certificate in kestrel services

Once the certificate is generated, then it can be copied to the runtime folder/docker-container.


E.g.
```
FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY bin/Release/net5.0/publish/ App/
WORKDIR /App

COPY certificate.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
```

note: the paths to the certificates should be configured in the service's configuration file (e.g. appsettings.Development.json).
```
{
  "Port" : "443",
  "CertPath" : "/opt/certs/certificate.pfx",
  "PassPath" : "/opt/certs/exportphrase"
}
```

#### 5) Clients

Client containers, that communicate with the kestrel service; should trust the newly generated certificate.


E.g.
```
FROM mcr.microsoft.com/dotnet/aspnet:5.0

COPY certificate.pem /usr/local/share/ca-certificates/configurationservice.crt
RUN update-ca-certificates
```

## Http Client

An implementation of **IHttpClientService** is provided; it allows dependent libraries to Send/Receive REST messages.

```
public interface IHttpClientService
{
    TryOptionAsync<T> Get<T>(Option<Guid> correlationId, Option<string> uri);

    TryOptionAsync<string> GetStringContent(Option<Guid> correlationId, Option<string> uri);

    TryOptionAsync<T> Send<R, T>(Option<Guid> correlationId, Option<R> send, Option<string> uri, Option<HttpMethod> method);

    TryOptionAsync<Unit> Send<R>(Option<Guid> correlationId, Option<R> send, Option<string> uri, Option<HttpMethod> method);
}
```

**R** - the request Type is serialized to JSON and sent to the target URI

**T** - the expected reponse is deserialized and returned

Note: the "Correlation ID" can be specified when making a request; otherwise a default correlation will be assigned

### Http REST - Request Serialization - Response Deserialization

By providing a Serialization Provider for special types, any object can be sent/received using the JSON format.

The HTTP client uses **Newtonsoft.Json** to Serialize requests and Deserialize responses.

See Microservice.Serialization to define custom JSON serialization/deserialization.

### Http Resiliency
Http REST requests are based on the TCP protocol. The requests should not be long-running (for long-running, see Microservice.Grpc).

Network issues are generally sporadic and short-lived but can occur. 

E.g. the timeout for a request depends on many factors:
- Operating system 
- Network device (e.g. router) 
- Hosting Service

Therefore certain HTTP errors should be handled by retrying (but without overloading the target service).

Http Client uses Polly, with the **DecorrelatedJitterBackoffV2** Jitter strategy; it is used to avoid overloading the server:

See https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry

### TryOptionAsync<T>

The interface uses a Monad **TryOptionAsync<T>** to represent a return type. This allows the following encapsulation:
- An Async operation
- Potential Exceptions
- Successfully Result T is returned (otherwise Exception or Null is returned).

This allows better binding of pure functions. The callee can decide when to propagate the monad back; by calling **.Match**.

Additionally, the callee can also use **.Bind** to another TryOptionAsync; creating a flow of pure functions

E.g.
```
await _configurationRepository
          .GetConfiguration(uri)
          .Match(
              r => r, // Result found => return result
              CreateDefault(u), // Result is null => create a default value
              e => throw e // Exception => throw error or return a value; for error-cases
              );
```

Note: null checks are not necessary.

see https://github.com/louthy/language-ext

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