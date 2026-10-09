using Core.Enums;

namespace Core.Entities
{
    // One "generate for these jobs" run. Progress is counted from its tasks.
    public class GenerationBatch : EntityObject
    {
        public string Language { get; set; } = string.Empty;
        public BatchStatus Status { get; set; } = BatchStatus.Running;
        public DateTime? FinishedAt { get; set; }

        public ICollection<GenerationTask> Tasks { get; set; } = new List<GenerationTask>();
    }
}
