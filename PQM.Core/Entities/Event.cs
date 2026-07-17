using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("Event")]
    public class Event
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public string? Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public interface IBaseEvent
    {
        DateTime? Start_Time { get; }
        DateTime? End_Time { get; }
        string? Phase { get; }
        double? Duration { get; }
    }

    public class DipEvent : IBaseEvent
    {
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
        public string? Phase { get; set; }
        public double? Duration { get; set; }
        public double? Min_Voltage { get; set; }
    }

    public class InterruptEvent : IBaseEvent
    {
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
        public string? Phase { get; set; }
        public double? Duration { get; set; }
    }

    public class RVCEvent : IBaseEvent
    {
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
        public string? Phase { get; set; }
        public double? Duration { get; set; }
        public double? UMAX { get; set; }
        public double? USS { get; set; }
    }

    public class SwellEvent : IBaseEvent
    {
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
        public string? Phase { get; set; }
        public double? Duration { get; set; }
        public double? Max_Voltage { get; set; }
    }

    public class FlickerEvent : IBaseEvent
    {
        public DateTime? Start_Time { get; set; }
        public DateTime? End_Time { get; set; }
        public string? Phase { get; set; }
        public double? Duration { get; set; }
        public DateTime? Date { get; set; }
        public string? A { get; set; }
        public string? B { get; set; }
        public string? C { get; set; }
    }
}
