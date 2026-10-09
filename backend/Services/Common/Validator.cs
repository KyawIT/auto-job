using System.Net.Mail;

namespace Services.Common
{
    // Collects all field errors, then throws once: new Validator().Required(...).Email(...).ThrowIfInvalid();
    public sealed class Validator
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public Validator Required(string field, string? value) =>
            Check(!string.IsNullOrWhiteSpace(value), field, "Required.");

        public Validator Email(string field, string? value) =>
            Check(string.IsNullOrWhiteSpace(value) || MailAddress.TryCreate(value, out _), field, "Not a valid email address.");

        public Validator Check(bool isValid, string field, string message)
        {
            if (!isValid)
            {
                if (!_errors.TryGetValue(field, out var messages))
                    _errors[field] = messages = new List<string>();
                messages.Add(message);
            }
            return this;
        }

        public void ThrowIfInvalid()
        {
            if (_errors.Count > 0)
                throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
