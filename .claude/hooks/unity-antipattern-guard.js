#!/usr/bin/env node
// VeilBreakers Unity Anti-Pattern Guard
// Event: PreToolUse (Write)
// Purpose: Catch common Unity performance killers BEFORE they're written
// Impact: HIGH - prevents frame-dropping code from entering the codebase

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const filePath = data.tool_input?.file_path || '';
    const content = data.tool_input?.content || '';

    // Only check C# files
    if (!filePath.endsWith('.cs')) return;

    const issues = [];

    // Find() or FindObjectOfType() in Update/FixedUpdate/LateUpdate
    if (/void\s+(Update|FixedUpdate|LateUpdate)\s*\([^)]*\)\s*\{[^}]*(?:Find\(|FindObjectOfType|FindGameObjectWithTag|FindWithTag)/s.test(content)) {
      issues.push('Find() in Update loop - cache the reference in Awake/Start');
    }

    // new allocations in Update (new List, new Dictionary, new string[], etc.)
    if (/void\s+(Update|FixedUpdate|LateUpdate)\s*\([^)]*\)\s*\{[^}]*new\s+(List|Dictionary|HashSet|Queue|Stack|string\[|int\[|float\[)/s.test(content)) {
      issues.push('Heap allocation in Update loop - pre-allocate in Awake/Start');
    }

    // String concatenation in Update
    if (/void\s+(Update|FixedUpdate|LateUpdate)\s*\([^)]*\)\s*\{[^}]*\+\s*"[^"]*"/s.test(content)) {
      issues.push('String concatenation in Update - use StringBuilder or cached strings');
    }

    // GetComponent in Update without caching
    if (/void\s+(Update|FixedUpdate|LateUpdate)\s*\([^)]*\)\s*\{[^}]*GetComponent\s*</s.test(content)) {
      issues.push('GetComponent in Update - cache in Awake/Start');
    }

    // Camera.main in Update (it calls FindGameObjectWithTag internally)
    if (/void\s+(Update|FixedUpdate|LateUpdate)\s*\([^)]*\)\s*\{[^}]*Camera\.main/s.test(content)) {
      issues.push('Camera.main in Update - cache the reference (it calls FindWithTag internally)');
    }

    if (issues.length > 0) {
      process.stdout.write(JSON.stringify({
        hookSpecificOutput: {
          hookEventName: 'PreToolUse',
          permissionDecision: 'deny',
          permissionDecisionReason: `UNITY ANTI-PATTERNS DETECTED:\n${issues.map(i => '- ' + i).join('\n')}\nFix these performance issues before writing the file.`,
          additionalContext: 'These patterns cause frame drops in Unity. Cache references in Awake/Start, pre-allocate collections, and avoid allocations in hot paths.'
        }
      }));
      return;
    }
  } catch (e) {}
});
