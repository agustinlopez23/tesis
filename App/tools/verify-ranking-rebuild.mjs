import { createHash, randomUUID } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { cp, lstat, mkdir, readFile, readdir, rename, writeFile } from 'node:fs/promises';
import { dirname, isAbsolute, join, relative, resolve, sep, win32 } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';

const sha = bytes => createHash('sha256').update(bytes).digest('hex');
async function fileHash(path) {
  const hash = createHash('sha256');
  for await (const chunk of createReadStream(path)) hash.update(chunk);
  return hash.digest('hex');
}
async function noLinks(path) {
  for (let current = resolve(path); ; current = dirname(current)) {
    const info = await lstat(current).catch(e => { if (e.code !== 'ENOENT') throw e; });
    if (info?.isSymbolicLink()) throw new Error('Linked artifact path is not allowed');
    if (dirname(current) === current) break;
  }
}
function safePath(root, name) {
  if (isAbsolute(name) || name.includes('\\') || name.split('/').some(x => !x || x === '..' || x === '.' || /[<>:"|?*\x00-\x1f]/.test(x) || /[ .]$/.test(x)))
    throw new Error('Invalid artifact path');
  return join(root, ...name.split('/'));
}
async function inventory(root) {
  await noLinks(root);
  const entries = [];
  async function walk(folder) {
    for (const entry of await readdir(folder, { withFileTypes: true })) {
      const path = join(folder, entry.name);
      if (entry.isSymbolicLink()) throw new Error('Linked artifact path is not allowed');
      if (entry.isDirectory()) await walk(path);
      else entries.push([relative(root, path).split(sep).join('/'), await fileHash(path)]);
    }
  }
  await walk(root);
  return Object.fromEntries(entries.sort(([a], [b]) => a < b ? -1 : a > b ? 1 : 0));
}
export async function verifyDraft(directory, digest) {
  await noLinks(directory);
  await noLinks(join(directory, 'manifest.json'));
  const bytes = await readFile(join(directory, 'manifest.json'));
  if (sha(bytes) !== digest.toLowerCase()) throw new Error('External manifest pin/hash mismatch');
  const manifest = JSON.parse(bytes);
  if (manifest.schema !== 'business-freeze-draft-v1' || manifest.status !== 'draft' || manifest.policyFrozen !== false ||
      manifest.evaluationAuthorized !== false || !manifest.files || Array.isArray(manifest.files)) throw new Error('Invalid draft manifest');
  const seen = new Set();
  for (const [name, hash] of Object.entries(manifest.files)) {
    safePath(directory, name);
    if (seen.has(name.toLowerCase()) || name === 'manifest.json' || !/^[a-f0-9]{64}$/.test(hash)) throw new Error('Invalid artifact inventory');
    seen.add(name.toLowerCase());
  }
  const actual = await inventory(directory); delete actual['manifest.json'];
  if (Object.keys(actual).length !== seen.size || Object.entries(actual).some(([name, hash]) => manifest.files[name] !== hash))
    throw new Error('Artifact inventory mismatch');
  return manifest;
}
export async function compareFiles(expected, actual, names) {
  const left = await inventory(expected), right = await inventory(actual);
  return [...(names ?? new Set([...Object.keys(left), ...Object.keys(right)]))].sort().map(name => ({ name,
    expectedSha256: left[name] ?? null, actualSha256: right[name] ?? null,
    matches: left[name] !== undefined && left[name] === right[name] }));
}
export function buildOrigin(assemblies) {
  const roots = assemblies.map(({ project, bytes }) => {
    const magic = Buffer.from('RSDS'), start = bytes.indexOf(magic), end = bytes.indexOf(0, start + 24);
    if (start < 0 || end < start + 24 || bytes.indexOf(magic, start + 1) >= 0) throw new Error('Ambiguous or missing CodeView path');
    const pdb = bytes.subarray(start + 24, end).toString('utf8');
    const suffix = `\\App\\backend\\${project === 'Marketplace.Evaluation' ? 'tools' : 'src'}\\${project}\\obj\\Release\\net10.0\\${project}.pdb`;
    if (!pdb.endsWith(suffix)) throw new Error('Unexpected recorded build path');
    const root = pdb.slice(0, -suffix.length);
    if (!win32.isAbsolute(root) || /[,=]/.test(root)) throw new Error('Build root is not safely mappable');
    return root;
  });
  if (!roots.length || roots.some(r => r !== roots[0])) throw new Error('Recorded build roots disagree');
  return roots[0];
}

async function main(args) {
  const options = {};
  for (let i = 0; i < args.length; i += 2) {
    if (!['--draft', '--manifest-sha256', '--dotnet', '--output'].includes(args[i]) || !args[i + 1] || options[args[i]]) throw new Error('Invalid/repeated option');
    options[args[i]] = args[i + 1];
  }
  if (Object.keys(options).length !== 4 || process.platform !== 'win32') throw new Error('Use --draft --manifest-sha256 --dotnet --output on Windows');
  const repository = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
  const draft = resolve(options['--draft']), output = resolve(options['--output']), dotnet = resolve(options['--dotnet']);
  const manifest = await verifyDraft(draft, options['--manifest-sha256']);
  const overlaps = (a, b) => a === b || a.startsWith(b + sep) || b.startsWith(a + sep);
  if (overlaps(output.toLowerCase(), draft.toLowerCase()) || !output.startsWith(repository + sep)) throw new Error('Output must be separate and within the repository');
  await noLinks(output); await noLinks(dotnet);
  if (await lstat(output).catch(e => { if (e.code !== 'ENOENT') throw e; })) throw new Error('Output must not exist');
  const projects = ['Marketplace.Evaluation', 'Marketplace.BLL', 'Marketplace.Models', 'Marketplace.DTOs'];
  const origin = buildOrigin(await Promise.all(projects.map(async project => ({ project, bytes: await readFile(join(draft, 'runtime', project + '.dll')) }))));
  const work = join(repository, `ranking-rebuild-work-${randomUUID()}`);
  await mkdir(output); await mkdir(join(output, 'logs'));
  const commands = [];
  const env = { ...process.env, DOTNET_ROOT: dirname(dotnet), DOTNET_CLI_HOME: join(output, 'cli'),
    DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE: 'true' };
  async function run(name, executable, arguments_, cwd, allowed = [0]) {
    const entry = { name, executable, arguments: arguments_, cwd, exitCode: null }; commands.push(entry);
    const result = await new Promise((done, reject) => {
      const child = spawn(executable, arguments_, { cwd, env, windowsHide: true, shell: false });
      let text = ''; child.stdout.on('data', chunk => { text += chunk; }); child.stderr.on('data', chunk => { text += chunk; });
      child.once('error', reject); child.once('close', code => done({ code, text }));
    });
    entry.exitCode = result.code;
    await writeFile(join(output, 'logs', name + '.log'), result.text, { flag: 'wx' });
    if (!allowed.includes(result.code)) throw new Error(`${name} failed (${result.code}); see logs`);
    return result;
  }
  let report = { schema: 'business-freeze-rebuild-v1', status: 'error', manifestSha256: options['--manifest-sha256'],
    sourceTreeSha256: manifest.sourceTreeSha256, policyFrozen: false, evaluationAuthorized: false, origin, commands };
  try {
    // An ignored work directory changes EmbeddedFiles in the SDK's portable PDBs.
    await mkdir(work);
    await cp(join(draft, 'sources'), join(work, 'sources'), { recursive: true, errorOnExist: true, force: false });
    const sourceRoot = join(work, 'sources'), backend = join(sourceRoot, 'App/backend');
    await writeFile(join(work, 'NuGet.Config'), '<configuration><packageSources><clear /></packageSources></configuration>');
    await run('git-ignore', 'git', ['check-ignore', '--no-index', join(sourceRoot, 'App/backend/tools/Marketplace.Evaluation/Program.cs')], repository, [1]);
    const revision = (await run('git-revision', 'git', ['rev-parse', 'HEAD'], repository)).text.trim();
    const version = (await run('sdk-version', dotnet, ['--version'], backend)).text.trim();
    const requested = JSON.parse(await readFile(join(backend, 'global.json'), 'utf8')).sdk.version;
    if (version !== requested) throw new Error('SDK version differs from snapshot');
    const project = 'tools/Marketplace.Evaluation/Marketplace.Evaluation.csproj';
    await run('restore', dotnet, ['restore', project, '--configfile', join(work, 'NuGet.Config'), '-p:NuGetAudit=false', '-m:1'], backend);
    await run('build', dotnet, ['build', project, '--configuration', 'Release', '--no-restore', '-m:1', `-p:PathMap=${sourceRoot}=${origin}`], backend);
    const rebuilt = join(backend, 'tools/Marketplace.Evaluation/bin/Release/net10.0');
    const runtime = await compareFiles(join(draft, 'runtime'), rebuilt, Object.keys(await inventory(join(draft, 'runtime'))));
    const replay = await run('replay', dotnet, [join(rebuilt, 'Marketplace.Evaluation.dll'), '--calibrate-review', join(draft, 'review-package'),
      '--package-sha256', manifest.packageSha256, '--output', join(output, 'replay')], backend, [0, 2]);
    const calibration = await compareFiles(join(draft, 'calibration'), join(output, 'replay'));
    await verifyDraft(draft, options['--manifest-sha256']);
    const sourceInventory = await inventory(sourceRoot);
    if (Object.entries(manifest.files).filter(([p]) => p.startsWith('sources/')).some(([p, hash]) => sourceInventory[p.slice(8)] !== hash))
      throw new Error('Captured sources changed while rebuilding');
    report = { ...report, status: runtime.every(x => x.matches) && calibration.every(x => x.matches) ? 'matches' : 'differs',
      sdkVersion: version, dotnetSha256: await fileHash(dotnet), gitRevision: revision, runtime, calibration,
      replayExitCode: replay.code, replayScope: replay.code === 2 ? 'pending-review-only' : 'complete-calibration',
      originalDraftUnchanged: true, buildSourceBytesUnchanged: true };
  } catch (error) { report.error = error.message; process.exitCode = 1; }
  // Move only the unique directory created by this run, after checking both absolute paths.
  const savedWork = join(output, 'work');
  if (work.startsWith(repository + sep + 'ranking-rebuild-work-') && savedWork.startsWith(output + sep)) {
    await noLinks(work); await noLinks(savedWork);
    if (await lstat(work).catch(e => { if (e.code !== 'ENOENT') throw e; })) await rename(work, savedWork);
  } else throw new Error('Unexpected work archive path');
  await writeFile(join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', { flag: 'wx' });
  await writeFile(join(output, 'sha256.json'), JSON.stringify(await inventory(output), null, 2) + '\n', { flag: 'wx' });
  if (report.status !== 'matches') process.exitCode = 1;
  console.log(`Reconstrucción: ${report.status}; política no congelada. Ver ${join(output, 'report.json')}`);
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url))
  main(process.argv.slice(2)).catch(error => { console.error(error.message); process.exitCode = 1; });
