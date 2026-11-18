# THE LAST SIGNAL — Project Report

Offline, single-player, first-person psychological horror game for Android (Unity 6.6, URP).
**Five playable levels** (the brief asked for three; the project owner changed this on 18 Nov 2026 —
see `Docs/Upgrade_Plan.md`), followed by an ending and credits.

---

## 1. How to play

| | Phone (touch) | Editor / PC |
|---|---|---|
| Move | Left-side floating joystick | W A S D / arrow keys |
| Look | Drag on the right side of the screen | Mouse (click the Game view first) |
| Interact | **USE** | E |
| Run | **RUN** (toggle; stops when you stop moving) | Hold Left Shift |
| Crouch | **HIDE / CROUCH** | C or Left Ctrl |
| Hold breath (only while hidden) | **HOLD BREATH** (hold) | Hold Space |
| Flashlight | **LIGHT** | F |
| Inventory + journal | **BAG** | I or Tab |
| Keypads / safes | On-screen keypad | Number keys, Enter, Backspace, Esc |
| Pause | **II** | Esc |

Start from `Assets/Scenes/MainMenu.unity` → **New Game**. Scene order:
MainMenu → Level01_Station → Level02_Basement → Level03_Quarters → Level04_BurnedWing → Level05_Tower → Ending.

## 2. Story and levels

Alex returns to the radio station where his sister Maya disappeared during a storm and fire ten years ago.
Something in the static — The Echo — copies voices. Each level adds evidence; the core story is unchanged:
Alex learns Maya saved him during the fire, broadcasts her final recording, shuts the rig down and escapes.

| Level | Difficulty | Route | Main puzzle (deduction) | Threat |
|---|---|---|---|---|
| 1 – The Abandoned Station | Easy | Rainy exterior → foyer → reception + office → service corridor → control room → basement | **Breakers**: six unlabelled breakers. The fuse-box sticker says *anything feeding the tower stays down*; the circuit diagram in the manager's office says which breaker feeds what (B1 up, B2 up, B3 down, B4 up, B5 down, B6 down). **Keypad**: Dad's note says the code is the time the last person signed out on the storm night → visitor log: Alex, 02:47 → **0247** | None; footsteps overhead, falling binder, bursting bulb, The Echo's shadow |
| 2 – The Underground Secrets | Medium | Corridors, storage, flooded section, pump room → generator chamber → elevator | **Generator**: find 3 parts; the start-up card is torn — second half in the pump room. Fuel valve → prime until the pressure needle is in the **green** → choke → starter. Over-priming floods it; a stall resets and is loud | The Echo patrols, listens, checks lockers; Maya glimpsed; a pipe crashes into the water and draws it |
| 3 – The Living Quarters (new) | Medium-hard | Lift lobby → long hallway; kids' room, bathroom, parents' room, kitchen, Dad's study, storage → fire door | **Dad's safe**: logbook — *the night everything ended, month first*; kitchen calendar — 14 October circled → **1014** → fire door key | The Echo active in the hallway; wardrobes/pantry; opening the safe bursts the lamps, slams the door and brings it |
| 4 – The Burned Wing (new) | Hard | Collapsed corridor → detour through the burned west office → Studio A (Maya's scorched tape) → junction room → tower stairs | **Junction J-4**: schematic (which feeds reach which lines) + fire marshal's report (what burned) → only **FEED B → LINE 3 → LIFT** works. Then The Echo hunts you to the stairs | Faster, more alert Echo; falling beam; heavy smoke |
| 5 – The Last Transmission | Hardest | Tower corridor → equipment + antenna rooms → broadcast chamber → escape | **Frequency**: refit 3 modules, tune to Dad's channel: "one-oh-three point-" (Level 2 tape, rest lost in static) + "always ended on seven" (Maya's diary, Level 3) → **103.7**, transmit; final recording (Maya appears beside the transmitter); shutdown; escape chase | Most alert Echo; hunt after shutdown |

Ending: sunrise epilogue at Maya's memorial — *"I heard you this time, Maya. Goodbye."* — then THE END and credits. No sequel hook.

**Journal.** Key facts from notes and recordings are written to the journal (shown in the inventory), so
clues found in earlier levels (the 02:47 sign-out, "one-oh-three", ".7") are never lost.

## 3. Architecture

Small components under `Assets/Scripts/`, one runtime assembly (`LastSignal.Runtime`), namespaces per folder.

| Area | Main scripts |
|---|---|
| Player | `PlayerInputRouter` (touch + keyboard/mouse), `PlayerController`, `PlayerLook`, `FlashlightController`, `PlayerBreath` (hold breath, gasp, scared breathing), `PlayerBody` (visible body, torch arm, two-bone IK hand reach, head-anchored), `CameraMotion` (head bob, breathing sway), `HandheldSway` |
| Interaction | `IInteractable`, `PlayerInteractor` (camera sphere cast + touch point), `PickupItem`, `Door` (open, slam), `NoteInteractable`, `ItemSlot`, `TapePlayer`, `ActionButton`, `HidingSpot`, `BatteryPickup` |
| Puzzles | `BreakerPuzzle` + `BreakerSwitch`, `GeneratorPuzzle` (primer + pressure gauge), `CodeLock` (keypads/safes), `JunctionPuzzle` + `SelectorSwitch`, `FrequencyPuzzle` |
| Enemy | `EchoAIController`, `EchoPresence` (posture, twitches, eyes, smoke, lamp flicker), `EchoHuntTrigger`, `NoiseSystem`, `NoiseEmitter`, `SurfaceNoiseZone` |
| Environment | `PowerGroup`, `FlickerLight`, `ShadowSighting` (apparitions/glimpses), `ScareEvent` (scripted scares) |
| Game flow | `GameFlowManager` (persistent: scenes, fades, checkpoints, retry, world flags, legacy-save mapping), `LevelController`, `ObjectiveManager`, `Checkpoint`, `TriggerZone`, `LevelExit`, `StoryEvents` (incl. journal clues), `FlagActivator`, `ElevatorSequence`, `FinaleSequence`, `EndingSequence` |
| Save | `GameState`, `SaveManager` (local JSON), `GameSettings` (PlayerPrefs) |
| Audio | `AudioManager` (ambience + threat loop), `SoundBank` (recordings with variations), `SubtitleManager`, `PlaceholderClips` (fallbacks + generated breathing/gasp) |
| UI | `HUDController`, `StoryHUD` (subtitles, notes, inventory + journal, breath meter), `CodePanelUI`, `MobileInputUI`, `VirtualJoystick`, `TouchLookArea`, `MobileButton`, `SafeAreaFitter`, `SettingsPanelUI`, `MainMenuController` |

**Checkpoints and saving.** Progress is stored as world flags (`door:`, `taken:`, `puzzle:`, `code:`, `played:`,
`clue:`, `scare:` …) plus inventory, objective and battery. Objects restore themselves from flags on load,
silently, so a reload never repeats story events or scares. Saves are local only. A save made before the
five-level update (scene `Level03_Tower`) continues in `Level05_Tower`.

**Scene building.** Every game scene is generated by editor code (`Assets/Scripts/Editor/`):
**The Last Signal → Build → Build All Game Scenes** rebuilds the Player/HUD/Echo/apparition prefabs, the
main menu, all five levels (with NavMesh bakes and post-processing profiles), the ending, and the build
scene list. Level layout changes are made in the builder scripts (`Level1Builder` … `Level5Builder`), then
the scenes are rebuilt.

## 4. The Echo (enemy AI)

A NavMesh agent with a state machine, a pacing "director" and an animated body.

| State | Behaviour | Leaves when |
|---|---|---|
| Patrol | Walks waypoints in an unpredictable order, pausing at each | Sees the player → Chase; hears noise → Investigate |
| Investigate | Walks to the noise (remembers the last few noise spots) | Sees the player → Chase; arrives → Listen |
| Listen | Stands still, slowly turning, listening | Hears more → Investigate; timer → Search |
| Chase | Follows the player at an erratic pace (surges and hesitations) | Loses sight, player hides, or time runs out → Search; reaches the player → caught |
| Search | Wanders the area, re-checks remembered noises; may decide to inspect a nearby hiding spot | Timer → Return |
| Inspect | Stands at the player's hiding spot. If the player is not holding their breath after a moment, or gasps when their breath runs out, it finds them | Inspection ends → Search |
| Return | Walks back to the nearest waypoint | Arrives → Patrol |
| Hunt (scripted escapes) | Knows roughly where the player is; prowls instead of camping a hiding spot | Player reaches the exit |

**Director:** if The Echo has been far from the player for a long time it gets a *vague* hint of their area
(never the exact spot); after a long time close by without a chase it backs off to a distant waypoint, so
tension rises and falls. **Lure:** every so often it calls out in Maya's voice from where it stands.

Fairness rules: sight needs a clear line and is reduced when crouching (longer with the flashlight on); walls
muffle noise; it never sees a hidden player and never catches one except through an inspection the player
was warned about ("It's checking your hiding place. Hold your breath!"); heartbeat, breathing, red screen-edge
pulse and stuttering lamps warn before it arrives; after a checkpoint reload it restarts at least 15 m away
with a 4-second grace period.

| Tuning | L2 | L3 | L4 | L5 |
|---|---|---|---|---|
| Sight range / angle | 10 m / 100° | 12 m / 110° | 13 m / 120° | 13 m / 120° |
| Hearing multiplier | 1.0 | 1.15 | 1.35 | 1.3 |
| Patrol / chase speed (player walks 2.2, sprints 4.0) | 1.2 / 3.2 | 1.35 / 3.35 | 1.45 / 3.5 | 1.5 / 3.5 m/s |
| Chance to inspect a nearby hiding spot | 35 % | 50 % | 65 % | 70 % |
| Director hint after / lure every | 55 s / 75 s | 45 s / 55 s | 30 s / 40 s | 35 s / 45 s |

Player noise per step: sprint 10 m, walk 4 m, crouch 1.2 m (×2 in water). Puzzle mistakes (keypad buzz,
generator stall, junction arc) and some scares also make noise.

**Body:** the Quaternius CC0 mannequin, stretched tall and thin, black, hunched, with long arms, a cocked head,
random head twitches, glowing eyes (red when hunting) and smoke; animations blend idle / stiff walk / jog /
sprint by speed, plus crouched listening, pressing against a hiding place, and a lunge.

## 5. Visuals, first-person presence and fear

- Poly Haven CC0 PBR textures (1K colour + normal) on world-scaled UVs; dark ambient and fog; post-processing
  (colour grading + vignette) on both presets. The flashlight is essential in most rooms.
- Flashlight: spot light with **real-time hard shadows on Low and Medium** (owner's decision); battery drain
  from Level 2.
- Alex's visible body: legs walk/crouch, the left hand holds the torch (lens glows when on), the right hand reaches
  the exact point being used (switches, items, doors; both hands push doors when the torch is off). The torch
  beam does not light the body (URP rendering layers); a dim body-only fill light does.
- Camera: footstep bob and sway, breathing rise that quickens with danger, stillness while holding breath; the
  light lags slightly behind fast turns.
- Scripted fear: footsteps on the floor above, falling objects, slamming doors, bursting lamps, whispers and radio
  voices, glimpses of Maya's ghost that vanish when looked at, The Echo's shadow, Maya beside the transmitter while
  her final recording plays.

## 6. Mobile and performance decisions

- URP **Low** and **Medium** presets (Android defaults to Medium). HDR off. Additional-light shadow atlas 512 (Low) / 1024 (Medium); soft shadows off.
- Compact indoor levels with doors and bends; few practical lights; most lights without shadows.
- Two skinned characters at most (Alex's body ~8.4k vertices, The Echo ~8.4k). Smoke and ember particle systems are capped at 30–50 particles; the Level 1 outdoor rain uses up to 1,200 tiny stretched particles.
- Compressed audio (Vorbis; long ambiences streamed), 1K compressed textures.
- 30 FPS target (`AppBootstrap`), screen kept awake, landscape only, safe-area aware HUD.
- No per-frame allocations in gameplay code (cached strings, non-alloc physics queries).
- **Not yet done:** profiling on a real phone; baked lighting; removing unused template packages (kept at the owner's request).

## 7. Testing

### Automated (Unity Test Framework, Play Mode) — 19 tests, all passing

| Test | What it proves |
|---|---|
| Prefab_ReferencesAreWired | Player prefab components and references are connected |
| Move_Forward_MovesPlayer, Sprint_IsFasterThanWalk | Movement and sprint |
| Crouch_LowersHeight_AndHeadroomBlocksStanding | Crouch; standing up is blocked under low ceilings |
| Flashlight_RequiresPickup_ThenToggles | Flashlight only works after pickup |
| Interactor_TargetsPickup_WhileInsideStoryTrigger | Story trigger volumes never block interaction |
| Switch_TogglesLight_ThroughInteractor | Interactor targeting and use |
| Body_HandReachesForWhatIsUsed | The right hand physically reaches what the player uses, then returns |
| Joystick_DragToEdge_GivesFullForwardInput | Virtual joystick output |
| Pause_FreezesTime_AndBlocksInput | Pause |
| Level1_CriticalPath_ReachesLevel2 | Level 1 incl. wrong breaker patterns, power but no keypad code, wrong code, code 0247 |
| Level2_CriticalPath_ReachesLevel3 | Level 2 incl. priming with fuel off, choke without pressure, flooding, wrong order |
| Level3_Quarters_CriticalPath_ReachesLevel4 | Level 3 incl. locked fire door, logbook → journal + objective, day-first code rejected, 1014 |
| Level4_BurnedWing_CriticalPath_ReachesLevel5 | Level 4 incl. dead routes (A, C), selector wrap-around, B-3-LIFT, stair door |
| Level5_Tower_CriticalPath_ReachesEnding | Level 5 incl. wrong frequency, final recording, shutdown, escape; save cleared |
| Echo_RestartsFarAway_AfterCheckpointRetry | Fair restart distance and grace period |
| Echo_Chases_LosesHiddenPlayer_Investigates_AndCatches | Sight → chase; hiding → search; noise → investigate; catch → game over → retry |
| Echo_InspectsHidingSpot_HoldingBreathKeepsPlayerSafe | Inspection + holding breath = safe |
| Echo_InspectsHidingSpot_FindsBreathingPlayer | Inspection without holding breath = found |

The walkthrough tests call interactions directly; they prove logic and scene flow, not that every object is
easy to reach by walking and aiming. That needs a human play-through.

### Device testing (fill in after testing on a phone)

| Device model | Android version | Preset | Observed FPS | Issues |
|---|---|---|---|---|
| | | Low | | |
| | | Medium | | |

## 8. Assets and packages

| Item | Source / licence |
|---|---|
| Level geometry, UI sprites, materials | Created in this project (generated by editor scripts) |
| Surface textures (concrete, plaster, planks, rusty metal, tiles, carpet, burned ground …) | **Poly Haven** (polyhaven.com) — **CC0** (`Assets/Art/Textures/PolyHaven/CREDITS.txt`) |
| The Echo, Alex's body, Maya's ghost (mesh + animations) | **Quaternius — Universal Animation Library (Standard)** — **CC0** (`Assets/Art/Models/Echo/Quaternius_UAL_License.txt`) |
| Footsteps, doors, impacts, switches, latches, generator, elevator, glitches | Kenney (kenney.nl) — **CC0** |
| Rain & thunder ambience | "Rain and Thunder Loop" by DoKashiteru, OpenGameArt.org — **CC BY 3.0** (credited in game) |
| Basement ambience | "Loopable Dungeon Ambience", OpenGameArt.org — **CC0** |
| The Echo snarls / attack | "Monster Snarls", OpenGameArt.org — **CC0** |
| The Echo ghostly voice, wind | Joseph Sardin (BigSoundBank / LaSonotheque) via Wikimedia Commons — **CC0** |
| Heartbeat | "Heartbeat.ogg" by HerbertBoland, Wikimedia Commons — **CC BY 3.0** (credited in game) |
| Electrical hum | "MainsBrum50Hz.ogg", Wikimedia Commons — **Public domain** |
| Breathing, gasp, fallback sounds | Generated in code (`PlaceholderClips`) |
| Font: Liberation Sans SDF | TextMeshPro default font (SIL Open Font License) |
| Unity 6.6, URP, Input System, uGUI/TextMeshPro, AI Navigation, Test Framework | Unity packages (Unity Companion License / Unity terms) |
| Unity MCP for Unity (CoplayDev, v10.3.0) | Editor-only development tool (MIT) — not part of the game build |
| Template content in `Assets/SourceFiles`, `Assets/Tutorials`, `GetStarted_Scene` | Unity "Get Started" template; not used by the game |

## 9. Known limitations

- Characters use one stylised CC0 mannequin (Alex, The Echo, Maya's ghost) rather than bespoke models.
- No voice acting; all story speech is subtitled. Breathing is a generated sound.
- Not yet played end-to-end by a person after the five-level update; not tested on a physical Android device; FPS unmeasured.
- Lighting is real-time only; baked lighting is a future optimisation.
- The brief specified three levels; the five-level structure is the owner's decision and should be confirmed with the assessor.
