#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# test_install.sh - reproducible clean-install test for PedAndPaw_AI
#
# Purpose: prove that a brand new GTA V Enhanced + ScriptHookVDotNet Enhanced
# installation works, the way a first-time user would experience it.
#
# Everything this script moves is backed up first, and every action is printed
# before it happens. Nothing is deleted. Dry-run is the default; pass --apply
# to actually change the game folder.
#
# Usage:
#   tools/test_install.sh status
#   tools/test_install.sh backup          [--apply]
#   tools/test_install.sh clean           [--apply]   # move mods aside
#   tools/test_install.sh install-shvdne <release.zip> [--apply]
#   tools/test_install.sh install-mod     [--apply]   # copy repo scripts/
#   tools/test_install.sh restore <backup-dir>         [--apply]
#
# Environment:
#   GAME_DIR      game folder (default: the path used on this machine)
#   BACKUP_ROOT   where backups go (default: sibling folder of the game)
# ---------------------------------------------------------------------------
set -euo pipefail

GAME_DIR="${GAME_DIR:-/run/media/syk/Games2/Spel/Grand Theft Auto V Enhanced}"
BACKUP_ROOT="${BACKUP_ROOT:-$(dirname "$GAME_DIR")/_paw_backups}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Mods that are NOT part of a clean install. They are moved aside, never deleted.
MOD_FILES=(
  ScriptHookVDotNet.asi
  ScriptHookVDotNet2.dll
  ScriptHookVDotNet3.dll
  ScriptHookVDotNet.ini
  ScriptHookVDotNet.log
  NativeTrainer.asi
  GameplayFixesV.asi
  GameplayFixesV.ini
  GameplayFixesV.log
  DirectStorageFix.asi
  DirectStorageFix.log
)

# Files a clean install keeps: the ASI loader and ScriptHookV itself.
CORE_FILES=(
  dinput8.dll
  ScriptHookV.dll
  ScriptHookV.log
  MinHook.x64.dll
)

APPLY=0
for arg in "$@"; do
  [[ "$arg" == "--apply" ]] && APPLY=1
done

run() {
  if [[ "$APPLY" == "1" ]]; then
    "$@"
  else
    printf 'DRY-RUN: %s\n' "$*"
  fi
}

require_game() {
  [[ -d "$GAME_DIR" ]] || { echo "ERROR: game folder not found: $GAME_DIR" >&2; exit 1; }
  [[ -f "$GAME_DIR/GTA5_Enhanced.exe" ]] || { echo "ERROR: GTA5_Enhanced.exe missing in $GAME_DIR" >&2; exit 1; }
}

cmd_status() {
  require_game
  echo "Game folder : $GAME_DIR"
  echo "Repo        : $REPO_ROOT"
  echo
  echo "Core files (must exist for the game to load ASI mods):"
  for f in "${CORE_FILES[@]}"; do
    if [[ -f "$GAME_DIR/$f" ]]; then echo "  present  $f"; else echo "  MISSING  $f"; fi
  done
  echo
  echo "Mod files (a clean install moves these aside):"
  for f in "${MOD_FILES[@]}"; do
    if [[ -f "$GAME_DIR/$f" ]]; then echo "  present  $f"; else echo "  absent   $f"; fi
  done
  echo
  echo "scripts/ folder:"
  if [[ -d "$GAME_DIR/scripts" ]]; then
    find "$GAME_DIR/scripts" -maxdepth 2 -printf '  %P\n' | head -20
  else
    echo "  MISSING"
  fi
}

cmd_backup() {
  require_game
  local stamp dir
  stamp="$(date +%Y%m%d-%H%M%S)"
  dir="$BACKUP_ROOT/$stamp"
  echo "Backup target: $dir"
  run mkdir -p "$dir"
  for f in "${MOD_FILES[@]}"; do
    [[ -f "$GAME_DIR/$f" ]] || continue
    run cp -p "$GAME_DIR/$f" "$dir/$f"
  done
  if [[ -d "$GAME_DIR/scripts" ]]; then
    run cp -a "$GAME_DIR/scripts" "$dir/scripts"
  fi
  echo
  echo "Restore later with:  tools/test_install.sh restore \"$dir\" --apply"
}

cmd_clean() {
  require_game
  local stamp dir
  stamp="$(date +%Y%m%d-%H%M%S)"
  dir="$BACKUP_ROOT/${stamp}-moved"
  echo "Moving mod files to: $dir"
  run mkdir -p "$dir"
  for f in "${MOD_FILES[@]}"; do
    [[ -f "$GAME_DIR/$f" ]] || continue
    echo "  move $f"
    run mv "$GAME_DIR/$f" "$dir/$f"
  done
  echo
  echo "Left in place (core): ${CORE_FILES[*]}"
}

cmd_install_shvdne() {
  local zip="${1:-}"
  [[ -n "$zip" && -f "$zip" ]] || { echo "ERROR: pass the SHVDNE release zip" >&2; exit 1; }
  require_game
  local tmp
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' RETURN
  python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" "$zip" "$tmp"
  local files=(ScriptHookVDotNet.asi ScriptHookVDotNet2.dll ScriptHookVDotNet3.dll ScriptHookVDotNet.ini)
  for f in "${files[@]}"; do
    [[ -f "$tmp/$f" ]] || { echo "ERROR: $f not found in the release zip" >&2; exit 1; }
    echo "  install $f"
    run cp -p "$tmp/$f" "$GAME_DIR/$f"
  done
  echo "Installed SHVDNE from $(basename "$zip")"
}

cmd_install_mod() {
  require_game
  local src="$REPO_ROOT/scripts"
  [[ -d "$src" ]] || { echo "ERROR: no scripts/ in the repository" >&2; exit 1; }
  if [[ ! -f "$src/PedAndPaw_AI.dll" ]]; then
    echo "WARNING: scripts/PedAndPaw_AI.dll is not built yet; building it now"
    if [[ -x "$HOME/.dotnet/dotnet" ]]; then
      ( cd "$REPO_ROOT" && PATH="$HOME/.dotnet:$PATH" dotnet build src/PedAndPaw_AI.csproj -c Release \
          -p:SHVDNE_DIR="${SHVDNE_DIR:-/tmp/shvdne}" >/dev/null )
    else
      echo "ERROR: no dotnet SDK at $HOME/.dotnet/dotnet" >&2; exit 1
    fi
  fi
  echo "Installing mod from $src"
  run mkdir -p "$GAME_DIR/scripts"
  run cp -a "$src/." "$GAME_DIR/scripts/"
  echo "Installed:"
  find "$src" -maxdepth 2 -printf '  %P\n' | head -20
}

cmd_restore() {
  local dir="${1:-}"
  [[ -n "$dir" && -d "$dir" ]] || { echo "ERROR: pass an existing backup directory" >&2; exit 1; }
  require_game
  echo "Restoring from $dir"
  for f in "${MOD_FILES[@]}"; do
    [[ -f "$dir/$f" ]] || continue
    echo "  restore $f"
    run cp -p "$dir/$f" "$GAME_DIR/$f"
  done
  if [[ -d "$dir/scripts" ]]; then
    echo "  restore scripts/"
    run cp -a "$dir/scripts/." "$GAME_DIR/scripts/"
  fi
}

case "${1:-}" in
  status)        cmd_status ;;
  backup)        cmd_backup ;;
  clean)         cmd_clean ;;
  install-shvdne) shift; cmd_install_shvdne "$@" ;;
  install-mod)   cmd_install_mod ;;
  restore)       shift; cmd_restore "$@" ;;
  *) sed -n '2,30p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 1 ;;
esac
