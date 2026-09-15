#!/usr/bin/env bash
set -euo pipefail
quartermaster_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
quartermaster_dotnet="${DOTNET:-dotnet}"
quartermaster_game="${VALHEIM_PATH:-/home/deck/.steam/steam/steamapps/common/Valheim}"
"$quartermaster_dotnet" build "$quartermaster_root/Quartermaster.csproj" -c Release -p:GamePath="$quartermaster_game" --nologo
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Behavior" -c Release
"$quartermaster_dotnet" build "$quartermaster_root/tests/ApiCheck" -c Release -p:GamePath="$quartermaster_game" --nologo
"$quartermaster_dotnet" run --no-build --project "$quartermaster_root/tests/ApiCheck" -c Release -- "$quartermaster_game" "$quartermaster_root/bin/Release/net472/Quartermaster.dll"
python3 "$quartermaster_root/scripts/package.py"
