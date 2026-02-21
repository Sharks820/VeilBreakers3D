#!/usr/bin/env node
// VeilBreakers Pre-Compaction Context Bridge
// Event: PreCompact (auto|manual)
// Purpose: Save key context before compaction so it survives the compression
// Impact: MEDIUM-HIGH for reasoning continuity across compactions

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const bridgeFile = path.join(process.cwd(), '.claude', 'hooks', '.pre-compact-context.txt');

try {
  const lines = [];
  lines.push(`Compacted: ${new Date().toISOString()}`);

  // Capture current git state
  try {
    const branch = execSync('git branch --show-current', {
      encoding: 'utf8', timeout: 3000, windowsHide: true
    }).trim();
    lines.push(`Branch: ${branch}`);
  } catch (e) {}

  // Capture modified files summary
  try {
    const status = execSync('git diff --name-only', {
      encoding: 'utf8', timeout: 3000, windowsHide: true
    }).trim();
    if (status) {
      const files = status.split('\n').slice(0, 10);
      lines.push(`Modified files: ${files.join(', ')}${status.split('\n').length > 10 ? '...' : ''}`);
    }
  } catch (e) {}

  // Capture staged files
  try {
    const staged = execSync('git diff --cached --name-only', {
      encoding: 'utf8', timeout: 3000, windowsHide: true
    }).trim();
    if (staged) {
      lines.push(`Staged: ${staged.split('\n').slice(0, 5).join(', ')}`);
    }
  } catch (e) {}

  // Write bridge file
  fs.writeFileSync(bridgeFile, lines.join('\n'));
} catch (e) {
  // Silent fail - don't interfere with compaction
}
