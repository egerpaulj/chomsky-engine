# Job Schedules

The table below describes the current Quartz job schedules for `Crawler.Scheduler.Core`.

| Job Name | Job Class | Schedule / Trigger | MongoDB Collection / Source | Target / Action |
| --- | --- | --- | --- | --- |
| Stock Collector Job | `StockCollectionJob` | Cron: `0 15 16 ? * MON-FRI` (16:15 Mon-Fri) | Configuration Repository | Publishes stock market URLs to crawler request queue |
| Unscheduled Crawl Processing | `UnscheduledUriCrawlJob` | Start now, then every 5 minutes | `crawl_uri` (top 100 ordered by `Created` asc) | Publishes unscheduled crawl requests to host queues (queue size limit: 100) |
| Onetime URI Processing | `OnetimeUriJob` | Start now, then every 5 minutes | `uri` (top 100 incomplete onetime URIs ordered by `Created` asc) | Publishes onetime crawl requests to host queues (queue size limit: 100) |
| Found URI Processing | `FoundUriJob` | Start now, then every minute | `uri` (top 100 found URIs ordered by `Created` asc) | Processes newly discovered URIs and queues them for crawling |
| Collector Job | `UriCollectionJob` | Cron schedule (`CronPeriod` configured per URI) | `uri` (top 100 collector URIs ordered by `Created` asc) | Publishes collection crawl requests for target URIs (queue size limit: 100) |
| Periodic URI Schedule Job | `PeriodUriCrawlJob` | Cron schedule (`CronPeriod` configured per URI) | `uri` (top 100 periodic URIs ordered by `Created` asc) | Schedules periodic crawls into `crawl_uri` collection (queue size limit: 100) |
