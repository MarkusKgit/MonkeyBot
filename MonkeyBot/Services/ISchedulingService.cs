using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface ISchedulingService
    {
        void ScheduleJobOnce(string jobID, DateTime time, Action job);
        void ScheduleJobOnce(string jobID, DateTime time, Task job);

        void ScheduleJobRecurring(string jobID, TimeSpan interval, Action job, TimeSpan? delay = null);
        void ScheduleJobRecurring(string jobID, TimeSpan interval, Task job, TimeSpan? delay = null);

        void ScheduleJobRecurring(string jobID, string cronExpression, Action job);
        void ScheduleJobRecurring(string jobID, string cronExpression, Task job);

        DateTime GetNextRun(string jobID);

        void RemoveJob(string jobID);
    }
}
