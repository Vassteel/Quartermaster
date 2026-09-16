# Quartermaster owl direction

User selected a stern, orderly BURROWING OWL to replace the Deposit Chest gull. Concept requested and approved 2026-09-15. Final visual direction: assets/concepts/quartermaster-burrowing-owl.png.

- Match the approved Helmsman puffin’s angular Valheim geometry, coarse textures and muted natural palette.
- Faded moss-green cloth waistcoat with coarse stitches, subtle patches and wooden buttons and small ring of chest keys; user explicitly requested a hat. Proposed hat: worn leather cap with a short brim, slightly crooked, eyebrows visible.
- Preserve current sorting behavior, three visual throws per sorted slot, small items bouncing then fading away, the annoyed unsorted state, and idle player tracking.
- Reuse SlotThrows / GullToss scheduling and effects. Only adapt mesh/rig, head/beak origin and poses where the new owl anatomy needs it.
- Keep the actor outside the chest hierarchy and use world-lit non-emissive materials, so chest glow does not affect the bird.
- Original faceted bird geometry, surface texture, clothing, work/idle/sleep poses and preserved toss timing are implemented in 0.1.18. DLLs are installed locally and on the server; Unity appearance and multiplayer still require playtesting. README revision remains open.

Species refinement: user supplied a burrowing owl reference. Preserve its long pale legs, compact body, mottled tan/brown plumage, flat rounded head, bright yellow non-glowing eyes and expressive sideways head tilt. Supersedes generic tawny-owl concept.

## Concept provenance

Generated with Codex’s built-in image generation tool, using the approved puffin sheet for art style and the user's burrowing-owl photo for anatomy. Original output is preserved in the session generated_images directory. This PNG is concept art, not a rigged game model.

Final edit prompt: Change only the dark leather vest in all four burrowing owl views to a short faded moss-green cloth waistcoat. Matte coarse wool/linen, stitching, patches, wooden buttons and worn hems. Keep long pale legs visible, preserve leather short-brim cap, keys, yellow non-glowing eyes, mottled feathers, expressive head tilts, tossing poses, chests, composition and low-poly Valheim style.
