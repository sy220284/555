/* global console, process */
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { githubApi, githubRepository, assertFullSha } from './policy.mjs';

export function validateReleaseTag(tag, version) {
  if (tag !== `v${version}`) return `Release tag must equal v${version}`;
  if (!/^v\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/u.test(tag)) return 'Release tag format is invalid';
  return null;
}

async function createRelease() {
  const tag = process.env.RELEASE_TAG;
  if (!tag) throw new Error('RELEASE_TAG is required');
  const target = assertFullSha(process.env.GITHUB_SHA, 'release target sha');
  const { owner, repo } = githubRepository();
  const existing = await githubApi(`/repos/${owner}/${repo}/releases/tags/${encodeURIComponent(tag)}`, {}, [404]);
  if (existing) throw new Error(`Release ${tag} already exists; overwrite is forbidden`);
  const release = await githubApi(`/repos/${owner}/${repo}/releases`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      tag_name: tag,
      target_commitish: target,
      name: tag,
      generate_release_notes: true,
      draft: false,
      prerelease: tag.includes('-'),
    }),
  });
  if (!release?.html_url) throw new Error('GitHub did not return a release URL');
  console.log(`Created ${tag}: ${release.html_url}`);
}

function selfTest() {
  assert.equal(validateReleaseTag('v1.2.3', '1.2.3'), null);
  assert.equal(validateReleaseTag('v1.2.3-rc.1', '1.2.3-rc.1'), null);
  assert.ok(validateReleaseTag('1.2.3', '1.2.3'));
  assert.ok(validateReleaseTag('v1.2.4', '1.2.3'));
  console.log('Release self-test passed.');
}

const command = process.argv[2] ?? 'create';
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (command === 'self-test') selfTest();
  else if (command === 'create') await createRelease();
  else throw new Error(`Unknown command: ${command}`);
}
