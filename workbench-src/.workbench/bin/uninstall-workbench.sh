#!/usr/bin/env bash
set -euo pipefail
ENV_FILE="${WORKBENCH_ENV:-${HOME}/.dsh/workbench.env}"
[[ -f "$ENV_FILE" ]] || { echo "找不到工作台环境记录：$ENV_FILE" >&2; exit 1; }
# shellcheck disable=SC1090
source "$ENV_FILE"
CONTROL="$WORKBENCH_ROOT/bin/workbench-control.sh"
[[ -x "$CONTROL" ]] && "$CONTROL" stop || true

restore_one() {
  local rel=$1 dst=$2
  if [[ -e "$BACKUP_DIR/$rel" || -L "$BACKUP_DIR/$rel" ]]; then
    rm -rf "$dst"
    mkdir -p "$(dirname "$dst")"
    cp -a "$BACKUP_DIR/$rel" "$dst"
  else
    rm -rf "$dst"
  fi
}
restore_one settings.yaml "$DSH_HOME/settings.yaml"
restore_one profiles/web/cordis.patch.yml "$DSH_HOME/profiles/web/cordis.patch.yml"
restore_one .agent-presets/chatgpt-takeover "$DSH_HOME/.agent-presets/chatgpt-takeover"
for s in chatgpt-takeover-ops repo-review repo-quality-gate docs-quality task-journal; do
  restore_one "skills/$s" "$DSH_HOME/skills/$s"
done
rm -f "$DSH_HOME/workbench.env"
echo "工作台覆盖层已回滚。基础 Harness 运行时未删除。备份：$BACKUP_DIR"
