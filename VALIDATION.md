# 0.1.40 — snap clearances across storage furniture

Extended the measured support-envelope snap pass to all 40 active furniture layouts. The four already-corrected cabinets retain their anchors; 36 further layouts change. Floor furniture has rear wall-clearance points and keeps existing front floor-snapping support. Wall pieces have rear-plane mounts plus a centre point. Ceiling pieces attach above their highest plate/beam surface; individual meat and fish hooks retain one mount. The corner cabinet uses both diagonal support planes of its convex collision shape.

Compared generated output with the 0.1.39 snapshot: mesh vertices, inventory sockets, layouts and all 56 art PNGs are identical. All seven shelved layouts retain their original snapping data. Native collision/placement rules remain enabled. Each active layout stays within the runtime 16-anchor limit.

Release build passed with zero warnings/errors. Passed 250,584 geometry/binary/art checks, 3,400 furniture checks, 13 native-material/init checks, and all 849 binary references, 64 hooks and 35 reflected targets. Embedded resources and assembly/plugin version 0.1.40 were verified. Evidence: output/all-storage-snaps/checks.log. These checks measure geometry and host behavior; actual wall, corner and ceiling placement needs an in-game playtest.

Installed and verified 0.1.40 after Valheim closed and the installer dry run passed. Both native-game and r2modman Mods DLLs match the Release build, and both manifests report 0.1.40. Rollback backup: backups/install-0.1.40-20260918-204137. In-game snapping acceptance remains pending. No README edits, commits or publication.

# 0.1.39 — cabinet wall snapping

The four freestanding apothecary cabinet layouts had anchors at local depth zero, inside the cabinet body. Native Player.UpdatePlacementGhost aligns those anchors with a wall's snap plane and then calls TestGhostClipping with a 0.2m penetration threshold. Aligning the old points leaves roughly a quarter metre of cabinet behind the plane even before wall thickness, explaining the reported obstruction. Native placement IL is recorded in output/placement-fix/native-placement-il.txt.

Clay Jar Cabinet, Crystal Flask Cabinet, Narrow Clay Cabinet and Low Clay Cabinet now use six rear-face anchors: left, centre and right at the actual frame's bottom/top. Anchors sit 1cm behind the rear mesh face. The original pair's hand-sized box colliders now follow the same frame bounds as other furniture. Native placement restrictions, wall collisions and clipping checks are retained. Mesh vertices, inventory sockets, dimensions, textures and icons remain unchanged; only these four layouts' snap coordinates changed in the authored asset.

Release build passed with zero warnings/errors. Geometry validation passed 248,856 checks, including reproduction of old over-penetration and corrected snaps within native tolerance for timber half-depths 0.025–0.175m. Also passed 3,400 furniture checks, 13 material/init checks, all 849 binary references/64 hooks/35 reflected targets, and exact embedded-resource comparisons. These are geometric/host checks; actual snapping against the user's wall requires a playtest. Logs: output/placement-fix/checks.log. No README changes or commits.

Installed and verified 0.1.39 after Valheim closed and the installer dry run passed. Rollback details: output/placement-fix/install.log. In-game snapping acceptance remains pending.

# 0.1.38 — missing furniture registration fix

The user's actual 0.1.37 game log showed two registration failures: ClayModel rejected the Flint template's shader, and ApothecaryArt required a stripped Unity Standard shader. Consequently only the independently registered chest/ledger appeared. Evidence retained in output/registration-fix/before.log.

StorageMaterials now obtains opaque shader references from native building materials and copies the loaded crystal-wall material for flasks, retaining native transparency properties/keywords. Missing optional crystal material uses an opaque fallback and a warning without blocking registration. Apothecary art initialization only becomes ready after all resources load and clears partial allocations on failure. The redundant chest is registered only as a network prefab for existing saves; it no longer receives a Hammer recipe or Quartermaster menu entry. The ledger and ordinary configurable chests remain available.

Release 0.1.38 builds with zero warnings/errors. Passed 13 material/resource-initialization regressions reproducing a stripped Standard shader and incompatible Flint shader, including all 131 authored model records and failed-initialization recovery; 7 legacy chest/ledger registration checks; 14 build-menu checks; 27 clay checks; and 3,400 furniture checks. All 849 binary members, 64 hooks and 35 reflected targets resolve. All 57 furniture resources and the pickup icon match their source assets. Logs are in output/registration-fix/. Native-prefab material appearance and menu visibility still require the next in-game launch; host fixtures do not validate Unity rendering.

Installed and verified 0.1.38 in the native game and r2modman Mods profile after a successful dry run. Rollback backup: backups/install-0.1.38-20260918-201957. No commit or publication. The compatibility-request README draft remains awaiting approval and was not applied.

# Storage core replacement — 0.1.37 installation candidate

Replaced WarehouseService, CraftingPatches and ExternalSlotCompatibility, plus all inherited counting, reserve, removal and type-query helpers in InventoryTransfers. Quartermaster-authored metadata-safe transfers, overflow ejection and world pickup remain intact. The new implementation uses a shared supply enumerator, protected-cell sort planning, nested crafting scopes with captured spending receipts, shared item matching and cached typed optional-API delegates. A broken detected equipment API protects slots instead of exposing them to bulk actions. Existing storage/save keys and caller signatures are unchanged.

Release builds without warnings/errors. All 29,068 existing behavior checks pass. Added 42 linked-production storage/crafting/equipment checks and two optional-mod-absent checks, including mixed player/storage payment, reservations, rejected removal, partial deposits, fixed slots, sorting stability, nested crafting, exception cleanup, player changes, upgrader ingredients and transpiler branch/exception metadata. The host harness uses Harmony contracts because the game's bundled Harmony cannot initialize under .NET 8; separate checks against the real installed assemblies resolve 847 members, 64 hooks and 35 reflection targets. Eight legacy-capacity regressions and four installer tests pass. Source comparison against the archived reference found no matching multi-line implementation runs in the four files (remaining single-line matches are interface declarations, standard syntax and simple returns). Evidence: output/source-rewrite/.

The refreshed package remains subject to actual Unity and multiplayer playtesting, especially crafting consumption and equipment-mod integration. No installation, commit or publication was performed. Earlier checks below describe the preceding candidate where counts differ.

# 0.1.37 — ready for local installation and playtest

Completed the approved furniture scope (40 storage pieces plus the pottery kiln, with original ground clay and pottery/flask crafting) and native-style chest, machine, ledger and pickup menus. NativeMenuTheme borrows the live inventory board, button/tab/checkbox/input artwork and title/body font materials without copying callbacks, shortcuts or animators. Numeric saving, permission checks, controller behavior and gameplay actions retain their existing paths. The crossed-out magnet is included. Trophy/treasure/gem prefabs and their assignment remain disabled; drawers, tackle, magic furniture and under-stair pieces remain deferred.

Release 0.1.37 builds with zero warnings/errors. Full checks passed across storage/automation (29,068 assertions), furniture (3,400), native item display (34), menu theme/handler isolation (15), ledger persistence (20), authenticated stack settings (17), respawn/world lifecycle (17), UI focus (8, including 300 password-screen frames), smelter queues/caps/fuel (11), pickup, owl models/navigation/courier, build menu, clay, stack compatibility and base protection. Resolved 849 API members, 64 Harmony hooks and 35 reflection targets. Geometry verification passed 248,712 furniture and 10,443 clay checks. The initial full run exposed an outdated respawn harness missing NativeHangingItem.ClearWarnings; the fixture now includes and asserts that cleanup, and all remaining tests passed after its correction. Logs: output/install-ready-0.1.37/build.log and remaining-checks.log.

Inspected the six-menu native-asset browser layout preview, including ledger at reduced scale; sampled labels/controls stayed within their bounds without text overflow. Preview files are under output/menu-redesign, with ledger-preview.png, machine-preview.png and layout-checks.json. This is a layout approximation using extracted local game assets, not a Unity screenshot or proof of live controller/saving behavior. Runtime uses borrowed game assets, not these preview files.

Verified dist/Quartermaster-0.1.37.zip integrity, manifest and assembly/plugin version, exactly one DLL identical to the Release build, README, changelog, icon and LICENSE. All 57 embedded furniture payloads and the pickup icon match authored assets. Regeneration left all 79 snapshotted artwork/README/installed-DLL files unchanged. Installer dry run passed without modifying installed files. Four isolated installer-planning tests cover clean upgrades, reversible conflict disabling and protection of a different prior backup. Removed the obsolete requirement for a previously removed legacy plugin DLL to still exist. No installation, commit or publication performed.

Remaining live acceptance after installation: native-art comparison alongside 3–5 vanilla pieces, loaded equipment/fish/armor poses, mounted-piece placement, player/owl door interactions, audio levels, actual menu rendering and controller input, ledger limits across tabs/reload, two-client ownership/save synchronization and full-storeroom performance. Automated network/persistence fixtures do not substitute for live multiplayer. The package is ready for installation and that playtest; full in-game acceptance is not claimed.

## Reference cleanup before installation

Removed the retired mod name and named attribution from maintained source, documentation and the current 0.1.37 package. The unused reference source was reversibly archived outside this checkout. Existing copyright and MIT permission text remains intact. README wording was reviewed and explicitly approved. Historical Git data and rollback archives were not rewritten.

Legacy capacity migration now uses the same persisted integer field IDs, verified against the installed game's stable hash implementation before replacement. Eight production-method regression checks preserve enlarged, partial and larger current capacities and cover missing/invalid views. Release rebuild passed with zero warnings/errors, four installer tests passed, and all 849 binary members, 64 hooks and 35 reflected targets resolved. Embedded resources still match authored files. Scanned active files and every current ZIP entry for removed names, including UTF-16 DLL strings. ZIP validation and installer dry run passed; both installed stable DLLs remain unchanged. Evidence: output/install-ready-0.1.37/reference-cleanup-checks.log and reference-cleanup-verification.log.

# Current scope — remaining furniture families deferred

The user also shelved the planned component drawers, fishing tackle storage and magic/reagent storage. Those families have not been implemented and require no runtime removal. The current expansion retains the 40 approved storage pieces plus the pottery kiln. Remaining work is validation of that scope, followed by the previously requested vanilla-style menu redesign. Trophy/treasure/gem pieces and under-stair storage remain deferred. This is a planning-only change; the most recent build/test results below still apply.

# Shelved trophy, treasure and gem batch

The user deferred the five pieces below. Their source/artwork remains available for a later update, but `DisplayStorage.Enabled` is false: no registration, Hammer entries, active definitions or automatic trophy/gem/valuables category changes. The active catalog is back to 40 previously approved storage pieces plus the kiln. The historical implementation checkpoint below is retained as evidence, not current release scope. No install, release package, README edit or commit.

After shelving: Debug build passed with zero warnings/errors; 3,400 furniture checks passed, including absence of all five prefabs and inactive default/override classifications. Future-batch checks remain gated alongside its implementation for resumption.

# Trophy, treasure and gem displays — historical development checkpoint (now shelved)

Added five shared-catalog pieces: Wall Trophy Shelf (2 slots), Trophy Cabinet (4), Treasure Coffer (4), Gem Sorting Tray (3) and Wall Gem Shelf (6). The active catalog now has 45 storage pieces plus the pottery kiln. Trophies use native item-type metadata. Otherwise unclassified saleable materials use valuables storage, while a small gem exception set covers native gems, amber and crystal. Existing ingredients, food, metals and equipment retain their routes. Explicit server overrides and per-container exclusions remain authoritative.

Each occupied slot borrows one bounded native item display, preserving upright orientation and resting its actual bounds on the shelf base. Quantity-only updates reuse the display; no per-unit meshes or gameplay/network components are instantiated. The treasure coffer extends the existing cosmetic cover state to player use, with player priority over owl requests and lifecycle resets. Owl gestures brace trophies, inspect/seat gems and open/reach into the coffer. Contact sounds reuse original restrained equipment, crystal and metal profiles. Real inventory transfer, save and network authority paths are unchanged.

Rendered and inspected output/display-storage/display-storage.png and five icons. The render shows empty furniture, including open and closed coffer geometry; native item poses are deliberately not represented by substitute artwork. Geometry checks cover clearances between display cells, shelf contact, wall mounting and the lid sweep. Existing model-count validation was raised from 128 to 192 for the 131 authored model payloads; part/vertex/display limits are unchanged.

Debug build: zero warnings/errors. Passed 34 native-display checks, 4,080 registration/crafting/assignment/motion checks, 124 owl courier regressions, 14 build-menu checks and 248,712 geometry/binary/socket/PNG checks. Resolved 814 API members, 64 Harmony hooks and 35 reflection targets without failures. Verified 57 embedded furniture payloads and the pickup icon against current sources. Preserved all 126 previous model payloads and 51 PNGs. Evidence: output/display-storage/validation.log, resources.log and preservation.log.

Live acceptance remains required: compare with 3–5 native Valheim furniture pieces, inspect actual trophy/gem/treasure models and native materials, verify player/owl cover interruptions, remote-client updates, ownership changes, saves/reloads, audio levels and furnished-room performance. These host tests do not establish Unity or multiplayer acceptance. Stable Release and both installed 0.1.36 DLLs remain byte-identical. No README edit, install, package or commit. Component drawers, fishing tackle and magic storage are next; menus remain queued after furniture.

# Armor wardrobes and pickup filter icon — development checkpoint

Added compact four-slot and wide six-slot armor wardrobes through the shared catalog (40 storage pieces plus the pottery kiln). Armor item types determine automatic assignment, with existing exclusions and synchronized overrides retained. Native armor displays preserve their upright orientation and hide behind closed doors. Modest one/two-door timber models use exact authored hinges. Player container use takes precedence over the owl's cosmetic opening request; cancellation releases that request, and distance/disable resets presentation. Existing inventory transfers, ownership and saves remain independent of animation. Quiet original hinge/latch sounds accompany door edges.

The pickup filter tab now uses an original crossed-out magnet sprite embedded in the Debug DLL. Its wooden tab, count, toggle and interaction remain unchanged. Inspected the icon preview and empty wardrobe renders, including both open and closed states. These studio renders do not validate native armor poses in-game.

Debug build: zero warnings/errors. Passed 29 native-display checks, 3,381 furniture checks, 124 owl courier regressions, 14 build-menu checks and 238,552 asset checks. Resolved 814 API members, 64 Harmony hooks and 35 reflection targets without failures. Verified 52 embedded furniture payloads and the magnet sprite against authored sources. All 124 previous model payloads and 49 PNGs remain unchanged. Evidence: output/wardrobe-storage/validation.log, resources.log, preservation.log and wardrobes.png.

Remaining live checks: compare beside 3–5 vanilla pieces; inspect native/modded armor fit, open-door clearance and player/owl interruption; verify save/reload, remote clients and ownership handoffs; audition audio; inspect the magnet at native UI scale; profile a furnished storeroom. Host tests do not establish Unity or multiplayer acceptance. Release and both installed 0.1.36 DLL hashes remain unchanged. No README edit, install, release package or commit. Full menu restyling is queued after the remaining furniture.

# Lighter armory wall mounts

Simplified the four new wall-mounted armory pieces after the user's visual feedback. Bolt storage is a shallow row of three pockets; bow and crossbow storage use independent narrow cleats with small pegs/rests; shield storage uses one hanging rail. Removed surrounding uprights, top framing, redundant rails and lower shield supports. Wall fasteners and snap points now follow the retained cleats. Freestanding furniture, recipes, capacity, display sockets, assignment and owl behavior are unchanged.

Rendered and inspected the focused empty-wall preview (output/armory-wall-revision/wall-racks.png), the full armory overview and four updated icons. Geometry validation includes wall-anchor contact and native-item display envelopes. Preserved all 120 unrelated model payloads, 45 unrelated PNGs and every inventory display socket. Debug build: zero warnings/errors; 3,219 furniture checks; 224,680 asset checks; 50 embedded furniture payloads match their current sources. Evidence: output/armory-wall-revision/validation.log and preservation.log. Stable installed/Release DLL hashes are unchanged. No README edit, install, package or commit. In-game mounting, loaded equipment poses and native-art comparison remain pending.

# Armory racks and ammunition — development checkpoint

Added nine pieces through the shared native-container catalog: arrow stand (3 slots), wall bolt rack (3), weapon rack (3), wide weapon rack (5), tall long-weapon rack (3), wall bow rack (2), wall crossbow rack (2), wall shield rack (2) and shield stand (3). The catalog now has 38 active storage pieces plus the pottery kiln. Armor wardrobes and doors remain the next slice; under-stair pieces remain unregistered.

ArmoryAssignment uses item type and weapon skill, with ammunition identified from compatible bow/crossbow ammo tokens. No equipment prefab-name list is required. Tokens shared by both weapon families remain unassigned for explicit override. Spears and two-handed weapons use long racks; shields have their own category. Staffs, fishing bait, tools and armor are not inferred into these racks. The existing synchronized override setting supports all seven new categories. Per-container exclusion, manual learning, receive/deposit rules and native saves remain authoritative.

The existing render-only item cache now also fits and centers equipment within authored rack envelopes. Horizontal bows/crossbows and outward-facing shields retain native mesh proportions and variants. The approved hook fitting is unchanged. A rack has at most five representative stored-item displays, each retaining the existing eight-part/30,000-vertex ceiling. Quantity changes reuse the model; no inventory item, physics, fish, scripts or network component is cloned. Native loaded poses and a furnished room still need runtime review.

The owl uses existing courier events for carrying, bracing and seating/lifting equipment, with distinct ammunition/equipment contacts. Gestures finish in 3.3 seconds and do not delay actual transfers. Render inspection corrected detached brackets, shoe braces, shield rests and wall-anchor clearance. Original timber frames, leather rests, pegs and quivers reuse existing atlases. Empty-frame previews are honest studio geometry reviews, not validation of loaded native models or final compatibility beside vanilla assets.

Debug build: zero warnings/errors. Automated checks: 3,219 furniture registration/crafting/assignment/motion assertions, 27 native display lifecycle/fitting checks, 124 owl courier regressions, 14 build-menu checks, 815 resolved API members, 64 Harmony hooks, 35 reflection targets and 227,040 geometry/binary/clearance/PNG checks. Evidence: output/armory-storage/validation.log. All 115 prior model payloads and 40 prior PNGs remain byte-equivalent (preservation.log); the small bronze plates are unchanged. README files and stable installed/Release 0.1.36 DLLs are unchanged. No install, release package or commit.

Required live review: place all rack sizes against walls and on floors; compare with 3–5 appropriate vanilla furniture pieces; display native and modded axes, swords, spears, two-handed gear, bows, crossbows, arrows, bolts and shield variants; swap/empty/save/reload each; check ownership handoffs and remote client updates, ward/in-use rules, owl contact/return poses, restrained SFX and a full storeroom's frame time. Automatic bound fitting does not guarantee every unusual modded mesh will sit perfectly on its rests. No multiplayer or in-game acceptance is claimed by these host tests.

# Individual bronze ceiling hooks and native contents

Supersedes the earlier wooden meat/fish rail revision below. Both development prefab IDs now create individual ceiling-mounted bronze plates, short three-link chains and hooks. Each has one native inventory slot and a recoverable two-bronze forge recipe. The user requested a smaller plate: width and depth are one-quarter of the first hook preview (9 × 7.25 cm), with a thinner profile and smaller rivets. All wooden rails and descending posts are removed. Earlier development three-slot capacity is superseded; these expansion pieces have not been installed in the stable build.

NativeHangingItem copies only visible meshes, materials and transform paths from the actual stored item prefab. It prefers native attach geometry, filters inactive branches and lower LODs, supports material variants, bounds each displayed item to the hook, and shares static/baked meshes between displays. No item, fish, physics, networking or gameplay components are instantiated. Quantity-only changes reuse the display. Item changes and empty slots release it; owned baked meshes are freed after the last user. Unsupported/oversized models produce an empty hook and one warning, preserving inventory. Native pose alignment is automatic and still requires in-game review, particularly different fish species and unusual modded prefabs.

The revised empty-hook preview and icons were rendered and inspected; these previews do not show or validate native meat/fish art. Chain/hook surface winding was corrected. All 113 unrelated model payloads and 38 unrelated PNGs match the pre-change snapshot. Debug build: zero warnings/errors. Checks: 22 native display lifecycle/asset-safety tests, 1,655 furniture tests, 124 owl courier regressions, 14 build-menu checks, 812 linked members, 64 Harmony hooks, 35 reflection targets, and 194,597 asset checks. Tests use host stubs and installed assembly inspection; they do not substitute for Unity playtesting. Evidence: output/native-hanging/validation.log, preservation.log and rails.png.

Pending: ceiling placement/snap contact, native meat and fish orientation/materials, item swaps and emptied hooks on two clients, owl contact/sway, quiet metal-clink audition, native-art comparison and furnished-room performance. Installed and Release 0.1.36 DLL hashes are unchanged. No install, packaging, README edit or commit.

# Pantry rail revision and footer hover text

Revised the fish rail so its short upper hangers stop at the crossbar; no frame geometry descends below the bar. Converted the meat stand to a wall rail with short back plates and brackets, reduced its authored dimensions to 1.80 × 0.78 × 0.42 m, and updated native wall-placement flags, wall snap points and build-menu name. Existing prefab IDs, three-slot inventories, recipes and food-family routing are preserved. Hanging meat remains original generic representative artwork, not native meat meshes.

Removed descriptive hover tooltips from Quartermaster's chest/inventory footer actions. Cloned UITooltip components are disabled and removed; button labels, click actions and other inventory tooltips retain their behavior.

The revised pair and updated full pantry overview were rendered and inspected. All 113 unrelated model payloads and 38 unrelated PNGs are unchanged. Debug build: zero warnings/errors. 1,649 furniture registration/assignment/motion checks and 14 build-menu checks pass. API validation: 803 members, 64 Harmony hooks, 35 reflection targets, zero failures. Asset validation: 188,301 checks. Evidence: output/pantry-rail-revision/validation.log; preview: output/pantry-rail-revision/rails.png. Live tooltip behavior, wall/ceiling placement, native-art comparison and multiplayer remain pending. No install, packaging, README edit or commit.

# Storage expansion — pantry and larder milestone

Added six native-container pieces: pantry shelf (6 slots), wall produce shelf (3), meat rail (3), suspended fish rail (3), lidded grain bin (3) and flour-sack stand (3). The shared catalog now has 29 active storage definitions plus the pottery kiln. Hammer registration, native inventories/saves, access and ownership checks, learned/ignored settings and real transfer paths are unchanged. Under-stair prototypes remain deferred and unregistered.

Food families are cached secondary classifications, preserving the established recipe-ingredient/apothecary route. Food stats, item type, recipe outputs and cooking conversions distinguish prepared food and raw supplies; native Fish metadata handles arbitrary fish names. Barley, flour materials and raw fish cuts use small conservative rules. Potions/status-effect consumables and equipment are excluded. Explicit category overrides remove/reassign both inferred paths, invalid overrides are ignored, and empty category tokens cannot admit unknown items. Changing the world database and existing registration-count invalidation refreshes both caches. A modded craftable raw food may need a produce override; no localized name matching is used.

Original representative contents include pantry portions, produce, berries, mushrooms, tied meat, whole fish/cuts, grain/flour mounds and woven sacks. They are bounded family samples, not exact replicas of every food. The grain bin has an articulated wooden lid and supported interior stock; hanging ties align with fixed pegs at all fill levels. Gentle hanging sway rotates around the tie height and resets on completion/cancel. Owl pantry placement, hanging and grain filling/retrieval reuse the existing courier. Quiet hanging contacts and grain rustles retain the spatial audio cooldown; owl calling frequency is unchanged. Actual inventory throughput remains independent of cosmetics.

The shared format remains QMA2, now with 115 named models within a 128-model bound and one additional original food material. Per-instance displays remain limited to inventory cells (at most six in this slice), with no gameplay prefab clones or new cosmetic RPCs. New full assemblies and alternative contents stay below 2,400 triangles. All 82 previous model payloads and 33 previous PNGs match output/pantry-storage/previous-assets.json. Studio review corrected hidden produce, two-item tie alignment, post clearance and the preview's lid rotation; sacks have subtle authored variation. The overview shows the grain bin open; runtime starts closed.

Validation: Debug build succeeds with zero warnings/errors. 1,648 registration/crafting/category/motion checks, 124 owl courier lifecycle checks, 14 build-menu checks, 29,068 behavior assertions, 17 synchronized stack-setting checks and 27 gull-material assertions pass. API inspection resolves 803 members, 64 Harmony hooks and 35 reflection targets without failures. Geometry, binary, UV, socket, stock-bound, lid-clearance and PNG verification passes 188,397 checks. Final shared resource verification covers 41 mesh/texture/icon payloads. Evidence: output/pantry-storage/validation.log and render logs. The installed native assembly was inspected to confirm Fish item metadata and CookingStation conversion types.

Pending: user art review; comparison alongside 3–5 native Valheim assets; live material/lighting, ceiling/wall support, owl reach and audio audition; native persistence, two-client ownership/late joins and furnished-room performance. These studio renders and automated stubs do not establish live game or multiplayer acceptance. Armory, display and small-goods families remain outstanding.

Heavy tools ran serially under MemoryMax=2G and CPUQuota=150% with two CPU threads. The large native asset bundle was not loaded. Stable Release, Steam and r2modman DLLs remain at the preserved 0.1.36 SHA256 2363705345678914efab4ae43235584dc75457f9f6f50115670ab8ee3008c73f. No install, release package, README edit, commit or publication.

# Storage expansion — masonry and overhead lumber milestone

Added four native-container pieces: stone pallet (3 slots), wide stone pallet (6), masonry crib (6) and rafter lumber rack (3). The shared catalog now has 23 active storage definitions plus the pottery kiln. Under-stair prototypes remain unregistered. All new pieces explicitly register in the Quartermaster Hammer category, retain native save/ownership/access rules and have positive refundable timber recipes. The rafter rack uses the existing ceiling placement flags and four top-face snap points.

Auto-assignment adds three explicit native masonry resources: Stone, BlackMarble and Grausten. Arbitrary stone-named valuables, sharpening stones, equipment and crystals are not inferred as masonry. The existing server-synchronized overrides accept masonry and can add, remove or reassign items. Ordinary chests, manual learning, ignored items and the per-container auto-assignment toggle retain their behavior.

Original fieldstone, chipped marble blocks and rough grausten loads use a separate restrained stone atlas. The tall crib has deeper low/medium/full piles, and its cosmetic contact point follows the reported pile height. Each occupied cell still uses one shared load renderer; the fullest crib has 2,148 triangles. The rafter rack reuses approved lumber loads and existing timber handling. Masonry uses a short weighted carry/place/retrieval profile and a dry, quiet contact sound. Real transfers never wait for the 3.5-second cosmetic performance. Owl calling frequency is unchanged.

The asset loader now permits up to 96 named models (82 used) and a fifth appearance material, without changing the QMA2 binary layout, saved prefab identities or inventory format. All 60 prior model payloads and all 28 prior PNGs match the pre-slice hashes in output/masonry-storage/previous-assets.json. New render previews were inspected and the crib's initially sparse piles were revised. Studio preview: output/masonry-storage/masonry-overview.png. The shared render helper selects the same representative material family and crib depth as runtime.

Validation: Debug build succeeds with zero warnings/errors. 1,310 registration/crafting/category/motion checks, 103 owl courier lifecycle checks, 14 build-menu checks, 29,068 behavior assertions, 17 synchronized stack-setting checks and 27 gull-material assertions pass. API inspection resolves 802 members, 64 Harmony hooks and 35 reflection targets without failures. Geometry, topology, UV, binary, cell clearance, ceiling mounts, stock bounds and PNG validation passes 125,718 checks. All 34 shared mesh/texture/icon payloads are verified embedded in the final Debug DLL. Evidence: output/masonry-storage/validation.log and render logs.

Pending: user art review; side-by-side comparison with 3–5 native Valheim pieces; live lighting, support/placement, owl reach and sound audition; actual performance and native save/reload; two-client ownership, simultaneous use and late join. The studio preview and automated harnesses do not establish these runtime results. Food, equipment and display furniture remain outstanding expansion work.

Heavy jobs ran serially under MemoryMax=2G and CPUQuota=150%, using two CPU threads. No large native bundle was loaded. Stable Release, Steam and r2modman DLLs remain at the preserved 0.1.36 SHA256 2363705345678914efab4ae43235584dc75457f9f6f50115670ab8ee3008c73f. No install, release packaging, README edit, commit or publication.

# Storage expansion — hide, textile, feather and bone milestone

Added six pieces to the shared catalog: hanging hide rail (3 slots), hide-roll shelf (6), textile shelf (6), wall textile shelf (3), feather coffer (3) and bone crate (6). There are now nineteen active storage-furniture definitions plus the pottery kiln. Under-stair prototypes remain unregistered. Native inventories, persistence, transfer paths, ward/ownership checks and explicit Quartermaster Hammer registration are reused.

Auto-assignment uses material metadata plus conservative prefab suffixes for hides/pelts, thread/cloth, feathers and bones, with a small set of irregular native names. Equipment is excluded even when its name ends in a matching suffix. ObjectDB item-count changes invalidate the cache for late registrations. Existing synchronized category overrides support hides/textiles/feathers/bones, including removal or reassignment; learned inclusion, ignored items and the per-piece auto-assignment toggle retain their precedence. No new per-frame classification or inventory scans.

The original art adds draped and rolled skins, folded cloth, thread hanks, feather bundles in wooden tubs and bone piles. Low/medium/full loads use shared meshes and subdued material colors; unrelated manual contents retain the neutral parcel fallback. At most six content renderers are used by these new pieces. The largest full hide/thread shelves remain under 2,400 triangles. A separate original soft-material atlas preserves prior surfaces. All 36 model payloads from before this slice match the recorded hashes in output/soft-storage/previous-models.json.

Owl handling includes carrying and settling hides/textiles, restrained folding/pressing motion, hanging-hide sway, gentle feather placement and bone toss/retrieval. The feather lid uses an authored rear-edge pivot and visible wooden hinge pins. It is open before placement, closes before the 4.2-second performance ends, and resets on completion, player interaction, culling or disable. Quiet rustle, bone and lid-contact events reuse the spatial SFX mixer routing and existing global cooldown. Storage throughput and owl vocalization timing remain unchanged.

Studio inspection corrected feather support, shelf-top plank construction, quill clearance, the lid pivot and visible hinge support. Previews: output/soft-storage/soft-overview.png and feather-lid.png. The frame collider now includes static lid bounds where present; previously approved pieces retain their geometry. Hinge sweep checks verify clearance above the stored contents at five opening angles. These are studio/art and mathematical clearance checks, not live-game physics or final native-art acceptance.

Validation passes: Debug build with zero warnings/errors; 1,236 registration/crafting/assignment/motion checks, 96 courier lifecycle checks, 14 build-menu checks, 29,068 behavior assertions, 17 synchronized stack-setting checks and 27 gull-material assertions. API inspection resolves 802 members, 64 Harmony hooks and 35 reflection/template targets without failures. Asset verification passes 97,145 geometry, topology, UV, socket, hinge-clearance, budget and PNG checks. All 29 current shared furniture mesh/texture/icon payloads are verified embedded in the final Debug DLL. Evidence: output/soft-storage/validation.log, final-build.log and render logs.

Pending: user art review, direct comparison with 3–5 native assets, in-game materials/placement/support, lid and owl reach/animation review, sound audition, furnished-room performance, and native save/reload/two-client ownership/late-join acceptance. The remaining furniture families and final expansion integration are still outstanding.

Heavy jobs ran serially with MemoryMax=2G, CPUQuota=150% and two Blender CPU threads; no large native bundle was loaded. Preserved Release, Steam and r2modman DLLs still match 0.1.36 SHA256 2363705345678914efab4ae43235584dc75457f9f6f50115670ab8ee3008c73f. No install, release package, README edit, commit or publication.

# Storage expansion — bulk furniture development milestone

Added six original pieces through the existing furniture registration and native Container path: lumber racks (4/6 slots), ingot racks (4/8), an ore/scrap bin (6) and a hooded coal bin (6). The seven approved cabinets remain active; both under-stair prototypes remain unregistered and deferred. All active pieces receive explicit Quartermaster Hammer registration and positive, refundable native-wood recipes. No storage save format, ownership rule, real transfer routine or inventory metadata handling changes.

Default categories now follow each furniture definition. Six native wood families and a small metal seed table combine with coal-fuelled Smelter conversions; exact XOre-to-X material pairs support conservatively inferred modded metal chains. Equipment and pottery conversions are excluded. Existing server-synchronized overrides accept lumber/ingots/ores/coal in addition to ingredients/reagents/none. Per-container disable, learned inclusion and ignored-item precedence remain available. World/scene and recipe/prefab-count changes invalidate classification caches.

Each occupied bulk cell has one shared representative mesh with low/medium/full thresholds, bounded to at most eight load renderers per piece. Logs, tapered ingots, rock/coal chunks and bent scrap silhouettes use muted species/material palettes; unrelated manually stored items use a neutral parcel. Displays update on native inventory snapshots, stack-limit and category changes, with existing distance culling. No display ItemDrop, cloned gameplay prefab, RPC or per-frame inventory scan. The fullest bulk assemblies stay below 2,400 triangles in geometry checks; this is not a measured game frame-time result.

The existing owl tour prefers ground approaches for low racks/bins and flights to higher shelves. Rack delivery holds one cosmetic item, then places it without tumbling; bins toss once. Retrieval moves a prop toward the beak. Weighted ingot poses and shorter handling times reuse the courier lifecycle, while inventory transfers remain immediate. Timber, metal and loose-material contact clips share existing spatial mixer routing and cooldowns; owl vocalization timing is unchanged. Placement props may cross the furniture's interaction collider and never create real items.

Validation: Debug build has zero warnings/errors; 595 furniture/crafting/assignment/motion checks, 68 courier lifecycle regressions, 14 build-menu checks, 29,068 behavior assertions, 17 synchronized stack-setting checks and 27 gull-material assertions pass. API inspection resolves 801 members, 64 Harmony hooks and 35 reflection/template targets without failures. Asset validation passes 58,314 geometry, topology, UV, socket, budget and PNG checks. First-pair approved model hashes remain unchanged. All 22 current mesh/texture/icon payloads are verified in the Debug DLL. Evidence: output/bulk-storage/validation.log, final-validation.log and render logs. Original cabinet/variation generators and artwork remain unchanged except adding new shared asset entries.

Studio inspection corrected overlapping beam/post faces, floating coal-hood pegs, reversed log-end winding and load/shelf clearance. The shared-scale preview is output/bulk-storage/bulk-overview.png. These are development art previews, pending user review and the required native-asset comparison; no final vanilla-style acceptance is claimed. Live game placement/support, lighting/material colors, sound audition, actual owl reach/ground movement, save/reload and two-client ownership/late-join behavior remain pending. Remaining soft-material, food, equipment and display furniture is still future expansion work.

Heavy jobs ran serially with MemoryMax=2G, CPUQuota=150% and two render threads; no large native asset bundle was loaded. Installed Steam and r2modman DLLs still match the preserved 0.1.36 Release DLL (SHA256 2363705345678914efab4ae43235584dc75457f9f6f50115670ab8ee3008c73f). No install, release packaging, README edit, commit or publication.

# Scope correction — under-stair storage deferred

The user approved the five non-angled variants and deferred both under-stair cupboards. The active catalog is now seven cabinets: the original clay/crystal pair plus narrow, low, wall, corner and rafter variants. Stair prototypes are removed from prefab and Hammer registration; their source artwork is retained only for future redesign. Historical nine-cabinet results below describe the earlier checkpoint, not the current catalog.

Native stair pitch has not been measured. The former 45-degree assumption is withdrawn; the future-work list requires native rise/run, snap-point and underside-clearance measurements before rebuilding. No installed build, release package or README is changed.

Verification after deferral: 508 apothecary registration/crafting/assignment/motion checks pass, including absence of both stair prototypes from prefab and Hammer registration. Debug build succeeds with zero warnings/errors.

# Storage expansion — cabinet variation development milestone

The user accepted the presented cabinet art. Seven further variations now register alongside the original pair: narrow clay (2×3), low clay (4×1), wall flasks (3×1), corner clay (2×2), left/right under-stair clay (3×1 each), and rafter flasks (4×1). Crafting quantities follow capacity and construction size. Auto-assignment, native inventories/persistence, labels, representative fill and owl interactions are shared with the first pair.

The internal QMA2 asset format carries authored snap points and optional collision-only parts. Corner and stair pieces have convex shaped collision instead of a rectangular envelope. Wall and rafter surface flags are set explicitly using the installed game's Piece API; new box collision derives from the rendered frame bounds so mounting battens reach supporting surfaces. Ceiling anchors are at the rack's top, wall anchors at the rear mounting face. The two original prefabs retain dimensions and collision behavior. Their authored mesh hashes are unchanged.

Sloped cupboards use mirrored 45-degree layouts with nominal two-metre rise/run. Their jars pull 0.45 metres forward before the shared lid-opening sequence, while the other cabinets use 0.23 metres. Approach and throw points follow that distance. Native stair fit, ceiling attachment, support wear and full owl clearance still require live placement testing; authored dimensions and metadata tests alone cannot establish those outcomes.

Checks pass: Debug build with zero warnings/errors; 527 apothecary registration/crafting/assignment/layout checks, 14 native build-menu filter tests, 49 courier lifecycle tests and 29,068 existing behavior assertions. Asset verification passes 27,078 checks, including exported geometry, UVs, PNGs, native convex-mesh limits, outward normals, open space above slopes/behind the corner, mounting points and mirrored cell order. API verification resolves 799 members, 64 Harmony hooks and 35 reflection/template targets with zero failures. Fifteen current apothecary mesh/texture/icon payloads are embedded in the Debug DLL. Evidence: output/apothecary/variants-validation.log and variants-render-refined.log.

All seven individual studio previews were inspected; corrected coplanar shelf/post faces on the corner model. These renders are art previews, not native-game lighting, multiplayer or performance acceptance. The existing first-pair approval does not imply approval or live testing of the new pieces. Heavy jobs remained serial with MemoryMax=2G, CPUQuota=150% and two Blender CPU threads. No large native bundle was loaded.

No installation, release package, README edit, commit or publication was performed. Live multiplayer, native-art comparisons and the remaining specialized storage families remain outstanding.

# Storage expansion — first apothecary development milestone

The user approved the original clay appearance. This development slice adds Unfired Jar (3 Raw Clay at a workbench), finished Clay Jar, Crystal Flask Blank (2 Crystal at a workbench), and finished Crystal Flask. A dedicated Pottery Kiln uses the native smelter body and queue/fuel/network behavior, with only the two new conversions, a ten-item queue, ten-coal capacity and two coal per 45-second conversion. It does not modify the vanilla smelter recipe list.

Original cabinet art now supplies an eight-cell Clay Jar Cabinet and a six-cell Crystal Flask Cabinet. Native Container persistence, ward/access checks, inventory dimensions, dismantling and transfers are retained. Recipes require one finished jar/flask per cell plus wood/fine wood. Both pieces and the kiln are explicitly added to the current usage-based Quartermaster Hammer category. The original one-argument ChestSettings.Accepts API remains available; ordinary chest defaults and the existing saved-data key are unchanged.

Automatic assignment uses consumable-recipe ingredient relationships plus a small explicit reagent/ingredient exception table. Cabinet category selection and disable are saved with existing chest settings; learned/manual inclusion and ignored-item exclusions remain available. General.StorageCategoryOverrides supports server-synchronized prefab=ingredients/reagents/none exceptions and invalidates the classification cache on changes. UI expands category matches only during a search, retaining the compact remembered-item list otherwise. Explicit/learned destinations precede category-only matches within the existing preferred/overflow tiers.

Visible jars map to real inventory cells. Localized labels update on identity/count changes; empty labels clear. Crystal fill is an original representative volume, not an exact replica of every ingredient. Eight/six bounded displays share authored meshes/materials; distant jar interiors are culled. Staggered 0.8-second inventory snapshots also observe remote native inventory reloads. No decorative ItemDrop, new inventory storage format, ownership claim or item-transfer RPC is introduced.

The existing owl tour can visit a cabinet after a detected inventory change. A single coalesced report expires after 90 seconds. Clear hovering approaches, jar/lid motion, one deposit or retrieval gesture, return motion and original quiet clay/glass contact audio extend the existing courier. Interrupted trips and player use reset articulated parts. Cosmetic work never delays or mutates inventory. Existing home-sort replays, station tours, book visits and cleanup remain in their established lifecycle.

Validation: Debug build has zero warnings/errors; 446 apothecary registration/crafting/assignment/motion assertions, 49 courier lifecycle checks, 14 build-menu checks and 29,068 existing inventory/behavior assertions pass. Also passing: clay 27, stack rules 18, authenticated stack RPC/settings 17, ledger tests, owl travel tests, smelter 11, respawn 16 and UI focus 8. The apothecary asset verifier passes 14,376 topology/UV/binary/socket/PNG checks; the clay verifier passes 10,443. Final DLL API validation resolves 791 members, 64 Harmony hooks and 35 reflection/template targets with zero failures. All eight current apothecary mesh/texture/icon payloads are present in the Debug DLL. Logs: output/apothecary/validation.log and final-check.log.

Studio renders in output/apothecary were inspected. Corrected clay-label clipping and removed degenerate cap triangles; added restrained ceramic tint, proportion and placement variation. Each cabinet frame is 216–252 triangles, each jar/flask 278–302, and each fill 72. These figures exclude font geometry and are not an in-game performance measurement. Heavy jobs ran serially under MemoryMax=2G and CPUQuota=150%; Blender used CPU rendering with two threads. No large native asset bundle was reloaded.

Pending: comparison alongside 3–5 native Valheim assets; live placement/snap and build discovery; label size/material lighting; glass transparency and fill; owl approach/prop clearance and contact-sound audition; save/reload/dismantling; two-client inventory, recipe discovery, settings and concurrent-use behavior. Tests use fixtures and binary API inspection, not two live game clients. Other furniture families, architectural pieces and cabinet variants remain outstanding. This is a development checkpoint, not full expansion or release acceptance.

Installed Steam and r2modman DLLs still match the preserved Release DLL (SHA256 2363705345678914efab4ae43235584dc75457f9f6f50115670ab8ee3008c73f). No installation, package regeneration, version bump, README change, commit or publication was performed.

# Storage expansion — original clay development milestone

The user authorized implementation and explicitly excluded ClayBuildPieces/other blacks7ar assets. The original clay generator produces a 237-triangle item, a 711-triangle ground patch and a deterministic 128px albedo. A small CPU-only Blender scene renders the same mesh/texture into a 128px transparent inventory icon and inspection previews at output/clay. No external clay artwork is input to the generator or embedded in the plugin.

ClayResource registers Quartermaster_RawClay and Pickable_Quartermaster_Clay from vanilla behavior templates, replaces both complete visual hierarchies, assigns the custom icon and item identity, and applies the existing stack policy. The normal limit is 50, weight is 1, and it is teleportable. A native Pickable yields three units before native resource-rate scaling, hides the complete patch when picked and respawns after 240 minutes. Jotunn vegetation uses native deterministic new-zone placement on gentle Meadows/Black Forest shores 0.1–2 metres above water, with obstacle checks, at most four groups of one or two patches per eligible zone, and networked initial scale. Previously generated zones are not modified. No custom harvesting RPC, inventory transfer, terrain scan or per-frame spawn loop is introduced.

Checks: 27 production registration/configuration tests, 10,443 mesh/UV/topology/binary/PNG checks and the existing stack-policy tests pass. Debug build completes with zero warnings/errors. Final binary API check resolves 750 members, 64 Harmony hooks and 32 reflection/template targets without failures. These checks do not simulate Unity rendering, world generation, native pickable RPC timing or live multiplayer.

Pending: direct 3–5-native-asset comparison, in-game material/icon readability, shoreline placement density, pickup/save/reload/respawn, resource modifiers, and two-client collection/ownership/scale. The model has been inspected in original-asset studio renders; this is not final vanilla art acceptance. Cabinets, jars/flasks and kiln progression are subsequent expansion work, not implemented by this resource milestone. No new package, installation, commit, publication or README edit.

After an abrupt host reboot, saved code/art/inspection notes survived. The journal inspected did not establish the cause. The 722 MB native asset bundle is not reloaded in the resumed work. Rendering and builds run serially with MemoryMax=2G and CPUQuota=150%; Blender uses two CPU threads. Existing installed/release 0.1.36 artifacts are preserved; the resource is built only into bin/Debug for development.

---

# Quartermaster 0.1.36 validation

Ledger persistence: a new cold-reload fixture reproduced a flaw where editing one book stored limits only on another selected authority, leaving the edited book without its own saved copy. SaveCap now clones the current base limits into the edited book, stamps a monotonic UTC revision, verifies the raw ZDO write and reads the effective cap back before reporting success. Authority prefers the newest revision, keeping legacy unversioned data readable. It claims only the edited book's settings ZDO. Logs identify the saved and opened authority with cap values to diagnose any remaining live reversion. Twenty ledger tests cover multiple books, dropped writes, ownership/ward denial, older lower-ID books loading later, old JSON, zero and invalid limits, multiple products, fresh-object reload and group edits. The user's exact running-game reversion has not yet been observed with the corrected build. Inventory and Production are merged into Inventory & Limits; Stack sizes and Machines remain.

Base protection: two General settings, included in existing server synchronization, enable native pathfinding out of Deposit Chest coverage and hostile spawn suppression. The existing BaseAI postfix runs only after native ownership/maintenance succeeds. Tamed, dead, player, passive animal, training-dummy and non-aggravated dverger cases remain unaffected. Exit candidates clear the full union of coverage spheres, with a small exit margin; navmesh probes are bounded and throttled and blocked routes are retried. World/raid spawn points, CreatureSpawner updates and SpawnArea-selected points are suppressed in coverage, with no spawner destruction or one-time flag changes. Removing coverage restores normal behavior. Fifty-nine tests exercise boundaries, overlaps, vertical separation, disabled settings, nonowner gating, friendly/tamed exclusions, blocked paths and spawn resumption. Real Unity navmesh, raids and multiplayer acceptance remain pending.

Owl movement: foot phase follows actual distance divided by model scale. Stance feet counter root translation, swing feet lift and settle, torso weight shifts and the head leads turns. Sharp turns brake movement and normal walking speed drops to 0.85 m/s. Separate floor samples adjust foot height, and the navigation floor offset drops from 2.5 cm to 4 mm. Flight adds climb/bank/approach poses; airborne path planning uses hovering wingbeats while grounded planning stays perched. Ambient calls use a 150-240 second idle interval with a shared 90-150 second minimum between calls; workstation arrivals are silent. Forty-five courier lifecycle checks and 153,767 navigation/motion/audio checks pass. Timing fixtures use bounded trip completion with the new slower gait. Twenty production gait poses were rendered using the shipped model and inspected at output/owl-gait-0.1.36; walking.mp4 is a studio preview, not live game footage. Actual foot contact, turns, ramps and sound frequency still need game review.

Ledger art: regenerated the shared game/preview mesh with separate chamfered planks, grain cuts, mortised supports, curved layered parchment, leather covers, stitches, brass corners and a two-sided turning leaf. The six material slots and existing perch footprint remain; the hinge follows the curved page gutter. The lectern and page total 5,688 triangles and 10,760 vertices. Studio renders inspect front, back, book details and a raised page; the build icon is regenerated from the same mesh. Native lighting and live page interaction still need game review.

Inventory controls: chest actions clone the vanilla Place stacks button, replacing its click callbacks and disabling copied controller shortcuts/hints. Buttons sit in a footer within extended Bkg, Darken and selection artwork; slot roots, header and scrolling are unchanged. Narrow rows wrap and extensions restore on hide/switch/disposal. Deposit All remains limited to Deposit Chests. Pickup uses the native loot-bag sprite, original icon/count geometry and native sibling layering behind Player/Bkg. Native-asset layout preview is at output/pickup-tab-review/native-controls-preview.png. UI scale, hover/click/controller and panel switching need live verification.

Additional verification: Release compilation has zero warnings/errors. Final API inspection resolves 701 binary members, 64 Harmony hooks and 30 reflection targets with zero failures. Package Quartermaster-0.1.36.zip passes CRC, single-DLL, manifest, license, README and built-DLL byte checks; the final model and icon bytes are embedded in that DLL. ZIP SHA256: e5cf46cdd8d4439bdc3bc8aaf759fc05394335a88d8b4cc15c18780393b4a2c7. Existing 29,068 behavior assertions, 17 stack-setting RPC checks, 16 respawn lifecycle checks and eight UI focus checks pass. README files are unchanged; no commit or external publication. Quartermaster 0.1.36 installed and byte-verified in Steam and the r2modman Mods profile on 2026-09-18. Installer dry-run passed before application; backup: ../backups/quartermaster-0.1.36-20260918T133614Z. Live-game acceptance remains pending.

## Previous release validation

# Quartermaster 0.1.35 validation

Respawn repair: the running 0.1.33 log records the local player being destroyed at 00:15:54 on September 18. Plugin.Update cleared the world registries whenever the local player disappeared, although already loaded chests and machines survive respawn and do not receive another Awake. The production Update regression reproduced this loss before the fix. It now pauses while the player is missing/dead, closing player UI and releasing its cached open-container reference without erasing world registration. A postfix on the active ZNetScene.OnDestroy performs full cleanup at actual world teardown; plugin shutdown uses the same cleanup. Ownership, ward and native container-use checks remain unchanged. Sixteen production-method lifecycle regressions cover player absence, dead objects, long respawn gaps, resumption without reopening chests, disabled automation, world teardown and clock reset. All eight UI focus checks and 29,068 behavior assertions pass.

Deposit sorting reports now queue cosmetic item samples while the owl travels. Returning replays three spaced throws per batch, including after the chest empties; interrupted batches resume without consuming throws during travel. At most 32 queued samples plus one coalesced overflow batch are retained. Disabling/culling the decoration clears this local queue. Inventory transfers and ownership remain independent of the presentation.

Owl travel prefers a bounded incremental ground search with floor support and swept body clearance, checking support along each edge and again while moving. Raised chest and book perches connect to the floor with short flights; missing ground routes fall back to the existing swept flight/hop/recovery behavior. Walking adds alternating legs, folded wings and head/body bobs. Work rounds track visited station IDs, continue after targets finish, and no longer use the old 24-second return cutoff. Due ledger visits can interrupt a round between jobs, then resume it. First visit is due after 20-35 seconds; later intervals are 45-75 seconds, with a 10-second retry when no allowed ledger is present. The ledger landing point moves 4 cm outward and 2 cm upward, remaining above its perch. An analytic check against the tilted desk collider found the old capsule overlapped by about 2 cm; the adjusted position has over 1 cm clearance.

Validation: Release build has zero warnings/errors. All 43 production courier/replay checks, 153,563 navigation/motion/audio checks, 29,068 behavior assertions, 16 hover checks and 14 ledger checks pass. Courier fixtures cover five-station rounds over 24 seconds, raised-perch flight/walk connections, station cancellation, blocked walking recovery, book visits during busy rounds, deferred and partially played sorting batches, metadata samples, queue limits and pickup safeguards. Ground fixtures exercise supported detours, gaps, changing terrain height and bounded search. API verification resolves 680 binary members, 60 Harmony hooks and 27 reflection/template targets with zero failures.

Twelve walking poses were rendered from the actual owl model and production OwlMotion data in output/owl-walking; opposing foot strides and folded wings were visually inspected. These are studio previews, not game footage. The ground search and courier tests use synthetic terrain/runtime fixtures; actual Unity collision geometry, stairways, doorways, ledger landing, long station rounds and delayed chest sorting still need in-game acceptance. Valheim was running during preparation, so local installation is pending. READMEs and the shelved menu work are unchanged. No commit or publication was made for 0.1.35.

## Previous release validation

# Quartermaster 0.1.34 validation

The screenshot reports partial resin and raw meat stacks. Investigation found that Quartermaster compared durability even for item definitions that do not use durability. The regression fixture failed before the change. Stack compatibility now compares durability only when either item's definition uses it, and treats absent and empty crafter names as equivalent. Item identity, quality, variant, world level, crafter ID/name, cheat flag and custom data distinctions remain protected. No saved inventories or flags were rewritten.

All 29,068 behavior assertions pass, including twelve new stack compatibility regressions: 6+23 resin, 24+5+6 raw meat, full-slot partial capacity and quantity conservation, durable items and meaningful metadata mismatches. Release compilation has zero warnings/errors. The existing 18 stack-limit checks pass. API verification resolves 679 members, 59 Harmony hooks and 27 reflection/template targets with zero failures.

The screenshot does not expose actual metadata, so it does not prove unused durability is the cause of these specific live stacks. The current log also contains a vanilla Stack All failure on a full chest. Manual dragging uses different native rules; clarification and live retesting are pending. This fix addresses Quartermaster sorting/transfers and does not override native world-level or other item compatibility rules. READMEs and the shelved menu redesign are unchanged.

## Previous release validation

# Quartermaster 0.1.33 validation

The ledger has a fourth Machines tab showing the loaded machines in its existing accessible base network. Rows display localized machine names, distance from the book and Automation.Status, refreshed every half second without rebuilding controls. Existing statuses cover fuel and input shortages, stock caps, storage capacity, pause/ownership/access restrictions and processing progress. Removed machines and disconnected bases replace stale row statuses. Search and pagination are preserved; entering/leaving machine search clears the previous item query.

The owl adds its current activity to the native crosshair hover text, retaining the original chest/interaction prompt. Courier status follows the actual travel, gathering, work, reading, return and recovery phases. The crosshair targets a local body bound, checks native interaction distance, camera occlusion and mist, and unregisters hidden owls. No physics collider or interactable is added and the player's native hover target is unchanged.

Release build: zero warnings/errors. Twenty-six production courier lifecycle checks and sixteen hover visibility/targeting checks pass. Hover fixtures cover walls, own-player colliders, saturated raycast buffers, distance, off-target aim, menus, mist, disabled state, nearest owl, recall shrink and cleanup. Fourteen existing ledger persistence regressions and eight UI focus regressions pass. API verification resolves 678 binary members, 59 Harmony hooks and 27 reflection/template targets without failures, including the new Hud.UpdateCrosshair hook.

Automated fixtures do not reproduce the full Unity HUD/physics or multiplayer timing. Live acceptance remains: Machines tab spacing at game UI scales, changing production conditions, multiple same-name machines, owl hover while perched/in flight, walls, chest prompts and controller aim. Menu redesign remains shelved; README files are unchanged.

## Previous release validation

# Quartermaster 0.1.32 validation

Ledger inventory and production-limit pages now start from this base's current chest contents, remembered storage item types and production drops. Only products with an actual queued count are added from machine recipes. The production-limit tab intersects those introduced items with supported products. An unused workstation recipe alone no longer creates a row; remembered types remain visible at zero stock. Existing search, pagination, production caps and stack-size settings are preserved.

Release build: zero warnings/errors. All fourteen existing ledger persistence/group/permission regressions pass. Binary verification resolves 658 members, 58 Harmony hooks and 27 reflection/template targets with zero failures. The filtering change was reviewed in source; these checks do not establish the visible page count or live menu appearance. In-game verification remains pending. The menu redesign remains shelved and README files are unchanged.

## Previous release validation

# Quartermaster 0.1.31 validation

The running 0.1.30 log reported that `piece_sign` could not be cloned, followed by a null reference in LedgerModel.Build. The installed asset manifest identifies `sign` as the correct prefab; direct asset inspection confirms its material uses the required Custom/Piece shader. Registration now uses that prefab and reports missing templates explicitly.

The current game builds its usage filters through ByUsagePieceList, independently of Jotunn's legacy category setting. Quartermaster now adds its own named filter without allocating usage flag bits, preserves other mods' filters, includes the Deposit Chest and Ledger Lectern plus native repair/remove actions, and hides the filter from unrelated tools.

Release build: zero warnings/errors. Thirteen build-menu regressions, fourteen ledger regressions and eight UI focus regressions pass. Binary verification resolves 658 game members, 58 Harmony hooks and 27 reflection/template targets with zero failures. It now checks cloned template names against the installed asset manifest and checks the new category hook signatures and injected fields.

The experimental wooden-board menu redesign is shelved outside the active source tree. Existing menus and README files are unchanged. In-game confirmation of the build filter, lectern placement and book interaction remains pending restart with this build. No live acceptance is claimed.

## Previous release validation

# Quartermaster 0.1.30 validation

Release build and all automated suites pass with zero build warnings/errors. The owl feature has 136,546 navigation, motion and synthesized-audio checks, 20 tests executing the production courier lifecycle, and 58,523 model checks. Existing suites pass: 29,056 behavior assertions (including real single-item transfer and overflow conservation), 27 material checks, 18 stack checks, 28 pickup-filter checks, 8 UI focus checks and 11 smelter-cycle regressions. Additional suites exercise 17 live stack settings/admin RPC cases and 14 ledger persistence/group/shared-cap cases. Binary inspection resolves 651 game members, 54 Harmony hooks and 25 reflection targets, including machine fields, native item removal and the loaded-item registry.

## Behavior and limits

- Idle home is the Deposit Chest. Active smelters/kilns, ovens, windmills, fermenters and supported cooking racks are selected from actual queued processing state. Paused, inaccessible or blocked machines are skipped. Work props use real recipe/fuel prefabs and input sockets; a kiln receives wood, while a smelter alternates ore and coal. Machines keep working independently of presentation.
- Oldest-visited stations get a turn. One nearby Deposit Chest owl represents each station. Visits last about five seconds, and the owl returns home periodically. Observed activity and routes are local visuals, not a synchronized network character.
- Incremental 3D A* uses swept capsule clearance, a bounded search budget, path smoothing and finer-grid retries. Flight rechecks clearance each step, brakes at corners, tries a checked raised hop, then replans or returns home. If even the return route remains blocked, a brief shrink-out recalls the cosmetic actor to the current chest perch. No route failure can stop production.
- After 20 seconds idle, cleanup trips collect at most three individual items within 25 metres (or the smaller base range). A 15-second ground-item grace period, production-output grace period, player pickup exclusions, native ownership, ward access, base authority and chest capacity are checked again at pickup. Only locally owned accessible items are collected; ownership is never stolen.
- Each collected item enters the persistent chest immediately; the return-flight carry mesh is cosmetic. The native RemoveOne handler preserves oversized piles when limits have been reduced. Full chests and failed ownership checks consume nothing. Disabling/culling the owl or changing scenes cannot lose an in-flight inventory because there is none. General/OwlCollectDroppedItems disables cleanup; Cosmetics/OwlVolume controls local calls.
- The hat is removed. Facial disks follow the skull instead of using bulging cheek geometry, and the eyes have flatter sockets, circular pupils and thin lids. Both feet articulate independently. The near/far models contain 17,664/6,114 triangles.
- Motion includes takeoff, flapping, gliding, banking, tucked toes, landing compression, alternating foot shifts, head bobs, side tilts, preening, stretches, feather shakes and existing sleep/blink behavior. New coos and clucks are original synthesized audio. The paired coo is informed by Cornell's species description: https://www.allaboutbirds.org/guide/Burrowing_Owl/sounds . No external recordings are bundled.

- Buildable Ledger Lectern: Hammer → Quartermaster; workbench, 15 wood, 5 fine wood and 2 deer hide. The native sign networking/build lifecycle supports an original 780-triangle wooden lectern, open ledger, turning page and owl perch. Its three tabs show loaded accessible base inventory, edit shared production limits and change global stack sizes. Refresh discovers new item types; visible stock/queued counts refresh every half second.
- Ledger limits are saved in the placed book's ZDO and override legacy per-machine production caps for that loaded base group. Without a ledger override the previous lowest-machine-cap rule applies. Other books edit the canonical configured ledger; adding an empty lectern cannot replace its saved limits. Destroying/unloading that configured book restores the machine-cap fallback. These are production limits, not storage quantity targets. The machine menu can edit an existing ledger override too.
- The owl occasionally visits a nearby ledger, scans the pages, turns a leaf and returns to its chest before resuming work. Reading is cosmetic and does not alter stock.
- The stack-size tab applies normal limits or a custom maximum from 2 to 100000 immediately. Host saves persist to configuration. Remote requests verify the authenticated server peer socket against the admin list, then synchronize the result. Clients receive updates without saving the server values into their own local configuration; disconnect restores local preferences. Failed config writes restore the previous live values. Normal client polling may take up to five seconds.
- Oversized chest stacks eject their excess as real legal-sized world drops in front of the chest. The native item persistence routine retains quality, crafter, custom data and other metadata. This covers newly lowered limits and old chests as they load. There is a maximum of four drops per chest and eight total per two-second cycle. Closed, accessible, locally owned chests only; no ownership stealing. Blocked front space or failed spawn retains the un-emitted contents for retry. Closed/open UI timing and sphere clearance still need live acceptance. Player inventory stacks are retained. Owl collection observes its normal grace/capacity rules, so pickups cannot overfill a legal stack and trigger an ejection loop.
- Face preview review found reversed winding on the new feather disks. Winding is corrected and exported crown/facial surfaces are checked against their analytic lighting normals. Refreshed body/face/motion previews use the corrected mesh.

## Evidence and remaining live checks

The navigation tests use analytical obstacle fixtures; courier tests execute the shipped controller with a deterministic runtime. Transfer tests execute the real inventory routine. These tests do not reproduce Unity physics, network timing or live-game appearance. Studio face/body renders and the animation study use the actual generated model and production pose samples, under Blender lighting. They are not Valheim screenshots or a live navigation recording.

In-game acceptance remains: multiple enclosed stations, narrow/off-grid doors, a door closing mid-flight, roof/beam clearance, a moved/open chest lid, busy/full chest pickup, multiplayer ownership changes, distance culling, and frame time with several owl actors. Also verify lectern placement, native damage/destruction, page interaction, controller focus, multiple ledgers, remote admin edits and non-admin denial, save/reload, closed-chest ejection on each storage tier, wall obstruction, multiplayer drop ownership and pickup conservation. Check grounded and airborne light probes, the revised eyes and wing silhouettes, call volume, and carried props. No installation or live-game acceptance is claimed. README files are unchanged.

## Previous release validation

# Quartermaster 0.1.29 validation

Release build: zero warnings/errors. All checks pass: 28,818 behavior assertions, 61,493 owl asset checks, 27 material checks, 18 stack-limit checks, 28 pickup-filter checks, 8 UI focus regressions and 11 smelter-cycle regressions. Binary inspection resolves 603 members, 54 Harmony hooks and 24 reflection targets with zero failures.

The smelter regression harness executes the production ProcessSmelter method with deterministic queue, stock and supply fixtures. It reproduced the previous misleading cap status before the fix. Queued work now shows its processing state even when loading more ingredients is capped; missing fuel keeps its fuel-enabled-storage explanation. Existing queue-only fueling and production-cap behavior are unchanged. The harness does not reproduce network ownership or real chest transfers.

Stack tests verify custom limits, disabling the feature, restoration across repeated settings changes, and preservation of oversized saved stacks. EnableStackSizes and MaximumStackSize already existed; this release clarifies their config guidance. Servers running Quartermaster supply these settings to clients.

No in-game or multiplayer acceptance is claimed. The reported player's exact coal-supply failure is unconfirmed; check their machine status, chest fuel permission, base group/range, and log. The build includes the current owl assets (20,704/6,138 near/far triangles); appearance still requires an in-game check. README files are unchanged.

## Previous release validation

# Quartermaster 0.1.28 validation

Release build: zero warnings/errors. 58,819 owl asset checks, 28,818 behavior assertions, 28 pickup-filter checks, 27 existing material checks, 14 stack-limit checks and 8 UI focus regressions pass. Binary inspection resolves 603 members, 54 Harmony hooks and 24 reflection targets with zero failures. Embedded owl model and texture match the source assets.

The deposit owl now uses a dedicated UV-mapped model with shared materials and two distance levels: 19,624 and 5,932 triangles. Existing body/head/wing/eye poses, range hiding and sorting callbacks drive the new model. The far model retains the same pivots and materials. Asset tests cover bounds, valid normals/UVs, triangle budgets, malformed input, truncation and index validation.

Blender previews use the actual shipped geometry and texture under studio lighting. They are not Valheim screenshots. Check all chest types, day/night materials, pecking/sleeping, distance transitions, repeated loading/unloading and frame time with multiple Deposit Chests in game. No in-game visual or server acceptance pass is claimed.

The optional coffer preview is a study of dormant custom-chest geometry. Custom chest visuals remain disabled in this release. README files are unchanged.

## Previous release validation

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
- Verified incompatibility identities against installed plugin metadata: the legacy storage plugin (0.1.7) and `TastyChickenLegs.AutomaticFermenters` (1.1.2).
- Shipped policy/transfer source compiled into a separate deterministic behavior harness: **28,200 assertions passed**, including **1,000 randomized inventory trials**.
- Behavior checks: quantities and item metadata conserved under repeated partial/full transfers; no over-limit stacks created; no colliding grid slots; callbacks see both inventories after the transfer; different custom data does not merge; learned types survive emptiness; forgetting does not immediately relearn existing contents; restoration; deposit/overflow routing policy; group normalization; protected wood; whole-batch caps; zero caps; arithmetic overflow; many producers sharing stock and queued allowance; restarting after consumption.
- Config-model roundtrip checked using the test host's JSON serializer. Unity `JsonUtility` and actual ZDO save/reload require the live test below.
- Source scan confirms no F6/F7 bindings, remote Store Matching, consolidation code, custom reserves or stack-size controls in the maintained `src` tree. Unused recovered upstream code is archival only and is not compiled or shipped.
- Existing legacy dimensions are read before a container's inventory is constructed; no new chest resizing is enabled.
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

## Pending feedback batch — 2026-09-23 (not installed or released)

- Native beehives register as machines. Ready honey is harvested through `RPC_Extract` only after production-limit and output-room checks. The preflight uses the native world-drop multiplier. A narrowly scoped `ItemDrop.SetStack` observer tags native hive drops (hive extraction does not call `OnCreateNew`) for existing delayed collection/sorting. Manual honey extraction is untagged. F9 and ledger machine controls include hives.
- Hive visits use a four-second startle/recoil and asymmetric flapping pose with seven reusable local bee motes. The motion uses existing ground/flight clearance checks; it does not move inventory or call audio. Recent native harvest cues remain available for two minutes, and each owl throttles repeat hive visits. The swarm hides on completion, cancellation/reset, and is destroyed with its owl.
- Automatic Fermenters no longer prevents the entire plugin from loading. Its metadata ID is an optional dependency; detected instances disable Quartermaster fermenter tasks, output tagging, visits and station controls with an explicit incompatibility notice. This is overlap avoidance, not a claim of tested full interoperability. GUIDE carries the same notice. README was not changed.
- Ledger Interface tab adds local Show Sort Inventory, horizontal/vertical offsets and Reset position controls; Configuration Manager exposes the same entries. Zero offsets preserve existing action layout. Hiding the only button removes the footer extension. Offsets affect only Sort Inventory.
- UI preview: `output/menu-preview/interface-preview.png`, generated from native UI assets by `render_interface.py`; not a game screenshot.

Investigation findings:
- Both build-menu modules use distinct named tags and guard against duplicate category insertion. Quartermaster's build registration unsubscribes after success. Repeated initialization/category tests do not reproduce duplicate entries. The single report is unconfirmed; no speculative Helmsman edits were made.
- Multiplayer's second click comes from `SharedCrafting.TrySharedPlacement`: the first reserves shared chest materials; a subsequent placement enters the held reservation. Native `Player.UpdatePlacement` consumes materials and stamina after `TryPlacePiece` succeeds. Automatically invoking only `TryPlacePiece` would skip those costs. This behavior is diagnosed, not changed in this batch; a safe single-click implementation must resume the full placement path and revalidate the preview.

Validation: Release build 0 warnings/errors; 956 binary members, 79 Harmony hooks and 51 reflection targets resolved with 0 failures. Honey harvest fixture: 7 checks including cap/room refusal, resource multiplier, one extraction, empty hive and exception cleanup. Owl courier: 136 checks including late harvest during panic, swarm cleanup and no item mutation. Owl navigation/pose/audio: 170,567 checks. UI layout: 628 checks. Behavior: 29,076 assertions. Multiplayer simulated transport: 54 checks. Build menu: 14 checks. Build registration: 7 checks.

Pending live acceptance: native honey pickup/sorting on a dedicated server; bee visibility, recoil and transitions in game; optional Automatic Fermenters coexistence; Interface tab/controller and nonzero offsets with expanded inventory mods. No new package, Gale install or upload performed.

## Craft/build material fetch — 2026-09-23 (unreleased)

- Retains shared-operation leases for the actual transfer. Craft/build choose a minimum covering set of up to four supply chests, with nearest-first ties and bounded selection work.
- Fetches only deficits after carried materials; preserves native recipe quality, upgrade-station filtering and alternative-ingredient selection. Multi-craft uses the requested multiplier.
- Revalidates selected sources and previews combined player capacity before moving materials. Failed/cancelled native actions leave fetched materials in the player inventory.
- Shared-operation continuation runs after fences are released and execution scope is cleared. Native crafting and funded placement consume player inventory only. Building still uses normal hold/repeat input after the asynchronous fetch; no chest lease waits for that input.
- Release build: zero warnings/errors. API check: 961 members, 81 Harmony hooks, 51 reflection targets; zero failures.
- Behavior: 29,592 assertions including minimum-cover oracle, exact deficit quantities, full/joint capacity, changed stock, quality and world-level checks. Multiplayer: 59 simulated transport checks including release-before-continuation and failed-operation cleanup.
- Live dedicated-server crafting/building, multi-craft/upgrades and inventory-mod acceptance remain pending. Not installed, packaged or published.

## Unpaid upgrade / silent busy build — 2026-09-23 (unreleased)

- Removed create-first/pay-shortfall-later crafting hooks; their removal results were not enforced. Native consumption now operates on prefetched player materials, with no post-output warehouse fallback.
- Applied the fetch-before-craft/place guard to single-player as well as networked sessions. Busy/reserved material requests report a throttled visible message instead of silently returning.
- Added `tests/CraftPayment`: 18 regressions execute the shipped craft/build prefixes against simulated native methods/transport. Cases include reserved iron, active automation, exact upgrade payment, multi-craft, full inventory, changed stock, death before grant and visible blocked placement.
- Release build: zero warnings/errors. API check: 961 members, 76 Harmony hooks, 51 reflection targets; zero failures. Dedicated-server owl-transfer reproduction remains pending. Not installed or published.

## 0.1.56 review follow-up — 2026-09-23

- Craft-count enumeration now excludes leased chests for UI requirements, ghost requirements, ingredient selection and quality planning. Unleased remote owners remain eligible for validated handoff; general inventory observation is unchanged.
- Deleted the no-op soft-count reserve and dead reserve arithmetic, unused direct warehouse spend helpers, and unused Hold/BeginHeld/EndHeld API. Concurrency remains protected by short write leases and fetch-before-output.
- StorageCore repaired: 48 checks pass with original and Plus equipment-slot API variants, plus 2 checks without the optional API. No stale EnterCraft/SpendRecipe/CoverShortfall reflection calls remain.
- CraftPayment: 26 checks, including reserved-only stock failing counts/requirements. Multiplayer: 58 simulated transport checks, using non-hold build fetch and release-before-continuation. Behavior: 29,592 assertions.
- Release build: zero warnings/errors. Binary API validation: 961 members, 76 Harmony hooks, 51 reflection targets; zero failures.
- Package: dist/Quartermaster-0.1.56.zip, SHA256 c18869607bcfd2a985b53120f3ea9c4d781ddaff62b5d5958b7dd980c84af785. Exactly one DLL, verified against build. Approved README unchanged.
- Valheim closed before install. Steam and Gale Test DLLs replaced and verified byte-for-byte; backup: backups/install-0.1.56-20260923-224830. Gale metadata/cache was not rewritten.
- Not externally published. Live two-client dedicated-server owl-transfer, crafting/upgrading and building verification remains pending. Prior 0.1.55 packages do not contain this fix.

## Odin's Food Barrels / Dynamic Storage Piles compatibility — 2026-09-24 (unreleased)

- Inspected Gale Test's Odin's Food Barrels 1.3.3 and Dynamic Storage Piles 0.8.1 DLLs. The requested "dynamic item stacks" mod is installed as Dynamic Storage Piles. No third-party binaries or assets are bundled.
- Added optional load-order dependencies and read-only delegate bindings to each mod's live container/item metadata. Register supported player-built containers without loosening the general ship/loot filter. Existing permissions, leases and supply settings still apply.
- Empty containers auto-assign their native item types; Chest Config can disable assignment or forget individual items. Dynamic piles retain their intended automatic item type when their hard item restriction is disabled; learning/manual assignment can then accept other items.
- All direct Quartermaster transfer admission checks honor the mods' live restrictions, including seed/meat allowlists and the Dynamic Storage Piles restriction toggle. Save/Changed notifications remain native; inspected VisualStack.Start subscribes UpdateVisuals to Inventory.m_onChanged.
- Release build: zero warnings/errors. API validation: 965 members, 76 Harmony hooks, 51 reflection targets, zero failures. Installed optional method signatures and their restriction behavior were inspected separately with Cecil.
- StorageCore: 58 checks, including absent adapters, metadata recognition, changed allowlists, restricted/unrestricted transfers, source preservation and destination notifications. Apothecary suite passed (3,493 existing checks plus mead checks and five external assignment/override checks). CraftPayment: 26; Multiplayer: 58 simulated transport checks.
- Not installed, packaged or published. In-game Gale Test and dedicated-server verification remain pending: empty barrel/pile sorting, matching crafting supply, machine supply, restriction toggle, live pile visuals, save/reload and two-player ownership handoff.

## 0.1.57 publication — 2026-09-24

- Published to Hexium under Vassteel, Thunderstore and Nexus. Hexium public latest and Nexus public/primary file show 0.1.57; Thunderstore returned upload success for the Valheim listing.
- Verified both archives: exactly one DLL matching the release build; no source/test files or third-party assets. Standard package has six files; Nexus package contains only DLL and license. Approved README unchanged from 0.1.56.
- Receipt and hashes: dist/published-0.1.57.json. No local installation performed; live-game acceptance remains pending.

## 0.1.58 custom locking removal — 2026-09-24

- Removed SharedOperations, OperationQueue and AutomationTasks from production source. Removed the component, all lease RPC registration, replicated fence checks/writes, and the Container.RPC_RequestOpen patch. Old saved fence markers are ignored.
- Restored native owner-only automation and write checks. No forced ownership changes. Remote-owned storage is excluded from craft counts and spending; open it normally or carry materials when needed.
- Kept exact prefetch plus native inventory payment, including capacity checks. No create-first fallback.
- Release build clean; native ownership/no-lock checks 9, craft payment checks 22, storage core checks 58. Binary API: 939 members, 75 Harmony hooks, 51 reflection targets, zero failures.
- Built and verified dist/Quartermaster-0.1.58.zip. Not installed or published. Live game testing pending. Previous lease transport tests retired with their removed implementation.

## 2026-09-24 — synchronized storage reads and player courtesy (unreleased 0.1.58)

- Remote stock/search/ledger reads deserialize synchronized item bytes into separate revision-aware inventories; no ownership requests or container save callbacks. Native-owned access refreshes through Container.Load before permitted writes.
- Automation skips locally or remotely busy chests and waits 0.5 seconds after observed use. This local scheduling hint never gates player Open, crafting, or building and is cleared on world reset/unregister. No custom lease/fence protocol restored.
- Release build: zero warnings/errors. StorageObservation: 17 checks; NativeStorage: 9; CraftPayment: 22; StorageCore: 58. API audit: 944 binary members, 75 Harmony hooks, 51 reflection targets; zero failures.
- Still requires a two-client dedicated-server test: remote stock changes without opening, chest use while sorting/refilling, crafting/upgrading during transfers, and item conservation. Remote writes still require native ownership; synchronized visibility alone does not grant spending access. No installation or publication performed for this pass.

## 2026-09-25 — foreign-owned craft supply (unreleased)

- Craft/build source enumeration now uses accessible, enabled supply chests in range, independent of owner. Native local/replicated open flags still exclude occupied chests; ward/privacy and parcel exclusions remain. Sources refresh via native Load before payment. Each successful withdrawal explicitly calls native Save; no ownership claim, custom lease, chest-open patch or additional mod dependency.
- Existing capacity simulation, exact deficit collection and full carried-inventory payment remain. Building continues on the same input after fetching; the local-only payment scope lasts through native UpdatePlacement consumption.
- CraftPayment: 39 checks, including separate A/B inventory copies sharing synchronized saved state, ownership reversal, explicit remote saves, partial carried payment, newly synchronized shortage, access/open/range restrictions, full inventory, upgrades and multicraft. These are simulated delivered updates, not a real network transport test.
- StorageCore: 58; NativeStorage: 9; StorageObservation: 17. Native networking inspected: ZDO data changes enqueue client synchronization without requiring ownership; receiver accepts newer data revisions.
- Limitation: native snapshot writes are not atomic across clients. Concurrent withdrawals from the same revision can conflict; this adaptation does not claim to solve that race. Live dedicated-server item conservation, simultaneous craft/automation and save/reload verification remain pending. No installation or publication performed.

## 2026-09-25 — Plant Easily batch funding (unreleased)

- Optional adapter inspected against Advize_PlantEasily source 2.2.2, commit 1deeb3962eb8681394961b0f35a4dfbc315c6f99. No planting mod installation or required dependency added. Reflection hooks initialize only when Plant Easily is loaded; changed/missing interfaces log a compatibility warning and block the storage-funded planting entry point.
- Before Quartermaster permits root placement, the adapter asks Plant Easily to evaluate its existing ghosts, respects its invalid/partial planting settings, caps the chosen group to affordable quantities and prefetches the total deficit. No transforms, spacing, grid dimensions, or resource costs are edited. Unfunded extras are marked LackResources for Plant Easily's own placement selection; its existing code consumes the carried payment once. Failed native placement can leave fetched materials carried, never silently returned/duplicated.
- Harvest/replant's HaveRequirements call is replaced with a fund-before-spawn check followed by vanilla carried-only requirements. Plant Easily still creates and consumes the one replant itself.
- 59 craft/build/planting checks pass. Includes the actual Quartermaster placement entry, 1,000 ghosts with only two plants funded, exact root/extra payment, no transform edits, whole-grid rejection without withdrawal, invalid-position settings, inventory capacity, and unfunded replants. Tests use a simulated Plant Easily interface/consumption contract, not an installed live plugin.
- Release build: zero warnings/errors. API audit: 947 members, 75 native Harmony hooks, 51 existing reflection targets; zero failures. Optional external reflection contracts are source-reviewed, not covered by that native API audit.
- Live Plant Easily and multiplayer validation pending; reporter's exact plugin version is absent from the supplied log excerpt. No install/package/publication performed.

## 0.1.59 publication — 2026-09-25

- Published both craft-access and Plant Easily funding changes to Hexium (Vassteel), Thunderstore and Nexus. Hexium latest and Nexus primary file verified as 0.1.59; Thunderstore returned upload success.
- Standard archive has six release files; Nexus has only DLL and LICENSE. Both DLLs match the clean release build. Approved README unchanged from 0.1.58. Hashes and destinations saved in dist/published-0.1.59.json.
- No local installation; live multiplayer/Plant Easily testing remains pending as stated in the changelog.

## Pending: automation transfer range (2026-09-25)

- Found that same-group Deposit Chests produce a union-wide supply list: a machine near one hub could consume a chest near a geographically separate hub.
- Ingredient/fuel withdrawals now require source and destination to share one Deposit Chest's configured BaseRange. Livestock withdrawals use the same constraint in addition to their existing feeding radius.
- Output routing and fallback deposits require shared coverage too. Sorting from a Deposit Chest also respects that source hub's own radius, including overlapping-hub layouts.
- Group membership and ledger limits are unchanged. No ownership or locking changes.
- Production-source fixture: 16 passing checks, including distant same-name bases, exact local withdrawal, empty local stock, radius boundary, vertical distance, overlapping hubs, output fallback, configured radius changes and removed hubs.
- Existing smelter regression fixture: 11 passing checks. Release build: 0 warnings, 0 errors.
- Not installed or published. Live reproduction of the reported world and multiplayer validation remain untested.

## Pending: geographically isolated networks and per-object authority (2026-09-26)

- Follow-up screenshot showed a fire 5,003m from the ledger and nearby stations waiting for base network ownership. Transfer bounds alone did not isolate the ledger's machine list or its hub-owner gate.
- Same-group hubs now form separate networks unless their coverage spheres connect. Network rebuilding recomputes components when hubs disappear or BaseRange changes. Existing shared-hub transfer bounds still prevent direct transfers across an overlap chain.
- Removed the extra requirement to own the first hub in a group. Automation retains native ownership checks on the actual machine, source/output object and storage containers, plus pause, ward and in-use checks. No leases, forced ownership transfers or chest locking were introduced.
- Local machine lists, stock and ledger authority now exclude geographically disconnected same-name bases. Existing saved ledger settings are not deleted or rewritten; each separate base needs its own configured desired quantities.
- Automation range fixture expanded to 29 passing checks, including actual network rebuilding and CanRun: 5km separation, hub-owner independence, station ownership, pauses, wards, touching/overlapping coverage, bridge removal and hub removal. Smelter: 11 passing checks; beehive: 7 passing checks. Release build: 0 warnings, 0 errors.
- No install or publication performed. Multiplayer/in-game acceptance remains pending.

## 0.1.60 release checks (2026-09-26)

- Completed the project build/package workflow, resuming at failed test steps after repairing three stale harness issues: missing CraftStorageAccess fixture in Behavior, XML-escaping the ampersand in the relocated FurnitureMaterials path, and missing ChestSearch fixture in Respawn. These changes affect test harnesses only.
- All remaining scripted checks completed, including native API audit and automation range regressions. Release DLL built without warnings/errors; some test fixtures emit unused-field warnings.
- Standard ZIP contains exactly manifest, unchanged README, icon, changelog, license and one plugin DLL. Nexus ZIP contains only the plugin DLL and license. Archive integrity and packaged DLL bytes verified.
- Live multiplayer acceptance remains pending. No local installation performed.

## 0.1.61 report fixes (2026-09-27)

- Removed creature repelling, base boundaries and hostile-spawn suppression, including their configuration entries. Ordinary ward access and feeding remain intact.
- Native KeyHints.Update cycles and saves the controller layout on F9. Filter only that native key result while Quartermaster's configured machine shortcut uses F9 and its modifiers match. Other native hints and controller input remain unchanged.
- Preserve saved quantities at Inventory's reconstruction clamp, shared by current and legacy loaders. Lower stack limits fill compatible partial stacks and free chest cells before emitting excess as legal-sized drops in front. Busy/unowned/unloaded chests and blocked or failed drops retain remaining stock for retry. Previously deleted quantities cannot be reconstructed.
- Release build: zero warnings/errors. Production assembly resolution now prioritizes explicit game references so test-only .NET 8 dependencies cannot enter the release build.
- Passed: 29,604 behavior assertions; 35 controller/saved-stack checks; 18 stack settings checks; 17 server settings checks; 8 UI focus checks; 628 inventory UI checks; 29 automation range checks; 58 equipment API checks for each normal and Plus fixture; 2 absent API checks; 59 craft/build/plant payment checks.
- Native binary audit: 933 member references, 72 Harmony hooks, 43 reflection targets, no failures. Verified exact native F9 call and saved-stack clamp sequences plus both current and legacy loader paths.
- Standard and Nexus archives validated for integrity, one DLL, license and byte equality with the release DLL; standard manifest, approved README, icon and changelog verified. Standard ZIP also copied to Downloads.
- Live controller, creature/raid behavior, loaded/unloaded chest conversion, overflow clearance and multiplayer acceptance remain pending. No local game installation changed.

## 0.1.62 pickup compatibility (2026-09-28)

- Investigated the supplied Windows Valheim 1.0.16 log: Quartermaster 0.1.60's AutoPickup transpiler found zero native eligibility reads, then also threw during rollback and later Epic Loot/Zen.ModLib patch rebuilds. The log does not identify the original method rewrite.
- Copy Harmony instruction objects before replacements, preserving incoming instructions, labels and exception blocks for subsequent patch rebuilds. Replace every native eligibility read; if absent, retain incoming IL and warn once instead of aborting startup.
- Scope ItemDrop.CanPickup and Humanoid.Pickup guards to the local player's automatic-pickup call. Manual pickup can still clear its ignored preference; other players and disabled Quartermaster are unaffected. Shared item flags are never changed. Custom mod attraction outside these paths may still move ignored drops.
- Disable the plugin and log the original startup error before rollback; handle unpatch and stack-restoration failures independently. Use the same guarded cleanup on destruction.
- Passed 42 pickup/cleanup checks using production source and real Harmony 2.4.2 under .NET 8: repeated add/remove patch rebuilds, replaced eligibility, bypassed CanPickup, direct input replay, multiple eligibility reads, warning deduplication, manual pickup after exceptions, remote players and disabled behavior. The shipped plugin still references the game's HarmonyX; test Harmony is not packaged.
- Release build: zero warnings/errors. Native audit: 933 binary members, 73 Harmony hooks, 43 reflection targets, zero failures; native CanPickup verified before attraction. Passed 35 controller/saved-stack checks, 17 respawn/world lifecycle checks, and 628 inventory UI checks.
- Standard and runtime-only Nexus ZIPs verified for integrity, exactly one DLL, DLL byte equality, correct license and version. README bytes unchanged from approved 0.1.61. Standard ZIP copied to Downloads.
- Supplied 75-mod Windows profile has not been reproduced in live gameplay. No local game installation changed.


## 0.1.63 owner-routed automation — 2026-10-02

Applied Claude's patch to the existing dirty checkout after backing up source/tests and release metadata. Production code compiled against the installed Valheim assemblies. Integration fixes: UiFocus pendingEdit stub, XML escaping in the new Ownership fixture, and a client routing correction: absence from GetPeers on a client does not establish disconnection because other clients are reached through the server. Two regression checks cover that topology; the ownership suite is now 40 rather than 38. No source reset, README edit or Git publication.

Full scripts/build.sh passes: Behavior 29,604; StorageCore 58; NativeStorage 9; StorageObservation 17; CraftPayment 59; AutomationRange 45; Ownership 40; HiveLimit 7, plus all other script fixtures. API audit resolves 938 members, 73 Harmony hooks and 43 reflection targets with zero failures. Full log: output/release-0.1.63/build-verified.log.

Verified release archives and installed the identical DLL into Gale Test with scripts/install_local.py after dry-run (one DLL changed; backup backups/install-0.1.63-20261002-173658). Full profile startup exited during other mods' initialization before Quartermaster loaded; RavenwoodRestorations type-load errors are recorded in output/release-0.1.63/gale-test-startup-failed.log. A separate Jotunn/Quartermaster control reached successful chainloader startup (2 loaded, 0 failed) and piece registration. Further game launching/testing was explicitly stopped by the user; test process stopped and temporary Gale launch arguments restored. No functional sorting/smelting/hive/F9 gameplay or two-client dedicated-server acceptance is claimed. No real saves or live server were edited.

Non-owner craft snapshot races remain; the touch hint is not a transaction guarantee. The release changelog states item loss/duplication risk and dedicated-server validation pending. Reporter exact message and DLL hash remain unknown. Hexium's existing server API warning concerns Inventory.AddItem; local installed-client API audit/control startup pass, but that distinct server reference build remains unverified. Publication status, archive and DLL hashes are in dist/published-0.1.63.json.


## 0.1.64 shared-chest candidate — 2026-10-07

Applied output/claude-shared-chests-2026-10-07/shared-chests.patch cleanly after backing up source, tests and release metadata. Added SharedChests to build.sh. Full build/test/package script passed. 143,985 shared-chest checks, 400 randomized sessions, 74,864 simulated actions; ownership 45, craft/payment 60, respawn 19. Native audit: 978 members, 84 hooks, 64 reflection targets, zero failures. Archive integrity and DLL bytes verified; packaged README unchanged. Detailed log and execution-result.json are beside the handoff.

Not installed or published. Live two-client dedicated-server validation remains pending. Mid-transaction world exit does not persist escrow and can lose items; closed-chest crafting retains the 0.1.63 race. Candidate changelog explicitly documents these instead of claiming loss/duplication is impossible.
