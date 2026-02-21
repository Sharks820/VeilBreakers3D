#!/usr/bin/env node
// VeilBreakers Smart Reasoning Injection
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

    // Silent on short messages, slash commands, git ops
    if (msg.length < 40 || msg.startsWith('/') || /^(commit|push|pull|save|merge|yes|no|ok|continue)\b/.test(msg)) return;

    // HIGH-RISK: Core game system keywords → full reasoning framework
    const highRisk = /brand|combat|damage|capture|corruption|synergy|path\s*(system|data)|save\s*(system|format|data)|type\s*chart|balance/i;
    if (highRisk.test(msg)) {
      process.stdout.write('HIGH-RISK SYSTEM DETECTED. Use sequential-thinking. Verify type matchups, save compatibility, and balance impact before implementing.');
      return;
    }

    // MULTI-SYSTEM: Mentions multiple areas → ripple effect warning
    const areas = ['ui', 'combat', 'monster', 'inventory', 'scene', 'audio', 'shader', 'network', 'database', 'animation'];
    const hits = areas.filter(a => msg.includes(a));
    if (hits.length >= 2) {
      process.stdout.write(`Multi-system change (${hits.join(', ')}). Check cross-system dependencies before implementing.`);
      return;
    }

    // Everything else: SILENT. No token waste.
  } catch (e) {}
});
