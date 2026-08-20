/* global console, process */
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, assertFullSha } from './policy.mjs';

export function requiredChecksReady({ checkRuns, statuses, requiredCheckRuns, requiredStatusContexts }) {
  const latestChecks = new Map();
  for (const run of checkRuns ?? []) {
    const previous = latestChecks.get(run.name);
    if (!previous || new Date(run.completed_at ?? run.started_at ?? 0) >= new Date(previous.completed_at ?? previous.started_at ?? 0)) {
      latestChecks.set(run.name, run);
    }
  }
  const latestStatuses = new Map();
  for (const status of statuses ?? []) {
    if (!latestStatuses.has(status.context)) latestStatuses.set(status.context, status);
  }
  return requiredCheckRuns.every((name) => latestChecks.get(name)?.conclusion === 'success') &&
    requiredStatusContexts.every((name) => latestStatuses.get(name)?.state === 'success');
}

async function eventPayload() {
  if (!process.env.GITHUB_EVENT_PATH) throw new Error('GITHUB_EVENT_PATH is required');
  return JSON.parse(await readFile(process.env.GITHUB_EVENT_PATH, 'utf8'));
}

async function associatedOpenPull(owner, repo, sha, lanes) {
  const pulls = await githubApi(`/repos/${owner}/${repo}/commits/${sha}/pulls?per_page=100`);
  return pulls.find(
    (pull) => pull.state === 'open' && pull.base?.ref === 'main' && lanes.includes(pull.head?.ref),
  );
}

async function updateBranch(owner, repo, pull) {
  const result = await githubApi(`/repos/${owner}/${repo}/pulls/${pull.number}/update-branch`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ expected_head_sha: pull.head.sha }),
  }, [202, 422]);
  if (!result) throw new Error(`Unable to update PR #${pull.number} to latest main`);
  console.log(`PR #${pull.number} base moved; requested automatic branch update and deferred merge.`);
}

async function main() {
  const policy = await loadPolicy();
  const event = await eventPayload();
  const run = event.workflow_run;
  if (!run || run.conclusion !== 'success') {
    console.log('Trigger workflow did not succeed; controlled merge is not eligible.');
    return;
  }
  const { owner, repo } = githubRepository();
  let candidateSha = run.head_sha;
  let pull = null;
  const hintedPr = run.pull_requests?.[0]?.number;
  if (hintedPr) {
    const hinted = await githubApi(`/repos/${owner}/${repo}/pulls/${hintedPr}`);
    if (hinted?.state === 'open' && hinted.base?.ref === 'main' && policy.integrationBranches.includes(hinted.head?.ref)) {
      pull = hinted;
      candidateSha = hinted.head.sha;
    }
  }
  candidateSha = assertFullSha(candidateSha, 'candidate head sha');
  if (!pull) pull = await associatedOpenPull(owner, repo, candidateSha, policy.integrationBranches);
  if (!pull) {
    console.log(`No open integration PR is associated with ${candidateSha}; nothing to merge.`);
    return;
  }
  if (pull.draft) {
    console.log(`PR #${pull.number} is draft; merge deferred.`);
    return;
  }
  if (pull.head?.repo?.full_name !== process.env.GITHUB_REPOSITORY) throw new Error('Cross-repository integration PR is forbidden');
  if (pull.head.sha !== candidateSha) {
    console.log(`PR #${pull.number} advanced after trigger; merge deferred to the new head.`);
    return;
  }

  const mainRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/main`);
  const mainSha = assertFullSha(mainRef?.object?.sha, 'main sha');
  if (pull.base?.sha !== mainSha) {
    await updateBranch(owner, repo, pull);
    return;
  }

  const checkRuns = await githubApi(`/repos/${owner}/${repo}/commits/${candidateSha}/check-runs?per_page=100`);
  const status = await githubApi(`/repos/${owner}/${repo}/commits/${candidateSha}/status`);
  if (!requiredChecksReady({
    checkRuns: checkRuns?.check_runs,
    statuses: status?.statuses,
    requiredCheckRuns: policy.requiredCheckRuns,
    requiredStatusContexts: policy.requiredStatusContexts,
  })) {
    console.log(`PR #${pull.number} does not yet have all required successful checks; merge deferred.`);
    return;
  }

  const competing = await githubApi(`/repos/${owner}/${repo}/pulls?state=open&base=main&per_page=100`);
  const sameLane = competing.filter((item) => item.head?.ref === pull.head.ref && item.number !== pull.number);
  if (sameLane.length > 0) throw new Error(`Another ${pull.head.ref} -> main PR is open`);

  const merge = await githubApi(`/repos/${owner}/${repo}/pulls/${pull.number}/merge`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      sha: candidateSha,
      merge_method: policy.mergeMethod,
      commit_title: `${pull.title} (#${pull.number})`,
    }),
  });
  if (!merge?.merged) throw new Error(`GitHub refused controlled merge for PR #${pull.number}: ${merge?.message ?? 'unknown reason'}`);
  const mergedMainSha = assertFullSha(merge.sha, 'merged main sha');
  console.log(`Controlled merge completed for PR #${pull.number}: ${mergedMainSha}. Main Verification will start from the main push.`);
}

function selfTest() {
  const now = new Date().toISOString();
  assert.equal(requiredChecksReady({
    checkRuns: [{ name: 'repository-gates / merge-gate', conclusion: 'success', completed_at: now }],
    statuses: [{ context: 'pr-policy', state: 'success' }],
    requiredCheckRuns: ['repository-gates / merge-gate'],
    requiredStatusContexts: ['pr-policy'],
  }), true);
  assert.equal(requiredChecksReady({
    checkRuns: [{ name: 'repository-gates / merge-gate', conclusion: 'failure', completed_at: now }],
    statuses: [{ context: 'pr-policy', state: 'success' }],
    requiredCheckRuns: ['repository-gates / merge-gate'],
    requiredStatusContexts: ['pr-policy'],
  }), false);
  console.log('Controlled merge self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
