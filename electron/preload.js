const { contextBridge, ipcRenderer } = require('electron');

// Everything the Angular renderer gets from Electron (typed in frontend/src/app/core/desktop.ts).
contextBridge.exposeInMainWorld('autojob', {
  // Where the backend is and its per-launch token.
  getBackend: () => ipcRenderer.invoke('backend:info'),
  // { nimApiKey?, adzunaAppId?, adzunaAppKey? }: undefined = unchanged, '' = remove.
  setApiKeys: (keys) => ipcRenderer.invoke('keys:set', keys),
  // Opens a file from the data folder (email drafts) with its default app.
  openFile: (fullPath) => ipcRenderer.invoke('file:open', fullPath),
  // Save dialog; resolves false when cancelled.
  saveFile: (suggestedName, bytes) => ipcRenderer.invoke('file:save', suggestedName, bytes),
});
