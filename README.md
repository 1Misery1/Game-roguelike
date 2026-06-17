# Embers of the Three Realms (三界余烬)

A 2D top-down roguelike action game built with **Unity 2022.3 LTS**.  
You are a nameless ember-spirit descending through three collapsing realms — Inferno, the Frost Realm, and the Chaos Abyss — possessing the bodies of fallen heroes, fighting through procedurally generated rooms, and uncovering the truth the kingdom buried: the real monster was never the Void.

---

## Game Idea

You begin as an ember-spirit in a **camp (hub)**, where hero selection is *diegetic* — you walk up to a fallen hero's pedestal and **possess** it, then descend. Each floor is a procedurally generated sequence of combat rooms → shop → boss; you collect weapons, pick talents, and forge / enchant / heal at the shop to build a run-specific loadout. Investigating story objects and making choices uncovers a branching truth and leads to one of several endings (Normal / Truth / Crown), including a hidden final boss, *Kingdom's Guilt*. Death resets the run; hero unlocks and recovered truths carry over.

**Core Loop:**  
Camp (possess a hero) → Enter Dungeon → Combat Rooms → Collect Weapons / Talents → Shop → Boss → Win or Die → Unlock Heroes & Truths → Restart

---

## Unity Version

**Unity 2022.3.44f1** (Universal Render Pipeline 2D)

Required packages (auto-installed via `Packages/manifest.json`):
- URP 2D
- Unity Input System
- Cinemachine

---

## Controls

| Action | Keyboard | Mouse |
|---|---|---|
| Move | WASD / Arrow Keys | — |
| Aim | — | Mouse position |
| Normal Attack | Space | Left Click |
| Weapon Skill | R | Right Click |
| Hero Active Skill | F | — |
| Switch Weapon | Q | — |
| Interact / Possess hero | E | — |
| Pause · Settings · Quit | Esc | — |

---

## Setup

1. Install **Unity Hub** and **Unity 2022.3.44f1**
2. Clone this repository
3. In Unity Hub: **Open** → select this folder
4. Unity installs packages from `Packages/manifest.json` on first open
5. Open the main scene under `Assets/Scenes/`

---

## Project Structure

```
Assets/Scripts/
├─ Bootstrap/   GameBootstrap (dungeon run), TrainingBootstrap, MapBuilder
├─ Core/        GameManager, RunState, PersistentState, GameSignals
├─ Data/        ScriptableObjects: Hero, Weapon, Talent, Buff, Skills, FloorTheme
├─ Actors/      Player (controller, weapon/skill handlers) + AI (15 enemy types, NavGrid A*)
├─ Combat/      CharacterStats, Health, StatModifier, Cooldown, IDamageable
├─ Dungeon/     procedural layout + room / shop / pedestal interactables
├─ Narrative/   DialogueBox, ChoiceBox, story items, branching endings
├─ UI/          code-built uGUI: HUD, weapon panel, pause menu, title, overlays
└─ Systems/     ModifierApplier (talent / buff / passive → stats)
```

---

## Scope — Feature Sorting (MoSCoW)

### Must-Have *(required for the game to function — build these first)*
- [x] Player movement and 8-directional aim
- [x] Normal attack + weapon skill system (melee / ranged / magic)
- [x] Enemy AI with pathfinding (NavGrid)
- [x] Wave-based room combat and room clearing
- [x] Health / damage / death system
- [x] Boss room and boss fight on each floor
- [x] Basic run state reset on death

### Should-Have *(important for quality, not required for first playable test)*
- [x] Multiple weapon types (26 weapons: Dagger, Longsword, Greatsword, Bow, Staff)
- [x] Talent / buff pick system between rooms
- [x] Shop room with upgrade / forge options
- [x] Multiple hero archetypes with unique active skills
- [x] Pixel-art sprites for heroes, enemies, weapons (32×32)
- [x] Sound effects and background music (combat SFX + per-floor BGM)
- [x] Proper scene transitions (title → camp → dungeon → ending)
- [x] Shared ESC pause menu (resume / settings + volume / main menu / quit)
- [x] Training arena to test a hero's weapons and skills before a run

### Could-Have *(nice extras if the core is stable and tested)*
- [ ] Animated sprite sequences for heroes and enemies
- [x] Hero unlock / cross-run progression (recovered truths carry over)
- [x] Run statistics / recap screen at end of run
- [ ] Additional enemy variants per floor
- [ ] Mobile touch controls

### Cut First *(remove if project scope becomes too large)*
- [ ] Multiplayer / co-op mode
- [ ] Procedurally generated tile maps (currently using hand-designed rooms)
- [ ] Full voice acting
- [ ] Online leaderboard

---

## GitHub Process Plan

| Milestone | Target | Status |
|---|---|---|
| W1–W3: Grey-box prototype — movement, one room, one enemy | Done | ✅ |
| W3–W6: Combat system — weapons, skills, talents, shop | Done | ✅ |
| W6–W9: Multi-floor dungeon + 6 room types + hero variety | Done | ✅ |
| W9–W12: Pixel-art sprites — heroes (8), enemies (15), weapons (26) | Done | ✅ |
| W12–W13: Difficulty curve, wave balance, Hades-style progression | Done | ✅ |
| W13–W14: Sound design, music, scene transitions | Done | ✅ |
| W14–W15: Playtesting, bug fixes, pause/HUD polish, final build | Done | ✅ |

Process: work is planned and tracked through **GitHub Issues** and a **project board** ("Game Roguelike Roadmap"); commits land on `main` and reference the issues they close.

---

## Response to Feedback

**Feedback received:** Difficulty ramping felt flat — early floors were too easy and there was no sense of escalating danger.

**What I changed:**
- Redesigned wave generation to follow a Hades-style curve: early waves use basic enemies, mid-floors introduce elites, later floors mix boss minions into normal rooms
- Added a numeric difficulty multiplier that scales enemy HP and damage per floor
- Rebalanced all enemy stat values and weapon damage numbers from scratch
- Added a visible floor indicator so players understand their progression depth

---

## Recent Additions

- **Audio system** — combat SFX and per-floor background music (AudioManager).
- **Full scene flow** — title menu → camp → dungeon → ending cutscenes, with a shared **ESC pause menu** (resume / settings + master volume / return to menu / quit to desktop).
- **Training arena** — try a hero's weapons and skills before committing to a run.
- **Branching narrative** — dialogue, player choices, story items, and three endings (Normal / Truth / Crown) including a hidden final boss.

## Known Limitations / Future Work

- Per-sprite asset provenance is best-effort inferred, not a per-file audit (see [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md)); the Gervais/DCSS tile licence still needs confirming.
- No sprite-sheet animation yet — directional sprites are static; ending cutscenes use multi-frame stills.
- Accessibility options (colour-blind palette, full keybinding remap) are not yet implemented.

---

## AI Assistance

AI coding tools were used during development (implementation, refactoring, and debugging — under my design and direction). See **[AI_USAGE.md](AI_USAGE.md)** for an honest breakdown of what was AI-assisted versus original design and decision work.

---

## Credits

| Role | Name |
|---|---|
| Game Design & Programming | 1Misery1 |
| Engine | Unity Technologies — Unity 2022.3 LTS |
| Sprites / Audio / Font | Third-party packs (0x72, Kenney, LPC/wulax, CraftPix, DCSS/Gervais, Ark Pixel font, OpenGameArt) — see [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md) |
| Key art (backgrounds / portraits / endings / boss) | **AI-generated with ChatGPT (OpenAI)** — see [`AI_USAGE.md`](AI_USAGE.md) |
| Sprite processing tooling | `extract_sprites.py` — extracts/re-packs frames from the third-party reference sheets above (not original artwork) |
| Inspiration | Hades (Supergiant Games), Binding of Isaac |

> **Note on art:** the in-game **sprites** are **derived from third-party asset packs**
> (not original artwork). The larger **key art** — floor backgrounds, hero portraits, ending
> illustrations and the hidden-boss art — is **AI-generated with ChatGPT (OpenAI)**, declared in
> [`AI_USAGE.md`](AI_USAGE.md). Some sprite sources (LPC, CraftPix) carry attribution / ShareAlike
> obligations — see [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md) for the per-source status and required actions.
