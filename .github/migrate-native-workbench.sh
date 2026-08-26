#!/usr/bin/env bash
set -euo pipefail

: "${NODE_VERSION:=22.19.0}"
: "${PNPM_VERSION:=11.7.0}"
: "${WORKBENCH_VERSION:=1.0.0}"
: "${UPSTREAM_VERSION:=0.1.1-rc.2}"
: "${UPSTREAM_COMMIT:=b150a551b8d465e31e418e1b2eaf5e79bbb7d28e}"

printf '==> validating migration input\n'
test -f workbench-src/package.json
test -f workbench-src/apps/cli/package.json
node -e "const p=require('./workbench-src/package.json'); if(p.version!==process.env.UPSTREAM_VERSION) throw new Error('upstream version mismatch: '+p.version)"

rm -rf /tmp/workbench-native /tmp/workbench-source
mkdir -p /tmp/workbench-native /tmp/workbench-source

if [[ -d overlay/skills ]]; then
  cp -a overlay/skills /tmp/workbench-native/skills
else
  cp -a workbench-src/.workbench/overlay/skills /tmp/workbench-native/skills
fi

rsync -a \
  --exclude='.workbench' \
  --exclude='WORKBENCH_SOURCE.md' \
  --exclude='WORKBENCH_SOURCE_MANIFEST.json' \
  workbench-src/ /tmp/workbench-source/

mkdir -p /tmp/workbench-native/upstream-workflows
if [[ -d /tmp/workbench-source/.github/workflows ]]; then
  cp -a /tmp/workbench-source/.github/workflows/. /tmp/workbench-native/upstream-workflows/ || true
  rm -rf /tmp/workbench-source/.github/workflows
fi

printf '==> promoting complete source to repository root\n'
find . -mindepth 1 -maxdepth 1 ! -name .git -exec rm -rf {} +
rsync -a /tmp/workbench-source/ ./
mkdir -p .github/workflows .github/upstream-workflows
cp -a /tmp/workbench-native/upstream-workflows/. .github/upstream-workflows/ 2>/dev/null || true

printf '==> integrating workbench into native source\n'
python <<'PY'
from __future__ import annotations
import json
import shutil
from pathlib import Path

root = Path.cwd()
cli = root / 'apps/cli'
preset_root = cli / 'config/agent-presets'
source_preset = preset_root / 'code'
workbench = preset_root / 'workbench'

if workbench.exists():
    shutil.rmtree(workbench)
shutil.copytree(source_preset, workbench)

skill_src = Path('/tmp/workbench-native/skills')
skill_dst = workbench / 'skills'
skill_dst.mkdir(parents=True, exist_ok=True)
mapping = {
    'chatgpt-takeover-ops': 'workbench-ops',
    'repo-review': 'repo-review',
    'repo-quality-gate': 'repo-quality-gate',
    'docs-quality': 'docs-quality',
    'task-journal': 'task-journal',
}
for src_name, dst_name in mapping.items():
    src = skill_src / src_name
    if not src.is_dir():
        raise SystemExit(f'missing skill source: {src}')
    dst = skill_dst / dst_name
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)

ops = skill_dst / 'workbench-ops/SKILL.md'
text = ops.read_text()
text = text.replace('name: chatgpt-takeover-ops', 'name: workbench-ops')
text = text.replace('ChatGPT Takeover', '工作台')
text = text.replace('ChatGPT takeover', '工作台')
ops.write_text(text)

(workbench / 'preset.yml').write_text(
    'name: 工作台模式\n'
    'description: 完整本地执行工作台：原生工具 + Code Mode、持久终端、技能、LSP；本地执行管理不依赖 Harness 自身模型凭据。\n'
)

comp_path = workbench / 'agent.cordis.yml'
comp = comp_path.read_text()
old_persona = 'You are a coding agent powered by the {{model}} model. Your working directory is {{cwd}}.'
new_persona = (
    'This is the Workbench preset. The current working directory is {{cwd}}. '
    'Use deterministic local execution, persistent terminals, code navigation, skills, '
    'background jobs, and explicit verification. Local workbench management must not '
    'assume that a model provider or web-search credential is configured.'
)
if old_persona not in comp:
    raise SystemExit('code preset persona anchor not found')
comp = comp.replace(old_persona, new_persona, 1)

terminal_anchor = "- id: tool-jobs\n  name: '@deepseek-ai/dsh-tool-jobs'\n"
terminal_block = terminal_anchor + r'''

# ── persistent terminal ─────────────────────────────────────────────────────

- id: workbench-terminal
  name: cordis:group
  group: true
  isolate:
    terminals: true
  config:
    - id: terminal-service
      name: '@deepseek-ai/dsh-terminal'

    - id: terminal-bash
      name: '@deepseek-ai/dsh-terminal-bash'
      disabled: !!js process.platform === 'win32'
      config:
        timeoutMs: 300000

    - id: terminal-tools
      name: '@deepseek-ai/dsh-tool-terminal'
      config:
        enableRunInBackground: true
        maxResultBytes: 262144
'''
if terminal_anchor not in comp:
    raise SystemExit('terminal insertion anchor not found')
comp = comp.replace(terminal_anchor, terminal_block, 1)

skill_anchor = "- id: tool-skill\n  name: '@deepseek-ai/dsh-tool-skill'\n"
lsp_block = skill_anchor + r'''

# ── semantic code navigation (LSP) ─────────────────────────────────────────

- id: workbench-lsp
  name: cordis:group
  group: true
  isolate:
    lsp: true
  config:
    - id: lsp-service
      name: '@deepseek-ai/dsh-lsp'

    - id: lsp-stdio
      name: '@deepseek-ai/dsh-lsp-stdio'
      config:
        servers:
          typescript:
            command: !!js process.execPath
            args:
              - !!js process.env.DSH_WORKBENCH_TS_LAUNCHER ?? process.argv[1].replace(/[\\/]lib[\\/]bin\.js$/, '/config/workbench/typescript-language-server.mjs')
              - --stdio
            extensionToLanguage:
              .ts: typescript
              .tsx: typescriptreact
              .mts: typescript
              .cts: typescript
              .js: javascript
              .jsx: javascriptreact
              .mjs: javascript
              .cjs: javascript
          clangd:
            command: !!js process.env.DSH_CLANGD_BIN ?? 'clangd'
            args: [--background-index]
            extensionToLanguage:
              .c: c
              .h: c
              .cc: cpp
              .cpp: cpp
              .cxx: cpp
              .hh: cpp
              .hpp: cpp

    - id: tool-lsp
      name: '@deepseek-ai/dsh-tool-lsp'
      config:
        maxLocations: 100
'''
if skill_anchor not in comp:
    raise SystemExit('LSP insertion anchor not found')
comp = comp.replace(skill_anchor, lsp_block, 1)

if 'mode: code' not in comp:
    raise SystemExit('tool presentation mode anchor not found')
comp = comp.replace('mode: code', 'mode: both', 1)

web_anchor = "- id: tool-web\n  name: '@deepseek-ai/dsh-tool-web'"
if web_anchor not in comp:
    raise SystemExit('tool-web anchor not found')
comp = comp.replace(web_anchor, "- id: tool-web\n  name: '@deepseek-ai/dsh-tool-web'\n  disabled: true", 1)
comp_path.write_text(comp)

launchers = cli / 'config/workbench'
launchers.mkdir(parents=True, exist_ok=True)
(launchers / 'typescript-language-server.mjs').write_text("""#!/usr/bin/env node
import { createRequire } from 'node:module'
import { pathToFileURL } from 'node:url'
const require = createRequire(import.meta.url)
const entry = require.resolve('typescript-language-server')
await import(pathToFileURL(entry).href)
""")
(launchers / 'mcp-filesystem.mjs').write_text("""#!/usr/bin/env node
import { createRequire } from 'node:module'
import { pathToFileURL } from 'node:url'
const require = createRequire(import.meta.url)
const entry = require.resolve('@modelcontextprotocol/server-filesystem')
await import(pathToFileURL(entry).href)
""")

cli_pkg_path = cli / 'package.json'
cli_pkg = json.loads(cli_pkg_path.read_text())
deps = cli_pkg.setdefault('dependencies', {})
deps.update({
    '@deepseek-ai/dsh-lsp': 'workspace:^',
    '@deepseek-ai/dsh-lsp-stdio': 'workspace:^',
    '@deepseek-ai/dsh-tool-lsp': 'workspace:^',
    '@deepseek-ai/dsh-tool-terminal': 'workspace:^',
    '@modelcontextprotocol/server-filesystem': '2026.7.10',
    'typescript-language-server': '5.3.0',
    'typescript': '6.0.3',
})
cli_pkg['dependencies'] = dict(sorted(deps.items()))
cli_pkg_path.write_text(json.dumps(cli_pkg, indent=2, ensure_ascii=False) + '\n')

root_pkg_path = root / 'package.json'
root_pkg = json.loads(root_pkg_path.read_text())
root_pkg['workbench'] = {
    'version': '1.0.0',
    'upstreamVersion': '0.1.1-rc.2',
    'upstreamCommit': 'b150a551b8d465e31e418e1b2eaf5e79bbb7d28e',
    'defaultPreset': 'workbench',
    'architecture': 'native-source-integrated',
}
scripts = root_pkg.setdefault('scripts', {})
scripts['workbench:web'] = 'node apps/cli/lib/bin.js web --no-open'
scripts['workbench:doctor'] = 'node scripts/workbench-doctor.mjs'
root_pkg_path.write_text(json.dumps(root_pkg, indent=2, ensure_ascii=False) + '\n')

web_patch_path = root / 'packages/bundle/web-app/cordis.patch.yml'
web = web_patch_path.read_text()
old_sqlite = """- id: session-query-sqlite
  config:
    path: ':memory:'
    openAt: never
"""
new_sqlite = """- id: session-query-sqlite
  config:
    path: !!js dshHomePath('storages', 'session-search.sqlite')
    openAt: startup
"""
if old_sqlite not in web:
    raise SystemExit('session sqlite anchor not found')
web = web.replace(old_sqlite, new_sqlite, 1)
if 'default: standard' not in web:
    raise SystemExit('agent preset default anchor not found')
web = web.replace('default: standard', 'default: workbench', 1)

insert_anchor = "- insert:\n    - id: code-runtime\n"
mcp = r'''- insert:
    - id: workbench-mcp-filesystem
      name: '@deepseek-ai/dsh-mcp-client'
      config:
        serverName: localfs
        transport: stdio
        command: !!js process.execPath
        args:
          - !!js process.env.DSH_WORKBENCH_MCP_LAUNCHER ?? process.argv[1].replace(/[\\/]lib[\\/]bin\.js$/, '/config/workbench/mcp-filesystem.mjs')
          - !!js process.env.DSH_WORKBENCH_MCP_ROOT ?? process.cwd()
        cwd: !!js process.cwd()
        toolCallTimeoutMs: 60000
        failOnStartupError: true
        reconnect:
          enabled: true
          initialDelayMs: 500
          maxDelayMs: 30000
          maxAttempts: 10

    - id: code-runtime
'''
if insert_anchor not in web:
    raise SystemExit('web insert anchor not found')
web = web.replace(insert_anchor, mcp, 1)
web_patch_path.write_text(web)

(root / 'docs/workbench.zh.md').write_text("""# 工作台

本仓库是 DeepSeek Harness `0.1.1-rc.2` 的源码内生工作台版本。所有工作台能力直接进入正式源码目录，不依赖 `.workbench`、`overlay` 或用户 Home 覆盖层。

## 原生改造位置

- `apps/cli/config/agent-presets/workbench/`：默认工作台预设与 5 个内置技能。
- `apps/cli/config/workbench/`：TypeScript 语言服务器和本地 MCP 启动器。
- `apps/cli/package.json`：工作台运行依赖。
- `packages/bundle/web-app/cordis.patch.yml`：默认预设、持久全文索引与本地文件 MCP。
- `scripts/workbench-doctor.mjs`：源码/构建静态验收。
- `.github/workflows/workbench-ci.yml`：冷启动和功能链验收。
- `.github/workflows/permanent-toolchain.yml`：直接构建本仓库源码的永久离线产物。

## 默认能力

工作台预设包含原生文件与 Bash、后台任务、持久终端、Code Mode (`both`)、5 个技能、TypeScript/JavaScript 与 C/C++ LSP、目标/计划/TODO、子智能体和工作流入口。Web Host 默认启用持久 SQLite 会话全文索引与 `localfs` MCP。

本地文件 MCP 默认只开放启动工作目录；可通过 `DSH_WORKBENCH_MCP_ROOT` 指定允许根目录。`clangd` 可通过 `DSH_CLANGD_BIN` 指定。

## 使用

```bash
corepack enable
corepack prepare pnpm@11.7.0 --activate
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
DSH_WORKBENCH_MCP_ROOT="$PWD" pnpm run workbench:web
```

默认地址：`http://127.0.0.1:3080`。

工作台的本地执行、文件、终端、LSP、MCP、会话和索引能力不要求模型凭据；如果直接在 Harness 网页中让 Harness 自主生成模型回复，仍需另行配置模型提供方。
""")
(root / 'docs/workbench.md').write_text("""# Workbench

This repository is a source-integrated Workbench build of DeepSeek Harness 0.1.1-rc.2. Workbench capabilities live directly in the normal source tree; there is no sidecar `.workbench`, `overlay`, or required user-home patch layer.

See `workbench.zh.md` for the primary operations guide.
""")

doctor = root / 'scripts/workbench-doctor.mjs'
doctor.write_text("""#!/usr/bin/env node
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
  'docs/workbench.zh.md',
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
""")

for readme_name, banner in {
    'README.md': """# Workbench source-integrated fork

This `555` repository contains the complete DeepSeek Harness source plus the Workbench modifications directly in the normal source tree. See [`docs/workbench.zh.md`](docs/workbench.zh.md). No external overlay is required.\n\n---\n\n""",
    'README.zh.md': """# 工作台源码内生版

`555` 根目录就是完整的 DeepSeek Harness 工作台源码，所有改造已进入正式源码目录，不需要额外覆盖层。详细说明见 [`docs/workbench.zh.md`](docs/workbench.zh.md)。\n\n---\n\n""",
}.items():
    path = root / readme_name
    body = path.read_text()
    if not body.startswith(banner.splitlines()[0]):
        path.write_text(banner + body)
PY

printf '==> refreshing dependency lock and building\n'
pnpm install --no-frozen-lockfile
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor

printf '==> cold-starting with an empty DSH_HOME\n'
export DSH_HOME="${RUNNER_TEMP:-/tmp}/dsh-workbench-home"
export DSH_WORKBENCH_MCP_ROOT="$PWD"
rm -rf "$DSH_HOME"
mkdir -p "$DSH_HOME"
LOG="${RUNNER_TEMP:-/tmp}/workbench-native.log"
node apps/cli/lib/bin.js web --no-open > "$LOG" 2>&1 &
WB_PID=$!
cleanup() { kill "$WB_PID" 2>/dev/null || true; }
trap cleanup EXIT
for _ in $(seq 1 60); do
  curl -fsS http://127.0.0.1:3080/ >/dev/null 2>&1 && break
  sleep 1
done
curl -fsS http://127.0.0.1:3080/ >/dev/null

CREATE=$(curl -fsS -X POST http://127.0.0.1:3080/api/session.create \
  -H 'content-type: application/json' \
  --data "{\"type\":\"client-request\",\"rpcId\":\"ci-create\",\"method\":\"session.create\",\"payload\":{\"cwd\":\"$PWD\"}}")
echo "$CREATE"
SESSION_ID=$(node -e "const x=JSON.parse(process.argv[1]); if(!x.result?.ok) process.exit(2); if(x.result.value.agentPreset!=='workbench') process.exit(3); process.stdout.write(x.result.value.sessionId)" "$CREATE")

SKILLS=$(curl -fsS -X POST http://127.0.0.1:3080/api/skill.list \
  -H 'content-type: application/json' \
  --data "{\"type\":\"client-request\",\"rpcId\":\"ci-skills\",\"method\":\"skill.list\",\"payload\":{\"sessionId\":\"$SESSION_ID\"}}")
echo "$SKILLS"
node -e "const x=JSON.parse(process.argv[1]); const n=new Set(x.result?.value?.skills?.map(s=>s.name)||[]); for(const s of ['workbench-ops','repo-review','repo-quality-gate','docs-quality','task-journal']) if(!n.has(s)) throw new Error('missing skill '+s)" "$SKILLS"
test -f "$DSH_HOME/storages/session-search.sqlite"
if grep -E '(^| )ERROR|Unhandled|unhandledRejection' "$LOG"; then
  cat "$LOG"
  exit 1
fi
kill "$WB_PID"
wait "$WB_PID" || true
trap - EXIT

printf '==> writing final native CI workflows\n'
cat > .github/workflows/workbench-ci.yml <<'YAML'
name: 工作台源码验收
on:
  workflow_dispatch:
  push:
    branches: [main]
    paths:
      - 'apps/**'
      - 'packages/**'
      - 'scripts/**'
      - 'docs/workbench*'
      - 'package.json'
      - 'pnpm-lock.yaml'
      - '.github/workflows/workbench-ci.yml'
permissions:
  contents: read
jobs:
  build-and-smoke:
    runs-on: ubuntu-24.04
    timeout-minutes: 60
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22.19.0'
      - run: corepack enable && corepack prepare pnpm@11.7.0 --activate
      - run: pnpm install --frozen-lockfile
      - run: pnpm run build
      - run: pnpm run workbench:doctor
      - name: Cold-start workbench
        shell: bash
        run: |
          set -euo pipefail
          export DSH_HOME="$RUNNER_TEMP/dsh-home"
          export DSH_WORKBENCH_MCP_ROOT="$GITHUB_WORKSPACE"
          node apps/cli/lib/bin.js web --no-open > "$RUNNER_TEMP/workbench.log" 2>&1 &
          PID=$!
          trap 'kill $PID 2>/dev/null || true' EXIT
          for i in $(seq 1 60); do
            curl -fsS http://127.0.0.1:3080/ >/dev/null 2>&1 && break
            sleep 1
          done
          curl -fsS http://127.0.0.1:3080/ >/dev/null
YAML

cat > .github/workflows/permanent-toolchain.yml <<'YAML'
name: 构建工作台永久离线产物
on:
  workflow_dispatch:
  push:
    branches: [main]
    paths:
      - '.github/workflows/permanent-toolchain.yml'
      - 'apps/cli/config/agent-presets/workbench/**'
      - 'apps/cli/config/workbench/**'
      - 'packages/bundle/web-app/cordis.patch.yml'
      - 'apps/cli/package.json'
      - 'pnpm-lock.yaml'
permissions:
  contents: write
env:
  NODE_VERSION: '22.19.0'
  PNPM_VERSION: '11.7.0'
  WORKBENCH_VERSION: '1.0.0'
  RELEASE_TAG: 'workbench-1.0.0'
jobs:
  build-linux-x64:
    runs-on: ubuntu-24.04
    timeout-minutes: 90
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
      - run: corepack enable && corepack prepare "pnpm@${PNPM_VERSION}" --activate
      - run: pnpm install --frozen-lockfile
      - run: pnpm run build
      - run: pnpm run workbench:doctor
      - name: Package toolchain and ready source
        shell: bash
        run: |
          set -euo pipefail
          mkdir -p out/toolchain stage
          curl -fL --retry 5 "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz" -o "out/toolchain/node-v${NODE_VERSION}-linux-x64.tar.xz"
          (cd out/toolchain && npm pack "pnpm@${PNPM_VERSION}")
          rsync -a --exclude='.git' --exclude='out' ./ stage/workbench/
          cat > stage/workbench/OFFLINE_BUILD_INFO.txt <<INFO
          Workbench ${WORKBENCH_VERSION}
          source commit: ${GITHUB_SHA}
          Node.js: ${NODE_VERSION}
          pnpm: ${PNPM_VERSION}
          frozen dependencies: installed
          production build: completed
          default preset: workbench
          INFO
          tar -I 'zstd -10 -T0' -cf "out/workbench-${WORKBENCH_VERSION}-ready-linux-x64.tar.zst" -C stage workbench
          tar -I 'zstd -19 -T0' -cf "out/workbench-toolchain-linux-x64.tar.zst" -C out toolchain
          (cd out && sha256sum *.tar.zst > SHA256SUMS.txt)
      - uses: actions/upload-artifact@v4
        with:
          name: workbench-offline-linux-x64
          path: |
            out/workbench-1.0.0-ready-linux-x64.tar.zst
            out/workbench-toolchain-linux-x64.tar.zst
            out/SHA256SUMS.txt
          compression-level: 0
          retention-days: 90
      - name: Publish permanent release
        env:
          GH_TOKEN: ${{ github.token }}
        shell: bash
        run: |
          set -euo pipefail
          gh release view "$RELEASE_TAG" >/dev/null 2>&1 || gh release create "$RELEASE_TAG" --title "工作台 ${WORKBENCH_VERSION} 永久离线产物" --notes "由 555 根目录的源码内生工作台直接构建。"
          gh release upload "$RELEASE_TAG" out/*.tar.zst out/SHA256SUMS.txt --clobber
YAML

printf '==> final shape audit\n'
test -f package.json
test -f apps/cli/config/agent-presets/workbench/agent.cordis.yml
test -f packages/bundle/web-app/cordis.patch.yml
test -f docs/workbench.zh.md
test -f scripts/workbench-doctor.mjs
test ! -e workbench-src
test ! -e overlay
test ! -e .workbench
test ! -e bin
FILES=$(find . -path ./.git -prune -o -type f -print | wc -l | tr -d ' ')
echo "root source files: $FILES"
test "$FILES" -gt 7800
pnpm run workbench:doctor

printf '==> committing native integrated source\n'
git config user.name 'github-actions[bot]'
git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
git add -A
git commit -m 'feat: integrate workbench directly into complete source'
git push origin HEAD:main
