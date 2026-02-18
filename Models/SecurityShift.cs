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

    public class ScheduleSheet
    {
        public string Date { get; set; } = string.Empty;
        public List<SecurityPosition> Positions { get; set; } = new();
    }
}