#!/usr/bin/env node
// VeilBreakers Unity Anti-Pattern Guard v3
// Event: PreToolUse (Edit|Write)
// Purpose: Catch common Unity performance killers BEFORE they're written
// Impact: HIGH - prevents frame-dropping code from entering the codebase
//
// Strategy: Two-phase detection
//   Phase 1: Check if file has Update/FixedUpdate/LateUpdate methods
//   Phase 2: Check if file has hot-path anti-patterns
//   If BOTH are true, flag it (avoids regex fragility with nested braces)
//
// v3 fixes: Reads file from disk for Edit ops, improved Update regex,
//           fixed += false positive, CRLF-safe

const fs = require('fs');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const filePath = data.tool_input?.file_path || '';

    // Only check C# files
    if (!filePath.endsWith('.cs')) return;

    // Get content: Write tool has content, Edit tool has old_string/new_string
    // For Edit ops, read the file from disk to get full content + new changes
    let content = data.tool_input?.content || '';
    if (!content) {
      // Edit tool - read existing file and apply the new_string conceptually
      try {
        const existing = fs.readFileSync(filePath, 'utf8');
        const newStr = data.tool_input?.new_string || '';
        content = existing + '\n' + newStr; // Check both existing and incoming code
      } catch (e) {
        return; // Can't read file, skip check
      }
    }

    const issues = [];

    // Phase 1: Does file contain Update-family methods?
    // Matches: void Update(), private void Update(), public override void FixedUpdate(), etc.
    const hasUpdateLoop = /\b(?:(?:public|protected|private|internal|virtual|override|sealed|new)\s+)*void\s+(?:Update|FixedUpdate|LateUpdate)\s*\(/.test(content);
    if (!hasUpdateLoop) return; // No hot path, no problem

    // Phase 2: Check for anti-patterns that are dangerous IN FILES with Update loops
    // (Safer than trying to regex inside method bodies with nested braces)

    // Find() family - should always be cached
    if (/\b(?:Find|FindObjectOfType|FindObjectsOfType|FindObjectsByType|FindGameObjectWithTag|FindWithTag|FindAnyObjectByType)\s*[<(]/.test(content)) {
      issues.push('Find/FindObjectOfType in file with Update loop - cache references in Awake/Start');
    }

    // GetComponent family - should always be cached
    if (/\b(?:GetComponent|GetComponentInChildren|GetComponentInParent|GetComponents|GetComponentsInChildren)\s*[<(]/.test(content)) {
      // Flag all GetComponent in files with Update loops - regex can't determine method scope
      issues.push('GetComponent in file with Update loop - ensure references are cached in Awake/Start');
    }

    // Camera.main (calls FindWithTag internally)
    if (/\bCamera\.main\b/.test(content)) {
      issues.push('Camera.main in file with Update loop - cache the reference (calls FindWithTag internally)');
    }

    // Heap allocations - new collections (NOT structs like Vector3, Quaternion, Color)
    const heapAllocs = content.match(/\bnew\s+(List|Dictionary|HashSet|Queue|Stack|ArrayList|LinkedList|SortedList|StringBuilder|string\s*\[|int\s*\[|float\s*\[|byte\s*\[|object\s*\[)/g);
    if (heapAllocs) {
      issues.push(`Heap allocation (${[...new Set(heapAllocs.map(m => m.replace(/^new\s+/, '')))].join(', ')}) in file with Update loop - pre-allocate in Awake/Start`);
    }

    // LINQ in hot-path files (allocates enumerators/delegates)
    if (/\.\s*(?:Where|Select|OrderBy|GroupBy|ToList|ToArray|ToDictionary|Any|All|First|Last|Count|Sum|Average|Min|Max|Distinct|Union|Intersect|Except|Concat|Zip)\s*\(/.test(content)) {
      issues.push('LINQ query in file with Update loop - LINQ allocates. Use loops or cached results');
    }

    // String concatenation via + operator (exclude += assignments)
    if (/(?<![+=])\s*\+\s*"[^"]*"/.test(content) || /\$"[^"\\]*(?:\\.[^"\\]*)*\{/.test(content)) {
      // Only flag if there's evidence of frequent string ops (interpolation or concatenation)
      const stringOps = (content.match(/\$"/g) || []).length + (content.match(/"\s*\+\s*"/g) || []).length;
      if (stringOps >= 2) {
        issues.push('Multiple string operations in file with Update loop - use StringBuilder or cached strings');
      }
    }

    // Resources.Load in hot-path files
    if (/\bResources\.Load\s*[<(]/.test(content)) {
      issues.push('Resources.Load in file with Update loop - cache loaded resources');
    }

    if (issues.length > 0) {
      process.stdout.write(JSON.stringify({
        hookSpecificOutput: {
          hookEventName: 'PreToolUse',
          permissionDecision: 'ask',
          permissionDecisionReason: `UNITY PERFORMANCE WARNING (${issues.length} issues):\n${issues.map(i => '- ' + i).join('\n')}\nVerify these are cached/pre-allocated, not called in Update loops.`,
          additionalContext: 'This file contains Update loops AND patterns that cause frame drops if used in hot paths. The guard cannot determine exact method placement - review and confirm these are properly cached.'
        }
      }));
      return;
    }
  } catch (e) {}
});
