# Bifrost Brew

A shimmering blue mead that carries you home across the rainbow bridge.

Drink a Bifrost Brew and, after a short channel, you are teleported to your bed. Think of it as a hearthstone for Valheim that still plays by the game's portal rules.

## Features

- **Mead base: Bifrost**, brewed at the Mead Ketill.
- **Bifrost Brew**, fermented from the base. Drinking it opens the Bifrost and sends you home.
- A rune-ringed portal opens under your feet while blue light spirals around you, then you vanish in a pillar of light. Other players see it too.
- Blue versions of the lingering stamina mead and its mead base, with matching inventory icons and a blue glow.
- Configurable channel time and cooldown, synced from the server in multiplayer.

## Recipe

| Step | Station | Input | Output |
|---|---|---|---|
| Brew the base | Mead Ketill | 10 Honey, 5 Greydwarf Eye, 1 Surtling Core | 1 Mead base: Bifrost |
| Ferment | Fermenter | 1 Mead base: Bifrost | 6 Bifrost Brew |

## How it works

1. Drink a Bifrost Brew. A **Bifrost** status effect counts down (5 seconds by default).
2. When the countdown ends, you are teleported to your bed. If you have no bed set, you go to the starting stones.
3. If you are sitting, steering a ship or riding, you are dismounted first.

Rules:

- **Portal rules apply.** You can't drink it while carrying anything a portal won't take (ores, metal bars, etc.), and the brew isn't used up. If you pick up such an item during the countdown, the teleport fails and the brew is spent.
- **Dying cancels it.** If you die during the countdown, you aren't teleported.
- **One at a time.** You can't drink another while the countdown or a cooldown is active.

## Configuration

Edit `BepInEx/config/com.preston.bifrostbrew.cfg` (created on first launch), your mod manager's config editor, or an in-game configuration manager.

| Setting | Default | Range | Description |
|---|---|---|---|
| Channel seconds | 5 | 0–30 | Delay between drinking and teleporting. 0 teleports instantly. |
| Cooldown minutes | 0 | 0–120 | Time after a teleport before another brew can be drunk. 0 disables the cooldown. |

In multiplayer, the server's settings apply to everyone and only admins can change them.

## Installation

Install with a mod manager (r2modman, Gale, Thunderstore Mod Manager), which also installs the dependencies:

- BepInExPack Valheim
- Jötunn

To install manually, copy `BifrostBrew.dll` into `BepInEx/plugins/BifrostBrew/`.

**Multiplayer:** the server and every player must have the mod installed, with matching major and minor versions.

## Known issues

- Logging out clears an active cooldown.
- If your bed has been destroyed, the brew still takes you to where it stood.
- Taking damage doesn't interrupt the countdown; only death does.

## Changelog

**0.1.0**
- Initial release: Mead base: Bifrost, Bifrost Brew, and the teleport home.

## Feedback and source

Report bugs and suggestions on [GitHub](https://github.com/voxxsidus/BifrostBrew).
