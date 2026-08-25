#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${WORKBENCH_ENV:-${HOME}/.dsh/workbench.env}"
if [[ -f "$ENV_FILE" ]]; then
  # shellcheck disable=SC1090
  source "$ENV_FILE"
fi

WORKBENCH_ROOT="${WORKBENCH_ROOT:-/mnt/data/workbench}"
DSH_ROOT="${DSH_ROOT:-/mnt/data/dsh-direct/dsh}"
NODE_BIN="${NODE_BIN:-/mnt/data/dsh-runtime/node/bin/node}"
DSH_HOME="${DSH_HOME:-${HOME}/.dsh}"
MCP_ALLOWED_ROOT="${MCP_ALLOWED_ROOT:-/mnt/data}"
CLANGD_BIN="${CLANGD_BIN:-$(command -v clangd || true)}"
LOG_FILE="${LOG_FILE:-${WORKBENCH_ROOT}/workbench.log}"
PID_FILE="${PID_FILE:-${WORKBENCH_ROOT}/workbench.pid}"
URL="${WORKBENCH_URL:-http://127.0.0.1:3080}"
PROFILE_MODULE_ROOT="${DSH_HOME}/profiles/node_modules/@deepseek-ai"
export DSH_HOME

mkdir -p "$WORKBENCH_ROOT"

repair_links() {
  mkdir -p "$PROFILE_MODULE_ROOT"
  ln -sfn "$DSH_ROOT/packages/terminal/tool-terminal" "$PROFILE_MODULE_ROOT/dsh-tool-terminal"
  ln -sfn "$DSH_ROOT/packages/lsp/lsp" "$PROFILE_MODULE_ROOT/dsh-lsp"
  ln -sfn "$DSH_ROOT/packages/lsp/lsp-stdio" "$PROFILE_MODULE_ROOT/dsh-lsp-stdio"
  ln -sfn "$DSH_ROOT/packages/lsp/tool-lsp" "$PROFILE_MODULE_ROOT/dsh-tool-lsp"
}

running_pid() {
  local p=""
  if [[ -f "$PID_FILE" ]]; then p=$(cat "$PID_FILE" 2>/dev/null || true); fi
  if [[ -n "$p" ]] && kill -0 "$p" 2>/dev/null; then echo "$p"; return 0; fi
  p=$(pgrep -f "^${NODE_BIN//\//\\/} apps/cli/lib/bin.js web --no-open$" | head -1 || true)
  [[ -n "$p" ]] && echo "$p"
}

rpc() {
  local method=$1 payload=${2:-'{}'} id
  id=$(cat /proc/sys/kernel/random/uuid)
  curl -fsS -H 'content-type: application/json' \
    -d "{\"type\":\"client-request\",\"rpcId\":\"$id\",\"method\":\"$method\",\"payload\":$payload}" \
    "$URL/api/$method"
}

remote_rpc() {
  local method=$1 args=${2:-'{}'} id
  id=$(cat /proc/sys/kernel/random/uuid)
  curl -fsS -H 'content-type: application/json' \
    -d "{\"type\":\"client-request\",\"rpcId\":\"$id\",\"method\":\"$method\",\"payload\":{\"args\":$args}}" \
    "$URL/api/$method"
}

start() {
  repair_links
  [[ -x "$NODE_BIN" ]] || { echo "缺少 Node.js 运行时：$NODE_BIN" >&2; exit 1; }
  [[ -f "$DSH_ROOT/apps/cli/lib/bin.js" ]] || { echo "缺少已构建 Harness：$DSH_ROOT" >&2; exit 1; }
  if p=$(running_pid); then echo "工作台已运行 pid=$p"; return 0; fi
  cd "$DSH_ROOT"
  : > "$LOG_FILE"
  nohup "$NODE_BIN" apps/cli/lib/bin.js web --no-open >> "$LOG_FILE" 2>&1 &
  echo $! > "$PID_FILE"
  for _ in $(seq 1 180); do
    if curl -fsS "$URL/" >/dev/null 2>&1; then
      probe=$(rpc host.describe '{}' 2>/dev/null || true)
      if jq -e '.result.ok == true' >/dev/null 2>&1 <<<"$probe"; then
        echo "工作台启动完成 pid=$(cat "$PID_FILE") url=$URL api=ready"
        return 0
      fi
    fi
    sleep 0.1
  done
  cat "$LOG_FILE" >&2
  exit 1
}

stop() {
  p=$(running_pid || true)
  [[ -n "${p:-}" ]] || { echo '工作台未运行'; return 0; }
  kill -TERM "$p"
  for _ in $(seq 1 80); do
    kill -0 "$p" 2>/dev/null || { echo "工作台已停止 pid=$p"; return 0; }
    sleep 0.1
  done
  kill -KILL "$p" 2>/dev/null || true
  echo "工作台已强制停止 pid=$p"
}

status() {
  if p=$(running_pid); then
    echo "工作台运行中 pid=$p"
    curl -fsS -o /dev/null -w 'HTTP %{http_code}\n' "$URL/" || true
  else
    echo '工作台未运行'; return 1
  fi
  pgrep -af '/@modelcontextprotocol/server-filesystem/dist/index.js' | grep -F -- "$MCP_ALLOWED_ROOT" | head -1 || true
}

verify() {
  p=$(running_pid) || { echo 'FAIL 服务未运行' >&2; exit 1; }
  [[ "$(curl -sS -o /dev/null -w '%{http_code}' "$URL/")" == 200 ]] || { echo 'FAIL 首页 HTTP' >&2; exit 1; }
  rpc host.describe '{}' | jq -e '.result.ok == true' >/dev/null || { echo 'FAIL 核心 API' >&2; exit 1; }
  rpc agentPreset.list '{}' | jq -e '.result.ok == true and any(.result.value.presets[]; .id=="chatgpt-takeover" and .isDefault==true and (.broken|not))' >/dev/null || { echo 'FAIL 默认接管预设' >&2; exit 1; }
  [[ -f "$DSH_HOME/storages/session-search.sqlite" ]] || { echo 'FAIL 会话全文索引' >&2; exit 1; }
  pgrep -af '/@modelcontextprotocol/server-filesystem/dist/index.js' | grep -F -- "$MCP_ALLOWED_ROOT" >/dev/null || { echo 'FAIL MCP localfs' >&2; exit 1; }
  for skill in chatgpt-takeover-ops repo-review repo-quality-gate docs-quality task-journal; do
    [[ -f "$DSH_HOME/skills/$skill/SKILL.md" ]] || { echo "FAIL 技能 $skill" >&2; exit 1; }
  done
  [[ -n "$CLANGD_BIN" && -x "$CLANGD_BIN" ]] || { echo 'FAIL clangd' >&2; exit 1; }
  find "$DSH_ROOT/node_modules/.pnpm" -path '*/typescript-language-server/lib/cli.mjs' -print -quit | grep -q . || { echo 'FAIL TypeScript 语言服务器' >&2; exit 1; }
  echo "OK pid=$p preset=chatgpt-takeover http=200 mcp=localfs session-index=durable lsp=typescript+clangd skills=5"
}

inventory() {
  echo "== 版本 =="
  "$NODE_BIN" --version
  (cd "$DSH_ROOT" && "$NODE_BIN" -e "const p=require('./package.json'); console.log('Harness '+p.version); console.log('packageManager '+p.packageManager)")
  echo
  echo "== 插件运行时清单统计 =="
  remote_rpc 'pluginInventory/list' '{}' | jq -r '.result.value.entries | length as $n | ([.[] | select(.enabled==true and .fiberPhase=="active")] | length) as $a | ([.[] | select(.enabled==false)] | length) as $d | ([.[] | select(.enabled==true and .fiberPhase!="active")] | length) as $x | "总条目=\($n) 激活=\($a) 关闭=\($d) 异常启用=\($x)"'
  echo
  echo "== 全局技能 =="
  find "$DSH_HOME/skills" -mindepth 2 -maxdepth 2 -name SKILL.md -printf '%h\n' 2>/dev/null | xargs -r -n1 basename | sort
}

doctor() {
  verify
  echo
  inventory
  echo
  echo "== 路径 =="
  printf 'WORKBENCH_ROOT=%s\nDSH_ROOT=%s\nNODE_BIN=%s\nDSH_HOME=%s\nMCP_ALLOWED_ROOT=%s\nCLANGD_BIN=%s\n' \
    "$WORKBENCH_ROOT" "$DSH_ROOT" "$NODE_BIN" "$DSH_HOME" "$MCP_ALLOWED_ROOT" "$CLANGD_BIN"
  echo
  echo "== 最近日志 =="
  tail -40 "$LOG_FILE" 2>/dev/null || true
}

case "${1:-status}" in
  start) start ;;
  stop) stop ;;
  restart) stop; start ;;
  status) status ;;
  verify) verify ;;
  doctor) doctor ;;
  inventory) inventory ;;
  repair-links) repair_links; echo repaired ;;
  rpc) shift; rpc "${1:?缺少方法名}" "${2:-{}}" ;;
  remote-rpc) shift; remote_rpc "${1:?缺少方法名}" "${2:-{}}" ;;
  *) echo "用法：$0 {start|stop|restart|status|verify|doctor|inventory|repair-links|rpc|remote-rpc}" >&2; exit 2 ;;
esac
