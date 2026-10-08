# Development source record

- Historical development reference: MIT-licensed storage code was used in early versions. Its source snapshot is archived outside the active checkout and is not built or distributed.
- Replacement storage core (0.1.37): warehouse operations, crafting hooks, equipment-slot compatibility and the inherited inventory count/removal/type helpers now have newly written implementations. Existing public contracts and native API signatures remain for compatibility. Quartermaster-authored metadata-safe transfers, overflow and world pickup logic are retained. No implementation bodies from the reference snapshot remain in these four files; the historical reference is not the source of the new furniture or artwork.
- New implementation: chest memory, routing, machine automation, fermentation, interface, procedural gear art and tests.
- AutomaticFermenters plugin metadata was read for conflict detection; its code and assets are not included.
- Game and BepInEx assemblies are local build references and are not distributed.

## Helmeted Deposit gull (0.1.8)

Original procedural helmet and readable-mesh animation rig adapted from this workspace’s Helmsman project. No Helmsman DLL is required. The Seagal sitting mesh/materials and transferred-item meshes/textures are taken from loaded vanilla/game prefabs at runtime; no game assets are redistributed. Tossed props are renderer-only nodes with local scenery collision queries, never instantiated item prefabs.

## Original clay resource (storage expansion, development)

- `scripts/build_clay.py` authors the irregular lump, ground-patch geometry, UVs and 128px earth-pigment texture from mathematical shapes and deterministic noise. It takes no external art inputs.
- `scripts/render_clay.py` renders the original lump into the inventory icon. The texture, mesh and icon are embedded as Quartermaster resources.
- Runtime uses the installed game's Flint and Pickable_Flint as native behavior templates and a native shader. Both visual hierarchies are replaced by the authored clay art. The native collection, world generation, networking and respawn systems remain responsible for gameplay.
- No ClayBuildPieces or other blacks7ar code, meshes, textures, icons, sounds or prefabs are used, and no such mod is a dependency.
- User-provided cabinet photos are planning references only and are not embedded or packaged as game assets.

## Original apothecary assets (storage expansion, development)

- `scripts/build_apothecary.py` authors cabinet joinery, pottery, flasks, lids, tags and fill meshes, plus a deterministic 128px timber/ceramic atlas. `scripts/render_apothecary.py` renders the same meshes into the six original inventory/build icons. No external meshes or texture images are inputs.
- Runtime clones native Flint item behavior and wooden-chest inventory/network behavior; replacement cabinet art uses the installed native building shader. Crystal borrows the loaded vanilla crystal-wall material, preserving its native transparency settings. Clay and metal use the loaded native building shader; no stripped built-in shader is required. The new Pottery Kiln deliberately retains the installed native smelter body/effects with its own conversion list. No native bundle is redistributed.
- `FurnitureAudio` synthesizes original short clay-contact and glass-contact clips. It references the game's existing SFX mixer routing, not another mod's sounds.
- The supplied cabinet photographs inform layouts only; they are not embedded or included by packaging. No ClayBuildPieces or other blacks7ar asset/code dependency is introduced.

### Apothecary variations

- `scripts/apothecary_variants.py` authors seven related arrangements, convex corner/slope collision geometry and wall/beam snap positions from the existing original board/mesh primitives. It imports no model or image assets.
- `scripts/render_apothecary_overview.py` renders the actual seven model assemblies together at one scale for review. The overview is a newly rendered 3D scene, not a modification of the supplied reference photographs.
- Geometry hashes for the previously approved models were checked against the saved development snapshot; their geometry remains unchanged. Runtime prefab IDs and first-pair inventory dimensions remain stable.

## Bulk furniture development assets

- `scripts/bulk_storage_models.py` authors original narrow/wide lumber and ingot racks, ore/scrap and hooded coal bins, and bounded representative log/ingot/loose-material piles. It reuses Quartermaster's original board/lathe primitives; no external models or mod textures are inputs.
- `scripts/build_apothecary.py` exports these through the existing shared mesh format and creates an original neutral metal texture, preserving the approved cabinet atlas and geometry.
- `scripts/render_bulk_overview.py` renders the same authored meshes, fill thresholds and material palettes. Representative piles depict material families rather than exact replicas of every inventory item; manually stored unrelated items use a neutral parcel. Runtime furniture contact clips are original synthesized timber, metal and loose-material sounds, using the existing spatial SFX limits.

## Hide, textile, feather and bone furniture

- `scripts/soft_storage_models.py` authors the original hanging hide rail, hide-roll shelf, floor/wall textile shelves, feather coffer with wooden hinge pins, and bone crate. Original representative loads include draped and rolled skins, folded cloth, thread hanks, feathers and bones. No external meshes, textures or other mods' assets are used.
- The original `soft_albedo.png` atlas is generated by `scripts/build_apothecary.py`; previous approved model payloads are checked against `output/soft-storage/previous-models.json`. Existing cabinet/bulk art is preserved.
- `scripts/render_soft_overview.py` and `scripts/render_feather_lid.py` render the same authored geometry, including the runtime lid pivot. These studio previews do not establish final native-Valheim material compatibility. Rustle, bone and lid contact audio reuse the original synthesis and bounded spatial SFX system.

## Masonry and overhead lumber furniture

- `scripts/masonry_storage_models.py` authors the original stone pallets, braced masonry crib, beam-hung lumber rack and bounded stone/marble/grausten piles. No external models, photographs, other mods' assets or sound recordings are inputs. The rafter rack reuses Quartermaster's already authored lumber loads.
- `scripts/build_apothecary.py` generates the original 128px stone atlas with muted grain and mineral seams. `scripts/render_masonry_overview.py` renders these exact meshes; visual compatibility against native game pieces remains pending.
- All 60 earlier model payloads and 28 earlier PNGs match the pre-slice snapshot in `output/masonry-storage/previous-assets.json`. Quiet stone contacts extend the existing original synthesized audio; native templates remain behavior/shader references only.

## Pantry and larder furniture

- `scripts/pantry_storage_models.py` authors six original furniture arrangements, projecting hanging pegs, tied meat/fish, pantry portions, produce, berry/mushroom alternatives, grain/flour mounds, sacks and the grain-bin lid. All geometry uses Quartermaster's original primitives, without external mod or photographic inputs.
- `scripts/build_apothecary.py` generates the original 128px food/sack atlas. Short hanging-contact and grain-rustle sounds extend Quartermaster's original synthesis under the existing SFX cooldown.
- `scripts/render_pantry_overview.py` renders these exact models. The grain bin is shown open in that preview; its runtime default is closed. Previous artwork is verified against `output/pantry-storage/previous-assets.json` (82 models and 33 PNGs). Native-Valheim art comparison and runtime audio audition remain pending.


## Individual bronze ceiling hooks and native food displays

- `scripts/ceiling_hook_model.py` authors original small bronze plates, rivets, interlinked low-poly chains and open hooks. It replaces both wooden pantry rails. The material reuses Quartermaster's original neutral metal atlas with a restrained bronze tint. Contact sounds are original synthesized metallic clinks.
- `src/NativeHangingItem.cs` borrows meshes/materials from the stored item's already-loaded game prefab at runtime. Skinned meshes are baked once per referenced prefab and freed after their final display is removed. No game models, textures or third-party mod assets are extracted, redistributed or embedded. No gameplay components are copied.
- `scripts/render_pantry_rails.py` renders the authored empty hooks; native stored-item poses must be reviewed in-game. The two hook icons are also empty. Existing generic pantry load payloads remain original assets but are no longer used by the meat/fish hook displays.


## Armory racks and ammunition

- `scripts/armory_storage_models.py` authors nine original timber rack arrangements with split retaining teeth, projecting shoes and braces, leather quivers/rests, bolt pockets and low-poly forged pegs. Existing original wood, leather and bronze materials are reused. No other mod's models, icons, textures or audio are inputs.
- `scripts/render_armory_overview.py` and the shared icon renderer show empty frames. Stored equipment meshes and materials come only from already-loaded game/item prefabs at runtime through the existing render-only cache. Native game art is not embedded or redistributed.
- Two quiet original synthesized contact profiles cover ammunition seating and equipment placement, using the established SFX mixer, range and cooldown. Native-art comparison and runtime audio audition remain pending.
- All 115 previous authored model payloads and 40 prior PNG files match output/armory-storage/previous-assets.json, including the user-approved small ceiling plates.

## Armor wardrobes and pickup filter icon

- `scripts/wardrobe_storage_models.py` authors original plank wardrobes, shelves, doors, hinges and straps using existing original Quartermaster materials. `scripts/render_wardrobe_overview.py` renders the empty furniture with the runtime hinge pivots. Stored armor meshes are borrowed only from already-loaded item prefabs; no native or third-party armor artwork is embedded.
- Hinge and latch sounds are original synthesized clips under the existing range, mixer and cooldown rules. Runtime audio audition and native-art comparison remain pending.
- `scripts/build_pickup_icon.py` authors the original procedural crossed-out magnet in `assets/ui/pickup-filter.png`. It does not copy the trader bag or screenshot artwork. The preview is `output/pickup-icon/magnet-preview.png`.

## Trophy, treasure and gem displays

- `scripts/display_storage_models.py` authors five original low-poly timber arrangements, raised trophy rests, shallow gem pockets, bronze fittings and a hinged coffer. Materials reuse Quartermaster's original timber/metal atlases. No external models, textures or mod assets are inputs.
- `scripts/render_display_overview.py` renders the exact authored empty pieces and coffer hinge geometry. Native trophy, valuables and gemstone meshes are borrowed only from already-loaded item prefabs at runtime through the existing bounded display cache; no native artwork is embedded or redistributed.
- Audio reuses Quartermaster's original synthesized equipment contact, crystal clink, metal contact and hinge/latch profiles. In-game audio audition and comparison beside native furniture remain pending.

## Native-style configuration menus

- `src/NativeMenuTheme.cs` borrows already-loaded Valheim UI sprites, font assets and font materials at runtime. No native callbacks or gameplay components are cloned into configuration controls.
- The browser layout preview under `output/menu-redesign` references locally extracted native sprites/fonts under `output/menu-preview/native` for review only. These preview files and extracted native assets are not included in the install ZIP. Runtime menus use the game's own loaded resources.

## 2026-09-25 craft supply access reference

- Inspected AzuCraftyBoxes 1.8.24 from https://valheim.hexium.gg/mods/Azumatt/AzuCraftyBoxes (public release DLL, static decompilation only). Its ordinary-container path counts accessible nearby inventories without requiring ownership, removes materials and calls native Container.Save explicitly.
- CraftStorageAccess is independently written Quartermaster code applying that access/persistence pattern to Quartermaster's existing exact-prefetch and player-only payment implementation. No AzuCraftyBoxes source, DLL, dependency or assets are included. Automation retains its existing owner-only behavior.

## 2026-09-25 Plant Easily compatibility

- Reviewed https://github.com/AdvizeGH/Advize_ValheimMods at commit 1deeb3962eb8681394961b0f35a4dfbc315c6f99 (Plant Easily 2.2.2) to identify the ghost-status, batch-payment and harvest/replant contracts.
- PlantBatchPlan and PlantEasilyCompatibility are original Quartermaster adapter code. They reference the installed mod through reflection; no Plant Easily source, binaries or assets are bundled. Its geometry evaluation and planting remain in that mod.
