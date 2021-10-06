using System;

namespace MonkeyBot.Modules.Reminders
{
    public class Reminder
    {
        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ReminderType Type { get; set; }

        public string Name { get; set; }

        public string Message { get; set; }

        public DateTime? ExecutionTime { get; set; }

        public string CronExpression { get; set; }
    }

    public enum ReminderType
    {
        Once,
        Recurring
    }
}