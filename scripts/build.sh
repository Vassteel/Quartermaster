#!/usr/bin/env bash
set -euo pipefail
quartermaster_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
quartermaster_dotnet="${DOTNET:-dotnet}"
quartermaster_game="${VALHEIM_PATH:-/home/deck/.steam/steam/steamapps/common/Valheim}"
python3 "$quartermaster_root/scripts/build_deposit_chest.py"
python3 "$quartermaster_root/scripts/verify_deposit_chest.py"
"$quartermaster_dotnet" build "$quartermaster_root/Quartermaster.csproj" -c Release -p:GamePath="$quartermaster_game" --nologo
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Behavior" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/GullMaterials" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Stacks" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Pickup" -c Release
"$quartermaster_dotnet" build "$quartermaster_root/tests/ApiCheck" -c Release -p:GamePath="$quartermaster_game" --nologo
"$quartermaster_dotnet" run --no-build --project "$quartermaster_root/tests/ApiCheck" -c Release -- "$quartermaster_game" "$quartermaster_root/bin/Release/net472/Quartermaster.dll"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/UiFocus/run.py"
python3 "$quartermaster_root/scripts/package.py"
