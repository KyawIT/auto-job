namespace Core.Exceptions
{
    // The row is still referenced (e.g. a document attached to an application) and can't be deleted.
    public class ReferenceConstraintException : Exception
    {
        public ReferenceConstraintException(Exception innerException)
            : base("The entry is still in use and can't be deleted.", innerException) { }
    }
}
