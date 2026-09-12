#!/usr/bin/env bash
#
# Smoke-tests a plugin by loading it into the Valheim dedicated server under BepInEx.
#
# This catches the failures that a successful build cannot: BepInEx refusing to load the
# plugin, and Harmony patches whose target signature no longer matches the shipped game
# build. It needs no Valheim client and no Steam account.
#
# It only exercises code that runs headlessly. Anything touching the local player, input,
# or UI has to be tested in the real client.
#
# Usage: tools/run-test-server.sh [path/to/Mod.csproj ...]
#        defaults to every mod under src/

set -euo pipefail

BEPINEX_VERSION=5.4.2350
BEPINEX_URL="https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/${BEPINEX_VERSION}/"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
server_dir="${VALHEIM_SERVER_DIR:-$HOME/valheim_server}"
cache_dir="${XDG_CACHE_HOME:-$HOME/.cache}/valheim-mods"

log() { printf '==> %s\n' "$*"; }

if [[ ! -x "$server_dir/valheim_server.x86_64" ]]; then
  echo "Dedicated server not found in $server_dir. Run tools/fetch-game-libs.sh first." >&2
  exit 1
fi

if [[ ! -f "$server_dir/BepInEx/core/BepInEx.Preloader.dll" ]]; then
  log "Installing BepInExPack_Valheim $BEPINEX_VERSION into $server_dir"
  mkdir -p "$cache_dir"
  archive="$cache_dir/BepInExPack_Valheim-${BEPINEX_VERSION}.zip"
  [[ -f "$archive" ]] || curl -sSL -o "$archive" "$BEPINEX_URL"

  staging="$(mktemp -d)"
  trap 'rm -rf "$staging"' EXIT
  unzip -oq "$archive" -d "$staging"
  cp -r "$staging/BepInExPack_Valheim/." "$server_dir/"
  chmod +x "$server_dir/start_server_bepinex.sh"
fi

projects=("$@")
if [[ ${#projects[@]} -eq 0 ]]; then
  while IFS= read -r proj; do
    projects+=("$proj")
  done < <(find "$repo_root/src" -maxdepth 2 -name '*.csproj' | sort)
fi

if [[ ${#projects[@]} -eq 0 ]]; then
  echo "No mod projects found under $repo_root/src." >&2
  exit 1
fi

# A mod that depends on a library mod (Jotunn and the like) will not load without it.
# Those are already declared in each mod's Thunderstore manifest, so install from there
# rather than keeping a second list in sync.
install_dependency() {
  local full_name="$1"
  local namespace="${full_name%%-*}"
  local rest="${full_name#*-}"
  local version="${rest##*-}"
  local name="${rest%-*}"

  if [[ "$namespace" == "denikson" ]]; then
    return 0 # BepInEx itself, installed above.
  fi

  local archive="$cache_dir/${namespace}-${name}-${version}.zip"
  [[ -f "$archive" ]] || {
    log "Fetching dependency $full_name"
    curl -sSL -o "$archive" "https://thunderstore.io/package/download/${namespace}/${name}/${version}/"
  }

  local staging
  staging="$(mktemp -d)"
  unzip -oq "$archive" -d "$staging"

  # Thunderstore packages either mirror the game folder or ship a plugins/ directory.
  if [[ -d "$staging/BepInEx" ]]; then
    cp -r "$staging/BepInEx" "$server_dir/"
  elif [[ -d "$staging/plugins" ]]; then
    mkdir -p "$server_dir/BepInEx/plugins/$name"
    cp -r "$staging/plugins/." "$server_dir/BepInEx/plugins/$name/"
  else
    mkdir -p "$server_dir/BepInEx/plugins/$name"
    find "$staging" -maxdepth 1 -name '*.dll' -exec cp {} "$server_dir/BepInEx/plugins/$name/" \;
  fi

  rm -rf "$staging"
}

mkdir -p "$cache_dir"
for proj in "${projects[@]}"; do
  manifest="$(dirname "$proj")/thunderstore/manifest.json"
  [[ -f "$manifest" ]] || continue
  while IFS= read -r dep; do
    [[ -n "$dep" ]] && install_dependency "$dep"
  done < <(python3 -c "
import json, sys
with open(sys.argv[1]) as f:
    print('\n'.join(json.load(f).get('dependencies', [])))
" "$manifest")
done

# Deploying through each project's own Deploy target copies exactly the plugin
# assembly, rather than whatever else happens to be in the output directory.
plugin_dir="$server_dir/BepInEx/plugins"
mkdir -p "$plugin_dir"
for proj in "${projects[@]}"; do
  log "Building and deploying $(basename "$proj")"
  dotnet build "$proj" -t:Deploy -p:ModDeployPath="$plugin_dir" --nologo -v minimal
done

log "Starting server. Watch for 'method(s) patched'; Ctrl-C to stop."
cd "$server_dir"
exec ./start_server_bepinex.sh
