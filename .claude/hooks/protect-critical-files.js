#!/usr/bin/env node
// VeilBreakers Critical File Protection
// Event: PreToolUse (Edit|Write)
// Purpose: Warn before modifying core game system files
// Impact: HIGH for preventing accidental game-breaking changes

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const filePath = (data.tool_input?.file_path || '').replace(/\\/g, '/');

    // Core game system patterns - changes here affect game balance/integrity
    const critical = [
      { pattern: /BrandData|BrandConfig|BrandTable/i,      system: 'Brand Combat System (10-brand type chart)' },
      { pattern: /PathData|PathConfig|PathSystem/i,         system: 'Path System (4 paths)' },
      { pattern: /CorruptionData|CorruptionConfig/i,        system: 'Corruption System (0-100% tiers)' },
      { pattern: /SynergyData|SynergyConfig|SynergyTable/i, system: 'Synergy System (tier bonuses)' },
      { pattern: /SaveManager|SaveData|SaveSystem/i,        system: 'Save System (data persistence)' },
      { pattern: /TypeChart|TypeEffectiveness/i,            system: 'Type Effectiveness Chart' },
      { pattern: /CombatCalculat|DamageFormula/i,           system: 'Combat Math (damage formulas)' },
      { pattern: /CaptureRate|CaptureFormula/i,             system: 'Capture System (catch rates)' },
    ];

    for (const { pattern, system } of critical) {
      if (pattern.test(filePath)) {
        process.stdout.write(JSON.stringify({
          hookSpecificOutput: {
            hookEventName: 'PreToolUse',
            permissionDecision: 'ask',
            permissionDecisionReason: `CAUTION: Editing ${system} file. Core game balance at stake.`,
            additionalContext: `This file is part of the ${system}. Changes affect core game mechanics. Verify intent with user. Consider: Will this break existing saves? Does this change game balance? Are all type matchups still correct?`
          }
        }));
        return;
      }
    }
  } catch (e) {
    // Parse error - don't block
  }
});
