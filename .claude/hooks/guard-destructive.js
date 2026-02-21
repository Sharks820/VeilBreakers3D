#!/usr/bin/env node
// VeilBreakers Destructive Command Guard
// Event: PreToolUse (Bash)
// Purpose: Block dangerous git and filesystem operations
// Impact: HIGH for safety and code preservation

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const command = (data.tool_input?.command || '').trim();

    const destructive = [
      { pattern: /git\s+push\s+.*--force/i,           msg: 'Force push - could overwrite remote history' },
      { pattern: /git\s+push\s+-f\b/i,                msg: 'Force push (-f) - could overwrite remote history' },
      { pattern: /git\s+reset\s+--hard/i,              msg: 'Hard reset - discards all uncommitted changes' },
      { pattern: /rm\s+-r[f]*\s+[\/\\.]/i,             msg: 'Recursive delete at root/project level' },
      { pattern: /rm\s+-f?r?f?\s+.*Assets\//i,         msg: 'Delete in Assets/ - Unity project files at risk' },
      { pattern: /git\s+branch\s+-D\s+(master|main|develop)/i, msg: 'Delete protected branch' },
      { pattern: /git\s+checkout\s+\.\s*$/i,           msg: 'Discard ALL uncommitted changes' },
      { pattern: /git\s+restore\s+\.\s*$/i,            msg: 'Restore ALL files - discards changes' },
      { pattern: /git\s+clean\s+-[fd]/i,               msg: 'Git clean - removes untracked files permanently' },
      { pattern: /git\s+stash\s+drop\s+--all/i,        msg: 'Drop all stashes - unrecoverable' },
      { pattern: /git\s+rebase\s+.*--force/i,          msg: 'Force rebase - rewrites history' },
    ];

    for (const { pattern, msg } of destructive) {
      if (pattern.test(command)) {
        process.stdout.write(JSON.stringify({
          hookSpecificOutput: {
            hookEventName: 'PreToolUse',
            permissionDecision: 'deny',
            permissionDecisionReason: `BLOCKED: ${msg}. Ask the user for explicit confirmation before running destructive commands.`
          }
        }));
        return;
      }
    }
  } catch (e) {
    // Parse error - don't block
  }
});
