# Wolfenstein Infinite

---
💛 **Support on Patreon** — [patreon.com/pixelwhipped](https://www.patreon.com/15682673/join)  
---
# Wolfenstein Infinite
> *Get Psyched. Again. And Again.*
[https://youtu.be/_RcMwc3LSV4]

Wolfenstein Infinite is a fan-made, procedurally generated first-person shooter built in the spirit of the 1992 classic *Wolfenstein 3D* by id Software. Rather than fixed levels, each playthrough generates a unique map from composable sections, meaning no two runs are the same.

The game is built on a modular architecture — nearly everything from enemies and weapons to map sections and music is defined by mod files, meaning the game can be extended or reskinned without touching the engine.

## What's new?
### New weapons
 - Rocket Laucher 
 - Flame Thrower
### New items
 - Backpack, increase max ammo holding capcity
 - Rockets, for the Rocket Launcher
 - Gas, for the Flame Thrower
 - God Mode
### Mission objectives
 - Prisoner of War, Find him imprisoned and guide hime to the exit
 - Dynamite, find the dynamite and place a specific locations and run to the exit
 - Secret Message, Find the top secret information and transmite it from the Radio
### Other
 - Map size now 128x128 vs max prior of 64x64
 - Moddable, Simple to add/build new mods with a built in map editor 
 - Experimatal Bosses, these are generated on the fly based of sets of body parts assembled and colorized to create unique endless bosses
 - Prison doors, transparent doors
 - Blood Pools, enimies will pool blood on death
 - Dying directions, enimies can either die falling to left or right for slight variations
 - Floor and Ceiling Textures
 - More

## Thank you?

This project exists as a love letter to id Software and the game that defined a genre. Much love and respect to John Carmack, Adrian Carmack, John Romero, Tom Hall, Kevin Cloud, Jay Wilbur, and the entire id Software team.**
![Preview](https://github.com/pixelwhipped/WolfensteinInfinite/blob/master/WolfensteinInfinite/ScreenShots/Capture002.PNG)
---

## Road Map

Release 1.0.0.0 (done)
Inlcudes mod definitions for the shareware and full release version of Wolfenstein3D

Release 1.1.0.0
Add Spear of Destiny mod definition.

Release 1.2.0.0
Add Return to Danger mod definition.

Release 1.2.0.0
Add Ultimate Challenge mod definition.

![Experimental](https://github.com/pixelwhipped/WolfensteinInfinite/blob/master/WolfensteinInfinite/ScreenShots/Capture001.PNG)

---

## Requirements

- Windows (Linux/Mac not tested but may work with minor changes)
- .NET 8 or later
- SFML.Net
- NAudio

---

## Getting Started

### Demo Mode (No Additional Files Required)

The repository ships with the **Demo mod** which uses the original Wolfenstein 3D shareware data. The shareware episode was released by id Software for free distribution and is included here in that spirit. You can run the game immediately with this data.

### Adding Original Game Files

If you own a copy of Wolfenstein 3D you can add the full game data for a richer experience:

1. Locate your Wolfenstein 3D game files and place in `GameData\`
2. Launch the game — and in mods select rebuild to generate the mod data

> The original game files are **not included** in this repository and cannot be distributed here. You must own a legitimate copy.

---

## How to Play

| Key | Action |
|-----|--------|
| Arrow Keys | Move / Turn |
| Left Ctrl | Fire |
| Space | Open Door / Interact |
| Alt | Strafe |
| , / . | Cycle Weapons |
| Tab | Show Map (hold) |
| Pause | Pause Game |
| Escape | Menu |

Controls can be remapped in the Options menu.

### Objectives

Some levels have mission objectives that must be completed before the exit can be used. Objective icons appear on the HUD when active:

- **Key** — Find the key and use it to unlock the locked door
- **Secret** — Pick up the secret document and transmit it at the radio
- **Dynamite** — Collect the dynamite and place it at all marked locations. Once all placed, get to the exit before the timer runs out
- **POW** — Rescue the prisoner and escort them to the exit
- **Boss** — Defeat the boss enemy

---

## Modding

Wolfenstein Infinite is built to be modded. Each mod lives in its own folder under `GameData\Mods\` and is defined by a `mod.json` file alongside supporting assets.

### Mod Structure

```
GameData/
  Mods/
    YourMod/
      mod.json          — Enemies, weapons, textures, projectiles, music
      map.json          — Composable map sections for procedural generation
      specialmap.json   — Optional: hand-crafted levels for every 10th level
      maptestlevel.json — Optional: test levels loaded with -t launch argument
      Textures/         — Wall and door textures
      Sprites/          — Enemy, item, and decal sprites
      Sounds/           — Enemy and weapon audio
      Music/            — Music tracks
```

### Map Sections

Maps are built from composable `MapSection` tiles defined in `map.json`. Each section is a small hand-crafted room or corridor with connection points (doors) on its edges. The generator stitches these together at runtime.

It is recommended sections are in multiples of 5x5

Sections can carry objectives, enemy placements, items, decals, and special tiles. Mod levels and sections can be authored in the Map Editor.

### Running in Test Mode

Launch with `-t` to load `maptestlevel.json` directly instead of generating a map. Useful for testing specific rooms or layouts without playing through generated content.

### Enabling the Map Editor

Launch with `-e` to enable the built-in map editor. The editor allows authoring and saving map sections directly. Note: the editor is currently WPF-based and can be slow when loading large section sets — this is a known issue being worked on.

### Cheat Codes

While playing, type these codes to activate cheats:

| Code | Effect |
|------|--------|
| `iddqd` | God mode — invincibility and full health |
| `idkfa` | All weapons, max ammo, all objectives complete |
| `iddt` | Reveal entire map |
| `idclev` | Select Level |

---

## Command Line Arguments

The game supports the following command line arguments:

| Argument | Description |
|----------|-------------|
| `-r` | Rebild gamedata from provided original game file |
| `-ri` | same as -r but generate map images |
| `-g` | Generate map Image |
| `-t` | Load `maptestlevel.json` instead of generating a map |
| `-e` | Enable the built-in map editor |

Examples:
```
WolfensteinInfinite.exe -r
WolfensteinInfinite.exe -ri
WolfensteinInfinite.exe -g
WolfensteinInfinite.exe -t
WolfensteinInfinite.exe -e
```

---

## Special Levels

Every 10th level loads a hand-crafted special level rather than a generated one, if the active mods provide them via `specialmap.json`. These are fully authored levels and can be used for boss encounters, unique scenarios, or anything else a mod author wants to create.

---

## Supporting the Project

This project is free and open source. If you enjoy it or find it useful:

- ⭐ **Star the repository** at [github.com/pixelwhipped/WolfensteinInfinite](https://github.com/pixelwhipped/WolfensteinInfinite)
- 🐛 **Report bugs** — open an issue with as much detail as you can
- 🔧 **Contribute** — PRs are welcome, especially for polish, optimization, and mod content
- 💬 **Spread the word** — tell other Wolf3D fans about it
- 💛 **Support on Patreon** — [patreon.com/pixelwhipped](https://www.patreon.com/15682673/join) 

---

## Credits

**Wolfenstein Infinite** is developed by **Ben Tarrant** ([@pixelwhipped](https://github.com/pixelwhipped)).

**Wolfenstein 3D** was created by id Software in 1992.
Much love to **John Carmack, Adrian Carmack, John Romero, Tom Hall, Kevin Cloud, and Jay Wilbur** — thank you for making something that still inspires people more than 30 years later.

This project is not affiliated with or endorsed by id Software, Bethesda Softworks, or ZeniMax Media. Wolfenstein is a registered trademark of ZeniMax Media Inc.

The shareware episode of Wolfenstein 3D is included under id Software's original shareware distribution terms. The full game data is not included and must be supplied by the user.

---

## License

Source code is released under the [MIT License](LICENSE).

Game assets from the original Wolfenstein 3D shareware are the property of id Software / ZeniMax Media and are included under shareware distribution terms only.

Original assets created for this project are released under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
