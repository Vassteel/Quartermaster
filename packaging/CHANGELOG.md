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
