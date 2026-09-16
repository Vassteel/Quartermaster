# Quartermaster

<img src="assets/quartermaster-gull-icon.png" alt="Viking gull overseeing storage chests" width="320">

**SKRAAA! Twelve chests of “miscellaneous”? Give the bird a job.**

## Storage

- **Deposit Chests:** turn an ordinary chest into an intake point. Sorts one occupied slot per step into storage that remembers its item types—even when empty.
- **Chest rules:** searchable assignments, Forget/Restore, preferred and overflow destinations, base groups, copied settings and separate supply permissions.
- **Inventory tools:** Deposit All, Sort Chest, Sort Inventory and compatible-stack merging. Deposit All leaves hotbar, equipped and supported special slots alone.
- **Gull assistant:** helmet, three bouncing trinkets per sorted slot, annoyed pecks and idle glances. Squawks and asks where leftovers belong; suggests extra storage only when all receiving chests are full.
- **Visual feedback:** glowing gears and runes, delivery highlights and a temporary range display.

## Base work

- **Craft, build and upgrade** using permitted chest materials.
- **Extended building coverage:** a station inside a Deposit Chest's range supports construction and structure repairs across that area. Item crafting and repair still need the station.
- **Production:** supplies compatible processors, cooking racks and fermenters; refuels fires and torches. Per-product caps include queued output. Valuable wood stays protected unless enabled.
- **Output collection:** finished goods eject normally, wait **60 seconds** for pickup, then enter storage. Ordinary dropped loot stays put.
- **Animal feeding:** supplies hungry, calm creatures; feeding animals being tamed is opt-in.
- **Optional Helmsman integration:** ask the ship's gull to unload nearby cargo into base storage. Request only.
- **Item notices:** configurable cheat-item tag cleanup and notice hiding.

## Set up

Requires **BepInEx**. Disable **Hearthkeeper** and **AutomaticFermenters**.

1. Open a chest → **Chest Config → Use as Deposit Chest**.
2. Put sample items in receiving chests to teach their assignments.
3. Put supplies in the Deposit Chest and close it.

**F9:** machine settings. **Controller:** left-stick click + A while inventory is open. Default base radius: **100 m**, configurable.

**Loaded areas only. Multiplayer untested.** Normal recipes, processing times and stack limits remain. No automatic player restocking.

[GitHub](https://github.com/Vassteel/Quartermaster) · [Discord](https://discord.gg/abN7R2tWyK) · [Guide](GUIDE.md) · [Validation](VALIDATION.md)

Based on Hearthkeeper. Code, artwork and documentation developed with generative AI.
