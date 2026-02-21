#!/usr/bin/env node
// VeilBreakers C# Compilation Verification Clear
// Event: PostToolUse (mcp__mcp-unity__recompile_scripts)
// Purpose: Clear C# pending verification marker after successful recompilation
// Impact: MEDIUM - completes the edit-compile-verify cycle

const fs = require('fs');
const path = require('path');

const markerFile = path.join(process.cwd(), '.claude', 'hooks', '.cs-pending-verification');

try {
  if (fs.existsSync(markerFile)) {
    fs.unlinkSync(markerFile);
  }
} catch (e) {
  // Silent fail
}
