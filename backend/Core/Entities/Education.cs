namespace Core.Entities
{
    public class Education : EntityObject
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;

        public string Institution { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string? FieldOfStudy { get; set; }
        public string? Grade { get; set; }   // text: "1,7", "gut", "3.8 GPA"
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
