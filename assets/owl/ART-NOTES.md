# Quartermaster owl asset

The 0.1.28 owl uses authored, articulated mesh assets with a generated material atlas. `scripts/build_owl.py` reproduces both levels of detail without Blender. Existing chest behaviour drives body, head, wing and eye pivots through `QuartermasterOwl`; storage/network logic is unchanged.

- Near: 19,624 triangles. Far: 5,932 triangles. Twelve renderers in each active LOD.
- Shared meshes, mipmapped texture and four world-lit materials are reference-counted across owl instances. Per-instance transforms preserve independent animations.
- No lights, particles, colliders or network objects are added by the model. Existing 30 m visibility handling remains in DepositGull.
- All game assets are embedded in Quartermaster.dll. No new mod dependency is introduced.
- Build checks cover binary parsing, geometry bounds, UV gutters, distance budgets, malformed inputs, Unity API resolution and embedded asset equality.

## Visual verification

Run `scripts/build_owl.py`, then Blender in background mode with `scripts/render_owl.py`. The previews and GLB in `output/owl-upgrade` use the actual generated model, atlas and pivots. Studio lighting is not Valheim lighting. The optional legacy coffer render shows dormant custom chest geometry; the current build keeps that chest appearance disabled.

Still needed in Valheim: day/night world lighting, all chest tiers, peck/sleep/hop poses, smooth distance transitions and frame time with many Deposit Chests. Geometry and compile checks do not establish in-game visual acceptance or performance.

## Texture provenance

Generated using the built-in image generation tool, saved as `assets/owl/albedo.png`. Generated source was copied unchanged. All quadrants are UV mapped into the 3D mesh with mip gutters. Game geometry and Blender previews are produced from code rather than generated images.

Exact texture prompt:

Create a game-ready square 2048x2048 diffuse/albedo MATERIAL ATLAS for a detailed stylized burrowing owl wearing olive canvas waistcoat and brown leather cap. This is a FLAT TEXTURE SHEET, not an owl illustration or a model. Exactly four equal square quadrants without gaps: TOP LEFT dense natural warm umber-and-cream mottled burrowing owl feather plumage, small overlapping down feathers and fine barbs; TOP RIGHT soft warm ivory facial down plumage with delicate pale tan radial feather fibers and subtle brown flecks; BOTTOM LEFT muted olive green woven linen/canvas, tiny fine textile weave with natural variation, no folds; BOTTOM RIGHT aged dark chestnut brown leather, fine pores and mild wear, no straps, seams, objects. The four squares meet exactly at image center x=50%, y=50%. Texture fills each quadrant edge-to-edge. Orthographic flat material scan, uniformly lit, no cast shadows, no highlights, no perspective, no text, no labels, no borders, no grid lines, no transparency. Realistic detailed material fibers with slightly painterly Valheim-compatible appearance; no giant feather shapes, no eyes, no bird body, no buttons.

