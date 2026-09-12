#!/usr/bin/env bash
#
# Populates lib/valheim with the managed assemblies the mod compiles against.
#
# The assemblies come from the Valheim Dedicated Server (Steam app 896660), which
# Steam serves to anonymous logins at no cost. It ships the same assembly_valheim.dll
# and Unity module assemblies as the client, so it works as a reference-only source
# and nobody has to hand around copies of the game.
#
# These are Iron Gate's copyrighted binaries: they stay git-ignored and are never
# redistributed. Only our own compiled plugin dll is.

set -euo pipefail

VALHEIM_SERVER_APPID=896660

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dest_dir="${VALHEIM_MANAGED_DIR:-$repo_root/lib/valheim}"
steamcmd_dir="${STEAMCMD_DIR:-$HOME/steamcmd}"
server_dir="${VALHEIM_SERVER_DIR:-$HOME/valheim_server}"

log() { printf '==> %s\n' "$*"; }

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required." >&2
  exit 1
fi

if [[ ! -x "$steamcmd_dir/steamcmd.sh" ]]; then
  log "Installing SteamCMD into $steamcmd_dir"
  mkdir -p "$steamcmd_dir"
  curl -sSL https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz |
    tar zxf - -C "$steamcmd_dir"
fi

log "Downloading Valheim dedicated server (app $VALHEIM_SERVER_APPID) into $server_dir"
"$steamcmd_dir/steamcmd.sh" \
  +force_install_dir "$server_dir" \
  +login anonymous \
  +app_update "$VALHEIM_SERVER_APPID" validate \
  +quit

managed_src="$server_dir/valheim_server_Data/Managed"
if [[ ! -f "$managed_src/assembly_valheim.dll" ]]; then
  echo "assembly_valheim.dll not found under $managed_src" >&2
  exit 1
fi

log "Copying managed assemblies into $dest_dir"
mkdir -p "$dest_dir"
cp "$managed_src"/*.dll "$dest_dir/"

# Record which game build these references came from. Valheim updates routinely
# rename and re-sign members, so a patch that stops applying is usually a stale
# reference set rather than a bug in the patch.
build_id="$(sed -n 's/.*"buildid"[^"]*"\([0-9]*\)".*/\1/p' \
  "$server_dir/steamapps/appmanifest_${VALHEIM_SERVER_APPID}.acf" 2>/dev/null | head -1)"
{
  echo "appid=$VALHEIM_SERVER_APPID"
  echo "buildid=${build_id:-unknown}"
  echo "fetched=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
} >"$dest_dir/SOURCE.txt"

log "Done: $(find "$dest_dir" -maxdepth 1 -name '*.dll' | wc -l | tr -d ' ') assemblies, server buildid ${build_id:-unknown}"
