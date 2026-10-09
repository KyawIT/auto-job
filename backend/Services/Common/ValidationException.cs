namespace Services.Common
{
    // Input breaks a business rule. Errors are keyed by field name (mapped to HTTP 400).
    public class ValidationException : Exception
    {
        public ValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("One or more fields are invalid.")
        {
            Errors = errors;
        }

        public IReadOnlyDictionary<string, string[]> Errors { get; }
    }
}
