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
├─ Data/        ScriptableObjects: Hero, Weapon, Talent, Buff, Skills, Enums
├─ Core/        GameManager, RunState, PersistentState
├─ Combat/      StatModifier, CharacterStats, Health, Cooldown, IDamageable
├─ Player/      PlayerController, PlayerWeaponHandler, HeroSkillHandler
├─ AI/          15 enemy types + NavGrid pathfinding
├─ Dungeon/     DungeonGenerator, RoomController (6 room types)
└─ Systems/     ModifierApplier (applies talent/buff/passive to stats)
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
- [ ] Sound effects and background music
- [ ] Proper scene transitions (main menu → floor → game over)

### Could-Have *(nice extras if the core is stable and tested)*
- [ ] Animated sprite sequences for heroes and enemies
- [ ] Achievement / unlock progression system
- [ ] Run statistics screen at end of run
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
| W13–W14: Sound design, music, scene transitions | In Progress | 🔄 |
| W14–W15: Playtesting, bug fixes, final build | Next | ⬜ |

Branch strategy: feature branches merged to `main` via PR after each milestone.

---

## Response to Feedback

**Feedback received:** Difficulty ramping felt flat — early floors were too easy and there was no sense of escalating danger.

**What I changed:**
- Redesigned wave generation to follow a Hades-style curve: early waves use basic enemies, mid-floors introduce elites, later floors mix boss minions into normal rooms
- Added a numeric difficulty multiplier that scales enemy HP and damage per floor
- Rebalanced all enemy stat values and weapon damage numbers from scratch
- Added a visible floor indicator so players understand their progression depth

---

## What I Will Build Next

1. **Sound system** — integrate background music per floor and one-shot SFX for attacks, hits, and deaths using Unity's AudioManager pattern
2. **Scene transitions** — main menu → hero select → dungeon → game-over/win screen with proper async loading
3. **Playtesting pass** — fix edge cases in weapon skill interactions, ensure all 15 enemies have correct AI behaviour on every floor

---

## AI Assistance

AI coding tools were used during development (implementation, refactoring, and debugging — under my design and direction). See **[AI_USAGE.md](AI_USAGE.md)** for an honest breakdown of what was AI-assisted versus original design and decision work.

---

## Credits

| Role | Name |
|---|---|
| Game Design & Programming | 1Misery1 |
| Engine | Unity Technologies — Unity 2022.3 LTS |
| Art / Audio / Font assets | Third-party packs (0x72, Kenney, LPC/wulax, CraftPix, DCSS/Gervais, Ark Pixel font, OpenGameArt) — see [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md) |
| Sprite processing tooling | `extract_sprites.py` — extracts/re-packs frames from the third-party reference sheets above (not original artwork) |
| Inspiration | Hades (Supergiant Games), Binding of Isaac |

> **Note on art:** the in-game sprites are **derived from third-party asset packs**,
> not original or procedurally generated artwork. Some sources (LPC, CraftPix) carry
> attribution / ShareAlike obligations that must be met before public release — see
> [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md) for the per-source status and required actions.
