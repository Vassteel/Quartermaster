# Local installation — Quartermaster 0.1.16

Installed the chest-shader and light-probe change with Valheim and r2modman closed. All 16 changed game/profile/cache files were verified.

- Backup: `backups/install-0.1.16-20260915-202150`
- Package: `dist/Quartermaster-0.1.16.zip`
- Zero build warnings/errors; 28,799 behavior and 27 material checks pass; 478 API references and all hook checks pass.

The gull's brightness still needs a live visual check in the user's scene.

## Previous installation

# Local installation — Quartermaster 0.1.15

Installed the native-material repair with Valheim and r2modman closed. All 16 changed game/profile/cache files were verified.

- Backup: `backups/install-0.1.15-20260915-200840`
- Package: `dist/Quartermaster-0.1.15.zip`
- 28,799 behavior assertions, 27 material assertions and API/hook checks pass. Zero build warnings/errors.

Restart to verify the Deposit Chest gull appears and shades normally. Live acceptance pending.

## Previous installation

# Local installation — Quartermaster 0.1.14

Installed with Valheim and r2modman closed. The installer verified 18 changes across the game, Mods profile, manager cache and profile metadata.

- Backup: `backups/install-0.1.14-20260915-194004`
- Package: `dist/Quartermaster-0.1.14.zip`
- 28,799 behavior assertions, 20 material checks and API/hook validation pass. Live gull audio/chat acceptance remains pending.

## Previous installation

# Local update — Quartermaster 0.1.13

Installed the Deposit gull material revision into the Steam game, r2modman Mods profile and 0.1.13 manager cache. All 16 changed files verified byte-for-byte; game and manager were closed. Existing configuration preserved.

Backup: `backups/install-0.1.13-20260915-182814`. Package: `dist/Quartermaster-0.1.13.zip`.

Build and material/behavior/API checks pass. Actual indoor/day/night appearance still needs in-game verification; see `VALIDATION.md`.

## Previous installation — 0.1.12

Installed and verified in the Steam game, r2modman Mods profile and versioned manager cache. Both standalone NoTaintTooltip DLLs are disabled as `.dll.old`; their local project is archived at `../archive/NoTaintTooltip`.

Config controls under General: `ClearCheatItemTagsOnLoad` for the startup scan, and `HideCheatItemMessages` for tooltip and pickup/removal notices. Existing settings are preserved. Public changelog unchanged.

Validation: zero build warnings/errors; 28,786 behavior assertions; 443 binary members; 44 Harmony hooks; 19 reflection targets. All 20 installed paths match the backup manifest, and unrelated manager entries are unchanged. Live UI acceptance still requires a game launch.

Backup: `/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.12-20260915-113636`.

Package: `dist/Quartermaster-0.1.12.zip`.
