# Quartermaster

The approved storage/automation design, implemented as an independent Hearthkeeper-derived local mod with new fermentation support. Build version: **0.1.3**.

See [usage and installation](packaging/README.md) and [validation](VALIDATION.md).

## Build

Requires .NET SDK 8 and an existing Valheim/BepInEx installation. Set `DOTNET` to your SDK executable and optionally `VALHEIM_PATH` to the game root, then run:

```sh
bash scripts/build.sh
```

On this workspace's host:

```sh
DOTNET_CLI_HOME=/tmp/wildglow-cli DOTNET=/tmp/wildglow-dotnet/dotnet bash scripts/build.sh
```

Build from this `Quartermaster` directory. The build script compiles the plugin, runs behavior and API checks, then packages the ZIP. The icon PNG is checked in; regenerating it requires Python with Pillow and `python scripts/icon.py`.

The behavior harness compiles the actual shipped Policy and InventoryTransfers sources against small deterministic interfaces. It does not run Unity. ApiCheck independently resolves the compiled plugin's game/runtime references, Harmony target signatures and reflected private APIs against the installed DLLs.

`src` is the maintained implementation. `upstream` is an attribution/recovery snapshot only. `dist` contains the local installation package. Local installation details and backups are recorded in `INSTALLATION.md`.

## AI disclosure and publishing

Significant portions of Quartermaster's code, tests, documentation and procedural artwork code were generated using OpenAI Codex from user-provided requirements. The packaged README includes the disclosure. Select **AI Generated** on the Thunderstore listing when publishing, as required by [Thunderstore's disclosure notice](https://old.thunderstore.io/c/valheim/).
