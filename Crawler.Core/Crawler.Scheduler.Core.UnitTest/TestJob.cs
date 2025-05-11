using Quartz;

namespace SchedulerText
{
    public class TestJob : IJob
    {
        internal static int NoOfCalls { get; set; }

        public Task Execute(IJobExecutionContext context)
        {
            NoOfCalls++;
            return Task.CompletedTask;
        }
    }
}
