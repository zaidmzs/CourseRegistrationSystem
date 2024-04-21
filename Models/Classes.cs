namespace LMS.Models
{
    public class Classes
    {
        public int ClassID { get; set; }
        public string? GradeLevel { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? MaxClassSize { get; set; }

    }
}
