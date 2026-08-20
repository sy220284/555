/* global console, process */
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, listAllPages } from './policy.mjs';

export function deriveRepositoryState({ openPulls, mainVerified, lanesSynchronized, invalidBranches }) {
  if (invalidBranches > 0) return 'BRANCH_HYGIENE_BLOCKED';
  if (openPulls > 0) return 'PR_ACTIVE';
  if (!mainVerified) return 'MAIN_VERIFYING';
  if (!lanesSynchronized) return 'SYNC_PENDING';
  return 'IDLE';
}

async function listBranches(owner, repo) {
  return listAllPages((page) => `/repos/${owner}/${repo}/branches?per_page=100&page=${page}`);
}

async function main() {
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
  const mainVerified = combined?.statuses?.some(
    (entry) => entry.context === policy.mainVerificationContext && entry.state === 'success',
  ) ?? false;
  const lanesSynchronized = policy.integrationBranches.every((lane) => {
    const active = lanePulls.some((pull) => pull.head?.ref === lane);
    return active || refs[lane] === mainSha;
  });
  const state = deriveRepositoryState({
    openPulls: lanePulls.length,
    mainVerified,
    lanesSynchronized,
    invalidBranches: invalid.length,
  });
  console.log(JSON.stringify({ state, mainSha, refs, openPulls: lanePulls.map((pull) => pull.number), invalid }, null, 2));
}

function selfTest() {
  assert.equal(deriveRepositoryState({ openPulls: 0, mainVerified: true, lanesSynchronized: true, invalidBranches: 0 }), 'IDLE');
  assert.equal(deriveRepositoryState({ openPulls: 1, mainVerified: true, lanesSynchronized: false, invalidBranches: 0 }), 'PR_ACTIVE');
  assert.equal(deriveRepositoryState({ openPulls: 0, mainVerified: false, lanesSynchronized: true, invalidBranches: 0 }), 'MAIN_VERIFYING');
  assert.equal(deriveRepositoryState({ openPulls: 0, mainVerified: true, lanesSynchronized: false, invalidBranches: 0 }), 'SYNC_PENDING');
  assert.equal(deriveRepositoryState({ openPulls: 0, mainVerified: true, lanesSynchronized: true, invalidBranches: 1 }), 'BRANCH_HYGIENE_BLOCKED');
  console.log('Repository state self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
