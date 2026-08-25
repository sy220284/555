#!/usr/bin/env bash
set -euo pipefail

REPO_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
WORKBENCH_VERSION=$(cat "$REPO_DIR/VERSION")
HARNESS_VERSION='0.1.1-rc.2'
HARNESS_SHA='b150a551b8d465e31e418e1b2eaf5e79bbb7d28e'
NODE_VERSION='22.19.0'
PNPM_VERSION='11.7.0'
RELEASE_TAG='dsh-offline-0.1.1-rc.2'
RELEASE_BASE="https://github.com/sy220284/555/releases/download/${RELEASE_TAG}"
HARNESS_ARCHIVE="deepseek-harness-${HARNESS_VERSION}-ready-linux-x64.tar.zst"
TOOLCHAIN_ARCHIVE='dsh-toolchain-linux-x64.tar.zst'

WORKBENCH_ROOT=${WORKBENCH_ROOT:-/mnt/data/workbench}
DSH_ROOT=${DSH_ROOT:-/mnt/data/dsh-direct/dsh}
RUNTIME_ROOT=${RUNTIME_ROOT:-/mnt/data/dsh-runtime}
NODE_BIN=${NODE_BIN:-${RUNTIME_ROOT}/node/bin/node}
DSH_HOME=${DSH_HOME:-${HOME}/.dsh}
MCP_ALLOWED_ROOT=${MCP_ALLOWED_ROOT:-/mnt/data}
OFFLINE_DIR=''
SKIP_RUNTIME=0
ALLOW_PARTIAL=0

usage() {
  cat <<USAGE
用法：$0 [选项]

选项：
  --offline-dir DIR   从本地目录读取永久产物，不访问 GitHub
  --skip-runtime      不安装 Harness/Node，只安装工作台覆盖层
  --workbench-root P  工作台管理目录，默认 /mnt/data/workbench
  --dsh-root P        Harness 目录，默认 /mnt/data/dsh-direct/dsh
  --runtime-root P    Node 工具链目录，默认 /mnt/data/dsh-runtime
  --dsh-home P        Harness 用户目录，默认 \$HOME/.dsh
  --mcp-root P        MCP 可访问根目录，默认 /mnt/data
  --allow-partial     缺少 clangd 时允许继续（C/C++ LSP 将不可用）
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --offline-dir) OFFLINE_DIR=$2; shift 2 ;;
    --skip-runtime) SKIP_RUNTIME=1; shift ;;
    --workbench-root) WORKBENCH_ROOT=$2; shift 2 ;;
    --dsh-root) DSH_ROOT=$2; shift 2 ;;
    --runtime-root) RUNTIME_ROOT=$2; NODE_BIN="$2/node/bin/node"; shift 2 ;;
    --dsh-home) DSH_HOME=$2; shift 2 ;;
    --mcp-root) MCP_ALLOWED_ROOT=$2; shift 2 ;;
    --allow-partial) ALLOW_PARTIAL=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "未知参数：$1" >&2; usage >&2; exit 2 ;;
  esac
done

need() { command -v "$1" >/dev/null 2>&1 || { echo "缺少命令：$1" >&2; exit 1; }; }
need tar; need zstd; need curl; need jq; need sha256sum; need sed; need find

mkdir -p "$WORKBENCH_ROOT" "$DSH_HOME" "$DSH_HOME/backups"
BACKUP_DIR="$DSH_HOME/backups/workbench-$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$BACKUP_DIR"

backup_one() {
  local src=$1 rel=$2
  [[ -e "$src" || -L "$src" ]] || return 0
  mkdir -p "$BACKUP_DIR/$(dirname "$rel")"
  cp -a "$src" "$BACKUP_DIR/$rel"
}
backup_one "$DSH_HOME/settings.yaml" settings.yaml
backup_one "$DSH_HOME/profiles/web/cordis.patch.yml" profiles/web/cordis.patch.yml
backup_one "$DSH_HOME/.agent-presets/chatgpt-takeover" .agent-presets/chatgpt-takeover
for s in chatgpt-takeover-ops repo-review repo-quality-gate docs-quality task-journal; do
  backup_one "$DSH_HOME/skills/$s" "skills/$s"
done

acquire() {
  local name=$1 out="$WORKBENCH_ROOT/cache/$1"
  mkdir -p "$WORKBENCH_ROOT/cache"
  if [[ -n "$OFFLINE_DIR" ]]; then
    [[ -f "$OFFLINE_DIR/$name" ]] || { echo "离线目录缺少：$OFFLINE_DIR/$name" >&2; exit 1; }
    cp -f "$OFFLINE_DIR/$name" "$out"
  elif [[ ! -f "$out" ]]; then
    echo "下载永久产物：$name"
    curl -fL --retry 5 --retry-delay 2 "$RELEASE_BASE/$name" -o "$out"
  fi
  echo "$out"
}

verify_batch_checksum() {
  local file=$1 sums=$2 name expected actual
  name=$(basename "$file")
  expected=$(awk -v n="$name" '$2==n {print $1; exit}' "$sums")
  [[ -n "$expected" ]] || { echo "校验清单中没有：$name" >&2; exit 1; }
  actual=$(sha256sum "$file" | awk '{print $1}')
  [[ "$actual" == "$expected" ]] || { echo "SHA-256 校验失败：$file" >&2; echo "expected=$expected actual=$actual" >&2; exit 1; }
}

install_runtime() {
  if [[ -f "$DSH_ROOT/apps/cli/lib/bin.js" && -x "$NODE_BIN" ]]; then
    echo "检测到现有已构建运行时，跳过基础运行时安装。"
    return 0
  fi
  local h t sums tmp
  h=$(acquire "$HARNESS_ARCHIVE")
  t=$(acquire "$TOOLCHAIN_ARCHIVE")
  sums=$(acquire 'SHA256SUMS.txt')
  verify_batch_checksum "$h" "$sums"
  verify_batch_checksum "$t" "$sums"
  tmp=$(mktemp -d)
  trap 'rm -rf "$tmp"' RETURN
  tar -I zstd -xf "$h" -C "$tmp"
  [[ -d "$tmp/dsh" ]] || { echo 'Harness 永久产物结构异常：缺少 dsh/' >&2; exit 1; }
  mkdir -p "$(dirname "$DSH_ROOT")"
  rm -rf "$DSH_ROOT"
  mv "$tmp/dsh" "$DSH_ROOT"

  rm -rf "$tmp/toolchain"
  mkdir -p "$tmp/toolchain"
  tar -I zstd -xf "$t" -C "$tmp/toolchain"
  local node_tar
  node_tar=$(find "$tmp/toolchain" -name "node-v${NODE_VERSION}-linux-x64.tar.xz" -print -quit)
  [[ -n "$node_tar" ]] || { echo '工具链永久产物缺少 Node.js 压缩包' >&2; exit 1; }
  mkdir -p "$RUNTIME_ROOT"
  rm -rf "$RUNTIME_ROOT/node"
  tar -xJf "$node_tar" -C "$RUNTIME_ROOT"
  mv "$RUNTIME_ROOT/node-v${NODE_VERSION}-linux-x64" "$RUNTIME_ROOT/node"
  mkdir -p "$RUNTIME_ROOT/toolchain"
  cp "$tmp/toolchain"/toolchain/pnpm-${PNPM_VERSION}.tgz "$RUNTIME_ROOT/toolchain/" 2>/dev/null || true
  trap - RETURN
  rm -rf "$tmp"
}

if [[ "$SKIP_RUNTIME" -eq 0 ]]; then install_runtime; fi
[[ -f "$DSH_ROOT/apps/cli/lib/bin.js" ]] || { echo "Harness 未就绪：$DSH_ROOT" >&2; exit 1; }
[[ -x "$NODE_BIN" ]] || { echo "Node.js 未就绪：$NODE_BIN" >&2; exit 1; }

ACTUAL_VERSION=$(cd "$DSH_ROOT" && "$NODE_BIN" -e "process.stdout.write(require('./package.json').version)")
[[ "$ACTUAL_VERSION" == "$HARNESS_VERSION" ]] || { echo "Harness 版本不匹配：需要 $HARNESS_VERSION，当前 $ACTUAL_VERSION" >&2; exit 1; }

MCP_SERVER=$(find "$DSH_ROOT/node_modules/.pnpm" -path '*/@modelcontextprotocol/server-filesystem/dist/index.js' -print -quit)
TS_SERVER=$(find "$DSH_ROOT/node_modules/.pnpm" -path '*/typescript-language-server/lib/cli.mjs' -print -quit)
[[ -n "$MCP_SERVER" ]] || { echo '找不到 MCP filesystem 服务端' >&2; exit 1; }
[[ -n "$TS_SERVER" ]] || { echo '找不到 TypeScript 语言服务器' >&2; exit 1; }
CLANGD_BIN=$(command -v clangd || true)
if [[ -z "$CLANGD_BIN" && "$ALLOW_PARTIAL" -eq 0 ]]; then
  echo '找不到 clangd；使用 --allow-partial 可跳过 C/C++ LSP。' >&2
  exit 1
fi
[[ -n "$CLANGD_BIN" ]] || CLANGD_BIN=/bin/false

render() {
  local src=$1 dst=$2
  local a b c d e f g
  a=$(printf '%s' "$DSH_ROOT" | sed 's/[&|]/\\&/g')
  b=$(printf '%s' "$NODE_BIN" | sed 's/[&|]/\\&/g')
  c=$(printf '%s' "$CLANGD_BIN" | sed 's/[&|]/\\&/g')
  d=$(printf '%s' "$DSH_HOME" | sed 's/[&|]/\\&/g')
  e=$(printf '%s' "$MCP_ALLOWED_ROOT" | sed 's/[&|]/\\&/g')
  f=$(printf '%s' "$MCP_SERVER" | sed 's/[&|]/\\&/g')
  g=$(printf '%s' "$TS_SERVER" | sed 's/[&|]/\\&/g')
  sed -e "s|__DSH_ROOT__|$a|g" -e "s|__NODE_BIN__|$b|g" -e "s|__CLANGD_BIN__|$c|g" -e "s|__DSH_HOME__|$d|g" -e "s|__MCP_ALLOWED_ROOT__|$e|g" -e "s|__MCP_SERVER__|$f|g" -e "s|__TS_SERVER__|$g|g" "$src" > "$dst"
}

mkdir -p "$DSH_HOME/.agent-presets/chatgpt-takeover" "$DSH_HOME/profiles/web" "$DSH_HOME/skills" "$DSH_HOME/storages"
cp "$REPO_DIR/overlay/agent-presets/chatgpt-takeover/preset.yml" "$DSH_HOME/.agent-presets/chatgpt-takeover/preset.yml"
render "$REPO_DIR/overlay/agent-presets/chatgpt-takeover/agent.cordis.template.yml" "$DSH_HOME/.agent-presets/chatgpt-takeover/agent.cordis.yml"
render "$REPO_DIR/overlay/profile/cordis.patch.template.yml" "$DSH_HOME/profiles/web/cordis.patch.yml"
cp "$REPO_DIR/overlay/settings.yaml" "$DSH_HOME/settings.yaml"
for s in chatgpt-takeover-ops repo-review repo-quality-gate docs-quality task-journal; do
  rm -rf "$DSH_HOME/skills/$s"
  mkdir -p "$DSH_HOME/skills/$s"
  cp "$REPO_DIR/overlay/skills/$s/SKILL.md" "$DSH_HOME/skills/$s/SKILL.md"
done

mkdir -p "$WORKBENCH_ROOT/bin"
cp "$REPO_DIR/bin/workbench-control.sh" "$WORKBENCH_ROOT/bin/workbench-control.sh"
chmod +x "$WORKBENCH_ROOT/bin/workbench-control.sh"
cp "$REPO_DIR/overlay/AGENTS.md" "$WORKBENCH_ROOT/AGENTS.md"
rm -rf "$WORKBENCH_ROOT/docs"
cp -a "$REPO_DIR/docs" "$WORKBENCH_ROOT/docs"
cp "$REPO_DIR/README.md" "$WORKBENCH_ROOT/README.md"
cp "$REPO_DIR/MANIFEST.json" "$WORKBENCH_ROOT/MANIFEST.json"
cp "$REPO_DIR/VERSION" "$WORKBENCH_ROOT/VERSION"

cat > "$DSH_HOME/workbench.env" <<ENV
WORKBENCH_ROOT='$WORKBENCH_ROOT'
DSH_ROOT='$DSH_ROOT'
NODE_BIN='$NODE_BIN'
DSH_HOME='$DSH_HOME'
MCP_ALLOWED_ROOT='$MCP_ALLOWED_ROOT'
CLANGD_BIN='$CLANGD_BIN'
LOG_FILE='$WORKBENCH_ROOT/workbench.log'
PID_FILE='$WORKBENCH_ROOT/workbench.pid'
WORKBENCH_URL='http://127.0.0.1:3080'
BACKUP_DIR='$BACKUP_DIR'
WORKBENCH_VERSION='$WORKBENCH_VERSION'
HARNESS_VERSION='$HARNESS_VERSION'
HARNESS_SHA='$HARNESS_SHA'
ENV

"$WORKBENCH_ROOT/bin/workbench-control.sh" repair-links
"$WORKBENCH_ROOT/bin/workbench-control.sh" restart
"$WORKBENCH_ROOT/bin/workbench-control.sh" verify

cat > "$WORKBENCH_ROOT/WORKBENCH_STATE.json" <<STATE
{
  "workbenchVersion": "$WORKBENCH_VERSION",
  "harnessVersion": "$HARNESS_VERSION",
  "harnessCommit": "$HARNESS_SHA",
  "nodeVersion": "$NODE_VERSION",
  "pnpmVersion": "$PNPM_VERSION",
  "workbenchRoot": "$WORKBENCH_ROOT",
  "dshRoot": "$DSH_ROOT",
  "dshHome": "$DSH_HOME",
  "mcpAllowedRoot": "$MCP_ALLOWED_ROOT",
  "backupDir": "$BACKUP_DIR",
  "installedAtUtc": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
STATE

echo
echo "工作台部署完成：$WORKBENCH_ROOT"
echo "控制：$WORKBENCH_ROOT/bin/workbench-control.sh doctor"
