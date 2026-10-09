namespace Core.Exceptions
{
    // The row would duplicate an existing one (e.g. the same job from the same source).
    public class UniqueConstraintException : Exception
    {
        public UniqueConstraintException(Exception innerException)
            : base("An entry with the same unique values already exists.", innerException) { }
    }
}
