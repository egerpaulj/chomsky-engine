using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Quartz;

namespace Crawler.Scheduler.Core;

// Cron expression in quartz
//         1. Seconds [0,59]
//         2. Minutes [0,59]
//         3. Hour [0,23]
//         4. Day of the month [1,31]
//         5. Month of the year [1,12]
//         6. Day of the week ([0,6] with 0=Sunday)
//         7. Year [Not mandatory]
//
// Cron Operators in quartz
// * all values
// ? no specific value. E.g. in 6. don't care which day of the week
// - a range. E.g. 1-15
// , multiple values
// / increments: 0/15 in seconds --> mean 0,15,30,45
// L last values
// W weekday


// EXAMPLES
//         1. Clean up core files every weekday morning at 3:15 am:

//                15 3 * * 1-5 find "$HOME" -name core -exec rm -f {} + 2>/dev/null

//         2. Mail a birthday greeting:

//                0 12 14 2 * mailx john%Happy Birthday!%Time for lunch.

//         3. As an example of specifying the two types of days:

//                0 0 1,15 * 1

//            would  run  a  command on the first and fifteenth of each month, as
//            well as on every Monday. To specify days by  only  one  field,  the
//            other field should be set to '*'; for example:

//                0 0 * * 1

//            would run a command only on Mondays.


public interface IJobFactory
{
    Task<IEnumerable<Tuple<IJobDetail, ITrigger>>> GetPeriodicUriJobs();
    Tuple<IJobDetail, ITrigger> GetUnscheduledCrawlsJob();
    Task<IEnumerable<Tuple<IJobDetail, ITrigger>>> GetUriCollectorJobs();
    Tuple<IJobDetail, ITrigger> GetFoundUriProcessingJob();
    Tuple<IJobDetail, ITrigger> GetOnetimeUriProcessingJob();
}

public class JobFactory : IJobFactory
{
    private readonly ICrawlerConfigurationService _crawlConfiguration;
    public JobFactory(ICrawlerConfigurationService crawlConfiguration)
    {
        _crawlConfiguration = crawlConfiguration;
    }

    public async Task<IEnumerable<Tuple<IJobDetail, ITrigger>>> GetUriCollectorJobs()
    {
        var sourceData = await _crawlConfiguration
                            .GetCollectorUri()
                            .Match(r => r, () => throw new Exception("Failed to get Url collector data"), ex => throw ex);

        return sourceData.Select(s =>
            new Tuple<IJobDetail, ITrigger>(
                JobBuilder.Create<UriCollectionJob>()
                    .WithDescription($"Collector job: {s.Uri}")
                    .UsingJobData(UriCollectionJob.JobDataIdKey, s.Id)
                    .UsingJobData(UriCollectionJob.JobDataUriKey, s.Uri)
                    .Build(),
                TriggerBuilder.Create()
                    .WithDescription($"Collector job: {s.Uri}")
                    .WithCronSchedule(s.CronPeriod, x => x.WithMisfireHandlingInstructionFireAndProceed())
                    .Build()));
    }

    public async Task<IEnumerable<Tuple<IJobDetail, ITrigger>>> GetPeriodicUriJobs()
    {
        var sourceData = await _crawlConfiguration
                            .GetPeriodicUri()
                            .Match(r => r, () => throw new Exception("Failed to get Periodic Uri data"), ex => throw ex);

        

        return sourceData.Select(s =>
        {
            var job = JobBuilder.Create<PeriodUriCrawlJob>()
                    .WithDescription($"Periodic Uri Schedule job: {s.Uri}")
                    .UsingJobData(UriCollectionJob.JobDataIdKey, s.Id)
                    .UsingJobData(UriCollectionJob.JobDataUriKey, s.Uri)
                    .Build();

            return new Tuple<IJobDetail, ITrigger>(
                job,
                TriggerBuilder.Create()
                    .WithDescription($"Periodic Uri Schedule job: {s.Uri}")
                    .ForJob(job)
                    .WithCronSchedule(s.CronPeriod, x => x.WithMisfireHandlingInstructionFireAndProceed())
                    .Build());
        });
    }

    public Tuple<IJobDetail, ITrigger> GetUnscheduledCrawlsJob()
    {
        var unscheduledJob = JobBuilder.Create<UnscheduledUriCrawlJob>()
                .WithDescription("Unscheduled Crawl Processing")
                .Build();
        var unscheduledTrigger = TriggerBuilder.Create()
                .WithDescription("Unscheduled Crawl Processing")
                .WithSimpleSchedule(s => 
                {
                    s.WithIntervalInSeconds(15);
                    s.RepeatForever();
                })
                .ForJob(unscheduledJob)
                .Build();

        return new Tuple<IJobDetail, ITrigger>(unscheduledJob, unscheduledTrigger);
    }

    public Tuple<IJobDetail, ITrigger> GetFoundUriProcessingJob()
    {
        var unscheduledJob = JobBuilder.Create<FoundUriJob>()
                .WithDescription("Found URI Processing")
                .Build();
        var unscheduledTrigger = TriggerBuilder.Create()
                .WithDescription("Found URI Processing")
                .WithSimpleSchedule(s => 
                {
                    s.WithIntervalInSeconds(15);
                    s.RepeatForever();
                })
                .ForJob(unscheduledJob)
                .Build();

        return new Tuple<IJobDetail, ITrigger>(unscheduledJob, unscheduledTrigger);
    }

    public Tuple<IJobDetail, ITrigger> GetOnetimeUriProcessingJob()
    {
        var unscheduledJob = JobBuilder.Create<OnetimeUriJob>()
                .WithDescription("Onetime URI Prcessing")
                .Build();
        var unscheduledTrigger = TriggerBuilder.Create()
                .WithDescription("Onetime URI Prcessing")
                .WithSimpleSchedule(s => 
                {
                    s.WithIntervalInSeconds(15);
                    s.RepeatForever();
                })
                .ForJob(unscheduledJob)
                .Build();

        return new Tuple<IJobDetail, ITrigger>(unscheduledJob, unscheduledTrigger);
    }
}
