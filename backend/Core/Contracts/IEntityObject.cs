namespace Core.Contracts
{
    public interface IEntityObject
    {
        int Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
        Guid RowVersion { get; set; }
    }
}
