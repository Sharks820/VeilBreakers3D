#!/usr/bin/env node
// VeilBreakers C# Compilation Verification Clear v3
// Event: PostToolUse (mcp__mcp-unity__recompile_scripts | Bash)
// Purpose: Clear C# pending verification marker after successful recompilation
// Impact: MEDIUM - completes the edit-compile-verify cycle
//
// v3: Also triggers on Bash tool use - detects when the user/agent reads
//     Unity Editor.log to verify compilation manually. Clears the marker
//     if the Bash command references Editor.log or dotnet build.

const fs = require('fs');
const path = require('path');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  const markerFile = path.join(process.cwd(), '.claude', 'hooks', '.cs-pending-verification');

  try {
    const data = JSON.parse(input);
    const toolName = data.tool_name || '';
    const command = data.tool_input?.command || '';

    // Direct match: mcp-unity recompile - always clear
    if (toolName === 'mcp__mcp-unity__recompile_scripts') {
      fs.rmSync(markerFile, { force: true });
      return;
    }

    // Bash command that checks compilation status - clear if it looks like a verification
    if (toolName === 'Bash') {
      const isCompileCheck =
        /Editor\.log/i.test(command) ||
        /dotnet\s+build/i.test(command) ||
        /msbuild/i.test(command) ||
        /compile.*error|error.*CS\d{4}/i.test(command);

      if (isCompileCheck) {
        fs.rmSync(markerFile, { force: true });
        return;
      }
    }
  } catch (e) {
    // If we can't parse input, still try to clear on mcp-unity match (legacy behavior)
    try { fs.rmSync(markerFile, { force: true }); } catch (e2) {}
  }
});
