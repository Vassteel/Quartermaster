# Development source record

- Foundation: Hearthkeeper 0.1.7 by MaddCatter, recovered from the installed DLL using ILSpy 9.1.0.7988. The reference snapshot is in `upstream/Hearthkeeper`.
- Derived code: warehouse sorting/material accounting, crafting hooks, external slot compatibility and inventory count/removal helpers.
- New implementation: chest memory, routing, machine automation, fermentation, interface, procedural gear art and tests.
- AutomaticFermenters plugin metadata was read for conflict detection; its code and assets are not included.
- Game and BepInEx assemblies are local build references and are not distributed.

## Helmeted Deposit gull (0.1.8)

Original procedural helmet and readable-mesh animation rig adapted from this workspace’s Helmsman project. No Helmsman DLL is required. The Seagal sitting mesh/materials and transferred-item meshes/textures are taken from loaded vanilla/game prefabs at runtime; no game assets are redistributed. Tossed props are renderer-only nodes with local scenery collision queries, never instantiated item prefabs.
