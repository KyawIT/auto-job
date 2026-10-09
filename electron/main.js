const { app, BrowserWindow, ipcMain, protocol, net, dialog, shell } = require('electron');
const { spawn } = require('node:child_process');
const crypto = require('node:crypto');
const path = require('node:path');
const fs = require('node:fs');
const { pathToFileURL } = require('node:url');

const isDev = !app.isPackaged;
const token = crypto.randomBytes(32).toString('hex');
let backend;
let backendUrl;
let quitting = false;

// Must run before app is ready. Gives Angular the origin "app://autojob" (allowed by backend CORS).
protocol.registerSchemesAsPrivileged([
  { scheme: 'app', privileges: { standard: true, secure: true, supportFetchAPI: true } },
]);

// Two instances would mean two backends on the same SQLite file.
if (!app.requestSingleInstanceLock()) app.quit();

function startBackend() {
  const env = {
    ...process.env,
    Sidecar__Token: token,
    Sidecar__Port: '0',
    Sidecar__ParentPid: String(process.pid),
  };

  if (isDev) {
    // Development: run from source, data goes to backend/api/.data.
    // cwd must be the project folder, otherwise appsettings*.json and .data resolve against electron/
    const project = path.join(__dirname, '../backend/api');
    backend = spawn('dotnet', ['run', '--project', project], { env, cwd: project });
  } else {
    const dir = path.join(process.resourcesPath, 'backend');
    env.ASPNETCORE_ENVIRONMENT = 'Production';
    env.Sidecar__DataDirectory = app.getPath('userData');
    const exe = process.platform === 'win32' ? 'api.exe' : 'api';
    // cwd matters: the backend loads appsettings.json from its working directory
    backend = spawn(path.join(dir, exe), [], { env, cwd: dir });
  }

  return new Promise((resolve, reject) => {
    let buffer = '';
    backend.stdout.on('data', (chunk) => {
      if (isDev) process.stdout.write(chunk);
      if (backendUrl) return;
      buffer += chunk;
      const match = buffer.match(/AUTOJOB_READY (\S+)/);
      if (match) {
        backendUrl = match[1];
        resolve();
      }
    });
    backend.stderr.on('data', (chunk) => process.stderr.write(chunk));
    backend.on('exit', (code) => {
      if (!backendUrl) reject(new Error(`Backend exited with code ${code}`));
    });
  });
}

function serveAngular() {
  const root = path.join(process.resourcesPath, 'renderer');
  protocol.handle('app', (request) => {
    const { pathname } = new URL(request.url);
    let file = path.join(root, decodeURIComponent(pathname));
    // Unknown paths (Angular routes) fall back to index.html; also blocks ../ escapes
    if (!file.startsWith(root) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
      file = path.join(root, 'index.html');
    }
    return net.fetch(pathToFileURL(file).toString());
  });
}

function createWindow() {
  const win = new BrowserWindow({
    width: 1280,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });
  const appOrigin = isDev ? 'http://localhost:4200' : 'app://autojob';

  // External links open in the user's browser, never inside the app window.
  win.webContents.setWindowOpenHandler(({ url }) => {
    if (url.startsWith('https://') || url.startsWith('http://')) shell.openExternal(url);
    return { action: 'deny' };
  });
  win.webContents.on('will-navigate', (event, url) => {
    if (!url.startsWith(appOrigin)) event.preventDefault();
  });

  win.loadURL(`${appOrigin}/`);
}

app.whenReady()
  .then(async () => {
    if (!isDev) serveAngular();
    await startBackend();
    ipcMain.handle('backend:info', () => ({ url: backendUrl, token }));
    createWindow();
  })
  .catch((err) => {
    dialog.showErrorBox('AutoJob', `Backend failed to start:\n${err.message}`);
    app.quit();
  });

app.on('window-all-closed', () => app.quit());

// Graceful stop: ask the backend to shut down, kill it only if it doesn't within 5s.
app.on('before-quit', (event) => {
  if (quitting || !backend || backend.exitCode !== null) return;
  event.preventDefault();
  quitting = true;
  const timer = setTimeout(() => backend.kill(), 5000);
  backend.once('exit', () => {
    clearTimeout(timer);
    app.quit();
  });
  fetch(`${backendUrl}/shutdown`, { method: 'POST', headers: { 'X-AutoJob-Token': token } })
    .catch(() => backend.kill());
});