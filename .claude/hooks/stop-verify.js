#!/usr/bin/env node
// VeilBreakers Stop Verification Hook v3
// Event: Stop
// Purpose: Quality gate - ensure C# compilation was verified before finishing
// Impact: HIGH for coding output quality
//
// v3: Replaced mcp-unity recompile_scripts dependency with Editor.log check.
//     Now reads Unity Editor.log to detect compile errors directly.
//     Falls back to a warning (non-blocking) if Editor.log is unavailable.

const fs = require('fs');
const path = require('path');
const os = require('os');

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

        // Try to check Unity Editor.log for compile errors instead of requiring mcp-unity
        const editorLogPath = path.join(
          process.env.LOCALAPPDATA || path.join(os.homedir(), 'AppData', 'Local'),
          'Unity', 'Editor', 'Editor.log'
        );

        let hasCompileErrors = false;
        let logCheckSucceeded = false;

        try {
          if (fs.existsSync(editorLogPath)) {
            const stat = fs.statSync(editorLogPath);
            // Read last 64KB of the log to check for recent compile errors
            const readSize = Math.min(stat.size, 65536);
            const fd = fs.openSync(editorLogPath, 'r');
            const buffer = Buffer.alloc(readSize);
            fs.readSync(fd, buffer, 0, readSize, Math.max(0, stat.size - readSize));
            fs.closeSync(fd);
            const tail = buffer.toString('utf8');

            logCheckSucceeded = true;

            // Look for compile error indicators in the log tail
            // Unity logs "Compilation failed" or "error CS" for C# compile errors
            const errorPatterns = [
              /error CS\d{4}:/,                    // C# compiler errors
              /Compilation failed/i,                // Unity compilation failure
              /Scripts have compiler errors/i,      // Unity editor message
            ];

            hasCompileErrors = errorPatterns.some(p => p.test(tail));

            // Also check if the log shows a successful compilation AFTER any errors
            // by looking for the recompilation-complete message after the last error
            if (hasCompileErrors) {
              const lastError = Math.max(
                tail.lastIndexOf('error CS'),
                tail.lastIndexOf('Compilation failed'),
                tail.lastIndexOf('Scripts have compiler errors')
              );
              const lastSuccess = Math.max(
                tail.lastIndexOf('successfully reloaded assembly'),
                tail.lastIndexOf('LogAssemblyErrors (0ms)'),
                tail.lastIndexOf('Refresh completed'),
                tail.lastIndexOf('compilationFinished')
              );

              // If the last successful compile is AFTER the last error, compilation recovered
              if (lastSuccess > lastError && lastSuccess !== -1) {
                hasCompileErrors = false;
              }
            }
          }
        } catch (e) {
          // Can't read Editor.log - fall through to advisory warning
        }

        if (logCheckSucceeded && !hasCompileErrors) {
          // Editor.log shows no compile errors - clear the marker and allow stop
          try { fs.unlinkSync(markerFile); } catch (e) {}
          return;
        }

        if (logCheckSucceeded && hasCompileErrors) {
          // Editor.log confirms compile errors exist
          process.stdout.write(JSON.stringify({
            decision: 'block',
            reason: `C# files were modified and Unity Editor.log shows compile errors. ` +
                    `Modified: ${files.join(', ')}. ` +
                    `Fix the compile errors before finishing.`
          }));
          return;
        }

        // Editor.log unavailable (Unity not open, etc.) - issue advisory warning, don't block
        // This prevents the hook from trapping the user when Unity isn't running
        process.stdout.write(JSON.stringify({
          decision: 'block',
          reason: `C# files were modified but compilation status could not be verified ` +
                  `(Unity Editor.log not found - is the Unity Editor open?). ` +
                  `Modified: ${files.join(', ')}. ` +
                  `Open Unity to verify compilation, or confirm you want to stop anyway.`
        }));
        return;
      }
    }

    // No pending edits - allow stop
  } catch (e) {
    // On error, allow stop (don't trap user)
  }
});
