# AI Usage Statement

This project was developed with the help of AI tools — an AI **coding** assistant (Anthropic's Claude, used through the Claude Code CLI) and AI **image generation** (ChatGPT / OpenAI) for the game's key art. In the interest of academic honesty, this document sets out how AI was and was not used.

## What AI was used for

- **Implementation** — turning my designs and instructions into C# (components, systems, scene-building code).
- **Refactoring** — most notably migrating the entire user interface from Unity's immediate-mode `OnGUI` (IMGUI) to retained-mode **uGUI** (Canvas), and tidying the code afterwards.
- **Debugging** — diagnosing and fixing runtime issues I found while testing.
- **Editor automation** — an AI-driven Unity Editor plugin (Coplay, via the Model Context Protocol) was used during development to run builds, inspect and adjust scene objects, and run play-tests.
- **Drafting comments and documentation.**
- **Key-art image generation** — the floor backgrounds, hero portraits, ending illustrations and the hidden-boss art were generated with **ChatGPT (OpenAI image generation)**, then imported, cropped/arranged and wired into the game by me.

## What is my own work

- **Game design** — the concept, the three-realms premise and narrative, the camp / ember-possession mechanic, the branching multi-ending structure, and the difficulty, content, and balance decisions.
- **System and architecture design** — the modular assembly layout, the data-driven (ScriptableObject) content pipeline, the stat-modifier system, the hazard-aware A\* navigation design, and the run / state-machine flow.
- **Direction and integration** — I directed the development throughout: choosing what to build, reviewing and accepting/rejecting output, making the design and technical decisions, integrating the pieces, and testing. The AI assistant helped write code from my specifications, but the architecture, the decisions, and the responsibility for understanding the codebase are mine — I can explain how each system works and modify it.
- **Iteration and play-testing** — running the game, judging the results, and deciding what to change next.

## Assets

- **Third-party (not AI-generated):** the 32×32 character/enemy/weapon sprites and tiles, the audio, and the font are **third-party assets** used under their licences (CC0 / CC-BY-SA 3.0 / SIL OFL 1.1 / CraftPix Free) and credited in `ATTRIBUTIONS.md`.
- **AI-generated:** the game's **key art** — the floor backgrounds, hero portraits, ending illustrations, and the hidden-boss art — was generated with **ChatGPT (OpenAI image generation)**. No third-party licence applies to it; it is declared here for transparency.
- **No AI-generated audio** was used.

## Notes

- AI tool configuration and tooling, and local working/notes folders (`.claude/`, `Packages/Coplay/`, `Docs/`, `Temp/`) are excluded from version control via `.gitignore` and are not part of the submission.
