/* global console, process */
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, listAllPages, validatePolicy } from './policy.mjs';
import { validateLaneReadyForMerge, validateTaskControl } from './task-control.mjs';

export function validatePullRequestShape({ head, base, sameRepository = true, lanes = ['work', 'governance'] }) {
  const errors = [];
  if (!lanes.includes(head)) errors.push(`PR head must be one of ${lanes.join(', ')}, found ${head || '<missing>'}`);
  if (base !== 'main') errors.push(`PR base must be main, found ${base || '<missing>'}`);
  if (!sameRepository) errors.push('PR head must belong to this repository');
  return errors;
}

export function validateLanePaths({ head, files }) {
  const errors = [];
  if (head === 'work') {
    const governancePaths = files.filter((file) =>
      file === 'AGENTS.md' ||
      file === 'agent.md' ||
      (file.startsWith('.github/') && file !== '.github/task-control/work.json'));
    if (governancePaths.length > 0) {
      errors.push(`work PR cannot modify repository governance automation or governance task state: ${governancePaths.join(', ')}`);
    }
  }
  return errors;
}

async function readRequiredFile(candidateDir, relative, markers, errors) {
  let content;
  try {
    content = await readFile(path.join(candidateDir, relative), 'utf8');
  } catch {
    errors.push(`Missing required governance file: ${relative}`);
    return null;
  }
  for (const marker of markers) {
    if (!content.includes(marker)) errors.push(`${relative} is missing required invariant: ${marker}`);
  }
  return content;
}

export async function validateCandidateAutomation(candidateDir) {
  if (!candidateDir) return [];
  const errors = [];
  const requirements = {
    'AGENTS.md': ['明确开发任务的默认授权', 'repository-state.mjs assert-delivered', 'DELIVERED'],
    'agent.md': ['repository-state = DELIVERED', 'delivery-ready'],
    '.github/governance/task-control.mjs': ['validateLaneReadyForMerge', 'Task control self-test passed.'],
    '.github/workflows/pr-policy.yml': ['pull_request_target:', 'statuses: write', 'path: trusted', 'persist-credentials: false'],
    '.github/workflows/controlled-merge.yml': ['workflow_run:', 'group: controlled-main-write', 'contents: write', 'pull-requests: write', 'persist-credentials: false'],
    '.github/workflows/main-verification.yml': ['push:', 'branches: [main]', 'main-verification.mjs', 'integration-synchronization.mjs', 'branch-hygiene.mjs', 'publish-delivery-status'],
    '.github/workflows/repository-gates.yml': ['name: repository-gates', 'repository-gates / merge-gate', 'windows-latest', 'macos-14', 'linux-landlock-native'],
    '.github/workflows/branch-hygiene.yml': ['branch-hygiene.mjs --repair', 'persist-credentials: false'],
  };
  for (const [relative, markers] of Object.entries(requirements)) {
    const content = await readRequiredFile(candidateDir, relative, markers, errors);
    if (content && relative.startsWith('.github/workflows/') && relative !== '.github/workflows/repository-gates.yml') {
      for (const match of content.matchAll(/uses:\s*[^\s@]+@([^\s#]+)/gu)) {
        if (!/^[0-9a-f]{40}$/iu.test(match[1])) errors.push(`${relative} contains an unpinned action reference: ${match[0]}`);
      }
    }
  }
  try {
    const candidatePolicy = JSON.parse(await readFile(path.join(candidateDir, '.github/governance/repository-policy.json'), 'utf8'));
    validatePolicy(candidatePolicy);
  } catch (error) {
    errors.push(`Candidate repository policy is invalid: ${error instanceof Error ? error.message : String(error)}`);
  }
  errors.push(...(await validateTaskControl(candidateDir)));
  return errors;
}

async function eventPayload() {
  if (!process.env.GITHUB_EVENT_PATH) return {};
  return JSON.parse(await readFile(process.env.GITHUB_EVENT_PATH, 'utf8'));
}

async function changedFiles(owner, repo, pullNumber) {
  return listAllPages(
    (page) => `/repos/${owner}/${repo}/pulls/${pullNumber}/files?per_page=100&page=${page}`,
  ).then((items) => items.map((item) => item.filename));
}

async function openLanePulls(owner, repo, lane) {
  return githubApi(`/repos/${owner}/${repo}/pulls?state=open&base=main&head=${owner}:${lane}&per_page=100`);
}

async function validate() {
  const policy = await loadPolicy();
  const event = await eventPayload();
  const pull = event.pull_request;
  if (!pull) throw new Error('PR Policy requires pull_request_target context');
  const { owner, repo } = githubRepository();
  const head = pull.head?.ref;
  const base = pull.base?.ref;
  const sameRepository = pull.head?.repo?.full_name === process.env.GITHUB_REPOSITORY;
  const errors = validatePullRequestShape({
    head,
    base,
    sameRepository,
    lanes: policy.integrationBranches,
  });

  if (policy.integrationBranches.includes(head)) {
    const pulls = await openLanePulls(owner, repo, head);
    const competing = pulls.filter((item) => item.number !== pull.number);
    if (competing.length >= policy.maxOpenPullRequestsPerLane) {
      errors.push(`Another ${head} -> main PR is already open`);
    }
  }

  const files = await changedFiles(owner, repo, pull.number);
  errors.push(...validateLanePaths({ head, files }));
  errors.push(...(await validateCandidateAutomation(process.env.CANDIDATE_DIR)));
  if (process.env.CANDIDATE_DIR && policy.integrationBranches.includes(head)) {
    errors.push(...(await validateLaneReadyForMerge(process.env.CANDIDATE_DIR, head)));
  }
  if (errors.length > 0) throw new Error(errors.join('\n'));
  console.log(`PR policy passed: ${head} -> ${base}, ${files.length} changed files, task state merge-ready.`);
}

async function publishStatus() {
  const state = process.env.POLICY_RESULT === 'success' ? 'success' : 'failure';
  const sha = process.env.CANDIDATE_SHA;
  if (!/^[0-9a-f]{40}$/iu.test(sha ?? '')) throw new Error('CANDIDATE_SHA is required');
  const { owner, repo } = githubRepository();
  await githubApi(`/repos/${owner}/${repo}/statuses/${sha}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      state,
      context: 'pr-policy',
      description: state === 'success' ? 'Trusted main policy accepted candidate and task state' : 'Trusted main policy rejected candidate',
      target_url: `${process.env.GITHUB_SERVER_URL}/${process.env.GITHUB_REPOSITORY}/actions/runs/${process.env.GITHUB_RUN_ID}`,
    }),
  });
  console.log(`Published pr-policy=${state} for ${sha}.`);
}

async function selfTest() {
  assert.deepEqual(validatePullRequestShape({ head: 'work', base: 'main' }), []);
  assert.deepEqual(validatePullRequestShape({ head: 'governance', base: 'main' }), []);
  assert.ok(validatePullRequestShape({ head: 'fix/a', base: 'main' }).length > 0);
  assert.ok(validatePullRequestShape({ head: 'work', base: 'release' }).length > 0);
  assert.ok(validatePullRequestShape({ head: 'work', base: 'main', sameRepository: false }).length > 0);
  assert.deepEqual(validateLanePaths({ head: 'governance', files: ['.github/workflows/x.yml'] }), []);
  assert.equal(validateLanePaths({ head: 'work', files: ['.github/workflows/x.yml'] }).length, 1);
  assert.deepEqual(validateLanePaths({ head: 'work', files: ['packages/core/a.ts', '.github/task-control/work.json'] }), []);
  const candidateErrors = await validateCandidateAutomation(process.cwd());
  assert.deepEqual(candidateErrors, []);
  console.log('PR policy self-test passed.');
}

const command = process.argv[2] ?? 'validate';
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (command === 'self-test') await selfTest();
  else if (command === 'validate') await validate();
  else if (command === 'publish-status') await publishStatus();
  else throw new Error(`Unknown command: ${command}`);
}
