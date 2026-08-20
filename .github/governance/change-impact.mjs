/* global console, process */
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { appendFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const prefixes = {
  web: ['apps/web/', 'packages/client/', 'packages/host/'],
  gui: ['apps/web/', 'packages/client/', 'packages/host/'],
  snapshot: ['apps/web/', 'packages/client/', 'packages/host/', 'packages/core/'],
  e2e: [
    'packages/core/',
    'packages/llm/',
    'packages/context/',
    'packages/session/',
    'packages/fs/',
    'packages/shell/',
    'packages/subprocess/',
    'packages/terminal/',
    'packages/code-runtime/',
    'packages/lsp/',
    'packages/sandbox/',
    'packages/tool/',
    'packages/subagent/',
    'packages/workflow/',
    'packages/jobs/',
    'packages/sdk/',
    'packages/acp/',
    'native/',
    'python/',
  ],
};

const fullProductRiskRoots = [
  'package.json',
  'pnpm-lock.yaml',
  'pnpm-workspace.yaml',
  'tsconfig',
  'vendor/',
  'patches/',
];

const governanceRoots = [
  '.github/',
  'AGENTS.md',
  'agent.md',
];

const laneTaskStateMetadata = new Set([
  '.github/task-control/work.json',
  '.github/task-control/governance.json',
]);

function matches(file, values) {
  return values.some((value) => (value.endsWith('/') ? file.startsWith(value) : file === value || file.startsWith(value)));
}

export function classifyImpact(files) {
  const unique = [...new Set(files.filter(Boolean))].sort();
  const riskFiles = unique.filter((file) => !laneTaskStateMetadata.has(file));
  const result = {
    files: unique,
    governance: riskFiles.some((file) => matches(file, governanceRoots)),
    full: riskFiles.some((file) => matches(file, fullProductRiskRoots)),
    web: riskFiles.some((file) => matches(file, prefixes.web)),
    gui: riskFiles.some((file) => matches(file, prefixes.gui)),
    snapshot: riskFiles.some((file) => matches(file, prefixes.snapshot)),
    e2e: riskFiles.some((file) => matches(file, prefixes.e2e)),
  };
  if (result.full) {
    result.e2e = true;
    result.web = true;
    result.gui = true;
    result.snapshot = true;
  }
  return result;
}

function changedFiles(base, head) {
  if (!base || /^0+$/u.test(base)) {
    return execFileSync('git', ['show', '--pretty=', '--name-only', head], { encoding: 'utf8' })
      .split(/\r?\n/u)
      .filter(Boolean);
  }
  return execFileSync('git', ['diff', '--name-only', base, head], { encoding: 'utf8' })
    .split(/\r?\n/u)
    .filter(Boolean);
}

async function main() {
  const baseIndex = process.argv.indexOf('--base');
  const headIndex = process.argv.indexOf('--head');
  const base = baseIndex >= 0 ? process.argv[baseIndex + 1] : null;
  const head = headIndex >= 0 ? process.argv[headIndex + 1] : 'HEAD';
  const result = classifyImpact(changedFiles(base, head));
  console.log(JSON.stringify(result, null, 2));
  if (process.env.GITHUB_OUTPUT) {
    for (const key of ['governance', 'full', 'web', 'gui', 'snapshot', 'e2e']) {
      await appendFile(process.env.GITHUB_OUTPUT, `${key}=${result[key]}\n`);
    }
  }
}

function selfTest() {
  assert.deepEqual(classifyImpact(['README.md']), {
    files: ['README.md'], governance: false, full: false, web: false, gui: false, snapshot: false, e2e: false,
  });

  const taskStateOnly = classifyImpact(['.github/task-control/work.json']);
  assert.equal(taskStateOnly.governance, false);
  assert.equal(taskStateOnly.full, false);
  assert.equal(taskStateOnly.e2e, false);

  const governance = classifyImpact(['.github/workflows/a.yml']);
  assert.equal(governance.governance, true);
  assert.equal(governance.full, false);
  assert.equal(governance.e2e, false);
  assert.equal(governance.web, false);
  assert.equal(governance.gui, false);
  assert.equal(governance.snapshot, false);

  const agentRules = classifyImpact(['AGENTS.md']);
  assert.equal(agentRules.governance, true);
  assert.equal(agentRules.full, false);

  const web = classifyImpact(['apps/web/src/a.tsx', '.github/task-control/work.json']);
  assert.equal(web.web, true);
  assert.equal(web.gui, true);
  assert.equal(web.snapshot, true);
  assert.equal(web.full, false);

  const shell = classifyImpact(['packages/shell/x.ts']);
  assert.equal(shell.e2e, true);
  assert.equal(shell.full, false);

  const dependency = classifyImpact(['pnpm-lock.yaml']);
  assert.equal(dependency.full, true);
  assert.equal(dependency.e2e, true);
  assert.equal(dependency.web, true);
  assert.equal(dependency.gui, true);
  assert.equal(dependency.snapshot, true);

  console.log('Change impact self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
