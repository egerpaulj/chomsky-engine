# Chomsky Engine - ALPHA
MIT Professor Noam Chomsky, is well known as "The most important intellectual in our lifetime". He is also regarded as a "Truth Teller" in massive proportions.

Chomsky reads vast amounts of literature, and deciphers the information, representing an unvarnished truth of the "cause and effect" of 
- Government Policies/Strategies
- Global Corporate Strategies

Noam takes into account the significance of our historical contexts; giving a clear summary, helping us understand the evolution/application/implications, of the policies and strategies, that are applied to our daily lives.

The purpose of this project is to model Noam Chomsky's efforts programmatically. Hence the Chomsky engine will need several components:
1) Information Retrieval System
2) Information Storage  System
3) Information Analysis System
4) Information Publication System

## Information Retrieval System - BETA
There are vast amounts of factual/fictional information readily available. The purpose of the information retrieval system is to traverse the data and retrieve the key messages.

Note: the system is NOT aware if the data is factual or fictional. Instead provides capabilities to gather and store information.
E.g. News articles, Financial Stock Data, Balance Sheets, Environmental Agency Reports, UN Resolutions and votes, Health Agency Reports etc.

### Microservice - BETA
To create a scallable distributed system, an Onion architecture will be used with many re-usable middlewear. 

Additionally, all operations will be performed on Asynchronous monads or utilize optional properties; this is done to avoid null checks and to provide functions without "side-effects".

ToDO Put diagram here

General Microservice related topics will be handled by the Microservice layers
#### Logging
Serilog is used and is provided as a configurable for all Microservices. 

Serilog allows several target syncs: e.g. Databases, Elastic Search, File, Console

#### CorrelationID
Middlewear ensures all microservices communicate using a correlation ID. This allows distrubuted services' logs to be analysed with ease.

#### Metrics (duration, counts, etc.)
Logging and Monitoring are two different concepts. Hence each microservice will track metrics and make them available for a scraper. 
Prometheus is used to scrape the Metric information.

These metrics are fed to Grafana, for visualization, alerts. 
The metrics can also be used to model statistical anomalies based on time; or any other identified dimension

ToDo Include image

#### Security
Mostly covers HTTPS.

Since these services will not have external access; the hosting environment can block access to the relevant ports using a firewall or iptables.

#### Serialization
Generic Serialization might be needed for special types, or for any nested composite patterns. 
Newtonsoft.Json is used for Serialization.

#### REST
For quick requests a REST Middlewear is available. Allowing a quick, common setup of services; covering logging HTTPS, metrics, CorrelationID uniformly

#### GRPC
A generic implementation of the GRPC allows setting up GRPC Microservices fast; without having to create a contract for each type sent/received message

#### AMQP
Integration to external workers, or the ability to distribute work is important. 

Implementation of the RabbitMQ AMQP, allows quick bootstrapping/definition of exchanges/queues.

The subscriber provides an IObservable<T>, this allows utilizing all the capabilities of the reactive extension framework.

The result is quick integration of Publishers and Observables without having to worry about the infrastructure code.

### Resilliency (e.g. retry with jitter)
REST/AMQP/GRPC rely on TCP. This means all network issues envisaged might occur sporadically. A retry with a jittered back-off would allow some recovery without overloading any struggling services.
If a scheduled crawl were to fail, it will be marked in Monitoring (Prometheus, Grafana), and will be automatically rescheduled based on the period. Nevertheless it is possible to reschedule immediately by restarting the Scheduler Service.

### Crawler - BETA
The purpose of the Crawler is to navigate web data without disruption to services; the key features are:
- schedule daily/hourly crawls
- don't overload target systems
- extract relevant information only
- identify additional data/sources to extract information

TODO Put sequence diagram here

#### Crawler Scheduler 
Quartz Scheduler will be used to schedule work. The configurations and 'schedule definition' will be retrieved from a document storage system (e.g. MongoDB)

Each schedule is loaded into the Quartz scheduler defined by the period/frequency. 
Note: some domains should be crawled completley - but only once a day.
Note: some pages should not be crawled more than once

To ensure resources are not wasted on target and source systems, crawled sites will be recorded; and unnecessary crawls are avoided.

#### Request Management 
To avoid any overloads on target systems, and to avoid being blocked, the requests should be throttled.
The request management services keeps track of the last sent request; to a specific domain (e.g. siteOfInterest.com). Thus ensuring any consecutive requests are throttled. 

A Microservice using GRPC would allow all clients to make requests in parallel, and obtain necessary results; without overloading the target system

Note: a random number generator would ensure the jitters/throttling, would not contain an identifiable pattern (i.e. if the runtime has enough entropy)

#### Strategies to continue crawling
There can be several strategies for various data sources; including some non-generic custom strategies.

E.g.
- Crawl All Links in site
- Crawl All Links within the current domain
- Download images and files
- Identify all Links and schedule to crawl at a later point

#### Crawler Service
A general service, which takes a configuration consisting of
- several user actions for that domain
- the data to be parsed
- the continuation strategy

E.g. 
a) enter username/password
b) login
c) navigate to target page
d) extract structured data

The service can be wrapped in a generic Microservice with various GRPC asynchronous calls.

A headless selenium driver with Firefox/Chromium browser, allows navigation, form-filling of any website with javascript, HTML5, etc.

Note: this service will be open for web attacks from malicious target sites. Hence it is advised, to run these in containerized services and to regulary refresh the host environments
Note: e.g. in Kubernetes, a node can be tainted for these PODs, with restricted access. Then schedule to recreate the Node regularly, and restart the Crawler Service PODs


#### Crawler Configuration Service
The Crawler Configuration Service will provide configuraitons for a given domain or specific site:
- Uri
- DocumentParts to parse
- Download content (e.g. images/files)
- Continuation Strategy

#### Parsing Site Data
A composite parsing pattern is used with flexibility to define what to parse. 

Each site is modelled as a DocumentPart: e.g.
- Article
- Paragraph/Text
- Table
- Table Row
- Links
- Images/Files

A DocumentPart can have many parts within. Each part has an XPATH definition to identify the scope of data extraction.

If a Document Part is not defined, then Parts are automatically detected and ALL data will be extracted

Note: Parsing/storing data structurally, might allow better results, when modelling the obtained data for machine learning.

## Information Storage System
The vast amounts of information needs to be stored and indexed in a clustered storage system; and allows scaling.

This allows general analysis of publicly available data.

Currently the target storage repository is Elastic Search. This can be replaced with any other Repository by implementing and injecting the respective interface.
E.g. FileBase (store the target data in folders)
E.g. Mongodb (store the data in a document storage system)
E.g. MySql/postgres (relational database)

## Third-party Services - IN PROGRESS

Using Enterprise Integration Patterns, readily available in Apache CAMEL, integrations and publications to various thrid-party services would be possible (e.g. Twitter, E-mail etc.)

## Information Analysis System - IN PROGRESS
Advanced analysis using Machine Learning, would use state of the art artificial intelligence, to establish links/patterns within the large data sets.

Additionally, with the support of a community of non-profit, non-biased journalists, additional analysis should be performed/submitted.

## Information Publication - IN PROGRESS
Finally, the results of the analysis should be made publicly available.
Additionally, users should be granted access to the data, with tools providing insights to data

Hopefully, the result here is to provide an honest summary of the vast of amounts of data. This would allow any member, of any open/free society, to use the tool deduce insights, for their own spiritual, political or any other development.

## CI/CD Pipelines
Jenkins is used to build, start docker containers and integration test services.

Note: Jenkins will need access to docker. If Jenkins is running within a docker container; then ensure the user has access to /etc/DOCKERD (TODO Exact path)

ToDo Include Picture

## Development Environment
The entire environment can be bootstrapped using docker.

To Achieve this examine the startEnvironment.sh file. This will use local storage and start containers:
- Grafana
- Elastic Search
- Prometheus
- MongoDB
- RabbitMQ
- Jenkins

Additionally you can start the following via Visual Studio Code debug (or manually via dotnet run)
- Crawler Configuration Service
- Crawler Request Manager GRPC Service
- Crawler Selenium Service
- Crawler Scheduler Worker
- Integration Test Server (web server hosting websites)

GRPC requires valid HTTPS certificates. Hence the containers need to be started a with specific names; and configured accordingly. Jenkins build file; creates necessary certificates for the respective containers (and trusts the new certificate; within the containers and in the development environment):

Note: replace the hosting logic, CI/CD pipelines to host HTTPS with a fixed/different certificate

e.g.

``` 
stage('Generate Certificates') {
      steps {
        sh 'openssl rand -base64 32 > passphrase'
        sh 'openssl rand -base64 32 > exportphrase'
        sh 'cp exportphrase Crawler.Configuration.Server/.'
        sh 'cp exportphrase Crawler.WebDriver.Grpc.Server/.'
        sh 'cp exportphrase Crawler.RequestManager.Grpc.Server/.'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout configserverkey.pem -out configservercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=config_server"'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout webdriverkey.pem -out webdrivercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=webdriver_server"'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout requestmanagerkey.pem -out requestmanagercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=request_server"'
        sh 'openssl pkcs12 -export -out Crawler.Configuration.Server/configserver.pfx -inkey configserverkey.pem -in configservercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'openssl pkcs12 -export -out Crawler.WebDriver.Grpc.Server/webdriver.pfx -inkey webdriverkey.pem -in webdrivercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'openssl pkcs12 -export -out Crawler.RequestManager.Grpc.Server/requestmanager.pfx -inkey requestmanagerkey.pem -in requestmanagercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'cp configservercert.pem Crawler.Management.Service/.'
        sh 'cp requestmanagercert.pem Crawler.Management.Service/.'
        sh 'cp webdrivercert.pem Crawler.RequestManager.Grpc.Server/.'
        sh 'cp configservercert.pem /usr/local/share/ca-certificates/configserver.crt'
        sh 'cp requestmanagercert.pem /usr/local/share/ca-certificates/requestmanager.crt'
        sh 'cp webdrivercert.pem /usr/local/share/ca-certificates/webdriver.crt'
        sh 'update-ca-certificates'
      }
    }
``'''``

The docker file copies the generated certificate
```
COPY webdriver.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
```

The Generic Microservice configurator, configures HTTPS with kestrel
```
public static IWebHostBuilder UseKestrelHttps(this IWebHostBuilder builder)
        {
            builder.UseKestrel((context, options) =>
            {
                int.TryParse(context.Configuration["Port"], out var port);
                var certPath = context.Configuration["CertPath"];
                var passPath = context.Configuration["PassPath"];

                options.Listen(IPAddress.Any, port, listOptions =>
                        {
                            listOptions.UseHttps(certPath, File.ReadAllLines(passPath)[0]);
                            listOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });
                #if DEBUG
                #else
                    File.Delete(passPath);
                    File.Delete(certPath);
                #endif
            });

            return builder;
        }
```