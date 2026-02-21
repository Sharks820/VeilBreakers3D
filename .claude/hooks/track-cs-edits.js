#!/usr/bin/env node
// VeilBreakers C# Edit Tracker
// Event: PostToolUse (Edit|Write)
// Purpose: Track when C# files are modified so Stop hook can verify compilation
// Impact: MEDIUM - enables compilation quality gate

const fs = require('fs');
const path = require('path');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const filePath = data.tool_input?.file_path || '';

    // Only track C# file edits
    if (filePath.endsWith('.cs')) {
      const markerFile = path.join(process.cwd(), '.claude', 'hooks', '.cs-pending-verification');
      const timestamp = new Date().toISOString();
      const basename = path.basename(filePath);

      // Append to marker (track which files were edited)
      let existing = '';
      try { existing = fs.readFileSync(markerFile, 'utf8'); } catch (e) {}

      if (!existing.includes(basename)) {
        fs.writeFileSync(markerFile, existing + `${timestamp} ${basename}\n`);
      }
    }
  } catch (e) {
    // Silent fail - never block PostToolUse
  }
});
