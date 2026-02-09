#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const { spawn } = require("child_process");

const repoRoot = path.resolve(__dirname, "..", "..");
const packageCacheDir = path.join(repoRoot, "Library", "PackageCache");
const packagePrefix = "com.gamelovers.mcp-unity@";
const serverRelativePath = path.join("Server~", "build", "index.js");

function findUnityMcpServer() {
  if (!fs.existsSync(packageCacheDir)) {
    return null;
  }

  const candidates = fs
    .readdirSync(packageCacheDir, { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && entry.name.startsWith(packagePrefix))
    .map((entry) => {
      const packageDir = path.join(packageCacheDir, entry.name);
      const serverPath = path.join(packageDir, serverRelativePath);
      if (!fs.existsSync(serverPath)) {
        return null;
      }

      const stat = fs.statSync(packageDir);
      return {
        packageDir,
        serverPath,
        mtimeMs: stat.mtimeMs,
      };
    })
    .filter(Boolean)
    .sort((a, b) => b.mtimeMs - a.mtimeMs);

  return candidates.length > 0 ? candidates[0] : null;
}

const resolved = findUnityMcpServer();

if (!resolved) {
  console.error(
    `[unity-mcp-launcher] Could not find Unity MCP package in ${packageCacheDir}. ` +
      `Open the Unity project first so PackageCache is populated.`
  );
  process.exit(1);
}

if (process.argv.includes("--resolve-only")) {
  process.stdout.write(`${resolved.serverPath}\n`);
  process.exit(0);
}

console.error(`[unity-mcp-launcher] Using ${resolved.serverPath}`);

const child = spawn(process.execPath, [resolved.serverPath], {
  cwd: repoRoot,
  env: process.env,
  stdio: "inherit",
});

child.on("error", (error) => {
  console.error(`[unity-mcp-launcher] Failed to start Unity MCP: ${error.message}`);
  process.exit(1);
});

child.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }

  process.exit(code ?? 1);
});
