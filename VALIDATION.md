# Quartermaster 0.1.24 validation

Release build: zero warnings/errors. 28,818 behavior assertions, 28 pickup-filter checks, 27 material checks, 14 stack-limit checks and 8 UI focus regressions pass. Binary inspection resolves 585 members, 53 Harmony hooks and 19 reflection targets with zero failures.

The Pickup tab now clones the installed game's Armor tab, including its actual `bkg` sprite, native dimensions, outlined font/material and yellow count. Native asset inspection confirmed the background is a child, not an Image on the Armor root. The caption is smaller white text above the count; a native tooltip explains the filter and controller shortcut.

The custom Deposit Chest mesh and placement bounds are no longer attached during registration. The existing prefab ID, native black-metal model/icon, storage, recipe and owl decoration remain. API validation checks that custom-model construction stays disabled. Owl chest-entry animation is inactive without the custom model.

In-game appearance and existing placed-chest load still need a playtest. README files are unchanged.

## Previous release validation

# Quartermaster 0.1.23 validation

Release build: zero warnings/errors. **28 pickup filter checks**, **28,818 existing behavior assertions**, **27 material checks**, **14 stack-limit checks** and **8 UI focus regressions** pass. Binary inspection resolves **589 members, 53 Harmony hooks and 19 reflection targets** against the installed game, with zero failures.

The pickup filter checks cover individual item types, unchanged shared drop flags, local/remote player isolation, character save reloads, disabled behavior, full/partial/failed manual pickups, nested automatic pickup context, and transpiler shape changes. Tests use lightweight game/Harmony stubs; binary inspection separately confirms eligibility runs before attraction/ownership requests and both manual pickup paths reach the hooked method.

## Pickup playtest still required

- Open inventory and confirm the Pickup button fits between armor and weight at the normal UI scale and on Steam Deck. Controller shortcut: hold L-stick and press Y; use D-pad/A/B in the dialog.
- Ignore resin: carried items stay put, resin on the ground stays put, and other types still collect. Manually collect resin (including a partial stack) and confirm automatic resin pickup resumes. A failed pickup must keep the filter.
- Verify save/rejoin persistence, character isolation, and another player collecting a locally ignored drop, including ownership handoff.
- Confirm chest/machine configuration, native global auto-pickup toggle, and password-screen focus remain usable.

No in-game visual or multiplayer acceptance pass is claimed. README files are unchanged.

## Previous release validation

# Quartermaster 0.1.18 validation

Release build: zero warnings/errors. **28,806 behavior assertions**, **27 material checks**, **14 stack-limit checks** and **8 UI focus regressions** pass. Binary inspection resolves **533 members, 47 Harmony hooks and 19 reflection targets** against the installed game.

Behavior tests include multiple ship holds, blocked first holds, one-slot cadence, quantity/metadata conservation, unloading cancellation and normalized base groups. Stack tests cover configurable fixed limits, equipment exclusions, reloads and untouched existing quantities. Password focus remains checked over 300 pre-player frames.

The burrowing owl uses original faceted geometry, a cloth waistcoat and a world-lit material on a separate perch. Three tosses per sorted slot remain. Server gameplay settings are synchronized without overwriting local config files. Ordinary boat holds cannot become automatic base-storage candidates.

## Still to verify on the server

- Matching server/client settings and 1,000-item stacks with StackIncrease disabled; confirm quantities when lowering limits.
- Owl feet, texture, lighting, sleeping/head tracking and three bouncing/fading tosses on each chest type.
- Explicit unloading across every imported cargo hold, blocked migration/access states and concurrent players opening storage.
- Craft/build range, machine caps, daylight fishing output collection and restart persistence with the combined mod set.

No real multiplayer or GPU acceptance pass is claimed by these automated checks.

## Historical checks

## Chest lighting for the gull — 0.1.16

The user confirmed 0.1.15 restored the bird but it still looked glowing. Native asset inspection shows the gull uses Custom/Creature and the black-metal chest uses Custom/Piece, with emission already black on the bird. This update uses the actual chest renderer's shader reference and probe anchor, retaining gull textures and disabling glow/gloss/noise. The exact GPU cause of the earlier brightness is not proven; in-game appearance remains pending.

Zero build warnings/errors; 28,799 behavior assertions, 27 material assertions and 478 binary members / 44 Harmony hooks / 19 reflection targets pass. Check the gull in the same indoor scene and outdoors, including with lights off. Confirm the new chest-shader message in the log.

## Missing gull fix — 0.1.15

The live 0.1.14 log showed `No supported world-lit shader for the Deposit gull` from material creation. The failed attempt left an empty actor. Native prefab material cloning removes the failed lookup; creation now cleans up and retries on exceptions.

Release build: zero warnings/errors; 28,799 behavior assertions, 27 material assertions and 470 binary members / 44 Harmony hooks / 19 reflection targets pass. The new regression case makes named shaders unavailable and verifies native material/texture preservation and emission suppression. These tests do not simulate GPU rendering; a fresh in-game visual check remains required.

## Gull feedback — 0.1.14

- 28,799 behavior assertions and 20 material checks pass. Added 13 checks for assignment versus capacity advice, busy matching storage, remaining capacity for other items, and announcement cooldown/rearming.
- 473 binary member references, 44 Harmony hooks and 19 existing reflection targets resolve; the chat visibility field is also verified.
- In-game acceptance pending: native gull sound, visible chat, item localization, silent repeats, multiple nearby Deposit Chests and cargo leftovers. GPU appearance and multiplayer remain untested.

## 0.1.13 Deposit gull lighting

- Replaced inherited creature material state with a fresh supported `Custom/Piece` material (lit Standard fallback). Keep vanilla albedo, tint and UV mapping; disable emission, noise-glow variants, gloss/reflections, rain/snow and piece texture-noise defaults. Helmet uses the same factory with no unlit fallback. Original game assets remain untouched.
- Inspected the installed game's Seagal material and native `Custom/Piece` shader properties. The former already has black emission at rest; the screenshot alone does not establish the runtime cause. This changes the lighting/material path rather than repeating the previous emission-color-only cleanup.
- Release build: zero warnings/errors. 28,786 behavior assertions; 20 material-state regression assertions; 452 binary members, 44 Harmony hooks and 19 reflection targets resolve. Material tests cover contaminated source state, texture/tint/UV preservation, source isolation, helmet settings and supported-lit-shader fallback; they do not simulate GPU output.
- Live acceptance pending: compare the gull in the reported indoor scene with nearby wood, then daylight and darkness. Check that feathers/helmet respond to local light without self-illumination, with texture, perch and all sorting/idle gestures intact. Chest rune lighting remains enabled. Multiplayer untested.

## 0.1.12 requested ship unloading

- Added 24 request-loop assertions using shipped unloading/transfer code with host access/routing doubles: request-only execution, one-slot sequencing, throttle, quantity conservation, preserved visual snapshots, blocked/partial/full storage, destination group/range/access filters, cancellation, player changes and no auto-resume. Native access predicates still require live acceptance.
- Existing inventory, deposit-gull and station checks remain included. The binary API checker now also resolves the ship's private helm-occupancy method.
- Live acceptance pending: with both updated mods, approach a Deposit Chest without requesting (cargo must stay put); speak to the landed ship gull, request unloading and observe three bouncing/fading props per transferred slot. Check counts/metadata, unmatched/full storage, stopping mid-animation, leaving range, opening the hold and restarting the world. Repeat with either mod absent. Multiplayer untested.

## 0.1.10 sequential-slot sorting

- Release build: zero warnings/errors. 28,742 behavior assertions pass; 436 binary members, 44 Harmony hooks and 18 reflection targets resolve against the installed game.
- Production routing tests verify visible slot order, one successful slot per cycle, whole-stack transfers, skipped blocked slots, partial remainders, empty inventories and exact quantity conservation using the shipped transfer implementation.
- Production throw-schedule tests verify three spaced throws per slot, no fourth throw, slow-frame completion and clearing on hide/unload. The current visible gull must complete its three attempts before the next slot routes; hidden/missing visuals do not block automation. Existing global prop limits and unsupported-item visual fallbacks remain in effect.
- In-game acceptance pending: place several distinct stacks into Deposit, close it, and observe one stack route per normal two-second cycle with three throws; repeat with an unmatched first slot and partly full storage; confirm the remaining stacks and counts stay correct.

## 0.1.9 perch and lighting fix

- The installed black-metal chest prefab contains separate Closed and Open lid models. The old all-renderer bounds included the hidden raised lid. Perching now uses the visible lid alone and intersects its readable triangles at the perch point; unreadable meshes fall back to that lid's bounds. Foot vertices are aligned before the deformation rig/helmet are created.
- WildGlow 0.4.3 is installed and enumerates child renderers for chest emission. The gull now lives outside that hierarchy, follows the chest transform, uses private non-emissive bird materials and explicitly enables shadows. It remains owned and cleaned up by the chest component.
- Release build: zero warnings/errors. 28,725 behavior assertions pass, including flat/sloped/reversed/edge/outside/degenerate/vertical triangle cases. 436 binary members, 44 Harmony hooks and 18 reflection targets resolve. Binary checks also guard detached-root creation and disable/destroy cleanup.
- Live verification pending: closed/open black-metal lid contact; wooden/reinforced chests; gull/helmet in a dark room with WildGlow enabled; disable/re-enable Deposit and dismantle/unload the chest without leaving birds or props behind.

## 0.1.8 gull validation

- Release build: zero warnings/errors. 28,718 behavior assertions pass; 426 binary members, 44 Harmony hook declarations and 18 reflection targets resolve against the installed game.
- Added routing-state checks for partial success, blocked leftovers, manual removal, open/inaccessible chests, final-item transfers, polling and reset. Added bounded bounce/fade lifetime checks.
- Binary inspection rejects item-prefab instantiation, real drops, inventory mutations, colliders, rigidbodies or network components in the gull/prop implementation.
- Manual acceptance pending: inspect helmet while idle/sorting/pecking; test wooden, reinforced and black-metal chest lids; route items to matching storage, then fill/block/remove destinations; manually empty leftovers; watch props bounce/fade on wooden floors, slopes and terrain; disable Deposit/mod and dismantle the chest; leave/rejoin the world. Verify normal chest access and exact inventory counts. Multiplayer untested.

# Quartermaster 0.1.5 validation

Version 0.1.5 updates the README voice and release metadata.

Date: 2026-09-14. Version 0.1.0 was installed locally and the user reports initial playtesting is going well, including the gear appearance. Version 0.1.1 adds a 60-second range preview and glowing runic inscriptions; these changes, the revised bottom action bar and the three-second receipt outline still need in-game visual verification.

Version 0.1.2 replaces the +50 cap button with explicit numeric entry and Enter/Save. The cap save path no longer rebuilds the modal during input interaction. Recipe/fuel identity matching now uses prefab component names instead of nonserialized recipe ItemData references. Tests reproduce the old display-token versus prefab-ID mismatch for wood, coal, ore, metal and mead bases, and verify legacy cap migration. Loaded processors now report vanilla bake progress and smoke/roof/fuel/wind blockages. The user confirmed a manually loaded kiln stalled for several minutes with no queue decrease, then resumed without an installation or hot reload. The log has no kiln exception. LongerDays adjusts world time during saves/config changes; this is a plausible timer cause, not a confirmed diagnosis. A narrow guard now repairs negative/non-finite accumulated time and future update timestamps while retaining input queues and bake progress. Products now eject through native spawn/tap methods and wait 60 seconds before collection. API checks verify the smelter, fermenter and cooking rack production paths call the observed ItemDrop.OnCreateNew overload and that the smelter observer cannot suppress native Spawn. Policy checks cover pickup grace boundaries and invalid timer recovery. Live repeated-edit, supply and delayed collection testing is pending.

Cooking rack support covers the wooden/iron over-fire stations. Native cooking tick hooks remove completed items outside the rotating work budget. Checks cover raw/done/burnt slot handling, per-food queued allowance, native output tagging and the private loading/removal/fire-check methods. In-game cooking and all new 0.1.2 changes still need a playtest.

Version 0.1.3 extends only the build-station lookup within a shared Deposit Chest sphere. Twenty-five new checks execute the actual coverage source against host stubs: radius and height boundaries, exact station type, live/accessible hubs and stations, ward restrictions, immediate removal/config changes, distant bases, preservation of native results, and preservation of the original point before vanilla flattens its height. API checks verify the static station registry and both build/removal call paths. Crafting interaction, station levels and extension distances are not patched. Live building/repair and multiplayer still need playtesting.

Version 0.1.4 anchors the inventory actions 16 canvas units from the left edge and 12 from the bottom, fitting inside the left 28% to avoid the shown control hints. Gear and rune halos use a bilinear alpha falloff texture; two short-range, shadow-free lights tint nearby surfaces and switch off beyond 25 metres. Owned halo materials, meshes and texture are destroyed with the chest decoration. The layout and glow need an in-game visual check with the user's UI scale and chest tiers.

## Completed

- Release compilation against the user's installed Valheim/BepInEx managed assemblies: **0 warnings, 0 errors**.
- Binary linkage: **370 members** resolve to the installed game/runtime assemblies.
- Harmony target inspection: **44 hook declarations** match their target methods and injected parameter types; **18 private reflection targets** match. Each of the three material-count transpiler targets has one expected `Inventory.CountItems` call site. These are static checks, not a live `PatchAll` run in Unity.
- Verified incompatibility identities against installed plugin metadata: `MaddCatter.Hearthkeeper` (0.1.7) and `TastyChickenLegs.AutomaticFermenters` (1.1.2).
- Shipped policy/transfer source compiled into a separate deterministic behavior harness: **28,200 assertions passed**, including **1,000 randomized inventory trials**.
- Behavior checks: quantities and item metadata conserved under repeated partial/full transfers; no over-limit stacks created; no colliding grid slots; callbacks see both inventories after the transfer; different custom data does not merge; learned types survive emptiness; forgetting does not immediately relearn existing contents; restoration; deposit/overflow routing policy; group normalization; protected wood; whole-batch caps; zero caps; arithmetic overflow; many producers sharing stock and queued allowance; restarting after consumption.
- Config-model roundtrip checked using the test host's JSON serializer. Unity `JsonUtility` and actual ZDO save/reload require the live test below.
- Source scan confirms no F6/F7 bindings, remote Store Matching, consolidation code, custom reserves or stack-size controls in the maintained `src` tree. Unused recovered upstream code is archival only and is not compiled or shipped.
- Existing Hearthkeeper dimensions are read before a container's inventory is constructed; no new chest resizing is enabled.
- Original 256×256 package icon inspected. The release ZIP is checked for integrity, expected metadata and exactly one DLL. No game assemblies or AutomaticFermenters code/assets are included.

## Remaining in-game checks

Run on a copy of a world with the two originals and overlapping automation mods disabled:

- Put a workbench near one edge of the Deposit Chest area, then build and repair near the opposite edge. Repeat with forge/stonecutter pieces; a workbench alone must not satisfy those station types. Remove the station or Deposit role and confirm extra coverage ends. Test above/below the storage sphere, wards, another player owning the chest/station, and a separate base with the same group name. Confirm crafting/upgrading/item repair still requires station interaction, and Show Range matches the storage area.

1. Load the mod and check BepInEx for patch/runtime errors. Create/open each chest tier; check Chest Config, animated gear and inscription positions in open/closed states. Read the inscription from both sides and verify Show range remains visible for 60 seconds. Confirm successful deliveries briefly outline the destination chest, repeated deliveries refresh the effect, and blocked transfers cause no flash.
2. Teach wood and coal destinations, empty them, save/quit/reload, then deposit both types. Verify quantities exactly and confirm remembered destinations still work.
3. Fill a destination. Verify the remainder stays in the Deposit Chest, then moves after space opens. Exercise preferred destinations, explicit overflow, Forget/Restore and settings copy/paste.
4. Set the kiln cap repeatedly (for example 250 → 800 → 125 → 0) using both Enter and Save. Reopen Machine Config and verify the last saved number persists. Empty, negative and over-100000 values must show a validation message without changing the saved cap. Put 500 wood in a supply chest, set a coal cap of 200, and run multiple kilns. Verify stored + queued + tagged pending output stops new loading at the shared cap. Consume coal and confirm loading resumes. Verify fine/core wood remain untouched unless explicitly enabled.
5. Test wooden and iron cooking racks with their valid meats/fish: fire absent/present, full slots, supply disabled, food cap reached, completed-food removal before burning, pause/resume, and direct player pickup. Ensure no raw meat is removed early and automatic harvest creates one item per input. Then run smelters, blast furnaces, spinning wheels, windmills, fires/torches and fermenters. Verify normal fuel, timing and shelter requirements. Block output storage, including while a machine already has queued inputs. Every completed output, including tapped fermenter batches, should eject normally and remain available to the player for at least 60 seconds. Pick up some output before the deadline and verify it is not duplicated. Leave other output and verify later collection and destination glow. Save/reload during the grace period. Place an ordinary player drop beside products and verify it remains uncollected. Fill storage and confirm outputs remain physical; free space and confirm retries collect them. Confirm delayed output remains counted against caps.
6. Exercise crafting, building, upgrades and chest supply opt-outs. Verify costs exactly, including item quality/world-level checks and hotbar/equipment/quest protection in Deposit All.
7. Feed hungry tamed animals; verify food consumption and that wild tameables receive food only after opting in. Check save/reload while food is pending.
8. Check mouse, keyboard and controller/Steam Deck navigation at 1280×800 and 1280×720, including long item labels, text entry, modded inventories, large chest grids, the full-screen input shield, closing and reopening config, and moving out of reach.
9. Check a large base with 200+ chests, many machines and 100-metre coverage. Measure actual frame time and cycle latency. The implementation uses registered objects and bounded rotating work, but no in-world performance measurement has been made.
10. Test two players, private chests, wards, open containers, different base groups, ownership handoffs and disconnects. The current coordinator changes only objects it already owns and can pause with split ownership. Test dedicated-server behavior separately; do not treat this package as validated for unattended or unloaded-world automation.

## Practical limits

- Automated checks do not launch or modify a world. See INSTALLATION.md for local deployment history.
- Simulation covers the actual transfer/policy source with minimal host interfaces, not Unity physics, network persistence, container callbacks from other mods or full machine implementations.
- Decoration is procedural glowing gear and rune geometry on existing chests, not replacement texture maps or newly registered hammer pieces. Deposit roles inherit the chest tier's existing crafting cost and capacity.
