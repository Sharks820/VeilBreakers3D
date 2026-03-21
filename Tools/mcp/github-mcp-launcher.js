#!/usr/bin/env node
// GitHub MCP Launcher - dynamically gets token from gh CLI auth
// Avoids hardcoding tokens or relying on env vars
// Requires: gh CLI authenticated (gh auth login)

const { execSync } = require('child_process');
const path = require('path');

try {
  const token = execSync('gh auth token', { encoding: 'utf8', timeout: 5000 }).trim();
  if (!token) {
    process.stderr.write('Error: gh auth token returned empty. Run: gh auth login\n');
    process.exit(1);
  }
  process.env.GITHUB_PERSONAL_ACCESS_TOKEN = token;
} catch (e) {
  process.stderr.write('Error: Failed to get GitHub token from gh CLI. Run: gh auth login\n');
  process.stderr.write(e.message + '\n');
  process.exit(1);
}

// Load the GitHub MCP server from global install
const globalRoot = execSync('npm root -g', { encoding: 'utf8', timeout: 5000 }).trim();
const serverPath = path.join(globalRoot, '@modelcontextprotocol', 'server-github', 'dist', 'index.js');
require(serverPath);
