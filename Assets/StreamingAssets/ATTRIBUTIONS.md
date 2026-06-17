# Asset Attributions & Licenses

This project ships pixel-art sprites, tiles, audio, and a font that are **derived
from third-party asset packs downloaded from the web**. The visible character,
enemy, and weapon sprites in `Assets/Resources/` were produced by extracting and
re-packing frames from the reference sheets listed below (see `extract_sprites.py`,
`_RawSprites/fix_sprites.py`).

> **AI-generated key art.** Separately from the third-party sprites above, the game's **key art** —
> the floor backgrounds (`Resources/Backgrounds/`), hero portraits (`Resources/Portraits/`), ending
> illustrations (`Resources/Endings/`), and the hidden-boss art (`Resources/Enemies/KingdomGuilt`) —
> was **generated with ChatGPT (OpenAI image generation)**, not sourced from third-party packs. It
> carries no third-party licence and is disclosed in `AI_USAGE.md`.

> **Provenance note.** Exact per-sprite source was **not recorded** during
> development. The table below is a best-effort attribution by *source pack*, with a
> confidence flag. Until provenance is confirmed per sprite, the project must be
> treated as carrying the **union of all obligations below** — in particular the
> **LPC ShareAlike** requirement and the **CraftPix "no claim of authorship /
> no redistribution"** requirement.

Last reviewed: 2026-06-08.

---

## Summary

| Source pack | Author(s) | License | Used for | Obligation | Confidence |
|---|---|---|---|---|---|
| **0x72 — 16×16 DungeonTileset II** | Robert ("0x72") | **CC0 1.0** | Dungeon-style heroes, enemies, weapons | None (credit optional) | Confirmed (filename match) |
| **Kenney — Roguelike / Characters / Micro / Characters v2** | Kenney Vleugels (kenney.nl) | **CC0 1.0** | Characters, tiles, UI bits | None (credit optional) | Confirmed (bundled License.txt) |
| **Kenney — Tiny Dungeon** | Kenney Vleugels | **CC0 1.0** | Map tileset (`Resources/Tilesets/tiny_dungeon`) | None | Confirmed (bundled License.txt) |
| **LPC medieval fantasy character sprites** | **Johannes Sjölund (wulax)**; base by **Stephen Challener (Redshrike)** | **CC-BY-SA 3.0 / GPL 3.0 / OGA-BY 3.0** (pick one) | Humanoid hero/enemy directional sprites; skeleton | **MUST credit + ShareAlike** | Probable (64px directional sheets, `lpc_skeleton`) |
| **CraftPix — Free monster / swordsman packs** | CraftPix.net | **CraftPix Free License** | Monster & swordsman sprites (bosses/elites) | Commercial use & modification OK; **no resale/redistribution of source files; no AI/ML-training use**; attribution not required | Probable (pack contents) |
| **Gervais / Dungeon Crawl Stone Soup (DCSS) tiles** | David E. Gervais & DCSS team | Public domain / CC0 *(verify)* | Item & decor sprites | None if CC0 — **verify** | Probable (filename match) |
| **Ark Pixel font (方舟像素字体)** | TakWolf | **SIL Open Font License 1.1** | All in-game UI text | Keep OFL notice; don't sell font standalone | Confirmed (bundled `OFL.txt`) |
| **Audio (SFX & music)** | Kenney; JaggedStone; yd; MintoDog (OpenGameArt) | **CC0 1.0** | Combat SFX, BGM | None | Confirmed — see `Assets/Resources/Audio/CREDITS.md` |

License texts:
CC0 → <https://creativecommons.org/publicdomain/zero/1.0/> ·
CC-BY-SA 3.0 → <https://creativecommons.org/licenses/by-sa/3.0/> ·
GPL 3.0 → <https://www.gnu.org/licenses/gpl-3.0.html> ·
OGA-BY 3.0 → <https://static.opengameart.org/OGA-BY-3.0.txt> ·
SIL OFL 1.1 → <https://openfontlicense.org/> ·
CraftPix license → <https://craftpix.net/file-licenses/>

---

## Required attributions (verbatim-ready)

Paste these into in-game credits / about screen as well as keeping this file:

**0x72 DungeonTileset II** — by Robert (0x72), CC0. Source:
<https://0x72.itch.io/dungeontileset-ii>

**Kenney assets** — by Kenney (kenney.nl), CC0. Source: <https://kenney.nl>

**LPC character sprites** — "[LPC] Medieval Fantasy Character Sprites" by
**Johannes Sjölund (wulax)**, based on the Liberated Pixel Cup base assets by
**Stephen Challener (Redshrike)**. Licensed **CC-BY-SA 3.0 / GPL 3.0 / OGA-BY 3.0**.
Source: <https://opengameart.org/content/lpc-medieval-fantasy-character-sprites>
> ShareAlike: any sprite derived from this set must itself be offered under
> CC-BY-SA 3.0 (or GPL 3.0 / OGA-BY 3.0), with the above credit retained.

**CraftPix free assets** — by CraftPix.net, used under the CraftPix Free License
(verified 2026-06-08, <https://craftpix.net/file-licenses/>). Permitted: "use the
resources in any number of personal and commercial projects"; "modify the resources";
"sell and distribute games with our assets". Forbidden: "you can NOT resell the art
source files … or slightly modified version of the art", and the assets "may not be
used … for the purposes of training … any artificial intelligence (AI) / machine
learning (ML) … systems". Attribution is not required ("any credit will be highly
appreciated"). Our use (modified sprites integrated into a shipped game; raw files
kept out of the repo via `.gitignore`; deterministic non-AI extraction) is within
these terms.

**DCSS / Gervais tiles** — by David E. Gervais and the Dungeon Crawl Stone Soup
team. Source: <https://opengameart.org/content/dungeon-crawl-32x32-tiles> *(verify
the exact entry and its license before release).*

**Ark Pixel font** — by TakWolf, SIL OFL 1.1. Source:
<https://github.com/TakWolf/ark-pixel-font>

**Audio** — see `Assets/Resources/Audio/CREDITS.md` (all CC0).

---

## Compliance status

| Item | Status |
|---|---|
| Audio (CC0) | ✅ Compliant — fully credited |
| Tiles `tiny_dungeon` (Kenney CC0) | ✅ Compliant |
| Font (OFL) | ✅ Compliant — `OFL.txt` retained |
| 0x72 / Kenney sprites (CC0) | ✅ Compliant |
| **LPC-derived sprites** | ✅ Obligations met — credited in-game (Title → Credits) and in this file; `Assets/Resources/Characters/LICENSE-LPC.txt` distributes the CC-BY-SA 3.0 notice + statement of changes |
| **CraftPix-derived sprites** | ✅ Compliant — use is within the verified CraftPix Free License; raw source files kept out of the repo; credited (not required, but included) |
| **Gervais/DCSS tiles** | ⚠️ Verify the exact source entry's license before public release (expected public domain / CC0) |
| README "Art by 1Misery1 / procedural generation" claim | ✅ Corrected — README Credits now point here |

### Chosen path — Option B (keep the art, meet the obligations)

Done:
- Added an in-game **Credits** screen (Title menu → "制作名单 Credits") listing all
  required attributions so end users see them.
- Added `Assets/Resources/Characters/LICENSE-LPC.txt` — distributes the CC-BY-SA 3.0
  license, the wulax / Redshrike attribution, and the list of modifications, so the
  LPC ShareAlike requirement travels with the sprites.
- Verified the CraftPix Free License covers this use; corrected the README.

Remaining before public release / submission:
1. Verify the exact Gervais/DCSS OpenGameArt entry and its license.
2. (Optional, for certainty) confirm per-sprite provenance by visual diff against the
   `_RawSprites/` packs — current source attribution is high-confidence inference,
   not a per-file audit.
