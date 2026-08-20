/* global console, process */
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, listAllPages } from './policy.mjs';

export function extraBranchDecision({ openPulls, aheadBy }) {
  if (openPulls > 0) return { action: 'blocked', reason: 'open-pr' };
  if (aheadBy > 0) return { action: 'blocked', reason: 'unique-commits' };
  return { action: 'delete', reason: 'fully-contained-in-main' };
}

async function listBranches(owner, repo) {
  return listAllPages((page) => `/repos/${owner}/${repo}/branches?per_page=100&page=${page}`);
}

async function main() {
  const policy = await loadPolicy();
  const repair = process.argv.includes('--repair');
  const { owner, repo } = githubRepository();
  const mainRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/main`);
  const mainSha = mainRef.object.sha;
  let branches = await listBranches(owner, repo);
  const allowed = new Set([policy.branches.main, ...policy.integrationBranches]);
  const results = [];

  for (const lane of policy.integrationBranches) {
    if (branches.some((branch) => branch.name === lane)) continue;
    if (!repair) {
      results.push({ branch: lane, action: 'blocked', reason: 'missing-required-lane' });
      continue;
    }
    await githubApi(`/repos/${owner}/${repo}/git/refs`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ref: `refs/heads/${lane}`, sha: mainSha }),
    });
    const reread = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/${lane}`);
    if (reread?.object?.sha !== mainSha) throw new Error(`Failed to recreate ${lane}`);
    results.push({ branch: lane, action: 'create', reason: 'missing-required-lane' });
  }

  branches = await listBranches(owner, repo);
  for (const branch of branches.filter((item) => !allowed.has(item.name))) {
    const pulls = await githubApi(`/repos/${owner}/${repo}/pulls?state=open&head=${owner}:${encodeURIComponent(branch.name)}&per_page=100`);
    const comparison = await githubApi(`/repos/${owner}/${repo}/compare/${mainSha}...${branch.commit.sha}`);
    const decision = extraBranchDecision({ openPulls: pulls.length, aheadBy: comparison?.ahead_by ?? 1 });
    if (decision.action === 'delete' && repair) {
      const encoded = branch.name.split('/').map(encodeURIComponent).join('/');
      await githubApi(`/repos/${owner}/${repo}/git/refs/heads/${encoded}`, { method: 'DELETE' });
      const reread = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/${encoded}`, {}, [404]);
      if (reread) throw new Error(`Unexpected branch ${branch.name} still exists after deletion`);
      results.push({ branch: branch.name, action: 'delete', reason: decision.reason });
    } else {
      results.push({ branch: branch.name, ...decision });
    }
  }

  const finalBranches = (await listBranches(owner, repo)).map((branch) => branch.name).sort();
  const invalid = finalBranches.filter((name) => !allowed.has(name));
  const missing = [...allowed].filter((name) => !finalBranches.includes(name));
  const output = process.env.BRANCH_HYGIENE_OUTPUT ?? 'artifacts/branch-hygiene';
  await mkdir(output, { recursive: true });
  await writeFile(path.join(output, 'result.json'), `${JSON.stringify({ repair, mainSha, finalBranches, invalid, missing, results }, null, 2)}\n`);
  if (invalid.length > 0 || missing.length > 0) throw new Error(`Branch hygiene blocked; invalid=${invalid.join(',') || '-'} missing=${missing.join(',') || '-'}`);
  console.log('Branch hygiene passed: exactly main, work and governance remain.');
}

function selfTest() {
  assert.deepEqual(extraBranchDecision({ openPulls: 1, aheadBy: 0 }), { action: 'blocked', reason: 'open-pr' });
  assert.deepEqual(extraBranchDecision({ openPulls: 0, aheadBy: 2 }), { action: 'blocked', reason: 'unique-commits' });
  assert.deepEqual(extraBranchDecision({ openPulls: 0, aheadBy: 0 }), { action: 'delete', reason: 'fully-contained-in-main' });
  console.log('Branch hygiene self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
