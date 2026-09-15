# Local update — Quartermaster 0.1.2

Installed on 2026-09-14 after the user saved and closed Valheim. The Steam game folder, r2modman **Mods** profile and manager cache now contain 0.1.2. No hot reload was performed. Relaunch Valheim to load this version.

This update adds wooden/iron over-fire cooking-rack automation; native product ejection followed by a 60-second player pickup window; typed product caps with Enter/Save; corrected wood/coal recipe matching; legacy cap migration; processing progress/blockage messages; and safeguards for invalid processor clocks. The earlier visual/UI changes from 0.1.1 remain included.

Cooking racks require a lit fire and raw food in base chests with **Production ingredients** enabled. **F9** opens the rack's per-food caps, pause and base-group settings. Cooking and completed food already on racks count toward caps; completed products eject through native handlers and remain available for pickup before collection. The automation does not remove raw food from a rack early.

Validation passed: zero build warnings/errors; **28,158 behavior assertions**; **349 binary members**; **38 Harmony hook declarations**; **17 reflection targets**. New in-game cooking, output timing and UI behavior still need playtesting.

All **18 updated paths** were verified against their installation checksums. Unrelated manager entries and existing configuration files were preserved. DLL SHA256: `4109b48d85ee4bc76dc0cb8cd08a7b0e9e8796bdd37b1a04908aff35537de82b`.

Backup: `/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.2-20260914-143330`. This backup restores the prior 0.1.1 installation. With Valheim and r2modman closed, preview restoration:

```bash
python3 '/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/scripts/install_local.py' --restore '/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.2-20260914-143330'
```

Add `--apply` to restore. The script refuses to overwrite files changed since installation. Earlier installation records and backups follow.

---

# Local update — Quartermaster 0.1.1

Updated the Steam game installation, r2modman **Mods** profile and local manager cache on 2026-09-14. The previous 0.1.0 installation record is retained below.

- Show range now lasts **60 seconds**.
- Added **ᛞᛖᛈᛟᛊᛁᛏ ᚲᚺᛖᛊᛏ** beneath the glowing gears on both long faces, with matching status tint and transfer pulse.
- Replaced panel-relative buttons with one compact bottom action bar that fits the canvas width and hides unused actions.
- Successful deliveries briefly outline the receiving chest in blue for **3 seconds**. Repeated deliveries refresh the effect; failed transfers do not trigger it. Ordinary destination chests display the outline without permanent Deposit Chest decorations.

Release build passed with no warnings/errors, **28,089 behavior assertions**, **336 binary member checks**, **26 Harmony hook checks** and **12 reflection target checks**. Inscription geometry was visually compared with a runic font reference. In-game verification of the new UI placement and effects remains pending; the user reported initial 0.1.0 testing was going well.

All **18 updated files** were verified by checksum. Unrelated manager entries were checked unchanged; conflict settings and existing Quartermaster configuration were preserved. DLL SHA256: `f22a8ea56b6f688d9cdcf0bda4f6c9a5f1c54325599c69ef3260a74eccbab5c9`.

Update backup: `/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.1-20260914-140458`. This backup restores the pre-update 0.1.0 files and manager entry. With Valheim and r2modman closed, preview with:

```bash
python3 '/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/scripts/install_local.py' --restore '/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.1-20260914-140458'
```

Add `--apply` to restore. The script refuses to overwrite files modified since installation. The older backup below restores the original installation changes rather than just this update.

---

# Local installation — Quartermaster 0.1.0

Installed on 2026-09-14 into both the Steam Valheim folder and the r2modman **Mods** profile. The manager lists it as local-Quartermaster, enabled.

- Game DLL: `/home/deck/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/Quartermaster/Quartermaster.dll`
- Profile DLL: `/home/deck/.var/app/io.github.ebkr.r2modman/config/r2modmanPlus-local/Valheim/profiles/Mods/BepInEx/plugins/local-Quartermaster/Quartermaster.dll`
- Manager cache: `/home/deck/.var/app/io.github.ebkr.r2modman/config/r2modmanPlus-local/Valheim/cache/local-Quartermaster/0.1.0`
- DLL SHA256: `3b54bf8eb86f2b3eac6ff0344558e938fcbbcd0f0b7eaa71e703a427ac791439`

Hearthkeeper, AutomaticFermenters, TimedTorchesStayLit and CandlesForever were disabled in both installations by preserving their binaries as `.dll.old`. Their r2modman entries are disabled too. InventoryLink, CraftFromChests and SmartCraftStorage were already disabled and remain so.

LightMyFire remains installed to preserve its custom barrel pieces and inventories. Its `[General] Enabled = false` setting disables automatic feeding in both game and profile configurations. Its code registers barrel pieces independently of that setting. This setting is host-controlled when joining multiplayer.

StackIncrease, BlastFurnaceTakesAll, WildGlow and other unrelated mods retain their existing settings. No world or character saves were edited.

## Verification

The package checksum matches its checksum file, and the packaged DLL matches the Release binary. All 40 installation changes were verified byte-for-byte, including manager metadata and disabled binaries. There is exactly one Quartermaster DLL in each plugin tree; no active copies of the seven overlapping mods listed above remain. Unrelated manager entries were verified unchanged.

This verifies installation only. Valheim has not been launched for an in-game playtest.

Open an ordinary chest and choose **Chest Config → Use as Deposit Chest**. Put example items in destination chests, then deposit matching supplies and close the Deposit Chest. Machine Config uses **F9**; production caps default to 200. See the package README for controls and the playtest checklist in VALIDATION.md.

## Backup and restore

Backup directory: `/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.0-20260914-064907`

`manifest.json` records every affected path, original contents and installed checksums. Original files are preserved as numbered `.bin` files. The installer rolls back changes if installation fails.

With Valheim and r2modman closed, preview restoration:

```bash
python3 '/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/scripts/install_local.py' --restore '/home/deck/Documents/ChatGPT/Valheim mods/Quartermaster/backups/install-0.1.0-20260914-064907'
```

Add `--apply` to restore. Writing game/profile files requires filesystem permission in Codex. Restoration refuses to overwrite files changed since installation; those require individual review. It restores the affected files and removes newly installed files, leaving empty directories and any subsequently generated files alone. It does not revert world changes made during play.
