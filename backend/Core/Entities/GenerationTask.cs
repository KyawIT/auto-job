using Core.Enums;

namespace Core.Entities
{
    // Generation for one application inside a batch; picked up by the background worker.
    public class GenerationTask : EntityObject
    {
        public int GenerationBatchId { get; set; }
        public GenerationBatch GenerationBatch { get; set; } = null!;

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public GenerationTaskStatus Status { get; set; } = GenerationTaskStatus.Queued;
        public int Attempts { get; set; }
        public string? Error { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
