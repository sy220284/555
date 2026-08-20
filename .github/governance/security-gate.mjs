/* global console, process */
import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const baselinePath = path.join(here, 'security-baseline.json');
const severityRank = new Map([['info', 0], ['low', 1], ['moderate', 2], ['high', 3], ['critical', 4]]);

export function auditRegressions(audit, baseline) {
  const errors = [];
  const accepted = new Map((baseline?.acceptedAdvisories ?? []).map((entry) => [entry.id, entry]));
  const current = Object.values(audit?.advisories ?? {});
  for (const advisory of current) {
    const id = advisory.github_advisory_id;
    const severity = advisory.severity;
    if (!id || !severityRank.has(severity)) {
      errors.push(`Malformed advisory record: ${id ?? '<missing>'}`);
      continue;
    }
    if (severity === 'critical') errors.push(`Critical production advisory is forbidden: ${id}`);
    const previous = accepted.get(id);
    if (!previous) errors.push(`New production advisory is not baselined: ${id} (${severity})`);
    else if ((severityRank.get(severity) ?? 99) > (severityRank.get(previous.severity) ?? -1)) {
      errors.push(`Production advisory severity escalated: ${id} ${previous.severity} -> ${severity}`);
    }
  }
  return errors;
}

export function workflowPinErrors(relative, content) {
  const errors = [];
  if (!/^permissions:\s*$/mu.test(content)) errors.push(`${relative} must declare top-level permissions`);
  for (const match of content.matchAll(/uses:\s*([^\s@]+)@([^\s#]+)/gu)) {
    if (!/^[0-9a-f]{40}$/iu.test(match[2])) errors.push(`${relative} contains an unpinned action: ${match[1]}@${match[2]}`);
  }
  return errors;
}

async function scanRepository(root = process.cwd()) {
  const workflowDir = path.join(root, '.github/workflows');
  const files = (await readdir(workflowDir)).filter((file) => /\.ya?ml$/u.test(file)).sort();
  const errors = [];
  for (const file of files) {
    const relative = `.github/workflows/${file}`;
    errors.push(...workflowPinErrors(relative, await readFile(path.join(workflowDir, file), 'utf8')));
  }
  if (errors.length > 0) throw new Error(errors.join('\n'));
  console.log(`Security repository scan passed for ${files.length} workflows.`);
}

async function verifyAudit(file) {
  const baseline = JSON.parse(await readFile(baselinePath, 'utf8'));
  const audit = JSON.parse(await readFile(file, 'utf8'));
  const errors = auditRegressions(audit, baseline);
  if (errors.length > 0) throw new Error(errors.join('\n'));
  console.log(`Production dependency audit accepted ${Object.keys(audit.advisories ?? {}).length} known advisory records and found no regression.`);
}

function selfTest() {
  const baseline = { acceptedAdvisories: [{ id: 'GHSA-old', severity: 'moderate' }] };
  assert.deepEqual(auditRegressions({ advisories: { 1: { github_advisory_id: 'GHSA-old', severity: 'low' } } }, baseline), []);
  assert.equal(auditRegressions({ advisories: { 1: { github_advisory_id: 'GHSA-new', severity: 'low' } } }, baseline).length, 1);
  assert.equal(auditRegressions({ advisories: { 1: { github_advisory_id: 'GHSA-old', severity: 'high' } } }, baseline).length, 1);
  assert.deepEqual(workflowPinErrors('x.yml', 'permissions:\n  contents: read\nsteps:\n  - uses: actions/checkout@' + 'a'.repeat(40)), []);
  assert.equal(workflowPinErrors('x.yml', 'steps:\n  - uses: actions/checkout@v7').length, 2);
  console.log('Security gate self-test passed.');
}

const command = process.argv[2] ?? 'scan-repository';
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  if (command === 'self-test') selfTest();
  else if (command === 'scan-repository') await scanRepository();
  else if (command === 'audit') await verifyAudit(process.argv[3]);
  else throw new Error(`Unknown command: ${command}`);
}
