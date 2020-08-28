using System;

namespace MonkeyBot.Services
{
    public class HistoricData<T>
    {
        public DateTime Time { get; set; }
        public T Value { get; set; }

        public HistoricData()
        {
        }

        public HistoricData(DateTime time, T value)
        {
            Time = time;
            Value = value;
        }
    }
}
