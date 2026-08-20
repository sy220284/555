import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const policyPath = path.join(here, 'repository-policy.json');
const fullShaPattern = /^[0-9a-f]{40}$/iu;

export async function loadPolicy() {
  const policy = JSON.parse(await readFile(policyPath, 'utf8'));
  validatePolicy(policy);
  return policy;
}

export function validatePolicy(policy) {
  if (policy?.schemaVersion !== 1) throw new Error('Unsupported repository policy schema');
  if (policy?.branches?.main !== 'main') throw new Error('main branch policy must remain main');
  const lanes = policy?.integrationBranches;
  if (!Array.isArray(lanes) || lanes.length !== 2 || !lanes.includes('work') || !lanes.includes('governance')) {
    throw new Error('integrationBranches must be exactly work and governance');
  }
  if (policy?.allowDirectMainCommits !== false) throw new Error('Direct main commits must stay disabled');
  if (policy?.allowAdditionalBranches !== false) throw new Error('Additional branches must stay disabled');
  if (policy?.mainWriteMode !== 'serialized') throw new Error('main writes must be serialized');
  if (policy?.mergeMethod !== 'squash') throw new Error('merge method must remain squash');
  if (!Array.isArray(policy?.requiredStatusContexts) || policy.requiredStatusContexts.length < 1) {
    throw new Error('requiredStatusContexts must not be empty');
  }
  if (!Array.isArray(policy?.requiredCheckRuns) || policy.requiredCheckRuns.length < 1) {
    throw new Error('requiredCheckRuns must not be empty');
  }
  return policy;
}

export function assertFullSha(value, label = 'sha') {
  if (!fullShaPattern.test(value ?? '')) throw new Error(`Invalid ${label}: ${value ?? '<missing>'}`);
  return value;
}

export function isFullSha(value) {
  return fullShaPattern.test(value ?? '');
}

export function githubRepository() {
  const value = process.env.GITHUB_REPOSITORY;
  if (!value || !value.includes('/')) throw new Error('GITHUB_REPOSITORY is required');
  const [owner, repo] = value.split('/');
  return { owner, repo };
}

export async function githubApi(pathname, options = {}, accepted = []) {
  if (!process.env.GITHUB_TOKEN) throw new Error('GITHUB_TOKEN is required');
  const response = await globalThis.fetch(new URL(pathname, 'https://api.github.com'), {
    ...options,
    headers: {
      Accept: 'application/vnd.github+json',
      Authorization: `Bearer ${process.env.GITHUB_TOKEN}`,
      'X-GitHub-Api-Version': '2022-11-28',
      ...(options.headers ?? {}),
    },
  });
  const text = await response.text();
  if (!response.ok && !accepted.includes(response.status)) {
    throw new Error(`GitHub API ${response.status}: ${pathname}\n${text}`);
  }
  if (!response.ok || !text) return null;
  return JSON.parse(text);
}

export async function listAllPages(pathFactory) {
  const items = [];
  for (let page = 1; ; page += 1) {
    const batch = await githubApi(pathFactory(page));
    if (!Array.isArray(batch)) throw new Error('Expected paginated GitHub array response');
    items.push(...batch);
    if (batch.length < 100) return items;
  }
}
