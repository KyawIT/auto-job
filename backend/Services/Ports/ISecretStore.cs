namespace Services.Ports
{
    public enum SecretName
    {
        NimApiKey,
        AdzunaAppId,
        AdzunaAppKey
    }

    // API keys, held in memory only. Electron keeps them encrypted (safeStorage) and pushes them on start.
    public interface ISecretStore
    {
        string? Get(SecretName name);

        // Null or empty removes the secret.
        void Set(SecretName name, string? value);
    }
}
