using System;

namespace MonkeyBot.Services
{
    public interface ISchedulingService
    {
        void ScheduleJobOnce(string jobID, DateTime time, Action job);

        void ScheduleJobRecurring(string jobID, int intervalSeconds, Action job, int delaySeconds);

        void ScheduleJobRecurring(string jobID, string cronExpression, Action job);

        DateTime GetNextRun(string jobID);

        void RemoveJob(string jobID);
    }
}
