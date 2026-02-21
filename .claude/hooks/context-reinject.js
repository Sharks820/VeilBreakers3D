#!/usr/bin/env node
// VeilBreakers Context Re-Injection Hook
// Event: SessionStart (compact|resume|clear)
// Purpose: Re-inject critical game system knowledge after context loss
// Impact: CRITICAL for reasoning continuity

const fs = require('fs');
const path = require('path');

// Clear stale state from previous session
const pendingFile = path.join(process.cwd(), '.claude', 'hooks', '.cs-pending-verification');
try { if (fs.existsSync(pendingFile)) fs.unlinkSync(pendingFile); } catch (e) {}

// Read pre-compact context bridge if available
let bridgeContext = '';
const bridgeFile = path.join(process.cwd(), '.claude', 'hooks', '.pre-compact-context.txt');
try {
  if (fs.existsSync(bridgeFile)) {
    bridgeContext = fs.readFileSync(bridgeFile, 'utf8').trim();
    fs.unlinkSync(bridgeFile); // One-time read
  }
} catch (e) {}

// Core game systems reference card
const context = `
=== VEILBREAKERS CONTEXT (auto-injected after context loss) ===

COMBAT: 10 Brands - IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID
  Each: 2x vs 2 brands, 0.5x vs 2 brands, 1x vs 6 brands

PATHS: IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED

CORRUPTION (0-100%):
  0-10% ASCENDED +25% | 11-25% Purified +10% | 26-50% Unstable 0%
  51-75% Corrupted -10% | 76-100% Abyssal -20%

SYNERGY: FULL(3/3) +8% | PARTIAL(2/3) +5% | NEUTRAL 0% | ANTI 0%

CODE: namespace VeilBreakers.[Category] | _private | kConstant | PascalProperty | OnEvent
TECH: Unity + UI Toolkit (NOT IMGUI) | ScriptableObjects for data | Event-driven
AVOID: Find() in Update, allocations in hot paths, disabled components

KEY PATHS: Assets/Scripts/[Combat|Core|Systems|UI|Data]/
MEMORY: Read VEILBREAKERS.md for full project state
${bridgeContext ? '\nPRE-COMPACT NOTES:\n' + bridgeContext : ''}
===`;

process.stdout.write(context);
