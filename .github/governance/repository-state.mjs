/* global console, process */
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, listAllPages } from './policy.mjs';

export function deriveRepositoryState({ openPulls, mainVerified, deliveryReady, lanesSynchronized, invalidBranches }) {
  if (invalidBranches > 0) return 'BRANCH_HYGIENE_BLOCKED';
  if (openPulls > 0) return 'PR_ACTIVE';
  if (!mainVerified) return 'MAIN_VERIFYING';
  if (!lanesSynchronized) return 'SYNC_PENDING';
  if (!deliveryReady) return 'DELIVERY_VERIFYING';
  return 'DELIVERED';
}

export function successfulContext(statuses, context) {
  const matches = (statuses ?? []).filter((entry) => entry.context === context);
  matches.sort((a, b) => new Date(b.updated_at ?? b.created_at ?? 0) - new Date(a.updated_at ?? a.created_at ?? 0));
  return matches[0]?.state === 'success';
}

async function listBranches(owner, repo) {
  return listAllPages((page) => `/repos/${owner}/${repo}/branches?per_page=100&page=${page}`);
}

export async function readRepositoryState() {
  const policy = await loadPolicy();
  const { owner, repo } = githubRepository();
  const branches = await listBranches(owner, repo);
  const names = branches.map((branch) => branch.name).sort();
  const allowed = new Set([policy.branches.main, ...policy.integrationBranches]);
  const invalid = names.filter((name) => !allowed.has(name));
  const refs = Object.fromEntries(branches.map((branch) => [branch.name, branch.commit.sha]));
  const pulls = await githubApi(`/repos/${owner}/${repo}/pulls?state=open&base=main&per_page=100`);
  const lanePulls = pulls.filter((pull) => policy.integrationBranches.includes(pull.head?.ref));
  const mainSha = refs.main;
  const combined = await githubApi(`/repos/${owner}/${repo}/commits/${mainSha}/status`);
  const mainVerified = successfulContext(combined?.statuses, policy.mainVerificationContext);
  const deliveryReady = successfulContext(combined?.statuses, policy.deliveryStatusContext);
  const lanesSynchronized = policy.integrationBranches.every((lane) => {
    const active = lanePulls.some((pull) => pull.head?.ref === lane);
    return active || refs[lane] === mainSha;
  });
  const state = deriveRepositoryState({
    openPulls: lanePulls.length,
    mainVerified,
    deliveryReady,
    lanesSynchronized,
    invalidBranches: invalid.length,
  });
  return {
    state,
    mainSha,
    refs,
    openPulls: lanePulls.map((pull) => ({ number: pull.number, head: pull.head?.ref, sha: pull.head?.sha })),
    mainVerified,
    deliveryReady,
    lanesSynchronized,
    invalid,
  };
}

async function main(command) {
  const policy = await loadPolicy();
  const result = await readRepositoryState();
  console.log(JSON.stringify(result, null, 2));
  if (command === 'assert-delivered' && result.state !== policy.taskControl.finalRepositoryState) {
    throw new Error(`Repository is ${result.state}; final delivery requires ${policy.taskControl.finalRepositoryState}`);
  }
  if (command === 'assert-delivered') console.log('Repository delivery assertion passed: DELIVERED.');
}

function selfTest() {
  const base = { openPulls: 0, mainVerified: true, deliveryReady: true, lanesSynchronized: true, invalidBranches: 0 };
  assert.equal(deriveRepositoryState(base), 'DELIVERED');
  assert.equal(deriveRepositoryState({ ...base, openPulls: 1 }), 'PR_ACTIVE');
  assert.equal(deriveRepositoryState({ ...base, mainVerified: false }), 'MAIN_VERIFYING');
  assert.equal(deriveRepositoryState({ ...base, lanesSynchronized: false }), 'SYNC_PENDING');
  assert.equal(deriveRepositoryState({ ...base, deliveryReady: false }), 'DELIVERY_VERIFYING');
  assert.equal(deriveRepositoryState({ ...base, invalidBranches: 1 }), 'BRANCH_HYGIENE_BLOCKED');
  assert.equal(successfulContext([
    { context: 'delivery-ready', state: 'success', updated_at: '2026-08-20T00:00:00Z' },
    { context: 'delivery-ready', state: 'failure', updated_at: '2026-08-20T00:01:00Z' },
  ], 'delivery-ready'), false);
  assert.equal(successfulContext([
    { context: 'delivery-ready', state: 'failure', updated_at: '2026-08-20T00:00:00Z' },
    { context: 'delivery-ready', state: 'success', updated_at: '2026-08-20T00:01:00Z' },
  ], 'delivery-ready'), true);
  console.log('Repository state self-test passed.');
}

const command = process.argv[2] ?? 'status';
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (command === 'self-test') selfTest();
  else if (command === 'status' || command === 'assert-delivered') await main(command);
  else throw new Error(`Unknown command: ${command}`);
}
