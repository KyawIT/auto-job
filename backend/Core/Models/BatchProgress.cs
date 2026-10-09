namespace Core.Models
{
    // Task counts of one generation batch, computed from its tasks (never stored).
    public record BatchProgress(int Total, int Queued, int Running, int Done, int Failed, int Cancelled);
}
