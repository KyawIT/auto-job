const { contextBridge, ipcRenderer } = require('electron');

// The only thing the Angular renderer gets from Electron: where the backend is and its token.
contextBridge.exposeInMainWorld('autojob', {
  getBackend: () => ipcRenderer.invoke('backend:info'),
});
