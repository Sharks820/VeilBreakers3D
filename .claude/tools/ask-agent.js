#!/usr/bin/env node
/**
 * VeilBreakers Agent Bridge v2
 * Reliable non-interactive interface to Gemini, Codex, and MiniMax.
 * Gemini/Codex: spawned via absolute CLI paths (bypasses Windows PATH issues)
 * MiniMax M2.5: direct API call (OpenAI-compatible endpoint, no CLI install needed)
 *
 * Usage:
 *   node .claude/tools/ask-agent.js <agent> "<prompt>"
 *   node .claude/tools/ask-agent.js gemini "Review this UI layout"
 *   node .claude/tools/ask-agent.js codex "Debug this function"
 *   node .claude/tools/ask-agent.js minimax "Review architecture and SOLID compliance"
 *   node .claude/tools/ask-agent.js all "Review this code"   <-- asks all 3
 *
 * Piping files:
 *   node .claude/tools/ask-agent.js minimax "Review this" --file path/to/file.cs
 *
 * Options:
 *   --file <path>    Append file contents to prompt
 *   --timeout <ms>   Override default timeout (default: 120000)
 *
 * Team Roles:
 *   Codex  = #1 Code Partner (debugger, optimizer, senior dev)
 *   Gemini = Front-end Designer & Researcher (UI/UX, web search)
 *   MiniMax = Senior Code Reviewer (architecture, security, SOLID, quality gate)
 *
 * Environment:
 *   MINIMAX_API_KEY  - Required for MiniMax M2.5 API access
 *                      Get one at https://platform.minimax.io
 */

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const https = require('https');

// ============================================================
// Agent Configuration
// ============================================================

// CLI-based agents (spawned as child processes)
const CLI_AGENTS = {
  gemini: {
    name: 'Gemini',
    role: 'Front-end Designer & Researcher',
    command: 'node',
    args: (prompt) => [
      path.join('C:', 'nvm4w', 'nodejs', 'node_modules', '@google', 'gemini-cli', 'dist', 'index.js'),
      '-m', 'gemini-3-pro-preview',
      '-o', 'text',
      '-p', prompt
    ],
    timeout: 120000,
    env: {
      ...process.env,
      PATH: `C:\\nvm4w\\nodejs;${process.env.PATH || ''}`
    }
  },

  codex: {
    name: 'Codex',
    role: '#1 Code Partner (Debugger & Optimizer)',
    command: path.join('C:', 'nvm4w', 'nodejs', 'node_modules', '@openai', 'codex', 'node_modules',
      '@openai', 'codex-win32-x64', 'vendor', 'x86_64-pc-windows-msvc', 'codex', 'codex.exe'),
    args: (prompt) => [
      'exec',
      '--sandbox', 'read-only',
      '--ephemeral',
      '--json',
      prompt
    ],
    parseJsonl: true,  // Flag: output is JSONL, extract agent_message items
    timeout: 120000,
    env: {
      ...process.env,
      PATH: `C:\\nvm4w\\nodejs;${process.env.PATH || ''}`
    }
  }
};

// API-based agents (direct HTTP calls — more reliable, no CLI install needed)
const API_AGENTS = {
  minimax: {
    name: 'MiniMax M2.5',
    role: 'Senior Code Reviewer (Architecture, Security, SOLID)',
    model: 'MiniMax-M2.5',
    baseUrl: 'api.minimax.io',
    apiPath: '/v1/chat/completions',
    apiKeyEnv: 'MINIMAX_API_KEY',
    timeout: 120000,
    systemPrompt: `You are a senior code reviewer for a Unity 3D game project (VeilBreakers).
Your role: architecture validation, security audit, SOLID compliance, and quality gate.

Review priorities:
1. ARCHITECTURE: Event lifecycle, proper teardown, coupling issues, god-classes
2. SECURITY: Injection vectors, unsafe deserialization, hardcoded secrets
3. SOLID: Single responsibility, open-closed, dependency inversion violations
4. PERFORMANCE: Update() allocations, uncached Find/GetComponent, hot-path issues
5. UNITY-SPECIFIC: MonoBehaviour lifecycle, SerializeField usage, null checks

Be direct and specific. Flag issues by severity (CRITICAL / HIGH / MEDIUM / LOW).
Include line references when possible. Suggest concrete fixes, not vague advice.`
  }
};

// Combined lookup
const ALL_AGENT_KEYS = [...Object.keys(CLI_AGENTS), ...Object.keys(API_AGENTS)];

// ============================================================
// Argument parsing
// ============================================================
const args = process.argv.slice(2);
if (args.length < 2) {
  console.error(`Usage: node ask-agent.js <${ALL_AGENT_KEYS.join('|')}|all> "prompt" [--file path] [--timeout ms]`);
  console.error('\nTeam:');
  for (const key of Object.keys(CLI_AGENTS)) {
    console.error(`  ${key.padEnd(10)} ${CLI_AGENTS[key].role}`);
  }
  for (const key of Object.keys(API_AGENTS)) {
    console.error(`  ${key.padEnd(10)} ${API_AGENTS[key].role}`);
  }
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
// CLI-based agent runner (Gemini, Codex)
// ============================================================
function runCliAgent(agentKey) {
  return new Promise((resolve) => {
    const agent = CLI_AGENTS[agentKey];
    if (!agent) {
      resolve({ agent: agentKey, error: `Unknown CLI agent: ${agentKey}` });
      return;
    }

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
        let response = stdout.trim();

        // Parse JSONL output (Codex --json mode): extract agent_message text
        if (agent.parseJsonl && response) {
          try {
            const messages = [];
            let totalIn = 0, totalOut = 0;
            for (const line of response.split('\n')) {
              if (!line.trim()) continue;
              const obj = JSON.parse(line);
              if (obj.type === 'item.completed' && obj.item?.type === 'agent_message') {
                messages.push(obj.item.text);
              }
              if (obj.type === 'turn.completed' && obj.usage) {
                totalIn += obj.usage.input_tokens || 0;
                totalOut += obj.usage.output_tokens || 0;
              }
            }
            response = messages.join('\n\n');
            if (totalIn || totalOut) {
              response += `\n\n[Tokens: ${totalIn} in / ${totalOut} out]`;
            }
          } catch (e) {
            // If JSONL parsing fails, fall back to raw output
          }
        }

        resolve({ agent: agent.name, response });
      }
    });

    proc.on('error', (err) => {
      clearTimeout(timer);
      resolve({ agent: agent.name, error: `Spawn error: ${err.message}` });
    });
  });
}

// ============================================================
// API-based agent runner (MiniMax M2.5)
// ============================================================
function runApiAgent(agentKey) {
  return new Promise((resolve) => {
    const agent = API_AGENTS[agentKey];
    if (!agent) {
      resolve({ agent: agentKey, error: `Unknown API agent: ${agentKey}` });
      return;
    }

    let apiKey = process.env[agent.apiKeyEnv];

    // Windows fallback: load from user environment variables via registry
    if (!apiKey && process.platform === 'win32') {
      try {
        const { execSync } = require('child_process');
        apiKey = execSync(
          `powershell -Command "[System.Environment]::GetEnvironmentVariable('${agent.apiKeyEnv}', 'User')"`,
          { encoding: 'utf8', timeout: 5000 }
        ).trim();
        if (apiKey) {
          process.env[agent.apiKeyEnv] = apiKey;  // Cache for subsequent calls
        }
      } catch (e) { /* ignore fallback failure */ }
    }

    if (!apiKey) {
      resolve({
        agent: agent.name,
        error: `Missing ${agent.apiKeyEnv} environment variable. Get a key at https://platform.minimax.io`
      });
      return;
    }

    const timeout = customTimeout || agent.timeout;

    const payload = JSON.stringify({
      model: agent.model,
      messages: [
        { role: 'system', content: agent.systemPrompt },
        { role: 'user', content: prompt }
      ],
      max_tokens: 4096,
      temperature: 0.3
    });

    const options = {
      hostname: agent.baseUrl,
      port: 443,
      path: agent.apiPath,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`,
        'Content-Length': Buffer.byteLength(payload)
      }
    };

    let timedOut = false;
    const timer = setTimeout(() => {
      timedOut = true;
      req.destroy();
    }, timeout);

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => { body += chunk; });
      res.on('end', () => {
        clearTimeout(timer);
        if (timedOut) return;

        if (res.statusCode !== 200) {
          let errMsg = `HTTP ${res.statusCode}`;
          try {
            const errBody = JSON.parse(body);
            errMsg += `: ${errBody.error?.message || body.slice(0, 200)}`;
          } catch (e) {
            errMsg += `: ${body.slice(0, 200)}`;
          }
          resolve({ agent: agent.name, error: errMsg });
          return;
        }

        try {
          const data = JSON.parse(body);
          const content = data.choices?.[0]?.message?.content || '';
          const usage = data.usage;
          let response = content.trim();

          // Append token usage for cost tracking
          if (usage) {
            response += `\n\n[Tokens: ${usage.prompt_tokens} in / ${usage.completion_tokens} out` +
              ` | Est. cost: $${((usage.prompt_tokens * 0.15 + usage.completion_tokens * 1.20) / 1000000).toFixed(4)}]`;
          }

          resolve({ agent: agent.name, response });
        } catch (e) {
          resolve({ agent: agent.name, error: `Parse error: ${e.message}`, partial: body.slice(0, 500) });
        }
      });
    });

    req.on('error', (err) => {
      clearTimeout(timer);
      if (timedOut) {
        resolve({ agent: agent.name, error: `Timed out after ${timeout / 1000}s` });
      } else {
        resolve({ agent: agent.name, error: `Request error: ${err.message}` });
      }
    });

    req.write(payload);
    req.end();
  });
}

// ============================================================
// Unified runner
// ============================================================
function runAgent(agentKey) {
  if (CLI_AGENTS[agentKey]) return runCliAgent(agentKey);
  if (API_AGENTS[agentKey]) return runApiAgent(agentKey);
  return Promise.resolve({ agent: agentKey, error: `Unknown agent: ${agentKey}` });
}

// ============================================================
// Main
// ============================================================
async function main() {
  const startTime = Date.now();

  if (agentName === 'all') {
    console.log('=== Querying all agents in parallel ===\n');
    const results = await Promise.all(
      ALL_AGENT_KEYS.map(key => runAgent(key))
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
