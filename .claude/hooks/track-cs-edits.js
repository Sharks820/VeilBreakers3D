#!/usr/bin/env node
// VeilBreakers C# Edit Tracker v2
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
      const hooksDir = path.join(process.cwd(), '.claude', 'hooks');
      const markerFile = path.join(hooksDir, '.cs-pending-verification');

      // Ensure directory exists
      if (!fs.existsSync(hooksDir)) {
        fs.mkdirSync(hooksDir, { recursive: true });
      }

      const timestamp = new Date().toISOString();

      // Use relative path from project root to avoid basename collisions
      const projectRoot = process.cwd();
      let relativePath = filePath;
      try {
        relativePath = path.relative(projectRoot, filePath);
      } catch (e) {
        relativePath = path.basename(filePath);
      }
      // Normalize to forward slashes for consistency
      relativePath = relativePath.replace(/\\/g, '/');

      // Read existing entries and update timestamp or add new
      let existing = '';
      try { existing = fs.readFileSync(markerFile, 'utf8'); } catch (e) {}

      const lines = existing.trim().split('\n').filter(Boolean);
      const updated = lines.filter(line => !line.endsWith(` ${relativePath}`));
      updated.push(`${timestamp} ${relativePath}`);

      fs.writeFileSync(markerFile, updated.join('\n') + '\n');
    }
  } catch (e) {
    // Silent fail - never block PostToolUse
  }
});
