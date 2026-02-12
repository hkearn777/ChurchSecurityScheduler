namespace ChurchSecurityScheduler.Models
{
    public class SecurityShift
    {
        public string? Id { get; set; }
        public string VolunteerName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = "Church Security";
        public string? Notes { get; set; }
    }

    public class SecurityPosition
    {
        public string Position { get; set; } = string.Empty;
        public string TimeSlot8_30 { get; set; } = string.Empty;
        public string TimeSlot9_45 { get; set; } = string.Empty;
        public string TimeSlot11_00 { get; set; } = string.Empty;
    }

    public class ScheduleSheet
    {
        public string Date { get; set; } = string.Empty;
        public List<SecurityPosition> Positions { get; set; } = new();
    }
}