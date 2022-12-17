# Microservice.Exchange 

.NET Core library provides Core Web Crawler Functionality


## Crawler.Core

Core library to Parse HTML files. Document Parts can be defined; or are automatically detected. The **DocumentPart** composite is returned.

Library contains other Core interfaces/abstractions. 

E.g. 
- CrawlRequest
- CrawlResponse
- UserActions (interaction with a web page)


## Crawler.Strategies.Core

Core library with strategies to crawl web sites. Basic strategy with continuation is provided. Custom strategies can be introduced by implementing the following:
**ICrawlContinuationStrategy** and **ICrawlStrategy**

The following strategies are provided:
- Crawl all Links in Page
- Crawl Domain specific links
- Track links in Page


**Note:** the CrawlConfiguration can specify how to Crawl a particular web-site

## Crawler.WebDriver.Core

Core library defining interfaces for a Webdriver. The Crawler will interact with the web-driver for User-Actions and to extract data

## Crawler.Scheduler.Core

Core library provides scheduling logic for Crawls. 

E.g. 
- Hourly crawl of a website
- Crawl links found via Crawler Strategy
- Schedule outstanding crawls

## Crawler.RequestHandling.Core

Core library to manage Request Throttling. Target websites should NOT be over-loaded with requests (resulting in a denial-of-service). Instead requests should be throttled.

## Crawler.Management.Core

Core library to Bootstrap the Crawler Framework. With various data sources to read requests from and to publish results.

The following are provided:
- File
- RabbitMq
- Elasticsearch
- MongoDb


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
