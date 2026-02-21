#!/usr/bin/env node
/**
 * VeilBreakers Agent Bridge
 * Reliable non-interactive interface to Gemini, Codex, and Kimi CLIs.
 * Bypasses Windows PATH issues by using absolute paths.
 *
 * Usage:
 *   node .claude/tools/ask-agent.js <agent> "<prompt>"
 *   node .claude/tools/ask-agent.js gemini "Review this code for bugs"
 *   node .claude/tools/ask-agent.js codex "Explain this function"
 *   node .claude/tools/ask-agent.js kimi "Find performance issues"
 *   node .claude/tools/ask-agent.js all "Review this architecture"   <-- asks all 3
 *
 * Piping files:
 *   node .claude/tools/ask-agent.js gemini "Review this" --file path/to/file.cs
 *
 * Options:
 *   --file <path>    Append file contents to prompt
 *   --timeout <ms>   Override default timeout (default: 120000)
 */

const { spawn, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// ============================================================
// Agent Configuration - absolute paths, no PATH dependency
// ============================================================
const AGENTS = {
  gemini: {
    name: 'Gemini',
    // Node ESM entry point for gemini-cli
    command: 'node',
    args: (prompt) => [
      path.join('C:', 'nvm4w', 'nodejs', 'node_modules', '@google', 'gemini-cli', 'dist', 'index.js'),
      '--prompt', prompt
    ],
    timeout: 120000,
    env: {
      ...process.env,
      PATH: `C:\\nvm4w\\nodejs;${process.env.PATH || ''}`
    }
  },

  codex: {
    name: 'Codex',
    // Native binary
    command: path.join('C:', 'nvm4w', 'nodejs', 'node_modules', '@openai', 'codex', 'node_modules',
      '@openai', 'codex-win32-x64', 'vendor', 'x86_64-pc-windows-msvc', 'codex', 'codex.exe'),
    args: (prompt) => [
      'exec',
      '--sandbox', 'read-only',
      '--ephemeral',
      prompt
    ],
    timeout: 120000,
    env: {
      ...process.env,
      PATH: `C:\\nvm4w\\nodejs;${process.env.PATH || ''}`
    }
  },

  kimi: {
    name: 'Kimi',
    command: path.join('C:', 'Users', 'Conner', 'AppData', 'Local', 'Programs', 'Python', 'Python312', 'Scripts', 'kimi.exe'),
    args: (prompt) => [
      '--prompt', prompt,
      '--quiet'
    ],
    timeout: 120000,
    env: {
      ...process.env,
      PATH: `C:\\Users\\Conner\\AppData\\Local\\Programs\\Python\\Python312;C:\\Users\\Conner\\AppData\\Local\\Programs\\Python\\Python312\\Scripts;C:\\nvm4w\\nodejs;${process.env.PATH || ''}`,
      PYTHONIOENCODING: 'utf-8',
      PYTHONUTF8: '1'
    }
  }
};

// ============================================================
// Argument parsing
// ============================================================
const args = process.argv.slice(2);
if (args.length < 2) {
  console.error('Usage: node ask-agent.js <gemini|codex|kimi|all> "prompt" [--file path] [--timeout ms]');
  process.exit(1);
}

const agentName = args[0].toLowerCase();
let prompt = args[1];
let filePath = null;
let customTimeout = null;

for (let i = 2; i < args.length; i++) {
  if (args[i] === '--file' && args[i + 1]) {
    filePath = args[++i];
  } else if (args[i] === '--timeout' && args[i + 1]) {
    customTimeout = parseInt(args[++i], 10);
  }
}

// Append file contents to prompt if --file provided
if (filePath) {
  try {
    const absPath = path.isAbsolute(filePath) ? filePath : path.resolve(process.cwd(), filePath);
    const content = fs.readFileSync(absPath, 'utf8');
    const ext = path.extname(filePath);
    prompt += `\n\n--- File: ${path.basename(filePath)} ---\n\`\`\`${ext.slice(1)}\n${content}\n\`\`\``;
  } catch (e) {
    console.error(`Warning: Could not read file ${filePath}: ${e.message}`);
  }
}

// ============================================================
// Run agent
// ============================================================
function runAgent(agentKey) {
  return new Promise((resolve) => {
    const agent = AGENTS[agentKey];
    if (!agent) {
      resolve({ agent: agentKey, error: `Unknown agent: ${agentKey}` });
      return;
    }

    // Verify executable exists
    const executable = agent.command === 'node' ? 'node' : agent.command;
    if (executable !== 'node' && !fs.existsSync(executable)) {
      resolve({ agent: agent.name, error: `Executable not found: ${executable}` });
      return;
    }

    const timeout = customTimeout || agent.timeout;
    const agentArgs = agent.args(prompt);

    let stdout = '';
    let stderr = '';
    let timedOut = false;

    const proc = spawn(agent.command, agentArgs, {
      env: agent.env,
      cwd: process.cwd(),
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true,
      timeout: timeout
    });

    // Close stdin immediately (non-interactive)
    proc.stdin.end();

    proc.stdout.on('data', (data) => { stdout += data.toString(); });
    proc.stderr.on('data', (data) => { stderr += data.toString(); });

    const timer = setTimeout(() => {
      timedOut = true;
      proc.kill('SIGTERM');
    }, timeout);

    proc.on('close', (code) => {
      clearTimeout(timer);
      if (timedOut) {
        resolve({ agent: agent.name, error: `Timed out after ${timeout / 1000}s`, partial: stdout.trim() });
      } else if (code !== 0 && !stdout.trim()) {
        resolve({ agent: agent.name, error: `Exit code ${code}`, stderr: stderr.trim().slice(0, 500) });
      } else {
        resolve({ agent: agent.name, response: stdout.trim() });
      }
    });

    proc.on('error', (err) => {
      clearTimeout(timer);
      resolve({ agent: agent.name, error: `Spawn error: ${err.message}` });
    });
  });
}

async function main() {
  const startTime = Date.now();

  if (agentName === 'all') {
    // Run all three in parallel
    console.log('=== Querying all agents in parallel ===\n');
    const results = await Promise.all(
      Object.keys(AGENTS).map(key => runAgent(key))
    );

    for (const result of results) {
      console.log(`\n${'='.repeat(60)}`);
      console.log(`  ${result.agent}`);
      console.log(`${'='.repeat(60)}`);
      if (result.error) {
        console.log(`ERROR: ${result.error}`);
        if (result.stderr) console.log(`STDERR: ${result.stderr}`);
        if (result.partial) console.log(`PARTIAL OUTPUT:\n${result.partial}`);
      } else {
        console.log(result.response);
      }
    }
  } else {
    // Single agent
    const result = await runAgent(agentName);
    if (result.error) {
      console.error(`[${result.agent}] ERROR: ${result.error}`);
      if (result.stderr) console.error(`STDERR: ${result.stderr}`);
      if (result.partial) console.log(`PARTIAL:\n${result.partial}`);
      process.exit(1);
    } else {
      console.log(result.response);
    }
  }

  const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);
  console.error(`\n[Completed in ${elapsed}s]`);
}

main().catch(err => {
  console.error('Fatal error:', err.message);
  process.exit(1);
});
