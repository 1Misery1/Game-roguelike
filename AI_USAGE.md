# AI Usage Statement

This project was developed with the help of an AI coding assistant (Anthropic's Claude, used through the Claude Code CLI). In the interest of academic honesty, this document sets out how AI was and was not used.

## What AI was used for

- **Implementation and boilerplate** — turning my designs and instructions into C# (components, systems, scene-building code).
- **Refactoring** — most notably migrating the entire user interface from Unity's immediate-mode `OnGUI` (IMGUI) to retained-mode **uGUI** (Canvas), and tidying the code afterwards.
- **Debugging** — diagnosing and fixing runtime issues I found while testing.
- **Editor automation** — an AI-driven Unity Editor plugin (Coplay, via the Model Context Protocol) was used during development to run builds, inspect and adjust scene objects, and run play-tests.
- **Drafting comments and documentation.**

## What is my own work

- **Game design** — the concept, the three-realms premise and narrative, the camp / ember-possession mechanic, the branching multi-ending structure, and the difficulty, content, and balance decisions.
- **System and architecture design** — the modular assembly layout, the data-driven (ScriptableObject) content pipeline, the stat-modifier system, the hazard-aware A\* navigation design, and the run / state-machine flow.
- **Direction and integration** — I directed the development throughout: choosing what to build, reviewing and accepting/rejecting output, making the design and technical decisions, integrating the pieces, and testing. AI produced a large share of the line-by-line code, but the architecture, the decisions, and the responsibility for understanding the codebase are mine — I can explain how each system works and modify it.
- **Iteration and play-testing** — running the game, judging the results, and deciding what to change next.

## Assets (not AI-generated)

The art (pixel sprites and tiles), audio, and font are **third-party assets** used under their licenses (CC0 / CC-BY-SA 3.0 / SIL OFL 1.1) and credited in `Assets/StreamingAssets/ATTRIBUTIONS.md`. **No AI image, audio, or asset generation was used for any shipped asset.**

## Notes

- AI tool configuration and tooling, and local working/notes folders (`.claude/`, `Packages/Coplay/`, `Docs/`, `Temp/`) are excluded from version control via `.gitignore` and are not part of the submission.
