#!/usr/bin/env node
// VeilBreakers Reasoning Framework Injection
// Event: UserPromptSubmit
// Purpose: Inject brief thinking framework for complex tasks
// Impact: HIGH for deep thinking and systematic reasoning

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input);
    const message = (data.user_prompt || '').trim();

    // Skip short messages (confirmations, "yes", "ok", "continue")
    if (message.length < 30) return;

    // Skip slash commands (already have their own frameworks)
    if (message.startsWith('/')) return;

    // Skip pure git/commit operations
    if (/^(commit|push|pull|save|merge)\b/i.test(message)) return;

    // Inject reasoning framework for substantive messages
    process.stdout.write(
      'THINK BEFORE ACTING: ' +
      '(1) What systems does this touch? ' +
      '(2) Is this high-risk (combat/save/brand data)? If yes, use sequential-thinking. ' +
      '(3) Check for ripple effects across dependent systems. ' +
      '(4) Verify, then implement.'
    );
  } catch (e) {
    // Silent fail
  }
});
