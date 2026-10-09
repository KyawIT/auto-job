using Core.Enums;
using Core.Models;

namespace Services.Generation
{
    // Language null = each application keeps its own (new ones get posting language or default).
    public record StartBatchRequest(IReadOnlyList<int> JobPostingIds, string? Language = null);

    public record BatchDto(
        int Id,
        BatchStatus Status,
        DateTime CreatedAt,
        DateTime? FinishedAt,
        BatchProgress Progress,
        IReadOnlyList<BatchTaskDto> Tasks);

    public record BatchTaskDto(int Id, int ApplicationId, string JobTitle, string CompanyName, GenerationTaskStatus Status, int Attempts, string? Error);
}
