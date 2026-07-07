# WhereItFrom

SPT 4.0 client + server mod that appends an item-source line to item hover tooltips.

Author: SerLiunx
Version: 1.0.0

When the mouse is over an item, the client plugin reads the item template id and appends a line such as:

```text
My Custom Weapons Mod
```

Unexamined items do not show source information.

## How It Works

The EFT client does not know which server mod created an item. This project therefore has two parts:

- `WhereItFrom.Server.dll` scans `SPT/user/mods` on server startup and exposes a template-id to mod-name map over HTTP.
- `WhereItFrom.Client.dll` runs in BepInEx, patches the item hover tooltip, caches that map once, and renders the source line.

The server uses conservative JSON template scanning. It is designed to avoid false positives where a mod only references a vanilla item id. Items generated entirely in compiled code may need a manual mapping in the generated server config.

## Build

Install .NET SDK 9 or newer, then run:

```powershell
.\scripts\publish.ps1 -SPTPath "<SPT_INSTALL_PATH>"
```

Replace the path with your SPT 4.0 install directory.

The script builds both projects and copies:

- `BepInEx/plugins/WhereItFrom/WhereItFrom.Client.dll`
- `SPT/user/mods/WhereItFrom/WhereItFrom.Server.dll`

To create a distributable zip package, run:

```powershell
.\scripts\package.ps1 -SPTPath "<SPT_INSTALL_PATH>"
```

The package is written to:

```text
dist/WhereItFrom-v1.0.0-SPT4.0.zip
```

## Server Config

On first server start the mod creates:

```text
SPT/user/mods/WhereItFrom/config.json
```

Use `manualMappings` for code-generated items that cannot be found by JSON scanning:

```json
{
  "includeDynamicIdHeuristics": false,
  "manualMappings": {
    "677eed5f2e040616bc7246b6": "My Code Generated Mod"
  },
  "ignoredModFolders": []
}
```

`includeDynamicIdHeuristics` is off by default because scanning source code can incorrectly classify vanilla IDs as custom items.

## Client Config

Press `F12` in game and adjust the `WhereItFrom` BepInEx settings:

- enable/disable the tooltip line
- choose the prefix text. The default is empty, so only the mod name is shown
- style the prefix and mod name separately with RGBA color sliders (`PrefixColorRGBA`, `ModNameColorRGBA`), bold, italics, and underline
- limit long mod names with `ModNameMaxLength`; set it to `0` to disable truncation
- place the source block at the bottom or top of the tooltip
- enable/disable the separator line and change its text/color (`SeparatorColorRGBA`)
- set `SeparatorText` to empty or spaces if you only want a blank line
- show or hide an unknown-source line

## Compatibility

- Target: SPT 4.0.x
- Requires the SPT server mod and BepInEx client plugin to both be installed.
