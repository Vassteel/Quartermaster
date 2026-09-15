# Quartermaster guide

A local playtest build combining learned chest storage, base supply and fermentation in one mod.

## AI disclosure

Quartermaster was developed using OpenAI Codex. Significant portions of the mod code, tests, documentation, and procedural artwork code were AI-generated from user-provided requirements and feedback. The package icon uses generated artwork; in-game gear decorations are rendered procedurally.

**Thunderstore category: AI Generated.** Select this category when publishing the mod, as required by [Thunderstore's AI disclosure notice](https://old.thunderstore.io/c/valheim/). The README disclosure does not replace selecting the category on the listing.

## Start using it

1. Disable Hearthkeeper and AutomaticFermenters before enabling Quartermaster. They must not run together. Other overlapping storage, crafting-supply, feeding or refueling mods also need to be disabled for the playtest.
2. Build or open an ordinary chest. Click **Chest Config → Use as Deposit Chest**. Every supported chest tier can become a Deposit Chest: wooden, reinforced/iron, personal, black metal and ashwood. It keeps its normal model, build recipe and existing capacity, and gains two animated glowing gears and the inscription **ᛞᛖᛈᛟᛊᛁᛏ ᚲᚺᛖᛊᛏ** on each long face. No extra hammer recipes are required.
3. Put example items in your other storage chests. Each remembers the item types, including after you take the last item out. Items already present when the mod first loads are learned too.
4. Put incoming supplies into the Deposit Chest, then close it. Matching storage receives them and briefly glows with a blue outline for three seconds. Further deliveries refresh the outline; it fades away on its own. Unmatched items stay in the Deposit Chest; full or busy storage is reported on hover. Production outputs eject into the world first. After a 60-second pickup window they use this routing, with the Deposit Chest as a fallback.
5. Aim at a machine and press **F9** to open **Machine Config**. Each product starts with a cap of **200**. Click the outlined **Cap** number field, type the desired quantity, then press **Enter** or **Save**. You can edit and save repeatedly without reopening the panel. A cap of **0** stops loading that product. The lowest cap among machines making the same product in the same base group applies to that product across the group. Queued production is included. Changing a cap does not cancel material already processing.

**Player inventory is never automatically restocked.** Take what you want from your storage chests yourself. Deposit All works only at an open Deposit Chest and leaves the hotbar, equipped gear, quest items and supported Equipment and Quick Slots cells alone.

## Chest Config

- **Storage:** Deposit mode, accepting matching items, preferred destination, automatic learning, searchable remembered item list, Forget/Restore, and Forget All. Forgetting remains effective while the old item is still physically present; Restore explicitly permits it again.
- **Supply:** independent permission for crafting/building/upgrades, fuel, production ingredients and animal feeding. Feeding animals being tamed is an additional opt-in. A Keep Private preset disables all automated withdrawals. Personal-chest and ward access still apply.
- **Base & Copy:** base group, optional overflow destination, sorting and combining compatible stacks within this chest, and copying settings with or without remembered types. Overflow chests accept any item but do not automatically learn their temporary contents.

Deposit Chests are intake points; processing and feeding draw from ordinary storage. Crafting supply can be controlled independently. Source chests are not rebalanced against each other. There are no storage quantity targets, stack-size overrides, consolidation actions or F6/F7 bindings.

Matching is by exact item prefab, not broad categories. Stacking additionally preserves quality, variant, world level, crafting provenance, durability and custom data. Existing stack sizes are respected.

## Building coverage

Place the required workbench, forge, stonecutter or other station anywhere inside a Deposit Chest's **BaseRange**. That station supports building, repairing and dismantling structures throughout that same storage area. Both positions must be inside the same chest's radius; separate bases do not borrow stations just because they share a group name. Stations must be loaded and accessible. Removing the station or disabling Deposit mode removes the extra coverage. The station's normal nearby coverage still works.

Crafting, item upgrades and item repair still require walking up to the station. Recipes, station levels, shelter/fire requirements and upgrade attachment distances retain their normal checks. Set **General → ExtendStationCoverage = false** to disable the added building coverage. This uses the Deposit Chest's **BaseRange**, independently of **CraftRange** for container materials.

## Machines and animals

Supplies normal player-built Fireplace and Smelter pieces: fires, torches, kilns, smelters, blast furnaces, spinning wheels, windmills and other compatible standard-component processors. Wooden and iron over-fire cooking racks load suitable raw food from chests with **Production ingredients** enabled. They require a lit fire, keep normal cooking times, and remove completed food on cooking ticks before it burns while automation is active and the rack is loaded/owned. Finished food ejects for the 60-second pickup window; **F9** opens its per-food caps and pause/group settings. Paused or unloaded racks retain normal game behavior.

Fermenters load valid mead bases, ferment with ordinary timing and shelter requirements, then tap and eject the batch through the game’s normal output handling. Normal crafting, building and item upgrades can use eligible container materials while retaining their normal station and recipe checks. This does not add autonomous food crafting at recipe stations.

Kilns and compatible processors do not consume fine wood, core wood, Yggdrasil wood or ashwood unless **Allow valuable wood** is enabled on that machine. Machines pause input when output capacity or production allowance is exhausted. Loaded processors report their current processing time and queue size; smoke, roof, fuel and wind blockages are reported separately. A switched-off fire stays off. Pausing automation does not stop vanilla processing already underway.

Feed-enabled storage supplies normal food to hungry, calm tameable creatures. Food is placed for the normal feeding AI to eat, preserving its hunger/taming behavior. It is consumed from storage, never created for free.

Finished products eject normally and remain on the ground for at least **60 seconds**, available for player pickup. Quartermaster then collects only tagged production output when storage has room; otherwise it stays on the ground and collection is retried. The delay is saved with each output and survives reloads. These items count toward production caps throughout the pickup window. Native ground auto-stacking is suspended for tagged output and nearby drops to preserve their separate collection deadlines and prevent player-dropped items from merging into automated pickups. Ordinary player drops are not vacuumed into storage.

Processors recover from negative/non-finite accumulated time or a future last-update timestamp after a world-clock correction, preserving queued inputs and completed bake progress. This is a narrow timer safeguard; it does not bypass fuel, smoke, roof, or wind requirements.

## Controls

- Bottom action bar while a chest is open: **Chest Config**, **Deposit All** (Deposit Chests only), **Sort Chest**, **Sort Inventory**. The bar fits the screen width and hides unused actions.
- Inventory: **Sort Inventory** preserves hotbar, equipped and supported special slots. With no chest open, **Machine Config** appears for the last machine you aimed at while it is still within interaction distance.
- **F9**: config for the nearby machine/fire under the crosshair; rebind in the BepInEx config.
- Controller: hold **left-stick click + A** while inventory is open to open Chest Config, or the last hovered machine's config when no chest is open. Inside config, D-pad selects, A activates, B closes. Text/number fields use the device's keyboard input (Steam Deck keyboard when needed).
- Deposit Chest Storage tab: **Show range** draws coverage rings for 60 seconds.

## Base range and multiplayer limits

The default radius is **100 metres** around each Deposit Chest. It can be configured from 10–200 metres in `BepInEx/config/local.valheim.quartermaster.cfg`. Deposits with the same base group form a network over their loaded coverage areas. Give nearby independent bases different group names. Crafting has its own 100-metre default range.

This version processes **loaded objects only**, while a player is present. It does not keep distant zones loaded and does not run offline base simulation. Larger radii cannot force unloaded chests or machines to simulate.

Network mutations run only on the current owner. The first accessible Deposit Chest by network ID coordinates its group; only containers and devices already owned by that peer can be changed. Open chests are skipped. Split multiplayer ownership can therefore pause some transfers or machines until ownership reunites. Full dedicated-server and ownership-handoff playtesting remains outstanding; this version is intended for an isolated single-player/local-host playtest first. Do not interpret compile/linkage tests as multiplayer validation.

## Install the local build

Manual: extract the ZIP's `BepInEx` directory into the game/profile root. For r2modman, use its local mod import with the provided ZIP and disable the two original mods in that profile first. This package does not modify installed copies automatically.

On first load, existing Hearthkeeper-recorded chest dimensions are preserved to avoid shrinking occupied storage. Old reserve and consolidation controls are not migrated. Learned types and new settings are stored on chest/machine network objects in the world save. Use a copy of an existing world for the first playtest.

## Validation status

Compiled against the installed Valheim managed assemblies. Automated checks cover binary API and Harmony hook matching, transfer conservation, item metadata, production-cap arithmetic, memory policy and randomized inventories. The Unity UI, gear placement across chest tiers, long-range operation and multiplayer handoffs still need an in-game playtest. See `VALIDATION.md` for exact results and the playtest checklist.

## Deposit gull

A small helmeted gull perches on each Deposit Chest’s visible lid. Its feathers and helmet use normal scene lighting. Deposit sorting handles one occupied slot (its stack) per cycle, normally every two seconds, skipping blocked slots. Each successful slot transfer produces three tiny visual throws of that item; the next visible slot waits for those throws to finish. They bounce on nearby floors or terrain and fade within a second of their first floor bounce; props that miss a floor expire within four seconds. They cannot be picked up and do not change inventory counts. At most 24 props exist locally, with three throws queued per chest.

After the flourish, items without a destination or with full/busy storage make the gull double-peck the lid and glare. Empty chests return to ordinary idle gestures; the gull occasionally glances toward a player within eight metres. Open/inaccessible chests idle. Decorations stop beyond 30 metres, on disabling the mod or unmarking Deposit mode, and are removed with the chest.

Multiplayer untested. Routing feedback is local to the client processing the chest.
