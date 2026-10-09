using System.Collections.Concurrent;
using Services.Ports;

namespace Infrastructure.Secrets
{
    // Singleton. Keys live only in process memory and vanish when the backend stops.
    public class InMemorySecretStore : ISecretStore
    {
        private readonly ConcurrentDictionary<SecretName, string> _values = new();

        public string? Get(SecretName name) => _values.TryGetValue(name, out var value) ? value : null;

        public void Set(SecretName name, string? value)
        {
            if (string.IsNullOrEmpty(value)) _values.TryRemove(name, out _);
            else _values[name] = value;
        }
    }
}
