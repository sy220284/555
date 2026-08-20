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

const fullRiskRoots = [
  'package.json',
  'pnpm-lock.yaml',
  'pnpm-workspace.yaml',
  'tsconfig',
  '.github/',
  'vendor/',
  'patches/',
];

function matches(file, values) {
  return values.some((value) => (value.endsWith('/') ? file.startsWith(value) : file === value || file.startsWith(value)));
}

export function classifyImpact(files) {
  const unique = [...new Set(files.filter(Boolean))].sort();
  const result = {
    files: unique,
    governance: unique.some((file) => file.startsWith('.github/')),
    full: unique.some((file) => matches(file, fullRiskRoots)),
    web: unique.some((file) => matches(file, prefixes.web)),
    gui: unique.some((file) => matches(file, prefixes.gui)),
    snapshot: unique.some((file) => matches(file, prefixes.snapshot)),
    e2e: unique.some((file) => matches(file, prefixes.e2e)),
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
  const web = classifyImpact(['apps/web/src/a.tsx']);
  assert.equal(web.web, true);
  assert.equal(web.gui, true);
  assert.equal(web.snapshot, true);
  const shell = classifyImpact(['packages/shell/x.ts']);
  assert.equal(shell.e2e, true);
  const governance = classifyImpact(['.github/workflows/a.yml']);
  assert.equal(governance.full, true);
  assert.equal(governance.governance, true);
  assert.equal(governance.e2e, true);
  assert.equal(governance.web, true);
  assert.equal(governance.gui, true);
  assert.equal(governance.snapshot, true);
  console.log('Change impact self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
