const { safeStorage } = require('electron');
const fs = require('node:fs');
const path = require('node:path');

const KEY_NAMES = ['nimApiKey', 'adzunaAppId', 'adzunaAppKey'];

/**
 * API keys encrypted with the OS keychain (safeStorage) in userData/keys.json.
 * The backend only keeps keys in memory, so they are pushed to it on every start.
 */
class KeyStore {
  constructor(directory) {
    this.file = path.join(directory, 'keys.json');
  }

  /** Decrypted keys; missing or unreadable entries are skipped. */
  load() {
    if (!fs.existsSync(this.file) || !safeStorage.isEncryptionAvailable()) return {};
    const stored = JSON.parse(fs.readFileSync(this.file, 'utf8'));
    const keys = {};
    for (const name of KEY_NAMES) {
      if (!stored[name]) continue;
      try {
        keys[name] = safeStorage.decryptString(Buffer.from(stored[name], 'base64'));
      } catch {
        // Encrypted by another OS user or machine: treat as not set.
      }
    }
    return keys;
  }

  /** Applies a change: undefined = keep, '' = remove, anything else = set. Returns all keys. */
  update(change) {
    if (!safeStorage.isEncryptionAvailable()) {
      throw new Error('Secure storage is not available on this system.');
    }
    const keys = { ...this.load() };
    for (const name of KEY_NAMES) {
      const value = change[name];
      if (value === undefined || value === null) continue;
      if (typeof value !== 'string') throw new Error(`${name} must be a string.`);
      if (value === '') delete keys[name];
      else keys[name] = value;
    }

    const stored = {};
    for (const [name, value] of Object.entries(keys)) {
      stored[name] = safeStorage.encryptString(value).toString('base64');
    }
    fs.mkdirSync(path.dirname(this.file), { recursive: true });
    fs.writeFileSync(this.file, JSON.stringify(stored), { mode: 0o600 });
    return keys;
  }
}

module.exports = { KeyStore, KEY_NAMES };
