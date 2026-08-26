#!/usr/bin/env node
import fs from 'node:fs'
import path from 'node:path'

const root = process.cwd()
const cliManifestPath = path.join(root, 'apps/cli/package.json')

const required = [
  'apps/cli/config/agent-presets/workbench/agent.cordis.yml',
  'apps/cli/config/agent-presets/workbench/preset.yml',
  'apps/cli/config/agent-presets/workbench/skills/workbench-ops/SKILL.md',
  'apps/cli/config/agent-presets/workbench/skills/repo-review/SKILL.md',
  'apps/cli/config/agent-presets/workbench/skills/repo-quality-gate/SKILL.md',
  'apps/cli/config/agent-presets/workbench/skills/docs-quality/SKILL.md',
  'apps/cli/config/agent-presets/workbench/skills/task-journal/SKILL.md',
  'apps/cli/config/workbench/typescript-language-server.mjs',
  'apps/cli/config/workbench/mcp-filesystem.mjs',
  'packages/bundle/web-app/cordis.patch.yml',
  'README.md',
  'TECHNICAL.md',
]

for (const file of required) {
  if (!fs.existsSync(path.join(root, file))) throw new Error(`missing workbench source: ${file}`)
}

for (const legacy of ['workbench-src', '.workbench', 'overlay']) {
  if (fs.existsSync(path.join(root, legacy))) throw new Error(`legacy sidecar must not exist: ${legacy}`)
}

const [nodeMajor, nodeMinor] = process.versions.node.split('.').map(Number)
if ((nodeMajor === 22 && nodeMinor < 19) || nodeMajor === 23 || nodeMajor < 22) {
  throw new Error(`unsupported Node.js ${process.versions.node}; require ^22.19.0 or >=24.0.0`)
}

const pkg = JSON.parse(fs.readFileSync(path.join(root, 'package.json'), 'utf8'))
if (pkg.workbench?.architecture !== 'native-source-integrated') throw new Error('workbench metadata missing')

const cli = JSON.parse(fs.readFileSync(cliManifestPath, 'utf8'))
const declaredRuntimeDeps = [
  '@deepseek-ai/dsh-app-boot',
  '@deepseek-ai/dsh-mcp-client',
  '@deepseek-ai/dsh-lsp',
  '@deepseek-ai/dsh-lsp-stdio',
  '@deepseek-ai/dsh-tool-lsp',
  '@deepseek-ai/dsh-tool-terminal',
  '@modelcontextprotocol/server-filesystem',
  'typescript-language-server',
]
for (const dep of declaredRuntimeDeps) {
  if (!cli.dependencies?.[dep]) throw new Error(`missing runtime dependency declaration: ${dep}`)
}

for (const dep of declaredRuntimeDeps) {
  const depRoot = path.join(root, 'apps/cli/node_modules', ...dep.split('/'))
  try {
    const resolvedRoot = fs.realpathSync(depRoot)
    if (!fs.existsSync(path.join(resolvedRoot, 'package.json'))) {
      throw new Error('package.json missing')
    }
  } catch (error) {
    throw new Error(`unresolvable runtime dependency: ${dep}`, { cause: error })
  }
}

const runtimeEntries = [
  'apps/cli/node_modules/@modelcontextprotocol/server-filesystem/dist/index.js',
  'apps/cli/node_modules/typescript-language-server/lib/cli.mjs',
]
for (const entry of runtimeEntries) {
  if (!fs.existsSync(path.join(root, entry))) throw new Error(`missing runtime entry: ${entry}`)
}

const preset = fs.readFileSync(path.join(root, required[0]), 'utf8')
for (const marker of ['terminal-tools', 'lsp-stdio', 'mode: both']) {
  if (!preset.includes(marker)) throw new Error(`workbench preset marker missing: ${marker}`)
}

const web = fs.readFileSync(path.join(root, 'packages/bundle/web-app/cordis.patch.yml'), 'utf8')
for (const marker of ['default: workbench', 'workbench-mcp-filesystem', "dshHomePath('storages', 'session-search.sqlite')"]) {
  if (!web.includes(marker)) throw new Error(`web workbench marker missing: ${marker}`)
}

console.log(`workbench doctor: OK (Node.js ${process.versions.node})`)
