# Changelog

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
- Removed remote matching deposit, consolidation, reserves, stack overrides and new chest-size overrides. Previously enlarged Hearthkeeper chest capacity is preserved.
- Build, binary linkage and deterministic transfer/policy checks pass. In-game and multiplayer playtests remain pending.
