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
    [TestMethod]
    public async Task StartSchedulerThenJobsStarted()
    {
        // ARRANGE
        var id = Guid.NewGuid().ToString();
        var testJob = JobBuilder.Create<TestJob>().WithIdentity("job_" + id).Build();
        var trigger = TriggerBuilder
            .Create()
            .WithIdentity("trigger_" + id)
            .ForJob(testJob)
            .WithSimpleSchedule(s =>
            {
                s.WithIntervalInSeconds(1);
                s.RepeatForever();
            })
            .Build();

        var testee = Arrange(testJob, trigger);

        // ACT
        await testee.Start().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);

        await Task.Delay(1500);

        // ASSERT
        Console.WriteLine($"NoOfCall: {TestJob.NoOfCalls}");

        Assert.IsTrue(TestJob.NoOfCalls > 0, "TestJob should have executed at least once");

        await testee.Stop().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);
    }

    [TestMethod]
    public async Task StartScheduler_WhenCron_ThenJobsStarted()
    {
        // ARRANGE
        TestJob.NoOfCalls = 0;
        var id = Guid.NewGuid().ToString();
        var testJob = JobBuilder.Create<TestJob>().WithIdentity("job_" + id).Build();
        var trigger = TriggerBuilder
            .Create()
            .WithIdentity("trigger_" + id)
            .ForJob(testJob)
            .WithCronSchedule("* * * * * ?", b => b.WithMisfireHandlingInstructionFireAndProceed())
            .Build();

        var testee = Arrange(testJob, trigger);

        // ACT
        await testee.Start().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);

        await Task.Delay(1500);

        // ASSERT
        Console.WriteLine($"NoOfCall: {TestJob.NoOfCalls}");

        Assert.IsTrue(TestJob.NoOfCalls > 0, "TestJob should have executed at least once");

        await testee.Stop().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);
    }

    [TestMethod]
    public async Task StartUnscheduledUriCrawlJob()
    {
        // ARRANGE
        TestJob.NoOfCalls = 0;

        var id = Guid.NewGuid().ToString();
        var testJob = JobBuilder.Create<TestJob>().WithIdentity("job_" + id).Build();
        var trigger = TriggerBuilder
            .Create()
            .WithIdentity("trigger_" + id)
            .ForJob(testJob)
            .WithCronSchedule("* * * * * ?", b => b.WithMisfireHandlingInstructionFireAndProceed())
            .Build();

        var testee = Arrange(testJob, trigger);

        // ACT
        await testee.Start().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);

        await Task.Delay(1500);

        Assert.IsTrue(TestJob.NoOfCalls > 0, "TestJob should have executed at least once");

        await testee.Stop().Match(a => a, () => throw new Exception("Empty"), ex => throw ex);
    }

    private static Tuple<IJobDetail, ITrigger> CreateDummyJobAndTrigger()
    {
        var id = Guid.NewGuid().ToString();
        var dummyJob = JobBuilder.Create<TestJob>().WithIdentity("job_" + id).Build();
        var dummyTrigger = TriggerBuilder.Create().WithIdentity("trigger_" + id).StartNow().Build();
        return new Tuple<IJobDetail, ITrigger>(dummyJob, dummyTrigger);
    }

    private static CrawlerScheduler Arrange(IJobDetail testJob, ITrigger trigger)
    {
        TestJob.NoOfCalls = 0;

        var jobFactoryMock = new Mock<IJobFactory>();
        jobFactoryMock
            .Setup(m => m.GetStockCollectorJob())
            .Returns(CreateDummyJobAndTrigger());
        jobFactoryMock
            .Setup(m => m.GetUnscheduledCrawlsJob())
            .Returns(new Tuple<IJobDetail, ITrigger>(testJob, trigger));
        jobFactoryMock
            .Setup(m => m.GetFoundUriProcessingJob())
            .Returns(CreateDummyJobAndTrigger());
        jobFactoryMock
            .Setup(m => m.GetOnetimeUriProcessingJob())
            .Returns(CreateDummyJobAndTrigger());
        jobFactoryMock
            .Setup(m => m.GetPeriodicUriJobs())
            .ReturnsAsync(Enumerable.Empty<Tuple<IJobDetail, ITrigger>>());
        jobFactoryMock
            .Setup(m => m.GetUriCollectorJobs())
            .ReturnsAsync(Enumerable.Empty<Tuple<IJobDetail, ITrigger>>());

        var testee = new CrawlerScheduler(
            Mock.Of<ILogger<CrawlerScheduler>>(),
            jobFactoryMock.Object,
            null,
            Mock.Of<IRequestPublisher>()
        );
        return testee;
    }
}
