#!/usr/bin/env node
// VeilBreakers C# Compilation Verification Clear v2
// Event: PostToolUse (mcp__mcp-unity__recompile_scripts)
// Purpose: Clear C# pending verification marker after successful recompilation
// Impact: MEDIUM - completes the edit-compile-verify cycle
//
// v2 fixes: Drain stdin to prevent EPIPE, use rmSync for atomicity

const fs = require('fs');
const path = require('path');

// Drain stdin to prevent EPIPE (PostToolUse hooks receive data even if unused)
process.stdin.setEncoding('utf8');
process.stdin.on('data', () => {});
process.stdin.on('end', () => {
  const markerFile = path.join(process.cwd(), '.claude', 'hooks', '.cs-pending-verification');
  try {
    fs.rmSync(markerFile, { force: true });
  } catch (e) {
    // Silent fail - rmSync with force handles missing files
  }
});
