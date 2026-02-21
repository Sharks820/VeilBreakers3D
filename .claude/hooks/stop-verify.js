#!/usr/bin/env node
// VeilBreakers Stop Verification Hook v2
// Event: Stop
// Purpose: Quality gate - ensure C# compilation was verified before finishing
// Impact: HIGH for coding output quality

const fs = require('fs');
const path = require('path');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);

    // CRITICAL: Prevent infinite loop - always allow stop on re-entry
    if (data.stop_hook_active) {
      return;
    }

    // Check for unverified C# edits
    const markerFile = path.join(process.cwd(), '.claude', 'hooks', '.cs-pending-verification');

    if (fs.existsSync(markerFile)) {
      const pending = fs.readFileSync(markerFile, 'utf8').trim();
      if (pending) {
        // Check marker age - auto-expire after 30 minutes to prevent stale lockouts
        // CRLF-safe split (Windows writes \r\n)
        const lines = pending.split(/\r?\n/).filter(Boolean);
        const now = Date.now();
        const freshLines = lines.filter(line => {
          const clean = line.replace(/\r$/, '');
          const ts = clean.split(' ')[0];
          try {
            const age = now - new Date(ts).getTime();
            return age < 30 * 60 * 1000; // 30 minutes
          } catch (e) { return true; }
        });

        if (freshLines.length === 0) {
          // All entries expired - clean up and allow stop
          try { fs.unlinkSync(markerFile); } catch (e) {}
          return;
        }

        const files = freshLines
          .map(line => line.replace(/\r$/, '').split(' ').slice(1).join(' '))
          .filter(Boolean);

        process.stdout.write(JSON.stringify({
          decision: 'block',
          reason: `C# files were modified but Unity compilation was not verified. ` +
                  `Modified: ${files.join(', ')}. ` +
                  `Run recompile_scripts or manually verify compilation before finishing.`
        }));
        return;
      }
    }

    // No pending edits - allow stop
  } catch (e) {
    // On error, allow stop (don't trap user)
  }
});
