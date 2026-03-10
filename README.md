# Wolfenstein Infinite

> ⚠️ **VERY EARLY BETA — WORK IN PROGRESS**
> This project is in active development. Expect bugs, missing features, incomplete systems, and frequent breaking changes. Contributions, feedback, and patience are all very welcome.

---

## What Is This?

Wolfenstein Infinite is a fan-made, procedurally generated first-person shooter built in the spirit of the 1992 classic *Wolfenstein 3D* by id Software. Rather than fixed levels, each playthrough generates a unique map from composable sections, meaning no two runs are the same.

The game is built on a modular architecture — nearly everything from enemies and weapons to map sections and music is defined by mod files, meaning the game can be extended or reskinned without touching the engine.

**This project exists as a love letter to id Software and the game that defined a genre. Much love and respect to John Carmack, Adrian Carmack, John Romero, Tom Hall, Kevin Cloud, Jay Wilbur, and the entire id Software team.**

---

## Status

This is a very early public release. Core systems are in place but many things are incomplete, unbalanced, or actively being worked on. The list includes but is not limited to:

- Enemy AI is functional but basic
- Map generation works but needs balance tuning
- Some animations are not fully wired
- Music transitions are scaffolded but incomplete
- Map editor exists but is slow on large section sets
- Save/load system is in place but not battle tested
- Many polish items are outstanding

If you find bugs — and you will — please open an issue. If you want to contribute, read the modding section below.

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

1. Locate your Wolfenstein 3D game files
2. Run the extractor tool included with the project — it will parse the original game data and export it into the mod format the engine expects
3. The extracted data will be placed in `GameData\Wolfenstein3D\`
4. Launch the game — the full mod will be detected automatically

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

Sections can carry objectives, enemy placements, items, decals, and special tiles. Full documentation on section authoring is in the Map Editor.

### Running in Test Mode

Launch with `-t` to load `maptestlevel.json` directly instead of generating a map. Useful for testing specific rooms or layouts without playing through generated content.

### Enabling the Map Editor

Launch with `-e` to enable the built-in map editor. The editor allows authoring and saving map sections directly. Note: the editor is currently WPF-based and can be slow when loading large section sets — this is a known issue being worked on.

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

💛 **Support the developer** — if you'd like to support Ben's work financially, a donation link is coming soon. Watch this space.

---

## Credits

**Wolfenstein Infinite** is developed by **Ben Tarrant** ([@pixelwhipped](https://github.com/pixelwhipped)).

A significant portion of the engine architecture, game systems, and code in this project was designed and written in collaboration with **Claude** (Anthropic's AI assistant). Claude contributed across nearly every system — from the raycaster and enemy AI to map generation(Ah-hem claude. ya know I wrote the underlying game engine, though you helped wit quantization a bit well quite a bit), mission objectives, and save systems. This project is an experiment in what human creativity and AI collaboration can build together.

**Wolfenstein 3D** was created by id Software in 1992.
Much love to **John Carmack, Adrian Carmack, John Romero, Tom Hall, Kevin Cloud, and Jay Wilbur** — thank you for making something that still inspires people more than 30 years later.

This project is not affiliated with or endorsed by id Software, Bethesda Softworks, or ZeniMax Media. Wolfenstein is a registered trademark of ZeniMax Media Inc.

The shareware episode of Wolfenstein 3D is included under id Software's original shareware distribution terms. The full game data is not included and must be supplied by the user.

---

## License

Source code is released under the [MIT License](LICENSE).

Game assets from the original Wolfenstein 3D shareware are the property of id Software / ZeniMax Media and are included under shareware distribution terms only.

Original assets created for this project are released under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
