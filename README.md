# Bifrost Brew

A Valheim mod that adds **Bifrost Brew**, a shimmering blue mead that carries you home across the rainbow bridge. Drink it and, after a short channel, you are teleported to your bed. Think of it as a hearthstone for Valheim that still plays by the game's portal rules.

Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Jötunn](https://github.com/Valheim-Modding/Jotunn).

> **Status:** early development (v0.1.0). The recipe and balance are still being tuned.

## Features

- **Mead base: Bifrost**, brewed at the Mead Ketill.
- **Bifrost Brew**, fermented from the base. Drinking it opens the Bifrost and sends you home.
- Blue recolors of the lingering stamina mead and its mead base, including matching inventory icons and a blue glow.
- Server-synced config for channel time and cooldown.

## How to get it

| Step | Station | Input | Output |
|---|---|---|---|
| Brew the base | Mead Ketill | 10 Honey, 5 Greydwarf Eye, 1 Surtling Core | 1 Mead base: Bifrost |
| Ferment | Fermenter | 1 Mead base: Bifrost | 6 Bifrost Brew |

## How it works

1. Drink a Bifrost Brew. A **Bifrost** status effect counts down (5 seconds by default).
2. When the countdown ends, you are teleported to your bed spawn point. If you have no bed set, you go to the starting stones, the same place you would respawn.
3. If you are sitting, steering a ship or riding, you are dismounted first.

Rules:

- **Portal rules apply.** You can't drink it while carrying anything a portal won't take (ores, metal bars, etc.). The brew is not used up. If you pick up such an item during the countdown, the teleport fails and the brew is spent.
- **Dying cancels it.** If you die during the countdown, the teleport doesn't happen.
- **One at a time.** You can't drink another while the countdown or a cooldown is active.

## Configuration

Settings live in `BepInEx/config/com.preston.bifrostbrew.cfg`, which is created on first launch. You can also change them in game with a configuration manager mod.

| Setting | Default | Range | Description |
|---|---|---|---|
| Channel seconds | 5 | 0–30 | Delay between drinking and teleporting. 0 teleports instantly. |
| Cooldown minutes | 0 | 0–120 | Time after a teleport before another brew can be drunk. 0 disables the cooldown. |

In multiplayer, the server's values are synced to all clients and only admins can change them.

## Installation

Requirements:

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

Manual install: copy `BifrostBrew.dll` into `BepInEx/plugins/BifrostBrew/`.

Every player and the server must have the mod installed, with matching major and minor versions.

## Known limitations

- Status effects aren't saved, so logging out clears an active cooldown.
- If your bed has been destroyed, the brew still takes you to where it stood. (A normal respawn would send you to the starting stones instead.)
- Taking damage doesn't interrupt the countdown; only death does.

## Building from source

### Prerequisites

- Valheim (Steam)
- A BepInEx install with Jötunn, either in the game folder or a mod manager profile (r2modman, Gale, etc.)
- The [.NET SDK](https://dotnet.microsoft.com/download), or Visual Studio / Rider. The project targets .NET Framework 4.8.

### Local setup

Create `Environment.props` in the repository root (it is git-ignored) and point it at your install:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Valheim install folder -->
    <VALHEIM_INSTALL>C:\Program Files (x86)\Steam\steamapps\common\Valheim</VALHEIM_INSTALL>
    <!-- BepInEx folder to build against: the game's, or a mod manager profile's -->
    <BEPINEX_PATH>$(VALHEIM_INSTALL)\BepInEx</BEPINEX_PATH>
    <!-- Where Debug builds are deployed -->
    <MOD_DEPLOYPATH>$(BEPINEX_PATH)\plugins</MOD_DEPLOYPATH>
  </PropertyGroup>
</Project>
```

`DoPrebuild.props` has `ExecutePrebuild` set to `true`, so the first build makes Jötunn generate publicized Valheim assemblies (under `valheim_Data/Managed/publicized_assemblies`). The build needs these; don't turn it off.

### Build

```bash
dotnet build BifrostBrew.sln -c Debug
```

```bash
dotnet build BifrostBrew.sln -c Release
```

- **Debug** copies `BifrostBrew.dll` (plus `.pdb` and `.mdb` debug symbols) into `$(MOD_DEPLOYPATH)\BifrostBrew\`. Restart Valheim to load it.
- **Release** packages a Thunderstore zip at `BifrostBrew/bin/Release/net48/BifrostBrew.zip`, built from `BifrostBrew/Package/` (`manifest.json`, `icon.png`) and `BifrostBrew/README.md`.

Both builds also copy the game and mod assemblies into the Unity project (see below).

### After a Valheim update

Rebuild. The prebuild regenerates the publicized assemblies automatically.

### Project layout

| Path | Contents |
|---|---|
| `BifrostBrew/BifrostBrewPlugin.cs` | Plugin entry point: config, items, recipe, fermenter conversion, recolors, localization |
| `BifrostBrew/BifrostTeleport.cs` | The teleport status effect and the Harmony patch that enforces portal rules |
| `BifrostBrew/Package/` | Thunderstore manifest and icon |
| `BifrostBrewUnity/` | Unity project for authoring asset bundles (not used by the mod yet) |
| `scripts/` | Post-build deploy and packaging scripts |
| `DESIGN.md` | Design decisions and open questions |

### Unity project

`BifrostBrewUnity/` is set up for Unity 6000.0.61f1. Build the solution at least once before opening it; the build copies the required assemblies into `Assets/Assemblies`. Those DLLs are copyrighted game and library files and are git-ignored; keep it that way.

### Debugging

See [Debugging Plugins via IDE](https://github.com/Valheim-Modding/Wiki/wiki/Debugging-Plugins-via-IDE) on the Valheim Modding wiki.

## Credits

Created from the [JotunnModStub](https://github.com/Valheim-Modding/JotunnModStub) template.

## License

[MIT No Attribution](LICENSE).
