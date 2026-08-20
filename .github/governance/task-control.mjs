/* global console, process */
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const taskPolicyRelativePath = '.github/task-control/policy.json';
export const taskLanes = ['work', 'governance'];

async function readJson(root, relative) {
  return JSON.parse(await readFile(path.resolve(root, relative), 'utf8'));
}

export function validateTaskPolicy(policy) {
  const errors = [];
  if (policy?.schemaVersion !== 1) errors.push('Unsupported task-control policy schema');
  const statePaths = policy?.laneStatePaths;
  if (!statePaths || statePaths.work !== '.github/task-control/work.json' || statePaths.governance !== '.github/task-control/governance.json') {
    errors.push('laneStatePaths must bind work and governance to the permanent task-control files');
  }
  const statuses = policy?.statuses;
  if (!Array.isArray(statuses) || !['IDLE', 'IN_PROGRESS', 'IMPLEMENTED'].every((status) => statuses.includes(status))) {
    errors.push('task-control statuses must include IDLE, IN_PROGRESS and IMPLEMENTED');
  }
  const phases = policy?.phases;
  if (!Array.isArray(phases) || !['IDLE', 'ANALYZING', 'IMPLEMENTING', 'VERIFYING', 'AUDITING', 'FIXING', 'IMPLEMENTED'].every((phase) => phases.includes(phase))) {
    errors.push('task-control phases are incomplete');
  }
  if (policy?.mergeReadyStatus !== 'IMPLEMENTED') errors.push('mergeReadyStatus must remain IMPLEMENTED');
  if (policy?.finalRepositoryState !== 'DELIVERED') errors.push('finalRepositoryState must remain DELIVERED');
  if (policy?.finalStatusContext !== 'delivery-ready') errors.push('finalStatusContext must remain delivery-ready');
  if (policy?.rules?.singleActiveTaskPerLane !== true) errors.push('singleActiveTaskPerLane must remain enabled');
  if (policy?.rules?.requireGoalForActiveTask !== true) errors.push('requireGoalForActiveTask must remain enabled');
  if (policy?.rules?.requireVerificationPlanForActiveTask !== true) errors.push('requireVerificationPlanForActiveTask must remain enabled');
  if (policy?.rules?.implementedIsNotDelivered !== true) errors.push('implementedIsNotDelivered must remain enabled');
  return errors;
}

export function validateLaneState(state, lane, policy) {
  const errors = [];
  if (!taskLanes.includes(lane)) errors.push(`Unknown task lane: ${lane}`);
  if (state?.schemaVersion !== 1) errors.push(`${lane} task state has unsupported schema`);
  if (state?.executionBranch !== lane) errors.push(`${lane} task state executionBranch must be ${lane}`);
  if (!policy?.statuses?.includes(state?.status)) errors.push(`${lane} task state has invalid status: ${state?.status ?? '<missing>'}`);
  if (!policy?.phases?.includes(state?.phase)) errors.push(`${lane} task state has invalid phase: ${state?.phase ?? '<missing>'}`);

  if (state?.status === 'IDLE') {
    if (state.taskId !== null) errors.push(`${lane} IDLE state must have taskId=null`);
    if (state.goal !== null) errors.push(`${lane} IDLE state must have goal=null`);
    if (state.phase !== 'IDLE') errors.push(`${lane} IDLE state must have phase=IDLE`);
    if (!Array.isArray(state.verification) || state.verification.length !== 0) errors.push(`${lane} IDLE state must have an empty verification plan`);
    return errors;
  }

  if (typeof state?.taskId !== 'string' || state.taskId.trim().length < 3 || state.taskId.trim().length > 80 || /[\r\n]/u.test(state.taskId)) {
    errors.push(`${lane} active task must have a stable taskId between 3 and 80 characters`);
  }
  if (typeof state?.goal !== 'string' || state.goal.trim().length < 8) {
    errors.push(`${lane} active task must have a non-trivial goal`);
  }
  if (!Array.isArray(state?.verification) || state.verification.length < 1 || state.verification.some((item) => typeof item !== 'string' || item.trim().length === 0)) {
    errors.push(`${lane} active task must have a verification plan`);
  }
  if (state?.status === 'IMPLEMENTED' && state?.phase !== 'IMPLEMENTED') {
    errors.push(`${lane} IMPLEMENTED state must have phase=IMPLEMENTED`);
  }
  if (state?.status === 'IN_PROGRESS' && state?.phase === 'IDLE') {
    errors.push(`${lane} IN_PROGRESS state cannot have phase=IDLE`);
  }
  return errors;
}

export async function loadTaskPolicy(root = '.') {
  const policy = await readJson(root, taskPolicyRelativePath);
  const errors = validateTaskPolicy(policy);
  if (errors.length > 0) throw new Error(errors.join('\n'));
  return policy;
}

export async function loadLaneState(root, lane, policy = null) {
  const effectivePolicy = policy ?? await loadTaskPolicy(root);
  const relative = effectivePolicy.laneStatePaths?.[lane];
  if (!relative) throw new Error(`No task state path configured for ${lane}`);
  const state = await readJson(root, relative);
  const errors = validateLaneState(state, lane, effectivePolicy);
  if (errors.length > 0) throw new Error(errors.join('\n'));
  return state;
}

export function isLaneMergeReady(state, policy) {
  return state?.status === policy?.mergeReadyStatus && state?.phase === 'IMPLEMENTED';
}

export async function validateTaskControl(root = '.') {
  const errors = [];
  let policy;
  try {
    policy = await readJson(root, taskPolicyRelativePath);
    errors.push(...validateTaskPolicy(policy));
  } catch (error) {
    return [`Cannot read task-control policy: ${error instanceof Error ? error.message : String(error)}`];
  }
  if (errors.length > 0) return errors;
  for (const lane of taskLanes) {
    try {
      const state = await readJson(root, policy.laneStatePaths[lane]);
      errors.push(...validateLaneState(state, lane, policy));
    } catch (error) {
      errors.push(`Cannot read ${lane} task state: ${error instanceof Error ? error.message : String(error)}`);
    }
  }
  return errors;
}

export async function validateLaneReadyForMerge(root, lane) {
  const policy = await loadTaskPolicy(root);
  const state = await loadLaneState(root, lane, policy);
  if (!isLaneMergeReady(state, policy)) {
    return [`${lane} task ${state.taskId ?? '<none>'} is ${state.status}/${state.phase}; PR merge requires IMPLEMENTED/IMPLEMENTED`];
  }
  return [];
}

function selfTest() {
  const policy = {
    schemaVersion: 1,
    laneStatePaths: { work: '.github/task-control/work.json', governance: '.github/task-control/governance.json' },
    statuses: ['IDLE', 'IN_PROGRESS', 'IMPLEMENTED'],
    phases: ['IDLE', 'ANALYZING', 'IMPLEMENTING', 'VERIFYING', 'AUDITING', 'FIXING', 'IMPLEMENTED'],
    mergeReadyStatus: 'IMPLEMENTED',
    finalRepositoryState: 'DELIVERED',
    finalStatusContext: 'delivery-ready',
    rules: {
      singleActiveTaskPerLane: true,
      requireGoalForActiveTask: true,
      requireVerificationPlanForActiveTask: true,
      implementedIsNotDelivered: true,
    },
  };
  assert.deepEqual(validateTaskPolicy(policy), []);
  assert.deepEqual(validateLaneState({
    schemaVersion: 1,
    executionBranch: 'work',
    taskId: null,
    status: 'IDLE',
    phase: 'IDLE',
    goal: null,
    verification: [],
  }, 'work', policy), []);
  const implemented = {
    schemaVersion: 1,
    executionBranch: 'governance',
    taskId: 'GOV-123',
    status: 'IMPLEMENTED',
    phase: 'IMPLEMENTED',
    goal: '完成仓库治理闭环并验证最终交付状态',
    verification: ['node test.mjs'],
  };
  assert.deepEqual(validateLaneState(implemented, 'governance', policy), []);
  assert.equal(isLaneMergeReady(implemented, policy), true);
  assert.ok(validateLaneState({ ...implemented, phase: 'VERIFYING' }, 'governance', policy).length > 0);
  assert.equal(isLaneMergeReady({ ...implemented, status: 'IN_PROGRESS', phase: 'VERIFYING' }, policy), false);
  console.log('Task control self-test passed.');
}

const command = process.argv[2] ?? 'validate';
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (command === 'self-test') selfTest();
  else if (command === 'validate') {
    const errors = await validateTaskControl(process.argv[3] ?? '.');
    if (errors.length > 0) throw new Error(errors.join('\n'));
    console.log('Task control validation passed.');
  } else if (command === 'assert-ready') {
    const lane = process.argv[3];
    const errors = await validateLaneReadyForMerge(process.argv[4] ?? '.', lane);
    if (errors.length > 0) throw new Error(errors.join('\n'));
    console.log(`${lane} task state is ready for merge.`);
  } else {
    throw new Error(`Unknown command: ${command}`);
  }
}
