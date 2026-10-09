using Services.Ports;

namespace Services.Tests.Support
{
    public class FakeSecretStore : ISecretStore
    {
        private readonly Dictionary<SecretName, string> _values = new();

        public string? Get(SecretName name) => _values.GetValueOrDefault(name);

        public void Set(SecretName name, string? value)
        {
            if (string.IsNullOrEmpty(value)) _values.Remove(name);
            else _values[name] = value;
        }
    }
}
