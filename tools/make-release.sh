#!/usr/bin/env bash
# Builds the release zip (Phase 9): dist/HorsePersonalities-<version>.zip
#   scripts/HorsePersonalities.dll
#   scripts/HorsePersonalities/breeds.json, test-personalities.json
#   scripts/HorsePersonalities.ini        (the defaults the mod would write on first run)
#   README.md, LICENSE, CHANGELOG.md
# DevTools is never packed. Build against the ScriptHookRDRNetAPI.dll from the hook release you play with
# (lib/ by default; override with SHDN_API=/path/to/ScriptHookRDRNetAPI.dll).
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
api="${SHDN_API:-$root/lib/ScriptHookRDRNetAPI.dll}"
[ -f "$api" ] || { echo "ScriptHookRDRNetAPI.dll not found at $api (see lib/README.md)"; exit 1; }
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)"

dotnet run --project tools/sheet-to-json -- --check
dotnet test tests/HorsePersonalities.Tests -nologo -v q
dotnet build src/HorsePersonalities.Game -c Release -nologo -v q "/p:ShdnApiPath=$api"

stage="$root/dist/stage"
rm -rf "$stage" && mkdir -p "$stage/scripts/HorsePersonalities"
cp src/HorsePersonalities.Game/bin/Release/HorsePersonalities.dll "$stage/scripts/"
cp data/breeds.json data/test-personalities.json "$stage/scripts/HorsePersonalities/"
dotnet run --project tools/sheet-to-json -- --default-ini "$stage/scripts/HorsePersonalities.ini"
cp README.md LICENSE CHANGELOG.md "$stage/"

zipfile="$root/dist/HorsePersonalities-$version.zip"
rm -f "$zipfile"
(cd "$stage" && zip -qr "$zipfile" .)
rm -rf "$stage"
echo "Built $zipfile"
