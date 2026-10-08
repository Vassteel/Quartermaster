## 0.1.64 — Shared chests (candidate)
- Add shared access to registered chests, drawers and Apothecary storage between compatible Quartermaster clients. Viewer changes are validated by the chest owner; conflicting changes are rejected.
- Automation and crafting supply yield while shared storage is being viewed.
- Validate ownership-request sender identity and briefly delay writes after granted handoffs.
- Add Multiplayer/SharedChests. Unsupported clients retain normal one-at-a-time access.
- Known limitations: quitting during an unsettled transaction can lose items; recovery details are logged but not persisted. Closed-chest crafting retains the existing multiplayer write-conflict risk.
- Live two-client dedicated-server validation is required before treating this candidate as verified.

## 0.1.63 — Multiplayer chest ownership
- Run base automation on the owner of its first Deposit Chest and request closed chests and stations from other clients. Skip occupied chests and delay writes after fallback ownership claims.
- Request station ownership before saving machine settings, while retaining access checks.
- Harvest honey without a desired quantity; an explicit limit, including 0, still applies.
- Ask the owner to briefly pause automation when crafting from its chest.
- Add local Multiplayer/ClaimUnansweredOwnership and Diagnostics/OwnershipTrace settings.
- Known limitation: simultaneous withdrawals or competing inventory writes can still cause item loss or duplication. The crafting hint is not a transaction guarantee.
- Two-client dedicated-server validation is pending. Please report failures with OwnershipTrace enabled, the exact in-game message and the Quartermaster.dll SHA256 hash.

## 0.1.62
- Fix auto-pickup patch failures during mod startup and patch rebuilding.
- Keep ignored-item pickup checks working when another mod replaces the normal eligibility check.
- Ensure failed patch cleanup cannot prevent shutdown or stack restoration.

## 0.1.61
- Remove creature repelling and spawn suppression.
- Fix machine settings changing the controller layout.
- Preserve saved stock when lowering stack limits; fill available chest space and drop excess in front.

## 0.1.60
- Keep automation transfers within shared Deposit Chest coverage; prevent distant same-name bases from sharing supplies.
- Separate disconnected bases in ledger machine lists, stock counts and production limits. Configure desired quantities at each base.
- Remove the extra base-hub ownership requirement for automation; retain native ownership, access and occupied-chest checks.
- Automated checks passed; live multiplayer verification remains pending.

## 0.1.59
- Allow crafting and building from accessible supply chests regardless of which player last opened them. Explicitly save material withdrawals without chest locks or ownership transfers.
- Complete funded building on the first click while retaining full material payment checks.
- Add Plant Easily batch funding: only funded plants are enabled, respecting partial/all-or-nothing settings. Guard harvest-and-replant payment; spacing remains controlled by Plant Easily.
- Automated checks passed. Live multiplayer and Plant Easily playtesting remain pending; simultaneous chest writes can still conflict.

## 0.1.58
- Remove custom chest locks, leases and ownership handoffs; use native ownership checks.
- Refresh synchronized stock for the ledger and item search without taking control of chests.
- Automation yields to occupied chests and briefly pauses after player use.
- Keep full material payment before crafting and upgrades.
- Multiplayer playtesting remains pending.

## 0.1.57
- Add Odin's Food Barrels and Dynamic Storage Piles support for sorting, crafting supply and machine supply.
- Automatically assign matching items to empty containers while honoring custom item restrictions and Chest Config settings.

## 0.1.56
- Fetch only missing craft/build materials from up to four supply chests, then release chest leases before vanilla crafting or placement.
- Exclude leased chests from crafting requirements and ingredient selection; show a message when material collection is blocked.
- Remove the unsafe post-craft material fallback that could leave upgrades unpaid.
- Check player inventory capacity before fetching. Unused fetched materials remain with the player; building can finish with hold/repeat input.
- Automated payment, storage and network checks passed. Live dedicated-server multiplayer verification remains pending.

## 0.1.55
- Require a configured desired quantity before starting new production. Existing saved limits remain in use; 0 pauses production.
- Fix text-field caret visibility and selection highlighting.
- Route delivered mail into the receiving Deposit Chest for sorting. Add ledger delivery history with sender player and origin base.
- Add beehive honey collection, respecting stock limits and storage capacity, plus an owl panic animation with angry bee motes.
- Add local Sort Inventory visibility and position controls in the ledger’s Interface tab and Configuration Manager.
- Automatic Fermenters no longer blocks Quartermaster from loading. Its fermenter automation remains incompatible; Quartermaster leaves fermenters to that mod when detected.
- Automated checks passed. Live multiplayer and new animation testing remain pending.

## 0.1.54
- Reserve storage per automation task instead of locking the whole base.
- Recover unserved storage ownership on dedicated servers when players return to a base.
- Give chest-opening requests a retry window between automated transfers while preserving active transaction and access checks.

## 0.1.53
- Remodel clay/crystal apothecary shelves and add a finished-mead shelf with stored-item displays.
- Add one modular Apothecary Drawer Cabinet with twelve independent 24-slot drawers, numbered from the top-left.
- Correct cabinet scale and stacking; make drawer cabinets deeper and shorter and move upper shelves forward for wall clearance.
- Fix invalid drawer creation and recover missing drawers without replacing surviving inventories. Previously placed cabinet variants remain supported.
- Queue mail for unloaded destinations, with saved delivery status, partial deliveries and cancellation/return of remaining items.
- Add loading-focus and shutdown diagnostics. Loading/quit freezes remain under investigation.

## 0.1.52
- Restore Quartermaster’s previously released storage furniture to the build menu.
- Preserve existing furniture inventories, recipes and dismantling refunds. Vanilla storage is unchanged.

## 0.1.51
- Replace the mailbox with a rough-built timber model, uneven chamfers, native wood textures and an updated eagle landing point.
- Protect mailboxes from rain and water wear while retaining normal attack damage.
- Add Destinations and Settings tabs, base naming, local Deposit Chest selection, distance and destination availability.
- Deliver directly to a selected Deposit Chest when it has no mailbox; unnamed bases can receive mail. Destinations must be loaded.
- Retire other specialized storage furniture from the build menu. Clay/crystal apothecary variants and their kiln remain; existing placed storage and contents are preserved. Ledger and mailbox remain available.
- Build, asset, API and automated regression checks passed. In-game testing is pending.

## 0.1.50
- Allow editing Max Stock for every listed ledger item, regardless of available production machines.

## 0.1.49
- Show 16 ledger inventory entries per page with item icons, names on hover or controller selection, and compact stock and limit controls.
- Set explicit ledger text size bounds and fall back to item IDs for blank names.

## 0.1.48
- Hardened ledger Max Stock persistence and verification; added diagnostics for unreadable ledger data.
- Assign the vanilla font before activating menu labels to prevent LiberationSans warning spam.
- Automated checks passed; in-game confirmation is pending.

# Changelog

## 0.1.47 (development preview)

- Replace the mailbox with the approved mossy hollow stump, an uneven broken top and a jagged opening. Remove the door and plaque.

## 0.1.46 (development preview)

- Use Valheim’s native old rotten stump for the mailbox, with a taller trunk, fitted door frame and attached hinges.

## 0.1.45 (development preview)

- Repair mailbox roof faces and door boards; use native plank material and correctly mapped pine bark and cut-end textures.
- Remove the eagle explanation from the mail menu.

## 0.1.44 (development preview)

- Add a per-chest animal feeding radius (10 metres by default), range preview and copied settings.
- Add requested owl collection around a selected Deposit Chest, with radius, cancel control and three-item trips.
- Keep food dispatched to animals out of owl cleanup.
- Find stored items from the ledger with a chest glow that clears when each matching chest is opened.
- Restore the original Deposit Chest gears and keep animated owl legs attached to the body.
- Add a stump mailbox with native Black Forest bark and a postal eagle that arrives for sending, then leaves. This development pass supports loaded destination mailboxes only.
- Add a separate base display name in Deposit Chest settings.
- Penguin integration and unloaded-base mail delivery remain in development.

## 0.1.42

- Recognize EquipmentAndQuickSlotsPlus when protecting equipment, quick slots and hidden rows during inventory actions.
- Yield Quartermaster menus, footer buttons and shortcuts while Configuration Manager is open.

- Fix Quartermaster disabling itself at startup because Harmony mistook a multiplayer network handler for a patch setup method.
- Add a binary regression check for accidental Harmony lifecycle method names.

## 0.1.41

- Keep shared crafting materials visible when another player owns the chest.
- Coordinate short storage reservations for crafting, building, sorting and production; recheck stock before spending and recover from interrupted handoffs.
- Allow authorized players to configure workstations owned by another peer and share workstation status.
- Building may require holding build or placing again after a storage reservation is ready. Moving the placement preview cancels that reservation.
- Multiplayer playtest build: automated protocol checks pass; live listen-server and dedicated-server testing is pending. Install the same version on the server and all players.

## 0.1.40

- Extend measured snap clearances to all 40 active storage pieces, with rear mounts for floor/wall furniture, top mounts for ceiling racks/hooks and diagonal mounts for the corner cabinet.
- Preserve front floor snaps on racks, bins and pallets, and add centered wall/ceiling attachment points.

## 0.1.39

- Move floor cabinet snap points to the rear edge so snapping against timber walls no longer pulls the cabinet halfway into the wall.
- Fit cabinet collision to the authored frame and add rear-center snap points.

## 0.1.38

- Fix missing clay and storage furniture caused by unavailable shaders; use loaded Valheim building and crystal materials.
- Remove the redundant Deposit Chest recipe while preserving already-built chests and their contents.

## 0.1.37

- Replace the legacy storage and crafting implementations while preserving deposit, sorting and crafting-from-storage behavior. Protect inventory actions if the optional equipment-slot API fails.

- Restyle chest, machine, ledger and pickup menus with native wooden panels, fonts, tabs, buttons and numeric fields.

- Add compact and wide armor wardrobes with automatic armor assignment, upright native item displays and doors that open for players and owl interactions.
- Add restrained hinge/latch sounds and owl door-handling gestures.
- Replace the pickup filter bag icon with a crossed-out magnet.

- Add arrow and bolt storage, narrow/wide weapon racks, a tall long-weapon rack, bow/crossbow wall racks and wall/floor shield racks.
- Keep wall mounts light with simple rails, pegs and shallow pockets.
- Auto-assign equipment using native item and weapon metadata; keep exceptions configurable.
- Display stored equipment models and add owl seating/retrieval gestures with subdued rack contact sounds.

- Replace wooden meat/fish rails with individual bronze ceiling plates, short chains and hooks. Each holds one stack and shows the stored item’s native model.
- Remove footer-button hover descriptions.

- Add pantry and wall produce shelves, ceiling meat/fish hooks, a grain bin and flour-sack stand.
- Add food-family auto-assignment while preserving existing ingredient/jar routing and explicit exceptions.
- Show representative pantry food and grain plus native stored meat/fish models, with owl hanging/filling gestures and an opening grain-bin lid.

- Add narrow/wide stone pallets, a masonry crib and a beam-mounted lumber rack.
- Auto-assign stone, black marble and grausten, with changing stock displays and configurable exceptions.
- Add weighted owl stone placement/retrieval and quiet stone contact sounds.

- Add an original ground-pickable Raw Clay resource with its own model, texture and inventory icon.
- Generate clay patches on low Meadows and Black Forest shores in newly generated terrain, using native collection and respawn behavior.

- Add the development jar/flask crafting chain and a dedicated Pottery Kiln using the native smelter body.
- Add clay-jar and crystal-flask cabinets with native storage, ingredient auto-assignment, searchable exceptions and server-configurable category overrides.
- Show inventory labels and representative flask fill; add cosmetic owl jar handling, retrieval and quiet contact sounds.

- Add narrow, low, wall, corner and rafter apothecary variations, with matching recipes and placement points.

- Add two lumber-rack sizes, two ingot-rack sizes, an ore/scrap bin and a hooded coal bin.
- Assign wood, metal, ore and coal automatically, with existing learned-item exceptions and configurable category overrides.
- Show bounded stock levels and material colors; add owl carry/place and bin-toss gestures with quiet contact sounds.

- Add hanging and rolled hide storage, floor/wall textile shelves, a feather coffer and a bone crate.
- Show draped skins, folded cloth, thread hanks, feather bundles and bones with automatic material assignment.
- Add gentle owl handling, a hinged coffer lid and subdued rustle, lid and bone contact sounds.

## 0.1.36

- Save production limits on the book being edited, prefer the newest saved ledger and verify limits before confirming a save.
- Combine ledger Inventory and Production into Inventory & Limits.
- Refine the lectern with chamfered timber, wood grain, leather binding, curved page stacks, stitching and brass fittings.
- Route hostile creatures outside Deposit Chest coverage and suppress hostile ambient, raid and fixed-spawner attempts inside it.
- Give the owl distance-driven steps, planted feet, terrain adjustment, slower turns and improved flight transitions.
- Make owl calls occasional and remove workstation-arrival hoots.

- Put chest actions inside an extended wooden inventory panel using vanilla buttons and explanatory tooltips.
- Keep inventory sorting and nearby machine configuration on the player panel when no chest is open.
- Replace the Pickup label with a native loot-bag icon and tuck the wooden tab behind the inventory edge.

## 0.1.35

- Fix Deposit Chests and base automation losing track of loaded chests and machines after death/respawn.

- Replay Deposit Chest sorting animations after the owl returns, including sorting completed while he was away.
- Prefer walking around the base with an alternating-foot gait and head bobs; use short flights for perches and blocked ground routes.
- Visit each active workstation in a round before returning to the chest, skipping finished or unreachable stations.
- Visit the ledger sooner and between jobs, then resume the round. Adjust ledger landing clearance.

## 0.1.34

- Fix Quartermaster sorting and transfers leaving food/material stacks separate because their unused durability values differ.
- Treat missing and empty crafter names alike, while preserving meaningful item metadata differences and stack limits.

## 0.1.33

- Add a Machines tab to the ledger with live production status, machine names, distances and search.
- Show the owl’s current activity when aiming at it: resting, sorting, visiting a workstation, collecting items, reading the ledger or returning home.

## 0.1.32

- Keep the ledger inventory and production-limit lists to items stocked, learned by storage, or currently queued in that base. Unused machine recipes no longer fill the book.
- Keep learned items visible at zero stock so their production limits remain editable.

## 0.1.31

- Fix the Ledger Lectern failing to register because of an incorrect native prefab name.
- Add the Quartermaster filter to the current Hammer build menu, containing the Deposit Chest and Ledger Lectern.

## 0.1.30

- Add a buildable Ledger Lectern with base inventory, shared production limits and live stack-size controls. Hosts and admins can switch between normal limits and a custom maximum.
- Excess items from oversized chest stacks pop out in front as normal item stacks for the player or owl to collect. Blocked or busy chests wait safely.
- The owl occasionally visits the ledger and turns its pages.

- The owl visits active workstations, throws matching ingredient props and returns to its Deposit Chest. Automation runs independently of his animations.
- Add obstacle-aware flight, hops, route recovery, takeoff and landing poses, preening, stretches, head bobs and quiet owl calls.
- After resting, the owl collects up to three dropped items per trip. Items go safely into its Deposit Chest; full storage, access restrictions and ignored pickups are respected.
- Remove the hat and replace padded eye surrounds with flatter feathered facial disks.

## 0.1.29

- Keep queued smelter status visible when new production reaches its cap. Missing fuel now points to fuel-enabled storage.
- Clarify how to restore normal stack limits with EnableStackSizes. Custom limits remain available through MaximumStackSize.

## 0.1.28

- Detailed owl model, textured plumage and clothing, and distance detail levels.

## 0.1.27

- Refresh the README with current features, setup and controls.

## 0.1.26

- Recover existing chest registration when opening or accessing a chest after character changes or delayed loading.
- Explain config access failures instead of silently ignoring the button.

## 0.1.25

- Enabled protection wards repel hostile creatures near their boundary. Normal ward permissions remain unchanged.
- Add the server-synchronized WardRepelsMonsters setting.


## 0.1.24

- Match the Pickup tab to the vanilla inventory.
- Temporarily use the vanilla black-metal model for Deposit Chests. Existing chests and contents are preserved.

## 0.1.23

- Add an inventory pickup filter. Ignore item types; manually pick one up to enable auto pickup again. Preferences save per character.

## 0.1.22

- Add a Quartermaster build tab and remodeled black metal Deposit Chest with an owl perch.
- The owl hops into the open chest, throws three items per sorted slot, then returns to its perch. Player access keeps the lid open.
- Requires Jotunn for build-menu registration.

## 0.1.20

- Fit the owl’s cloth waistcoat to its body and remove blotchy clothing texture. Preserve sorting and item-toss animations.

## 0.1.19

- Shorten the README to the approved feature overview. Gameplay is unchanged from 0.1.18.

## 0.1.18 (server test build)

- Replace the deposit gull with a burrowing owl in a cloth waistcoat and cap. Preserve three bounced/fading item tosses per sorted slot; add sleeping, head tilts and idle tracking.
- Handle every Helmsman cargo hold, keeping one-slot cadence and skipping blocked slots without losing metadata. Block unloading from unmigrated cargo.
- Synchronize server gameplay settings without overwriting client files; exclude shipboard chests from automatic base storage.
- Retain the password-input focus fix from 0.1.17.
- Configurable stack limits default to 1,000 for stackable items. Single-item equipment, item weight and saved quantities are unchanged.
- Stop a requested boat unload if its Deposit Chest changes base group; cargo stays aboard until a new request.
- Clear destroyed chest status entries and ignore inactive lid geometry when locating the gull's perch.
- Simplify equipment-slot compatibility checks without changing protected-slot behavior.

## 0.1.17

- Fix server password entry losing focus: inventory cleanup now deselects only Quartermaster controls.

## 0.1.16

- Shade the Deposit gull with the actual chest's loaded Piece shader and light-probe anchor, retaining the gull texture and UVs.
- Keep emission, gloss, chest texture noise and triplanar mapping disabled. No shader-name lookup is needed.

## 0.1.15

- Fix the missing Deposit Chest gull: clone its native material rather than requiring named Piece/Standard shaders.
- Preserve the game's lighting variants and skin texture while disabling emission and noise glow; use native material references for the helmet too.
- Clean up failed gull creation and retry, instead of leaving an empty actor that never recovers.

## 0.1.14

- Deposit gull squawks and asks where unsorted items belong, once per unchanged problem while nearby.
- Distinguish unassigned items, full assigned storage and temporarily unavailable storage. Recommend more storage only when every receiving chest has no free slots or stack capacity.
- Apply the same explanation to leftover ship cargo for Helmsman to announce.
- Rewrite GitHub and Thunderstore READMEs as compact feature and control guides.

## 0.1.13

- Deposit gull and helmet now use private, matte world-lit materials with emission and noise glow disabled. Preserve the vanilla gull texture; remove the helmet's unlit shader fallback.
- Visual acceptance in the affected indoor scene is still pending.

## 0.1.11

- Optional Helmsman integration: explicitly request boat cargo unloading through the landed ship gull near a Deposit Chest.
- Routes one cargo slot per step into matching base storage with three gull throws. Unmatched/full-storage remainders stay aboard.
- Rechecks boat/base access each step; requests stop on departure or cancellation and never auto-resume. Quartermaster alone keeps its existing behavior.

## 0.1.10

- Deposit routing now transfers at most one occupied slot per automation cycle (normally two seconds). Blocked slots are skipped so later routable items can still move; partial transfers stop the cycle and preserve the remainder.
- Each slot with a successful transfer queues exactly three cosmetic throws, independent of stack size. Visible sorting waits for the previous three-throw sequence, including under slow frames. Hidden or unavailable visuals do not stall routing.

## 0.1.9

- Fixed the floating Deposit gull: select the currently visible lid, sample its surface where readable, and align the model's lowest foot vertices to the perch. Hidden open-lid geometry no longer raises the perch or chest decorations.
- The gull follows its chest outside the chest renderer hierarchy so chest glow effects cannot make the bird or helmet emissive. The bird owns non-emissive material copies and receives normal lighting/shadows.
- Preserve cleanup on chest destruction, disabling, unloading and Deposit mode changes; update the perch when the lid opens/closes.

## 0.1.8

- Added a helmeted gull atop Deposit Chests: sorting flourishes after transfers, annoyed double pecks for unsorted leftovers, and idle gestures with occasional nearby-player glances.
- Sorting tosses use tiny mesh-only props that bounce against scenery and fade after landing; bounded lifetime, distance and count limits, no physical inventory drops.
- Gull and props hide when Deposit mode or the mod is disabled and clean up with the chest.

## 0.1.7

- Updated README testing-status wording.

## 0.1.6

- Shortened the README and gave the gull a pitch specific to this mod.

## 0.1.5

- Rewrote the README in the voice of a Viking gull selling a well-used longship. Installation, controls and testing status remain documented.

# 0.1.4

- Moved inventory action buttons to the bottom-left and kept them clear of the centre/right control hints.
- Added feathered halos to the Deposit Chest gears and runes, plus soft nearby illumination matching their blue/amber status.

# 0.1.3

- A workbench, forge or other required station inside a Deposit Chest's area now supports building, structure repair and dismantling throughout that same area.
- Uses BaseRange, checks the correct station type and access permissions, and stops sharing when the station or Deposit Chest is removed. Distant bases do not share stations just because they have the same group name.
- Crafting, item upgrades and item repair still require using the actual station. Station upgrades and their attachment distances stay unchanged.
- Added General → ExtendStationCoverage (enabled by default), a Deposit Chest config hint, and coverage regression tests.

# 0.1.2

- Added wooden and iron over-fire cooking racks: load suitable raw food from production-enabled chests, retain normal cooking times/fire requirements, remove completed food on native cooking ticks, and eject it for the 60-second pickup window.
- Cooking racks now have F9 Machine Config, separate cooked-food caps and group/pause controls. Raw and cooked items on racks count toward their product caps.

- Products now eject through native Smelter.Spawn and Fermenter.DelayedTap. Tagged production drops remain available for pickup for 60 seconds before automatic collection; full storage leaves them on the ground.
- Preserve output tags and collection deadlines by preventing nearby native ground auto-stack merges. Pending output still counts toward production caps.
- Added recovery for negative/non-finite processor accumulators and future last-update timestamps after world-clock changes, without consuming inputs or altering completed progress.

- Fixed recipe/fuel item matching: prefab references now use their actual item IDs, so ordinary wood, coal and other stocked ingredients match before recipe prefabs run Awake. The same fix aligns stored/queued output counts with production caps.
- Preserve earlier cap values by migrating legacy display-name keys to the corresponding output item IDs.
- Loaded processors show their actual vanilla processing timer and queue size, including full queues, instead of incorrectly claiming ingredients are missing. Smoke, roof, fuel and wind blockages are identified.
- Added regression checks for repeated kiln cap edits, separate smelter product caps, uninitialized recipe metadata and legacy cap migration.

- Replaced the +50 production-cap button with an outlined numeric Cap field and explicit Save button. Enter also saves; accepted values remain 0–100000.
- Saving a production cap keeps the input and buttons alive instead of rebuilding the modal during the interaction, allowing repeated edits without reopening Machine Config.

# 0.1.1

- Destination chests briefly glow with a blue outline for three seconds after a successful automatic delivery. Repeated deliveries refresh the same effect; failed transfers do not trigger it.

- Replaced panel-relative action buttons with a compact, responsive bottom toolbar. Chest and player actions no longer spill into the chest header, and hidden actions leave no gaps.

- Show range now lasts 60 seconds; updated the button and status message.
- Added ᛞᛖᛈᛟᛊᛁᛏ ᚲᚺᛖᛊᛏ beneath the gears on both long faces of Deposit Chests. The runes share the gears' glow, status color and transfer pulse, with geometry that does not require runic font support.

# 0.1.0

- Added AI disclosure and the Thunderstore AI Generated category publishing note.

- Named the mod Quartermaster; updated plugin/package identity, project paths, documentation and icon. Existing world-data keys remain compatible.

- Initial local playtest build combining chest storage, base supply and fermentation.
- Physical Deposit Chest roles for all standard chest tiers, persistent learned types, preferred destinations and optional overflow.
- Replaced Chest Rules with Chest Config, item memory controls, independent supply permissions and copy/paste.
- Added shared base supply, fermentation, queued-output-aware production caps, protected wood inputs and per-device pause/status.
- Added procedural glowing, animated gear decoration and a temporary range preview.
- Removed remote matching deposit, consolidation, reserves, stack overrides and new chest-size overrides. Previously enlarged chest capacity is preserved.
- Build, binary linkage and deterministic transfer/policy checks pass. In-game and multiplayer playtests remain pending.
