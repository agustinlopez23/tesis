import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm, unlink } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { verifyDraft, compareFiles, buildOrigin } from './verify-ranking-rebuild.mjs';

const hash = bytes => createHash('sha256').update(bytes).digest('hex');
async function fixture(t) {
  const root = await mkdtemp(join(tmpdir(), 'rebuild-check-'));
  t.after(async () => {
    assert.ok(resolve(root).startsWith(resolve(tmpdir()) + '\\') || resolve(root).startsWith(resolve(tmpdir()) + '/'));
    await rm(root, { recursive: true });
  });
  const draft = join(root, 'draft'); await mkdir(draft);
  const manifest = { schema: 'business-freeze-draft-v1', status: 'draft', policyFrozen: false, evaluationAuthorized: false,
    files: { 'source.cs': hash('source') } };
  const bytes = JSON.stringify(manifest);
  await writeFile(join(draft, 'source.cs'), 'source'); await writeFile(join(draft, 'manifest.json'), bytes);
  return { root, draft, digest: hash(bytes), manifest };
}
test('validates the external pin and original inventory without modifying files', async t => {
  const f = await fixture(t);
  assert.deepEqual(await verifyDraft(f.draft, f.digest), f.manifest);
  assert.equal(await readFile(join(f.draft, 'source.cs'), 'utf8'), 'source');
  await assert.rejects(verifyDraft(f.draft, '0'.repeat(64)), /pin|hash/i);
});
for (const mode of ['changed', 'extra', 'missing']) test(`rejects ${mode} artifacts`, async t => {
  const f = await fixture(t);
  if (mode === 'changed') await writeFile(join(f.draft, 'source.cs'), 'tampered');
  if (mode === 'extra') await writeFile(join(f.draft, 'extra.cs'), 'extra');
  if (mode === 'missing') await unlink(join(f.draft, 'source.cs'));
  await assert.rejects(verifyDraft(f.draft, f.digest), /inventory|artifact/i);
});
test('a pinned path traversal cannot escape the draft', async t => {
  const f = await fixture(t);
  f.manifest.files = { '../outside.cs': hash('outside') };
  const bytes = JSON.stringify(f.manifest); await writeFile(join(f.draft, 'manifest.json'), bytes);
  await assert.rejects(verifyDraft(f.draft, hash(bytes)), /path/i);
});
test('comparison reports exact changes, missing and extra files; selected runtime names are explicit', async t => {
  const { root, draft } = await fixture(t);
  const other = join(root, 'other'); await mkdir(other);
  await writeFile(join(other, 'source.cs'), 'different'); await writeFile(join(other, 'extra'), 'extra');
  const rows = await compareFiles(draft, other);
  assert.equal(rows.length, 3); assert.ok(rows.every(x => !x.matches));
  const selected = await compareFiles(draft, other, ['source.cs']);
  assert.equal(selected.length, 1); assert.notEqual(selected[0].expectedSha256, selected[0].actualSha256);
});
function dll(project, root = 'C:\\repo') {
  const area = project === 'Marketplace.Evaluation' ? 'tools' : 'src';
  const path = `${root}\\App\\backend\\${area}\\${project}\\obj\\Release\\net10.0\\${project}.pdb`;
  return { project, bytes: Buffer.concat([Buffer.from('RSDS'), Buffer.alloc(20), Buffer.from(path), Buffer.from([0])]) };
}
test('relocation origin must agree across all recorded DLL paths', () => {
  const projects = ['Marketplace.Evaluation', 'Marketplace.BLL', 'Marketplace.Models', 'Marketplace.DTOs'];
  assert.equal(buildOrigin(projects.map(p => dll(p))), 'C:\\repo');
  assert.throws(() => buildOrigin([dll(projects[0]), dll(projects[1], 'C:\\different')]));
  assert.throws(() => buildOrigin([{ project: projects[0], bytes: Buffer.from('missing RSDS') }]));
  assert.throws(() => buildOrigin([dll(projects[0], 'relative')]));
});
