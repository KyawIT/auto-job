namespace Services.Common
{
    // The requested entity doesn't exist (mapped to HTTP 404).
    public class NotFoundException : Exception
    {
        public NotFoundException(string entity, object? id = null)
            : base(id == null ? $"{entity} not found." : $"{entity} {id} not found.") { }
    }
}
