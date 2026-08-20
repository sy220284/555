/* global console, process */
import assert from 'node:assert/strict';
import { appendFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, loadPolicy, assertFullSha } from './policy.mjs';
import { isLaneMergeReady, loadLaneState, loadTaskPolicy } from './task-control.mjs';

export function verifyProvenance({ mainSha, expectedSha, pull, sourcePr, sourceHeadSha, sourceBranch }) {
  const errors = [];
  if (mainSha !== expectedSha) errors.push(`Current main ${mainSha} does not equal expected ${expectedSha}`);
  if (pull?.number !== sourcePr) errors.push(`Source PR mismatch: expected #${sourcePr}, found #${pull?.number ?? '<missing>'}`);
  if (!pull?.merged_at) errors.push('Source PR is not merged');
  if (pull?.base?.ref !== 'main') errors.push('Source PR base is not main');
  if (pull?.head?.ref !== sourceBranch) errors.push(`Source branch mismatch: ${pull?.head?.ref ?? '<missing>'}`);
  if (pull?.head?.sha !== sourceHeadSha) errors.push('Source head SHA mismatch');
  return errors;
}

export function resultState(result) {
  return result === 'success' ? 'success' : 'failure';
}

export function bootstrapStatusException({ policy, required, sourceBranch, basePolicyPresent }) {
  return required === 'pr-policy' &&
    policy?.trustBootstrap?.requiresTrustedMainPolicy === true &&
    policy?.trustBootstrap?.candidateSelfCertificationAllowed === false &&
    policy?.trustBootstrap?.initialMergeRequiresExplicitUserApproval === true &&
    sourceBranch === policy?.trustBootstrap?.bootstrapBranch &&
    basePolicyPresent === false;
}

async function resolveSource(owner, repo, policy, expectedSha) {
  const requestedPr = Number.parseInt(process.env.SOURCE_PR ?? '', 10);
  if (Number.isSafeInteger(requestedPr) && requestedPr > 0) {
    const pull = await githubApi(`/repos/${owner}/${repo}/pulls/${requestedPr}`);
    return {
      pull,
      sourcePr: requestedPr,
      sourceHeadSha: assertFullSha(process.env.SOURCE_HEAD_SHA, 'source head sha'),
      sourceBranch: process.env.SOURCE_BRANCH,
    };
  }
  const pulls = await githubApi(`/repos/${owner}/${repo}/commits/${expectedSha}/pulls?per_page=100`);
  const pull = pulls.find(
    (item) => item.merged_at && item.base?.ref === 'main' && policy.integrationBranches.includes(item.head?.ref),
  );
  if (!pull) throw new Error(`Cannot resolve a merged work/governance PR for main ${expectedSha}`);
  return {
    pull,
    sourcePr: pull.number,
    sourceHeadSha: assertFullSha(pull.head?.sha, 'resolved source head sha'),
    sourceBranch: pull.head.ref,
  };
}

async function verify() {
  const policy = await loadPolicy();
  const expectedSha = assertFullSha(process.env.EXPECTED_SHA || process.env.GITHUB_SHA, 'expected main sha');
  const { owner, repo } = githubRepository();
  const mainRef = await githubApi(`/repos/${owner}/${repo}/git/ref/heads/main`);
  const mainSha = assertFullSha(mainRef?.object?.sha, 'current main sha');
  const source = await resolveSource(owner, repo, policy, expectedSha);
  if (!policy.integrationBranches.includes(source.sourceBranch)) throw new Error(`Invalid source branch ${source.sourceBranch}`);
  const errors = verifyProvenance({
    mainSha,
    expectedSha,
    pull: source.pull,
    sourcePr: source.sourcePr,
    sourceHeadSha: source.sourceHeadSha,
    sourceBranch: source.sourceBranch,
  });
  if (errors.length > 0) throw new Error(errors.join('\n'));

  const taskPolicy = await loadTaskPolicy(process.cwd());
  const laneState = await loadLaneState(process.cwd(), source.sourceBranch, taskPolicy);
  if (!isLaneMergeReady(laneState, taskPolicy)) {
    throw new Error(`Merged source lane task is not IMPLEMENTED: ${laneState.taskId ?? '<none>'} ${laneState.status}/${laneState.phase}`);
  }

  const checkRuns = await githubApi(`/repos/${owner}/${repo}/commits/${source.sourceHeadSha}/check-runs?per_page=100`);
  for (const required of policy.requiredCheckRuns) {
    const runs = (checkRuns?.check_runs ?? []).filter((run) => run.name === required);
    runs.sort((a, b) => new Date(b.completed_at ?? b.started_at ?? 0) - new Date(a.completed_at ?? a.started_at ?? 0));
    if (runs[0]?.conclusion !== 'success') throw new Error(`Required source check is not successful: ${required}`);
  }
  const sourceStatus = await githubApi(`/repos/${owner}/${repo}/commits/${source.sourceHeadSha}/status`);
  for (const required of policy.requiredStatusContexts) {
    const entries = (sourceStatus?.statuses ?? []).filter((status) => status.context === required);
    entries.sort((a, b) => new Date(b.updated_at ?? b.created_at ?? 0) - new Date(a.updated_at ?? a.created_at ?? 0));
    if (entries[0]?.state !== 'success') {
      const basePolicy = await githubApi(`/repos/${owner}/${repo}/contents/.github/governance/repository-policy.json?ref=${source.pull.base.sha}`, {}, [404]);
      const bootstrap = bootstrapStatusException({
        policy,
        required,
        sourceBranch: source.sourceBranch,
        basePolicyPresent: Boolean(basePolicy),
      });
      if (!bootstrap) throw new Error(`Required source status is not successful: ${required}`);
      console.log('Initial governance trust bootstrap detected: the source base had no trusted policy; one-time pr-policy absence accepted for post-merge verification.');
    }
  }
  if (process.env.GITHUB_OUTPUT) {
    await appendFile(process.env.GITHUB_OUTPUT, `source_pr=${source.sourcePr}\n`);
    await appendFile(process.env.GITHUB_OUTPUT, `source_head_sha=${source.sourceHeadSha}\n`);
    await appendFile(process.env.GITHUB_OUTPUT, `source_branch=${source.sourceBranch}\n`);
    await appendFile(process.env.GITHUB_OUTPUT, `task_id=${laneState.taskId}\n`);
  }
  console.log(`Main provenance verified for ${expectedSha} from PR #${source.sourcePr}, task ${laneState.taskId}.`);
}

async function publishCommitStatus({ expectedSha, context, state, successDescription, failureDescription }) {
  const { owner, repo } = githubRepository();
  await githubApi(`/repos/${owner}/${repo}/statuses/${expectedSha}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      state,
      context,
      description: state === 'success' ? successDescription : failureDescription,
      target_url: `${process.env.GITHUB_SERVER_URL}/${process.env.GITHUB_REPOSITORY}/actions/runs/${process.env.GITHUB_RUN_ID}`,
    }),
  });
  console.log(`Published ${context}=${state} for ${expectedSha}.`);
}

async function publishStatus() {
  const policy = await loadPolicy();
  const expectedSha = assertFullSha(process.env.EXPECTED_SHA || process.env.GITHUB_SHA, 'expected main sha');
  const state = resultState(process.env.VERIFY_RESULT);
  await publishCommitStatus({
    expectedSha,
    context: policy.mainVerificationContext,
    state,
    successDescription: 'Verified merged main, source provenance and task state',
    failureDescription: 'Main verification failed',
  });
  if (state !== 'success') throw new Error('Published failed main-verification status');
}

async function publishDeliveryStatus() {
  const policy = await loadPolicy();
  const expectedSha = assertFullSha(process.env.EXPECTED_SHA || process.env.GITHUB_SHA, 'expected main sha');
  const state = resultState(process.env.DELIVERY_RESULT);
  await publishCommitStatus({
    expectedSha,
    context: policy.deliveryStatusContext,
    state,
    successDescription: 'Main verified, integration lanes synchronized and branch hygiene passed',
    failureDescription: 'Final delivery closure failed',
  });
  if (state !== 'success') throw new Error('Published failed delivery-ready status');
}

function selfTest() {
  const sha = 'a'.repeat(40);
  const pull = { number: 7, merged_at: 'x', base: { ref: 'main' }, head: { ref: 'work', sha } };
  assert.deepEqual(verifyProvenance({ mainSha: sha, expectedSha: sha, pull, sourcePr: 7, sourceHeadSha: sha, sourceBranch: 'work' }), []);
  assert.ok(verifyProvenance({ mainSha: 'b'.repeat(40), expectedSha: sha, pull, sourcePr: 7, sourceHeadSha: sha, sourceBranch: 'work' }).length > 0);
  assert.equal(resultState('success'), 'success');
  assert.equal(resultState('failure'), 'failure');
  assert.equal(resultState('skipped'), 'failure');
  const bootstrapPolicy = {
    trustBootstrap: {
      requiresTrustedMainPolicy: true,
      candidateSelfCertificationAllowed: false,
      bootstrapBranch: 'governance',
      initialMergeRequiresExplicitUserApproval: true,
    },
  };
  assert.equal(bootstrapStatusException({ policy: bootstrapPolicy, required: 'pr-policy', sourceBranch: 'governance', basePolicyPresent: false }), true);
  assert.equal(bootstrapStatusException({ policy: bootstrapPolicy, required: 'pr-policy', sourceBranch: 'work', basePolicyPresent: false }), false);
  assert.equal(bootstrapStatusException({ policy: bootstrapPolicy, required: 'pr-policy', sourceBranch: 'governance', basePolicyPresent: true }), false);
  console.log('Main verification self-test passed.');
}

const command = process.argv[2] ?? 'verify';
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (command === 'self-test') selfTest();
  else if (command === 'verify') await verify();
  else if (command === 'publish-status') await publishStatus();
  else if (command === 'publish-delivery-status') await publishDeliveryStatus();
  else throw new Error(`Unknown command: ${command}`);
}
