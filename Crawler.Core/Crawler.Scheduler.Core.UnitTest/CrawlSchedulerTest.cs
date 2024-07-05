using Crawler.RequestHandling.Core;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Quartz;
using SchedulerText;

namespace Crawler.Scheduler.Core.UnitTest;


[TestClass]
public class CrawlSchedulerTest
{
    // ToDo Eh?
    //[TestMethod]
    public async Task StartSchedulerThenJobsStarted()
    {
        // ARRANGE
        var testJob = JobBuilder.Create<TestJob>().Build();
        var trigger = TriggerBuilder.Create().ForJob(testJob).WithSimpleSchedule(s =>
        {
            s.WithIntervalInSeconds(1);
            s.RepeatForever();
        }).Build();

        var testee = Arrange(testJob, trigger);

        // ACT
        await testee.Start().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);

        await Task.Delay(2000);

        // ASSERT
        Console.WriteLine($"NoOfCall: {TestJob.NoOfCalls}");

        Assert.AreEqual(3, TestJob.NoOfCalls);

        await testee.Stop().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);
    }


    // ToDo Eh?
    //[TestMethod]
    public async Task StartScheduler_WhenCron_ThenJobsStarted()
    {
        // ARRANGE
        TestJob.NoOfCalls = 0;
        var testJob = JobBuilder.Create<TestJob>().Build();
        var trigger = TriggerBuilder.Create().ForJob(testJob).WithCronSchedule("* * * * * ?",b => b.WithMisfireHandlingInstructionFireAndProceed()).Build();

        var testee = Arrange(testJob, trigger);

        // ACT
        await testee.Start().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);

        await Task.Delay(2000);

        // ASSERT
        Console.WriteLine($"NoOfCall: {TestJob.NoOfCalls}");

        Assert.AreEqual(3, TestJob.NoOfCalls);

        await testee.Stop().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);
    }

    //[TestMethod]
    public async Task StartUnscheduledUriCrawlJob()
    {
        // ARRANGE
        TestJob.NoOfCalls = 0;

        //IJobFactory
        var testJob = JobBuilder.Create<UnscheduledUriCrawlJob>().Build();
        var trigger = TriggerBuilder.Create().ForJob(testJob).WithCronSchedule("* * * * * ?",b => b.WithMisfireHandlingInstructionFireAndProceed()).Build();

        var testee = Arrange(testJob, trigger);

        // ACT
        await testee.Start().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);

        await Task.Delay(10000);
    }

    private static CrawlerScheduler Arrange(IJobDetail testJob, ITrigger trigger)
    {
        TestJob.NoOfCalls = 0;

        var jobFactoryMock = new Mock<IJobFactory>();
        jobFactoryMock
            .Setup(m => m.GetUnscheduledCrawlsJob())
            .Returns(new Tuple<Quartz.IJobDetail, Quartz.ITrigger>(
                testJob,
                trigger));

        var testee = new CrawlerScheduler(Mock.Of<ILogger<CrawlerScheduler>>(), jobFactoryMock.Object, null, Mock.Of<IRequestPublisher>());
        return testee;
    }
}
