#!/usr/bin/env node
// VeilBreakers Smart Reasoning Injection v2
// Event: UserPromptSubmit
// Purpose: Inject thinking framework ONLY for high-risk or multi-system tasks
// Token strategy: Silent on 90%+ of messages. Only fires when it truly matters.

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const msg = (data.user_prompt || '').trim().toLowerCase();

    // Silent on slash commands, git ops, short confirmations
    if (msg.startsWith('/') || /^\s*(commit|push|pull|save|merge|yes|no|ok|continue|done|stop|looks good)\b/.test(msg)) return;

    // HIGH-RISK: Core game system keywords -> full reasoning framework
    // Check BEFORE short-message filter (short prompts like "fix damage" are high-risk)
    const highRisk = /\b(brand|combat|damage|capture|corruption|synergy|path\s*(?:system|data)|save\s*(?:system|format|data)|type\s*chart|balance|formula|multiplier)\b/i;
    if (highRisk.test(msg)) {
      process.stdout.write('HIGH-RISK SYSTEM DETECTED. Use sequential-thinking. Verify type matchups, save compatibility, and balance impact before implementing.');
      return;
    }

    // Silent on very short messages that aren't high-risk
    if (msg.length < 30) return;

    // MULTI-SYSTEM: Mentions multiple areas -> ripple effect warning
    // Use word boundaries to avoid false positives (e.g., "build" matching "ui")
    const areas = [
      [/\bui\b|\bui\s*toolkit\b|\buss\b|\bvisual\s*element/i, 'ui'],
      [/\bcombat\b|\bbattle\b|\bfight/i, 'combat'],
      [/\bmonster\b|\bcreature\b|\bbrand\b/i, 'monster'],
      [/\binventory\b|\bitem\b|\bloot/i, 'inventory'],
      [/\bscene\b|\blevel\b|\bworld/i, 'scene'],
      [/\baudio\b|\bsound\b|\bmusic/i, 'audio'],
      [/\bshader\b|\bmaterial\b|\brender/i, 'shader'],
      [/\bnetwork\b|\bmultiplayer\b|\bsync/i, 'network'],
      [/\bdatabase\b|\bsave\b|\bpersist/i, 'data'],
      [/\banimation\b|\banimator\b|\btween/i, 'animation'],
      [/\bphysics\b|\bcollision\b|\brigidbody/i, 'physics'],
      [/\bai\b|\bbehavior\b|\bpathfind/i, 'ai'],
    ];
    const hits = areas.filter(([regex]) => regex.test(msg)).map(([, name]) => name);
    if (hits.length >= 2) {
      process.stdout.write(`Multi-system change (${hits.join(', ')}). Check cross-system dependencies before implementing.`);
      return;
    }

    // Everything else: SILENT. No token waste.
  } catch (e) {}
});
