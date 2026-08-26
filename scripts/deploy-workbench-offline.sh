#!/usr/bin/env bash
set -Eeuo pipefail

REPO="sy220284/555"
RELEASE_TAG="workbench-1.0.0"
WORKBENCH_VERSION="1.0.0"
TARGET="${HOME}/.local/share/555-workbench"
HOST="127.0.0.1"
PORT="3080"
ARTIFACT=""
ARTIFACT_DIR=""
WORKSPACE=""
START_AFTER=1
KEEP_BACKUP=0

usage() {
  cat <<'EOF'
555 工作台一键离线部署

用法：
  bash scripts/deploy-workbench-offline.sh [选项]

选项：
  --artifact <zip>       使用 GitHub Actions 下载的 workbench-offline-linux-x64.zip
  --artifact-dir <目录>  使用已存在的离线产物目录（含两个 tar.zst 与 SHA256SUMS.txt）
  --target <目录>        部署目录，默认 ~/.local/share/555-workbench
  --workspace <目录>     本地文件服务根目录，默认部署后的 workbench 源码目录
  --host <地址>          Web 监听地址，默认 127.0.0.1
  --port <端口>          Web 监听端口，默认 3080
  --no-start             只部署和验收，不启动服务
  --keep-backup          升级成功后保留上一版备份
  -h, --help             显示帮助

未指定离线产物时，会从 555 的永久 Release 下载经过工作流构建并实装验收的离线包。
脚本不会修改系统 Node.js/pnpm，也不会执行 pnpm install；新产物使用 tar.gz，不要求宿主机安装 zstd。
EOF
}

log() { printf '[555-deploy] %s\n' "$*"; }
die() { printf '[555-deploy] 错误：%s\n' "$*" >&2; exit 1; }

while (($#)); do
  case "$1" in
    --artifact) ARTIFACT=${2:?缺少 --artifact 参数}; shift 2 ;;
    --artifact-dir) ARTIFACT_DIR=${2:?缺少 --artifact-dir 参数}; shift 2 ;;
    --target) TARGET=${2:?缺少 --target 参数}; shift 2 ;;
    --workspace) WORKSPACE=${2:?缺少 --workspace 参数}; shift 2 ;;
    --host) HOST=${2:?缺少 --host 参数}; shift 2 ;;
    --port) PORT=${2:?缺少 --port 参数}; shift 2 ;;
    --no-start) START_AFTER=0; shift ;;
    --keep-backup) KEEP_BACKUP=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) die "未知参数：$1" ;;
  esac
done

[[ "$PORT" =~ ^[0-9]+$ ]] && ((PORT >= 1 && PORT <= 65535)) || die "端口必须在 1-65535 之间"
[[ -z "$ARTIFACT" || -z "$ARTIFACT_DIR" ]] || die "--artifact 与 --artifact-dir 只能二选一"

for cmd in tar sha256sum find; do
  command -v "$cmd" >/dev/null 2>&1 || die "缺少基础命令：$cmd"
done

TMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/555-deploy.XXXXXX")
ASSETS="$TMP_ROOT/assets"
STAGE="$TMP_ROOT/stage"
mkdir -p "$ASSETS" "$STAGE"
cleanup() { rm -rf "$TMP_ROOT"; }
trap cleanup EXIT

try_fetch() {
  local url=$1 dest=$2
  if command -v curl >/dev/null 2>&1; then
    if curl -fL --retry 3 --retry-delay 1 "$url" -o "$dest"; then
      return 0
    fi
    rm -f "$dest"
    return 1
  elif command -v python3 >/dev/null 2>&1; then
    if python3 - "$url" "$dest" <<'PYFETCH'
import sys, urllib.request
try:
    urllib.request.urlretrieve(sys.argv[1], sys.argv[2])
except Exception:
    raise SystemExit(1)
PYFETCH
    then
      return 0
    fi
    rm -f "$dest"
    return 1
  else
    return 127
  fi
}

fetch() {
  local url=$1 dest=$2
  try_fetch "$url" "$dest" || die "下载失败：$url；也可以通过 --artifact/--artifact-dir 提供工作流产物"
}

extract_zip() {
  local zip=$1 dest=$2
  if command -v unzip >/dev/null 2>&1; then
    unzip -q "$zip" -d "$dest"
  elif command -v python3 >/dev/null 2>&1; then
    python3 -m zipfile -e "$zip" "$dest"
  else
    die "解压 Actions ZIP 需要 unzip 或 python3"
  fi
}

if [[ -n "$ARTIFACT_DIR" ]]; then
  [[ -d "$ARTIFACT_DIR" ]] || die "产物目录不存在：$ARTIFACT_DIR"
  cp -a "$ARTIFACT_DIR"/. "$ASSETS"/
elif [[ -n "$ARTIFACT" ]]; then
  [[ -f "$ARTIFACT" ]] || die "产物 ZIP 不存在：$ARTIFACT"
  extract_zip "$ARTIFACT" "$ASSETS"
else
  BASE_URL="https://github.com/${REPO}/releases/download/${RELEASE_TAG}"
  log "从 555 永久工作流 Release 下载离线产物"
  fetch "$BASE_URL/SHA256SUMS.txt" "$ASSETS/SHA256SUMS.txt"
  if try_fetch "$BASE_URL/workbench-${WORKBENCH_VERSION}-ready-linux-x64.tar.gz" "$ASSETS/workbench-${WORKBENCH_VERSION}-ready-linux-x64.tar.gz"; then
    fetch "$BASE_URL/workbench-toolchain-linux-x64.tar.gz" "$ASSETS/workbench-toolchain-linux-x64.tar.gz"
  else
    log "当前 Release 没有新式 tar.gz，回退兼容旧 tar.zst 产物"
    fetch "$BASE_URL/workbench-${WORKBENCH_VERSION}-ready-linux-x64.tar.zst" "$ASSETS/workbench-${WORKBENCH_VERSION}-ready-linux-x64.tar.zst"
    fetch "$BASE_URL/workbench-toolchain-linux-x64.tar.zst" "$ASSETS/workbench-toolchain-linux-x64.tar.zst"
  fi
fi

READY=$(find "$ASSETS" -type f -name "workbench-*-ready-linux-x64.tar.gz" -print -quit)
[[ -n "$READY" ]] || READY=$(find "$ASSETS" -type f -name "workbench-*-ready-linux-x64.tar.zst" -print -quit)
TOOLCHAIN=$(find "$ASSETS" -type f -name 'workbench-toolchain-linux-x64.tar.gz' -print -quit)
[[ -n "$TOOLCHAIN" ]] || TOOLCHAIN=$(find "$ASSETS" -type f -name 'workbench-toolchain-linux-x64.tar.zst' -print -quit)
SUMS=$(find "$ASSETS" -type f -name 'SHA256SUMS.txt' -print -quit)
[[ -n "$READY" && -n "$TOOLCHAIN" && -n "$SUMS" ]] || die "离线产物不完整，需要 ready/toolchain/SHA256SUMS 三个文件"

verify_one() {
  local file=$1
  local name expected actual
  name=$(basename "$file")
  expected=$(awk -v n="$name" '$2==n || $2=="*"n {print $1; exit}' "$SUMS")
  [[ -n "$expected" ]] || die "SHA256SUMS.txt 中缺少 $name"
  actual=$(sha256sum "$file" | awk '{print $1}')
  [[ "$actual" == "$expected" ]] || die "$name 校验失败：期望 $expected，实际 $actual"
}
verify_one "$READY"
verify_one "$TOOLCHAIN"
log "离线产物 SHA-256 校验通过"

extract_tar() {
  local archive=$1 dest=$2
  case "$archive" in
    *.tar.gz) tar -xzf "$archive" -C "$dest" ;;
    *.tar.zst)
      command -v zstd >/dev/null 2>&1 || die "旧版 tar.zst 产物需要 zstd；建议改用最新永久 Release 的 tar.gz 产物"
      tar --zstd -xf "$archive" -C "$dest"
      ;;
    *) die "不支持的离线包格式：$archive" ;;
  esac
}

extract_tar "$READY" "$STAGE"
extract_tar "$TOOLCHAIN" "$STAGE"
[[ -d "$STAGE/workbench" && -d "$STAGE/toolchain" ]] || die "离线包目录结构异常"

# 兼容 2026-08-26 之前的旧工具链包：旧包只保存 Node.js tar.xz 与 pnpm tgz。
if ! find "$STAGE/toolchain" -maxdepth 4 \( -type f -o -type l \) -path '*/bin/node' -print -quit | grep -q .; then
  NODE_ARCHIVE=$(find "$STAGE/toolchain" -maxdepth 1 -type f -name 'node-v*-linux-x64.tar.xz' -print -quit)
  PNPM_ARCHIVE=$(find "$STAGE/toolchain" -maxdepth 1 -type f -name 'pnpm-*.tgz' -print -quit)
  [[ -n "$NODE_ARCHIVE" && -n "$PNPM_ARCHIVE" ]] || die "工具链中找不到可用 Node.js/pnpm"
  tar -xJf "$NODE_ARCHIVE" -C "$STAGE/toolchain"
  tar -xzf "$PNPM_ARCHIVE" -C "$STAGE/toolchain"
fi

NODE_BIN=$(find "$STAGE/toolchain" -maxdepth 4 \( -type f -o -type l \) -path '*/bin/node' -print -quit)
PNPM_MJS=$(find "$STAGE/toolchain" -maxdepth 4 -type f -path '*/package/bin/pnpm.mjs' -print -quit)
[[ -x "$NODE_BIN" ]] || die "工具链 Node.js 不可执行"
[[ -f "$PNPM_MJS" ]] || die "工具链 pnpm 入口缺失"
NODE_VERSION_ACTUAL=$("$NODE_BIN" --version)
log "使用离线工具链 Node.js ${NODE_VERSION_ACTUAL}"

mkdir -p "$STAGE/toolchain/bin"
NODE_REL=${NODE_BIN#"$STAGE/toolchain/"}
PNPM_REL=${PNPM_MJS#"$STAGE/toolchain/"}
cat > "$STAGE/toolchain/bin/pnpm" <<EOF
#!/usr/bin/env bash
set -euo pipefail
ROOT=\$(cd -- "\$(dirname -- "\$0")/.." && pwd)
exec "\$ROOT/$NODE_REL" "\$ROOT/$PNPM_REL" "\$@"
EOF
chmod +x "$STAGE/toolchain/bin/pnpm"
if [[ "$NODE_BIN" != "$STAGE/toolchain/bin/node" ]]; then
  ln -sfn "../$NODE_REL" "$STAGE/toolchain/bin/node"
else
  [[ -x "$STAGE/toolchain/bin/node" ]] || die "工具链 bin/node 链接不可执行"
fi
PNPM_VERSION_ACTUAL=$("$STAGE/toolchain/bin/pnpm" --version)
log "使用离线工具链 pnpm ${PNPM_VERSION_ACTUAL}"

BROKEN=$(find "$STAGE/workbench" -type l ! -exec test -e {} \; -print -quit)
[[ -z "$BROKEN" ]] || die "工作台存在断裂符号链接：$BROKEN"

cd "$STAGE/workbench"
"$NODE_BIN" scripts/workbench-doctor.mjs
"$NODE_BIN" - <<'NODE'
const { createRequire } = require('node:module')
const { resolve } = require('node:path')
const req = createRequire(resolve('apps/cli/package.json'))
for (const name of [
  '@deepseek-ai/dsh-app-boot',
  '@deepseek-ai/dsh-mcp-client',
  '@deepseek-ai/dsh-lsp',
  '@deepseek-ai/dsh-lsp-stdio',
  '@deepseek-ai/dsh-tool-lsp',
  '@deepseek-ai/dsh-tool-terminal',
]) {
  req.resolve(name)
}
NODE
for entry in \
  "apps/cli/node_modules/@modelcontextprotocol/server-filesystem/dist/index.js" \
  "apps/cli/node_modules/typescript-language-server/lib/cli.mjs"; do
  [[ -f "$entry" ]] || die "运行时入口无法解析：$entry"
done
log "源码、依赖解析与符号链接验收通过"

TARGET=$(mkdir -p "$(dirname "$TARGET")" && cd "$(dirname "$TARGET")" && printf '%s/%s' "$PWD" "$(basename "$TARGET")")
BACKUP="${TARGET}.backup"
OLD_DATA=""

if [[ -d "$TARGET" ]]; then
  if [[ -x "$TARGET/stop-workbench.sh" ]]; then "$TARGET/stop-workbench.sh" || true; fi
  rm -rf "$BACKUP"
  mv "$TARGET" "$BACKUP"
  if [[ -d "$BACKUP/data" ]]; then OLD_DATA="$BACKUP/data"; fi
fi

mkdir -p "$TARGET"
mv "$STAGE/workbench" "$TARGET/workbench"
mv "$STAGE/toolchain" "$TARGET/runtime"
if [[ -n "$OLD_DATA" ]]; then cp -a "$OLD_DATA" "$TARGET/data"; else mkdir -p "$TARGET/data"; fi

NODE_REL_TARGET=${NODE_BIN#"$STAGE/toolchain/"}
NODE_TARGET="$TARGET/runtime/$NODE_REL_TARGET"
[[ -x "$NODE_TARGET" ]] || die "部署后的 Node.js 不可执行：$NODE_TARGET"
if [[ -z "$WORKSPACE" ]]; then WORKSPACE="$TARGET/workbench"; fi
mkdir -p "$WORKSPACE"
WORKSPACE=$(cd "$WORKSPACE" && pwd)

cat > "$TARGET/start-workbench.sh" <<EOF
#!/usr/bin/env bash
set -euo pipefail
BASE=\$(cd -- "\$(dirname -- "\$0")" && pwd)
WORKBENCH="\$BASE/workbench"
NODE="\$BASE/runtime/$NODE_REL_TARGET"
PIDFILE="\$BASE/workbench-web.pid"
LOGFILE="\$BASE/workbench-web.log"
HOST="$HOST"
PORT="$PORT"
export DSH_HOME="\$BASE/data"
export DSH_WORKBENCH_MCP_ROOT="$WORKSPACE"
if [[ -f "\$PIDFILE" ]]; then
  PID=\$(cat "\$PIDFILE" 2>/dev/null || true)
  if [[ -n "\${PID:-}" ]] && kill -0 "\$PID" 2>/dev/null; then
    echo "工作台已运行：http://\$HOST:\$PORT (PID \$PID)"
    exit 0
  fi
fi
cd "\$WORKBENCH"
"\$NODE" scripts/workbench-doctor.mjs >/dev/null
nohup "\$NODE" apps/cli/lib/bin.js web --no-open --host "\$HOST" --port "\$PORT" >"\$LOGFILE" 2>&1 &
PID=\$!
echo "\$PID" > "\$PIDFILE"
for _ in \$(seq 1 40); do
  if "\$NODE" -e "fetch('http://\$HOST:\$PORT/').then(r=>process.exit(r.status===200?0:1)).catch(()=>process.exit(1))" >/dev/null 2>&1; then
    echo "工作台已启动：http://\$HOST:\$PORT (PID \$PID)"
    exit 0
  fi
  if ! kill -0 "\$PID" 2>/dev/null; then
    echo "工作台启动失败，日志：\$LOGFILE" >&2
    tail -80 "\$LOGFILE" >&2 || true
    rm -f "\$PIDFILE"
    exit 1
  fi
  sleep 0.5
done
echo "工作台未在预期时间内响应，日志：\$LOGFILE" >&2
tail -80 "\$LOGFILE" >&2 || true
exit 1
EOF
chmod +x "$TARGET/start-workbench.sh"

cat > "$TARGET/stop-workbench.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
BASE=$(cd -- "$(dirname -- "$0")" && pwd)
PIDFILE="$BASE/workbench-web.pid"
if [[ ! -f "$PIDFILE" ]]; then echo "工作台当前未记录运行进程。"; exit 0; fi
PID=$(cat "$PIDFILE" 2>/dev/null || true)
if [[ -n "${PID:-}" ]] && kill -0 "$PID" 2>/dev/null; then
  kill "$PID"
  for _ in $(seq 1 30); do kill -0 "$PID" 2>/dev/null || break; sleep 0.2; done
fi
rm -f "$PIDFILE"
echo "工作台已停止。"
EOF
chmod +x "$TARGET/stop-workbench.sh"

cat > "$TARGET/status-workbench.sh" <<EOF
#!/usr/bin/env bash
set -euo pipefail
BASE=\$(cd -- "\$(dirname -- "\$0")" && pwd)
NODE="\$BASE/runtime/$NODE_REL_TARGET"
PIDFILE="\$BASE/workbench-web.pid"
if [[ -f "\$PIDFILE" ]] && PID=\$(cat "\$PIDFILE") && kill -0 "\$PID" 2>/dev/null; then
  "\$NODE" -e "fetch('http://$HOST:$PORT/').then(r=>{console.log('运行中：HTTP '+r.status+'，PID '+process.argv[1]);process.exit(r.status===200?0:1)}).catch(e=>{console.error(e.message);process.exit(1)})" "\$PID"
else
  echo "未运行"
  exit 1
fi
EOF
chmod +x "$TARGET/status-workbench.sh"

if ((START_AFTER)); then
  if ! "$TARGET/start-workbench.sh"; then
    log "新部署启动失败，尝试回滚"
    rm -rf "$TARGET"
    if [[ -d "$BACKUP" ]]; then mv "$BACKUP" "$TARGET"; "$TARGET/start-workbench.sh" || true; fi
    exit 1
  fi
fi

if [[ -d "$BACKUP" && "$KEEP_BACKUP" -eq 0 ]]; then rm -rf "$BACKUP"; fi

log "部署完成：$TARGET"
log "启动：$TARGET/start-workbench.sh"
log "停止：$TARGET/stop-workbench.sh"
log "状态：$TARGET/status-workbench.sh"
