using Core.Enums;

namespace Core.Entities
{
    // Timeline entry of an application.
    public class ApplicationEvent : EntityObject
    {
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public ApplicationEventType Type { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? Note { get; set; }
    }
}
