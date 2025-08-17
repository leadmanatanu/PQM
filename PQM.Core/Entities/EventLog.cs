using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.Entities
{
    public class EventLog: IBaseEvent
    {
        [Key]
        public long Id { get; set; }
        public string EventType { get; set; }
        public int DeviceId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime Start_Time { get; set; }
        public DateTime End_Time { get; set; }
        public string Phase { get; set; }
        public double Duration { get; set; }
        public double? Min_Voltage { get; set; }
        public double? Max_Voltage { get; set; }
        public double? UMAX { get; set; }
        public double? USS { get; set; }
        [NotMapped]
        public string? DeviceName { get; set; }
    }

    public interface IBaseEvent
    {
        DateTime Start_Time { get; }
        DateTime End_Time { get; }
        string Phase { get; }
        double Duration { get; }
    }

    public class DipEvent : IBaseEvent
    {
        public DateTime Start_Time { get; set; }
        public DateTime End_Time { get; set; }
        public string Phase { get; set; }
        public double Duration { get; set; }
        public double Min_Voltage { get; set; }
    }

    public class InterruptEvent : IBaseEvent
    {
        public DateTime Start_Time { get; set; }
        public DateTime End_Time { get; set; }
        public string Phase { get; set; }
        public double Duration { get; set; }
    }

    public class RVCEvent : IBaseEvent
    {
        public DateTime Start_Time { get; set; }
        public DateTime End_Time { get; set; }
        public string Phase { get; set; }
        public double Duration { get; set; }
        public double UMAX { get; set; }
        public double USS { get; set; }
    }

    public class SwellEvent : IBaseEvent
    {
        public DateTime Start_Time { get; set; }
        public DateTime End_Time { get; set; }
        public string Phase { get; set; }
        public double Duration { get; set; }
        public double Max_Voltage { get; set; }
    }

    public class EventLogSearchResult
    {
        public int TotalCount { get; set; }
        public List<EventLog> EventLogSearch { get; set; }
    }
}
