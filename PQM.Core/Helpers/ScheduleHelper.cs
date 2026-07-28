using System;

namespace PQM.Core.Helpers
{
    public static class ScheduleHelper
    {
        public static DateTime? ComputeNextRunAtUtc(TimeSpan scheduledTime, string? timeZoneId, DateTime nowUtc)
        {
            TimeZoneInfo tz;
            try
            {
                tz = string.IsNullOrWhiteSpace(timeZoneId)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                tz = TimeZoneInfo.Utc;
            }

            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            DateTime candidateLocal = nowLocal.Date.Add(scheduledTime);

            if (candidateLocal <= nowLocal)
            {
                candidateLocal = candidateLocal.AddDays(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(candidateLocal, tz);
        }
    }
}
