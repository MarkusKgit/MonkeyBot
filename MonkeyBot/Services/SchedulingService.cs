using FluentScheduler;
using NCrontab;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class SchedulingService : ISchedulingService
    {
        private static Registry _registry;

        public SchedulingService()
        {
            if (_registry == null)
            {
                _registry = new Registry();
                JobManager.Initialize(_registry);
            }
        }

        public void ScheduleJobOnce(string jobID, DateTime time, Action job)
            => JobManager.AddJob(job, (x) => x.WithName(jobID).ToRunOnceAt(time));

        public async void ScheduleJobRecurring(string jobID, TimeSpan interval, Action job, TimeSpan? delay = null)
        {
            if (delay != null)
            {
                await Task.Delay((int)delay.Value.TotalSeconds);
            }
            JobManager.AddJob(job, (x) => x.WithName(jobID)
                                           .ToRunNow()
                                           .AndEvery((int)interval.TotalSeconds)
                                           .Seconds());
        }

        public void ScheduleJobRecurring(string jobID, string cronExpression, Action job)
        {
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
            if (cnSchedule == null)
            {
                throw new ArgumentException("Cron expression is wrong!");
            }

            // Add a new recurring job with the provided ID to the Jobmanager. The 5 seconds interval is only a stub and will be overridden.
            JobManager.AddJob(job, (x) => x.WithName(jobID).ToRunEvery(5).Seconds());
            // Retrieve the schedule from the newly created job
            Schedule schedule = JobManager.AllSchedules.FirstOrDefault(x => x.Name == jobID);
            // Because FluentScheduler does not support cron expressions we have to override the default method that
            // calculates the next run with the appropriate method from the CrontabSchedule scheduler
            if (schedule != null)
            {
                Type scheduleType = schedule.GetType();
                scheduleType
                  .GetProperty("CalculateNextRun", BindingFlags.NonPublic | BindingFlags.Instance)
                  .SetValue(schedule, (Func<DateTime, DateTime>)cnSchedule.GetNextOccurrence, null);
                scheduleType
                  .GetProperty("NextRun", BindingFlags.Public | BindingFlags.Instance)
                  .SetValue(schedule, cnSchedule.GetNextOccurrence(DateTime.Now));
            }
        }

        public DateTime GetNextRun(string jobID)
        {
            Schedule job = JobManager.GetSchedule(jobID);
            if (job == null)
            {
                throw new ArgumentException($"The specified job with id {jobID} does not exist", nameof(jobID));
            }

            return job.NextRun;
        }

        public void RemoveJob(string jobID)
            => JobManager.RemoveJob(jobID);
    }
}
