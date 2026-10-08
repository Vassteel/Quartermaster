#!/usr/bin/env bash
set -euo pipefail
quartermaster_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
quartermaster_dotnet="${DOTNET:-dotnet}"
quartermaster_game="${VALHEIM_PATH:-/home/deck/.steam/steam/steamapps/common/Valheim}"
python3 "$quartermaster_root/scripts/build_deposit_chest.py"
python3 "$quartermaster_root/scripts/verify_deposit_chest.py"
python3 "$quartermaster_root/scripts/build_owl.py"
python3 "$quartermaster_root/scripts/build_postal_models.py"
python3 "$quartermaster_root/scripts/build_ledger.py"
python3 "$quartermaster_root/scripts/build_pickup_icon.py"
python3 "$quartermaster_root/scripts/build_clay.py"
python3 "$quartermaster_root/scripts/verify_clay.py"
python3 "$quartermaster_root/scripts/build_apothecary.py"
python3 "$quartermaster_root/scripts/verify_apothecary.py"
"$quartermaster_dotnet" build "$quartermaster_root/Quartermaster.csproj" -c Release -p:GamePath="$quartermaster_game" --nologo -m:1 -p:UseSharedCompilation=false
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Behavior" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/StorageCore" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/StorageCore" -c Release -p:PlusSlotApi=true
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/ConfigManager" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/ConfigManager" -c Release -- absent
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/NativeStorage" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/StorageObservation" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/CraftPayment" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/SharedChests" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/StorageCore" -c Release -p:AbsentSlotApi=true -- absent
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/InventorySlots" -c Release
"$quartermaster_dotnet" build "$quartermaster_root/tests/InventorySlots" -c Release -p:BrokenSlotApi=true --nologo
"$quartermaster_dotnet" "$quartermaster_root/tests/InventorySlots/bin/broken/net8.0/InventorySlots.dll"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/InventoryUi/run.py"
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/GullMaterials" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/OwlModel" -c Release -- "$quartermaster_root/assets/owl/model.bin"
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/PostalAssets" -c Release -- "$quartermaster_root/assets/postal"
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Stacks" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/ReportFixes" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/ServerSettings" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Ledger" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/BuildMenu" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/BuildRegistration" -c Release
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/FurnitureMaterials/run.py"
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/OwlTravel" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/OwlCourier" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/OwlHover" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Clay" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Apothecary" -c Release
(cd "$quartermaster_root" && "$quartermaster_dotnet" run --project tests/MeadStorage -c Release)
(cd "$quartermaster_root" && "$quartermaster_dotnet" run --project tests/DrawerStorage -c Release)
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/NativeHanging" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/MenuTheme" -c Release
"$quartermaster_dotnet" run --project "$quartermaster_root/tests/Pickup" -c Release
"$quartermaster_dotnet" build "$quartermaster_root/tests/ApiCheck" -c Release -p:GamePath="$quartermaster_game" --nologo -m:1 -p:UseSharedCompilation=false
"$quartermaster_dotnet" run --no-build --project "$quartermaster_root/tests/ApiCheck" -c Release -- "$quartermaster_game" "$quartermaster_root/bin/Release/net472/Quartermaster.dll"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/Respawn/run.py"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/LegacyCapacity/run.py"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/UiFocus/run.py"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/Smelter/run.py"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/AutomationRange/run.py"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/Ownership/run.py"
DOTNET="$quartermaster_dotnet" python3 "$quartermaster_root/tests/HiveLimit/run.py"
python3 "$quartermaster_root/tests/Installer/test_plan.py"
python3 "$quartermaster_root/scripts/package.py"
