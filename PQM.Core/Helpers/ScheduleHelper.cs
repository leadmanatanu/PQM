using System;

namespace PQM.Core.Helpers
{
    public static class ScheduleHelper
    {
        public static DateTime? ComputeNextRunAt(
            TimeSpan scheduledTime,
            DateTime nowIST)
        {
            DateTime candidate = nowIST.Date.Add(scheduledTime);

            if (candidate <= nowIST)
                candidate = candidate.AddDays(1);

            return candidate;
        }
    }
}