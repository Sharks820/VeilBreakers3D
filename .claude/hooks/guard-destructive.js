#!/usr/bin/env node
// VeilBreakers Destructive Command Guard v2
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
      // Git - force push variants (order matters: check --force-with-lease before --force)
      { pattern: /git\s+push\s+.*--force-with-lease/i,      msg: 'Force push with lease - verify no one else pushed' },
      { pattern: /git\s+push\s+.*--force(?:\s|$)/i,         msg: 'Force push - could overwrite remote history' },
      { pattern: /git\s+push\s+.*\s-f(?:\s|$)/i,            msg: 'Force push (-f) - could overwrite remote history' },

      // Git - history rewriting
      { pattern: /git\s+reset\s+--hard/i,                   msg: 'Hard reset - discards all uncommitted changes' },
      { pattern: /git\s+rebase\s+(?!--abort|--continue)/i,  msg: 'Rebase - rewrites commit history' },
      { pattern: /git\s+filter-branch/i,                     msg: 'Filter-branch - rewrites entire history' },

      // Git - discard work
      { pattern: /git\s+checkout\s+\.\s*$/i,                msg: 'Discard ALL uncommitted changes' },
      { pattern: /git\s+checkout\s+--\s+\./i,               msg: 'Discard ALL uncommitted changes' },
      { pattern: /git\s+restore\s+\.\s*$/i,                 msg: 'Restore ALL files - discards changes' },
      { pattern: /git\s+restore\s+--staged\s+\./i,          msg: 'Unstage ALL files' },

      // Git - cleanup
      { pattern: /git\s+clean\s+-[a-z]*[fdx]/i,             msg: 'Git clean - removes untracked files permanently' },
      { pattern: /git\s+stash\s+drop/i,                     msg: 'Drop stash - unrecoverable' },
      { pattern: /git\s+stash\s+clear/i,                    msg: 'Clear all stashes - unrecoverable' },

      // Git - branch deletion
      { pattern: /git\s+branch\s+-D\s+(master|main|develop)/i, msg: 'Delete protected branch' },
      { pattern: /git\s+push\s+.*--delete\s+(master|main|develop)/i, msg: 'Delete remote protected branch' },

      // Filesystem - recursive/forced deletion
      { pattern: /rm\s+-[a-z]*r[a-z]*\s+[\/\\.]/i,          msg: 'Recursive delete at root/project level' },
      { pattern: /rm\s+-[a-z]*r[a-z]*\s+.*Assets[\/\\]/i,   msg: 'Delete in Assets/ - Unity project files at risk' },
      { pattern: /rm\s+-[a-z]*r[a-z]*\s+\*/i,               msg: 'Wildcard recursive delete' },
      { pattern: /rm\s+-[a-z]*r[a-z]*\s+\.\s*$/i,           msg: 'Recursive delete current directory' },

      // PowerShell equivalents (Windows)
      { pattern: /Remove-Item\s+.*-Recurse/i,               msg: 'PowerShell recursive delete' },
      { pattern: /del\s+\/[sq]/i,                           msg: 'Windows del with quiet/subdirectory flag' },
      { pattern: /rmdir\s+\/s/i,                            msg: 'Windows rmdir recursive' },

      // Git submodule
      { pattern: /git\s+submodule\s+deinit/i,               msg: 'Remove submodule - potential data loss' },
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
