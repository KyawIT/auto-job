namespace Core.Exceptions
{
    // Someone else (e.g. the batch worker) saved the same row after it was loaded.
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(Exception innerException)
            : base("The data was changed by someone else. Reload and try again.", innerException) { }
    }
}
