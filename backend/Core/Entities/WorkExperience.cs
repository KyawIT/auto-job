namespace Core.Entities
{
    public class WorkExperience : EntityObject
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;

        public string Company { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }   // null = current job
        public string? Description { get; set; }
    }
}
