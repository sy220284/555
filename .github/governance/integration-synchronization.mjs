/* global console, process */
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, assertFullSha } from './policy.mjs';

export function synchronizationDecision({ mainSha, branchSha, sourceHeadSha, openPulls, isSourceBranch, aheadBy = 0, behindBy = 0 }) {
  if (branchSha === mainSha) return { action: 'keep', reason: 'already-synchronized' };
  if (isSourceBranch) {
    if (openPulls > 0) return { action: 'blocked', reason: 'new-source-pr-open' };
    if (branchSha !== sourceHeadSha) return { action: 'blocked', reason: 'source-advanced-after-merge' };
    return { action: 'reset', reason: 'verified-squash-complete' };
  }
  if (openPulls > 0) return { action: 'skip', reason: 'active-sibling-lane' };
  if (aheadBy === 0 && behindBy > 0) return { action: 'fast-forward', reason: 'idle-sibling-is-main-ancestor' };
  return { action: 'blocked', reason: 'idle-sibling-has-unique-or-diverged-history' };
}

async function compare(owner, repo, mainSha, branchSha) {
  if (mainSha === branchSha) return { ahead_by: 0, behind_by: 0 };
  return githubApi(`/repos/${owner}/${repo}/compare/${mainSha}...${branchSha}`);
}

async function synchronizeBranch({ owner, repo, branchName, mainSha, sourceBranch, sourceHeadSha }) {
  const ref = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/${branchName}`, {}, [404]);
  if (!ref) {
    await githubApi(`/repos/${owner}/${repo}/git/refs`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ref: `refs/heads/${branchName}`, sha: mainSha }),
    });
    const finalRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/${branchName}`);
    if (finalRef?.object?.sha !== mainSha) throw new Error(`Failed to recreate ${branchName}`);
    return { branchName, action: 'create', finalSha: mainSha };
  }
  const branchSha = assertFullSha(ref.object.sha, `${branchName} sha`);
  const pulls = await githubApi(`/repos/${owner}/${repo}/pulls?state=open&base=main&head=${owner}:${branchName}&per_page=100`);
  const isSourceBranch = branchName === sourceBranch;
  const comparison = isSourceBranch ? { ahead_by: 0, behind_by: 0 } : await compare(owner, repo, mainSha, branchSha);
  const decision = synchronizationDecision({
    mainSha,
    branchSha,
    sourceHeadSha,
    openPulls: pulls.length,
    isSourceBranch,
    aheadBy: comparison?.ahead_by ?? 0,
    behindBy: comparison?.behind_by ?? 0,
  });
  if (decision.action === 'blocked') throw new Error(`${branchName} synchronization blocked: ${decision.reason}`);
  if (decision.action === 'skip' || decision.action === 'keep') return { branchName, ...decision, finalSha: branchSha };
  await githubApi(`/repos/${owner}/${repo}/git/refs/heads/${branchName}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sha: mainSha, force: decision.action === 'reset' }),
  });
  const finalRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/${branchName}`);
  if (finalRef?.object?.sha !== mainSha) throw new Error(`${branchName} synchronization postcondition failed`);
  return { branchName, ...decision, finalSha: mainSha };
}

async function main() {
  const policy = await loadPolicy();
  const mainSha = assertFullSha(process.env.EXPECTED_SHA, 'expected main sha');
  const sourceHeadSha = assertFullSha(process.env.SOURCE_HEAD_SHA, 'source head sha');
  const sourceBranch = process.env.SOURCE_BRANCH;
  if (!policy.integrationBranches.includes(sourceBranch)) throw new Error(`Invalid source branch ${sourceBranch}`);
  const { owner, repo } = githubRepository();
  const mainRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/main`);
  if (mainRef?.object?.sha !== mainSha) throw new Error('Verified main is no longer current');
  const status = await githubApi(`/repos/${owner}/${repo}/commits/${mainSha}/status`);
  if (!(status?.statuses ?? []).some((entry) => entry.context === policy.mainVerificationContext && entry.state === 'success')) {
    throw new Error('main-verification is not successful');
  }
  const results = [];
  for (const branchName of policy.integrationBranches) {
    results.push(await synchronizeBranch({ owner, repo, branchName, mainSha, sourceBranch, sourceHeadSha }));
  }
  const output = process.env.SYNCHRONIZATION_OUTPUT ?? 'artifacts/integration-synchronization';
  await mkdir(output, { recursive: true });
  await writeFile(path.join(output, 'result.json'), `${JSON.stringify({ mainSha, sourceBranch, sourceHeadSha, results }, null, 2)}\n`);
  console.log(JSON.stringify(results));
}

function selfTest() {
  const a = 'a'.repeat(40); const b = 'b'.repeat(40); const c = 'c'.repeat(40);
  assert.deepEqual(synchronizationDecision({ mainSha: a, branchSha: b, sourceHeadSha: b, openPulls: 0, isSourceBranch: true }), { action: 'reset', reason: 'verified-squash-complete' });
  assert.deepEqual(synchronizationDecision({ mainSha: a, branchSha: c, sourceHeadSha: b, openPulls: 0, isSourceBranch: true }), { action: 'blocked', reason: 'source-advanced-after-merge' });
  assert.deepEqual(synchronizationDecision({ mainSha: a, branchSha: b, sourceHeadSha: c, openPulls: 1, isSourceBranch: false, aheadBy: 2, behindBy: 1 }), { action: 'skip', reason: 'active-sibling-lane' });
  assert.deepEqual(synchronizationDecision({ mainSha: a, branchSha: b, sourceHeadSha: c, openPulls: 0, isSourceBranch: false, aheadBy: 0, behindBy: 2 }), { action: 'fast-forward', reason: 'idle-sibling-is-main-ancestor' });
  assert.equal(synchronizationDecision({ mainSha: a, branchSha: b, sourceHeadSha: c, openPulls: 0, isSourceBranch: false, aheadBy: 1, behindBy: 1 }).action, 'blocked');
  console.log('Integration synchronization self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
