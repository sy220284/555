/* global console, process */
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, assertFullSha, listAllPages } from './policy.mjs';

export function latestStatusReady(statuses, requiredContexts) {
  const latest = new Map();
  for (const status of statuses ?? []) {
    const previous = latest.get(status.context);
    const currentTime = new Date(status.updated_at ?? status.created_at ?? 0).getTime();
    const previousTime = new Date(previous?.updated_at ?? previous?.created_at ?? 0).getTime();
    if (!previous || currentTime >= previousTime) latest.set(status.context, status);
  }
  return requiredContexts.every((context) => latest.get(context)?.state === 'success');
}

export function validateSourceGateRun({ trigger, exactRun, latestRun, requiredJobs, jobs, workflowName }) {
  const errors = [];
  if (trigger?.name !== workflowName) errors.push(`Unexpected workflow: ${trigger?.name ?? '<missing>'}`);
  if (exactRun?.name !== workflowName) errors.push(`Exact run belongs to unexpected workflow: ${exactRun?.name ?? '<missing>'}`);
  if (exactRun?.id !== trigger?.id) errors.push('Exact workflow run ID does not match trigger');
  if (exactRun?.check_suite_id !== trigger?.check_suite_id) errors.push('Exact workflow check suite does not match trigger');
  if (exactRun?.head_sha !== trigger?.head_sha) errors.push('Exact workflow head SHA does not match trigger');
  if (latestRun?.id !== trigger?.id) errors.push(`Workflow run ${trigger?.id ?? '<missing>'} is no longer latest for this head SHA`);
  if (exactRun?.status !== 'completed') errors.push(`Source gate run is not completed: ${exactRun?.status ?? '<missing>'}`);
  if (exactRun?.conclusion !== 'success') errors.push(`Source gate run did not succeed: ${exactRun?.conclusion ?? '<missing>'}`);
  for (const required of requiredJobs ?? []) {
    const matches = (jobs ?? []).filter((job) => job.name === required);
    if (matches.length !== 1) errors.push(`Required source job must occur exactly once: ${required}`);
    else if (matches[0].run_id !== trigger?.id || matches[0].conclusion !== 'success') {
      errors.push(`Required source job is not bound and successful: ${required}`);
    }
  }
  return errors;
}

function matchesProtectedPath(file, protectedPath) {
  return protectedPath.endsWith('/') ? file.startsWith(protectedPath) : file === protectedPath;
}

export function trustRootChanges(files, protectedPaths) {
  return [...new Set((files ?? []).filter((file) => (protectedPaths ?? []).some((protectedPath) => matchesProtectedPath(file, protectedPath))))].sort();
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

async function pullFiles(owner, repo, pullNumber) {
  return listAllPages((page) => `/repos/${owner}/${repo}/pulls/${pullNumber}/files?per_page=100&page=${page}`)
    .then((items) => items.map((item) => item.filename));
}

async function latestWorkflowRun(owner, repo, workflowId, headSha) {
  const response = await githubApi(`/repos/${owner}/${repo}/actions/workflows/${workflowId}/runs?head_sha=${headSha}&per_page=100`);
  const runs = response?.workflow_runs ?? [];
  runs.sort((left, right) => Number(right.id) - Number(left.id));
  return runs[0];
}

async function sourceGateEvidence(owner, repo, trigger, policy) {
  const exactRun = await githubApi(`/repos/${owner}/${repo}/actions/runs/${trigger.id}`);
  const jobsResponse = await githubApi(`/repos/${owner}/${repo}/actions/runs/${trigger.id}/jobs?filter=all&per_page=100`);
  const latestRun = await latestWorkflowRun(owner, repo, trigger.workflow_id, trigger.head_sha);
  const jobs = jobsResponse?.jobs ?? [];
  const errors = validateSourceGateRun({
    trigger,
    exactRun,
    latestRun,
    requiredJobs: policy.requiredCheckRuns,
    jobs,
    workflowName: policy.sourceGate.workflowName,
  });
  return { exactRun, jobs, errors };
}

async function publishSourceGateStatus(owner, repo, policy, trigger, errors) {
  const state = errors.length === 0 ? 'success' : 'failure';
  const target = `${process.env.GITHUB_SERVER_URL}/${process.env.GITHUB_REPOSITORY}/actions/runs/${trigger.id}`;
  await githubApi(`/repos/${owner}/${repo}/statuses/${trigger.head_sha}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      state,
      context: policy.sourceGate.statusContext,
      description: state === 'success'
        ? `Bound source gates to workflow run ${trigger.id}`
        : `Source gate run ${trigger.id} rejected`,
      target_url: target,
    }),
  });
  console.log(`Published ${policy.sourceGate.statusContext}=${state} for ${trigger.head_sha}, run ${trigger.id}.`);
}

async function waitForRequiredStatuses(owner, repo, sha, requiredContexts) {
  for (let attempt = 0; attempt < 60; attempt += 1) {
    const status = await githubApi(`/repos/${owner}/${repo}/commits/${sha}/status`);
    if (latestStatusReady(status?.statuses, requiredContexts)) return true;
    const contexts = new Set((status?.statuses ?? []).map((entry) => entry.context));
    if (requiredContexts.every((context) => contexts.has(context))) return false;
    await new Promise((resolve) => setTimeout(resolve, 10_000));
  }
  return false;
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
  const trigger = event.workflow_run;
  if (!trigger || trigger.name !== policy.sourceGate.workflowName) {
    console.log('Trigger is not the configured repository-gates workflow; nothing to do.');
    return;
  }
  trigger.head_sha = assertFullSha(trigger.head_sha, 'trigger head sha');
  const { owner, repo } = githubRepository();
  const evidence = await sourceGateEvidence(owner, repo, trigger, policy);
  await publishSourceGateStatus(owner, repo, policy, trigger, evidence.errors);
  if (evidence.errors.length > 0) {
    console.log(`Exact source gate evidence rejected:\n${evidence.errors.join('\n')}`);
    return;
  }

  const candidateSha = trigger.head_sha;
  let pull = null;
  const hintedPr = trigger.pull_requests?.[0]?.number;
  if (hintedPr) {
    const hinted = await githubApi(`/repos/${owner}/${repo}/pulls/${hintedPr}`);
    if (hinted?.state === 'open' && hinted.base?.ref === 'main' && policy.integrationBranches.includes(hinted.head?.ref)) pull = hinted;
  }
  if (!pull) pull = await associatedOpenPull(owner, repo, candidateSha, policy.integrationBranches);
  if (!pull) {
    console.log(`No open integration PR is associated with ${candidateSha}; source evidence remains recorded.`);
    return;
  }
  if (pull.draft) {
    console.log(`PR #${pull.number} is draft; merge deferred.`);
    return;
  }
  if (pull.head?.repo?.full_name !== process.env.GITHUB_REPOSITORY) throw new Error('Cross-repository integration PR is forbidden');
  if (pull.head.sha !== candidateSha) {
    console.log(`PR #${pull.number} advanced after run ${trigger.id}; merge deferred to the new head.`);
    return;
  }

  if (!await waitForRequiredStatuses(owner, repo, candidateSha, policy.requiredStatusContexts)) {
    console.log(`PR #${pull.number} does not have the required trusted status contexts; merge deferred.`);
    return;
  }

  const changed = await pullFiles(owner, repo, pull.number);
  const protectedChanges = trustRootChanges(changed, policy.trustBootstrap.trustRootPaths);
  if (protectedChanges.length > 0) {
    console.log(`PR #${pull.number} changes trust-root files and requires explicit user-approved merge; automatic merge deferred: ${protectedChanges.join(', ')}`);
    return;
  }

  const mainRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/main`);
  const mainSha = assertFullSha(mainRef?.object?.sha, 'main sha');
  if (pull.base?.sha !== mainSha) {
    await updateBranch(owner, repo, pull);
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
      commit_message: [
        `Source-PR: ${pull.number}`,
        `Source-Branch: ${pull.head.ref}`,
        `Source-Head-SHA: ${candidateSha}`,
        `Source-Gate-Run: ${trigger.id}`,
        `Source-Gate-Suite: ${trigger.check_suite_id}`,
      ].join('\n'),
    }),
  });
  if (!merge?.merged) throw new Error(`GitHub refused controlled merge for PR #${pull.number}: ${merge?.message ?? 'unknown reason'}`);
  const mergedMainSha = assertFullSha(merge.sha, 'merged main sha');
  console.log(`Controlled merge completed for PR #${pull.number}: ${mergedMainSha}. Main Verification will start from the main push.`);
}

function selfTest() {
  const sha = 'a'.repeat(40);
  const trigger = { id: 42, name: 'repository-gates', workflow_id: 9, check_suite_id: 77, head_sha: sha };
  const exactRun = { ...trigger, status: 'completed', conclusion: 'success' };
  const requiredJobs = ['task-governance', 'quality / quality', 'security', 'performance', 'evidence'];
  const jobs = requiredJobs.map((name, index) => ({ id: index + 1, run_id: 42, name, conclusion: 'success' }));
  assert.deepEqual(validateSourceGateRun({ trigger, exactRun, latestRun: exactRun, requiredJobs, jobs, workflowName: 'repository-gates' }), []);
  assert.ok(validateSourceGateRun({ trigger, exactRun, latestRun: { ...exactRun, id: 43 }, requiredJobs, jobs, workflowName: 'repository-gates' }).length > 0);
  assert.ok(validateSourceGateRun({ trigger, exactRun, latestRun: exactRun, requiredJobs, jobs: jobs.slice(1), workflowName: 'repository-gates' }).length > 0);
  assert.equal(latestStatusReady([{ context: 'pr-policy', state: 'success', updated_at: '2026-08-20T00:00:00Z' }], ['pr-policy']), true);
  assert.equal(latestStatusReady([
    { context: 'pr-policy', state: 'success', updated_at: '2026-08-20T00:00:00Z' },
    { context: 'pr-policy', state: 'failure', updated_at: '2026-08-20T00:01:00Z' },
  ], ['pr-policy']), false);
  const protectedPaths = ['AGENTS.md', '.github/workflows/', '.github/governance/', '.github/task-control/policy.json'];
  assert.deepEqual(trustRootChanges(['packages/core/a.ts'], protectedPaths), []);
  assert.deepEqual(trustRootChanges(['.github/task-control/work.json'], protectedPaths), []);
  assert.deepEqual(trustRootChanges(['AGENTS.md', '.github/workflows/a.yml'], protectedPaths), ['.github/workflows/a.yml', 'AGENTS.md']);
  console.log('Controlled merge self-test passed.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === 'self-test') selfTest();
  else await main();
}
