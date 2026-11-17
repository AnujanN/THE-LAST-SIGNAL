# THE LAST SIGNAL - Claude Code Project Instructions

Read `The_Last_Signal_Game_Development_Brief.pdf` before making project changes. It is the primary source of truth for the story, three-level structure, gameplay, Android requirements, acceptance criteria and implementation order.

## Project goal
Build an offline, single-player, 3D first-person psychological horror game in Unity, Android-first, with exactly three playable levels:
1. The Abandoned Station (easy)
2. The Underground Secrets (medium)
3. The Last Transmission (hard)

The story follows Alex as he investigates his sister Maya's disappearance, encounters The Echo, discovers that Maya saved him during the station fire, broadcasts her final recording, and escapes. End with a sunrise epilogue, memorial scene, `THE END` and credits. Do not add a cliffhanger or additional playable level.

## Core gameplay
- First-person movement, camera look, sprint and crouch.
- Android touch controls: left movement joystick, right look drag, interact, sprint, crouch/hide, flashlight, inventory/pause.
- Keyboard/mouse fallback for Editor testing.
- Interaction prompts, pickups, simple inventory and objectives.
- Flashlight with battery pressure in Levels 2 and 3.
- Environmental puzzles: Level 1 breaker/fuse; Level 2 generator repair; Level 3 transmitter frequency puzzle.
- One enemy, The Echo, with patrol, investigate, chase, search and return states.
- Hiding spots, noise detection, audio cues, subtitles, checkpoints and scene transitions.
- No combat, online services, accounts or multiplayer in the core scope.

## Android-first constraints
- Use the installed compatible Unity 6 version and URP. Inspect versions and package compatibility before changing project settings.
- Aim for stable 30 FPS on the agreed baseline Android phone; do not claim this is achieved until profiled on a real device.
- Prefer baked lighting, few real-time lights, minimal/no real-time shadows on Low, simple shaders, compressed textures/audio, compact indoor scenes and controlled scene loading.
- Keep post-processing minimal. Provide Low and Medium graphics presets.
- Support landscape orientation, common aspect ratios and safe areas.
- Profile before optimizing; do not guess the bottleneck.

## Architecture guidance
Use small, reusable C# components. Suggested systems include:
- PlayerController, PlayerLook, MobileInputUI
- PlayerInteractor and IInteractable
- InventoryManager, ObjectiveManager
- FlashlightController, HidingSpot
- EchoAIController and NoiseEmitter
- BreakerPuzzle, GeneratorPuzzle, FrequencyPuzzle
- GameFlowManager, AudioManager, SaveManager

Suggested folders under `Assets/`: `Scenes/`, `Scripts/` (by system), `Prefabs/`, `Art/`, `Audio/`, `UI/`, `Settings/`, `Tests/`.

## Claude Code + Unity MCP rules
1. Inspect the project, Unity version, packages, scenes, Console and available MCP/CLI tools before editing.
2. Discover actual tool names and schemas. Never invent MCP tools or assume a capability exists.
3. Do not delete, overwrite or rename existing assets without checking references and explaining the change.
4. Use proper `.cs` source files for logic; use Unity MCP for supported Editor/scene operations.
5. Work in small milestones. Let Unity compile after changes and inspect Console errors.
6. Inspect scene hierarchy and serialized references after scene edits.
7. Test each feature in Play Mode when possible. State clearly what was and was not tested.
8. Never claim Android real-device performance was validated unless an Android build was run on a physical device.
9. Use version control and create a known-good checkpoint after each working milestone.
10. Ask before changing core story facts, adding another playable level, or introducing major systems outside the brief.

## Development order
- M0: inspect project, configure URP/Android, Git and MCP connection.
- M1: greybox vertical slice with player, mobile input, one pickup, one interaction and flashlight.
- M2: reusable interaction, inventory, objectives, puzzle and UI systems.
- M3: complete Level 1.
- M4: The Echo AI, noise and hiding prototype.
- M5: complete Level 2.
- M6: complete Level 3, final recording, escape and ending.
- M7: Android profiling, device testing, bug fixes and final build.

## First task
Do not build the whole game immediately. Read the PDF and inspect the current project and installed Unity MCP/CLI integration. Report the current state, connection status, version/package risks, blockers and a milestone plan. Do not modify or delete project content during this inspection phase.
