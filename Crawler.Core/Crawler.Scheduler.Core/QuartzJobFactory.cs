using System;
using Quartz;
using Quartz.Spi;

namespace Crawler.Scheduler.Core;

public class QuartzJobFactory : Quartz.Spi.IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public QuartzJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return _serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
    }

    public void ReturnJob(IJob job)
    {
        var disposableJob = job as IDisposable;

        disposableJob?.Dispose();
    }
}
