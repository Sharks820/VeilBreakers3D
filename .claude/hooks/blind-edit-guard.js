#!/usr/bin/env node
// VeilBreakers Blind Edit Guard v1
// Event: PreToolUse (Edit)
// Purpose: Warn when editing a file that hasn't been Read in this session
// Prevents the #1 cause of regressions: editing without reading context
//
// Tracks which files have been Read via a marker file.
// If an Edit targets a file not recently Read, issues advisory.

const fs = require('fs');
const path = require('path');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const tool = data.tool_name;
    const filePath = data.tool_input?.file_path || '';

    if (!filePath) return;

    const stateDir = path.join(process.cwd(), '.claude', 'hooks');
    const readTracker = path.join(stateDir, '.files-read-this-session');

    // Normalize path for comparison
    const normalPath = filePath.replace(/\\/g, '/').toLowerCase();

    // On Read: track the file
    if (tool === 'Read') {
      try {
        if (!fs.existsSync(stateDir)) fs.mkdirSync(stateDir, { recursive: true });
        let existing = '';
        try { existing = fs.readFileSync(readTracker, 'utf8'); } catch(e) {}
        const lines = new Set(existing.split('\n').filter(Boolean));
        lines.add(normalPath);
        fs.writeFileSync(readTracker, [...lines].join('\n'));
      } catch(e) {}
      return;
    }

    // On Edit: check if file was Read first
    if (tool === 'Edit') {
      // Only check .cs, .py, .js, .ts files (code files where blind edits hurt most)
      if (!/\.(cs|py|js|ts|jsx|tsx)$/.test(filePath)) return;

      let readFiles = new Set();
      try {
        const content = fs.readFileSync(readTracker, 'utf8');
        content.split('\n').filter(Boolean).forEach(f => readFiles.add(f));
      } catch(e) {}

      if (!readFiles.has(normalPath)) {
        // File hasn't been Read — warn but don't block
        process.stdout.write(
          `Advisory: Editing ${path.basename(filePath)} without reading it first. ` +
          `Read the file to avoid regressions.`
        );
      }
    }
  } catch(e) {}
});
