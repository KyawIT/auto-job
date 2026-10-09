namespace Core.Entities
{
    // Join table: which documents are attached to an application.
    public class ApplicationDocument
    {
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public int SortOrder { get; set; }
    }
}
