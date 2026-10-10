# Bifrost Brew

*Ride the rainbow bridge home.*

Deep in the swamp, bags full, night falling, and no portal in sight? Uncork a Bifrost Brew. The Bifrost opens beneath your feet and carries you straight back to your bed. Just don't try to smuggle your ore across. The gods have rules.

## Features

- **A mead that takes you home.** Drink it, and a few heartbeats later you're standing by your bed.
- **A proper send-off.** A rune-circled portal opens under your feet, light spirals around you, and you vanish in a pillar of light. Your friends get to watch.
- **Brewed like any other mead.** Make the base at the Mead Ketill, ferment it, and you're set.
- **Looks the part.** A shimmering, glowing bottle, and you can set it out on your tables and shelves with the Serving Tray. Drinking it straight off the table works too.
- **Tune it to taste.** The countdown, cooldown and recipe are all configurable, and servers keep everyone on the same settings.

## Brewing

1. At the **Mead Ketill**: 10 Honey, 10 Greydwarf Eye and 2 Surtling Core make a **Mead base: Bifrost**.
2. Let it ferment in a **Fermenter** for **6 Bifrost Brew**.

Server owners can change the recipe; see [Configuration](#configuration).

## Drinking it

Drink a Bifrost Brew and the bridge begins to open (5 seconds by default). When it's ready, you're home: at your bed, or at the starting stones if you haven't claimed one yet. If you're sitting, steering a ship or riding, you'll hop off first.

A few rules the gods insist on:

- **No smuggling.** Like a portal, the Bifrost won't carry ore, metal bars or anything else a portal refuses. You can't drink it while carrying them, and you keep the brew. Grab some ore while the bridge is opening and the trip fizzles, brew and all.
- **No cheating death.** Die before the bridge opens and the trip is off.
- **One trip at a time.** You can't drink another while the bridge is opening or on cooldown.

## Configuration

Change these in your mod manager's config editor, an in-game configuration manager, or `BepInEx/config/com.voxxsidus.bifrostbrew.cfg`.

| Setting | Default | What it does |
|---|---|---|
| Channel seconds | 5 | How long the bridge takes to open (0–30). 0 sends you home instantly. |
| Cooldown minutes | 0 | How long before you can drink another after a trip (0–120). 0 means no cooldown. |
| Mead base ingredients | `Honey:10, GreydwarfEye:10, SurtlingCore:2` | What goes into the mead base, as `Item:Amount` pairs. |
| Mead base station | `piece_MeadCauldron` | Where the base is made: `piece_MeadCauldron` (Mead Ketill), `piece_cauldron` (Cauldron) or `piece_workbench` (Workbench). Leave empty to make it anywhere. |
| Brews per base | 6 | How many brews one base ferments into (1–50). |

Ingredients use the game's item IDs, like `GreydwarfEye` or `SurtlingCore`. Changes apply right away, even mid-game. On a server, the server's settings win and only admins can change them.

## Installation

The easiest way is a mod manager (r2modman, Gale or Thunderstore Mod Manager), which also installs what Bifrost Brew needs:

- BepInExPack Valheim
- Jötunn

To install by hand, copy `BifrostBrew.dll` into `BepInEx/plugins/BifrostBrew/`.

**Playing together?** The server and every player need the mod.

## Known issues

- Logging out resets an active cooldown.
- If your bed has been destroyed, you'll still arrive where it used to stand.
- Taking damage doesn't interrupt the bridge; only death does.

## Changelog

**0.1.0**
- First release. Skål!
