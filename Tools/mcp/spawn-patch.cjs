// CJS preload script — patches child_process.spawn for Windows .cmd support
// Usage: node --require ./spawn-patch.cjs <esm-module.js>
//
// On Windows, Node's spawn() cannot execute .cmd files without shell:true.
// This patch must load BEFORE ESM imports resolve (--require runs first).
// Both mcp-gemini-cli and mcp-codex-cli use ESM import { spawn } which
// binds to the CJS module — this patch reaches them through Node's
// shared built-in module singleton.

if (process.platform === 'win32') {
  const cp = require('child_process');
  const originalSpawn = cp.spawn;
  cp.spawn = function patchedSpawn(command, args, options) {
    return originalSpawn.call(cp, command, args, { ...options, shell: true });
  };
}
