#!/usr/bin/env node
import fs from 'node:fs'
import path from 'node:path'
const root = process.cwd()
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
]
for (const file of required) {
  if (!fs.existsSync(path.join(root, file))) throw new Error(`missing workbench source: ${file}`)
}
for (const legacy of ['workbench-src', '.workbench', 'overlay']) {
  if (fs.existsSync(path.join(root, legacy))) throw new Error(`legacy sidecar must not exist: ${legacy}`)
}
const pkg = JSON.parse(fs.readFileSync(path.join(root, 'package.json'), 'utf8'))
if (pkg.workbench?.architecture !== 'native-source-integrated') throw new Error('workbench metadata missing')
const cli = JSON.parse(fs.readFileSync(path.join(root, 'apps/cli/package.json'), 'utf8'))
for (const dep of ['@deepseek-ai/dsh-lsp','@deepseek-ai/dsh-lsp-stdio','@deepseek-ai/dsh-tool-lsp','@deepseek-ai/dsh-tool-terminal','@modelcontextprotocol/server-filesystem','typescript-language-server']) {
  if (!cli.dependencies?.[dep]) throw new Error(`missing runtime dependency: ${dep}`)
}
const preset = fs.readFileSync(path.join(root, required[0]), 'utf8')
for (const marker of ['terminal-tools', 'lsp-stdio', 'mode: both']) {
  if (!preset.includes(marker)) throw new Error(`workbench preset marker missing: ${marker}`)
}
const web = fs.readFileSync(path.join(root, 'packages/bundle/web-app/cordis.patch.yml'), 'utf8')
for (const marker of ['default: workbench', 'workbench-mcp-filesystem', "dshHomePath('storages', 'session-search.sqlite')"]) {
  if (!web.includes(marker)) throw new Error(`web workbench marker missing: ${marker}`)
}
console.log('workbench doctor: OK')
