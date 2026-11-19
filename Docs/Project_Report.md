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
| Inspect | Stands at the player's hiding spot. If the player is not holding their breath after a moment, gasps when their breath runs out, or has the torch on (light through the gaps), it finds them | Inspection ends → Search (or back to Hunt) |
| Return | Walks back to the nearest waypoint | Arrives → Patrol |
| Hunt (scripted escapes) | Knows roughly where the player is; prowls instead of camping a hiding spot | Player reaches the exit |

**Director:** if The Echo has been far from the player for a long time it gets a *vague* hint of their area
(never the exact spot); after a long time close by without a chase it backs off to a distant waypoint, so
tension rises and falls. **Lure:** every so often it calls out in Maya's voice from where it stands.

**Torch light from a hiding place:** a torch left on inside a locker or wardrobe shines out of the front (a faint glow
outside the vents; a hint appears on hiding). If The Echo is in front of it, facing it, within 8 m with a clear view, it
comes straight over to inspect, from any state including a hunt, and finds the player if the torch is still on when it
gets there, even if they hold their breath. Turning the torch off in time (and holding breath) keeps them safe.

Fairness rules: sight needs a clear line and is reduced when crouching (longer with the flashlight on); walls
muffle noise; it never sees a hidden player and never catches one except through an inspection the player
was warned about ("Something is coming to check your hiding place...", then "It's right outside. Hold your breath!"
when it arrives, so a held breath lasts the inspection); heartbeat, breathing, red screen-edge
pulse and stuttering lamps warn before it arrives; after a checkpoint reload it restarts at least 15 m away
with a 4-second grace period.

| Tuning | L2 | L3 | L4 | L5 |
|---|---|---|---|---|
| Sight range / angle | 10 m / 100° | 12 m / 110° | 13 m / 120° | 13 m / 120° |
| Hearing multiplier | 1.0 | 1.15 | 1.35 | 1.3 |
| Inspection at a hiding spot (breath lasts 10 s) | 3 s | 3 s | 3.5 s | 3.5 s |
| Patrol / chase speed (player walks 2.2, sprints 4.0) | 1.2 / 3.2 | 1.35 / 3.35 | 1.45 / 3.5 | 1.5 / 3.5 m/s |
| Chance to inspect a nearby hiding spot | 35 % | 50 % | 65 % | 70 % |
| Director hint after / lure every | 55 s / 75 s | 45 s / 55 s | 30 s / 40 s | 35 s / 45 s |

Player noise per step: sprint 10 m, walk 4 m, crouch 1.2 m (×2 in water). Puzzle mistakes (keypad buzz,
generator stall, junction arc) and some scares also make noise.

**Voice:** wordless whispers when it starts a chase or vanishes, and a strangled, choking gasp when it catches you
(BigSoundBank). Sounds still ringing from a catch are cut off when you respawn.

**Body:** a Mixamo character (`Echo.fbx`) stretched tall (×1.15 high, ×0.95 wide), all black, hunched, with long
arms and long fingers, a cocked head, random head twitches, glowing eyes (red when hunting) and smoke; Mixamo zombie
animations blend idle / walk / run by speed, with the zombie idle for listening, the attack for inspecting a hiding
place and the scream for the catch. (A wall-and-ceiling crawl was tried and removed at the owner's request.)

## 5. Visuals, first-person presence and fear

- Poly Haven CC0 PBR textures (1K colour + normal) on world-scaled UVs; dark ambient and fog; post-processing
  (colour grading + vignette) on both presets. The flashlight is essential in most rooms.
- Flashlight: spot light with **real-time hard shadows on Low and Medium** (owner's decision); battery drain
  from Level 2.
- Alex's visible body: the Mixamo "Remy" character with walk, run, crouch and "running with torch" animations.
  When the torch is on he holds it up in his left hand (IK to a grip in front of the view); the right hand reaches
  the exact point being used (switches, items, doors), leaning the chest in when it is slightly out of reach; both
  hands push doors when the torch is off. The torch beam does not light the body (URP rendering layers); room lights
  and a dim body-only fill light do. The body uses its own matte URP materials built from Remy's textures (the imported
  ones used a specular setup with colour-imported normal maps, which left the body black or flat blue-grey). Humanoid avatars are built from the scaled skeletons (`MixamoSetup.BuiltAvatar`)
  because the importer's own avatars for these files kept a centimetre scale.
- Flashlight beam: 68° cone (30° bright centre), 26 m range, brighter than before.
- Camera: footstep bob and sway, breathing rise that quickens with danger, stillness while holding breath; the
  light lags slightly behind fast turns.
- Abandoned, lived-in rooms: real CC0 props (boxes, crates, barrels, tipped chairs, shelves, a wheelchair, dead rats,
  charred furniture in the burned wing) plus generated grime merged into a few meshes per level: water streaks,
  grime along wall bottoms, floor stains, puddles, mould, cracks, cobwebs in corners, soot, claw scratches, scattered
  papers and debris.
- Notes are handwritten (Caveat font) on six kinds of paper, picked per note: aged page, lined notebook page with
  punched holes (blue biro), graph paper (pencil), office memo with a printed header (typed), crumpled brown scrap
  with a torn edge (red-brown ink), water-damaged page with a burnt corner.
- A note lying on a desk or the floor is picked up (only within arm's reach, 1.4 m, like every hand-held pickup):
  Alex's right hand reaches the sheet where it lies, then brings it up in front of his face, holding its lower edge
  from behind so the writing stays clear, lit by a dim reading light (the game is paused; the hand IK runs on
  unscaled time). "PUT BACK" carries it back exactly where it was. Paper rustles when picked up and put down.
- Hands really hold things: each finger joint's bend axis is measured from the rig, so the left hand closes into a
  fist round the torch handle (knuckles along it) and the right hand grips a sheet; the hand is turned and placed
  so the fist or fingers sit on the object, not beside it.
  Notes pinned to walls open the full-screen reader on the matching paper.
- Room and area names hang on burnt-edged wooden boards on wires (a little crooked; slid down clear of pipes and
  wired to the real ceiling after the level is built); small labels on fuse boxes, controls and machines sit on
  worn brass tags.
- Keypads look like old mechanical door keypads: olive steel plate with screws, a counter window, black keys with
  a cross (clear) and a tick (enter), and green/red indicator lamps (green holds a moment before it opens, red
  flashes on a wrong code).
- Cobwebs stretched across the upper corners of almost every room (15–29 per level), double-sided, with loose
  strands hanging beside them. Each web takes its own pattern from 16 (classic orb, torn off-centre orb, tangle,
  sheet, broken orb with a hole, old slack web, tent web, small orb on guy lines), turned and sized at random;
  all in one atlas, so still one draw call per level.
- Life in the building (`AmbientLife`): every 18–40 s, where Alex is looking and never during a chase, a mouse darts
  across the floor or along a table or desk top squeaking (a big rat, ~30 cm), a rat gnaws inside the wall beside him, or a spider lowers itself on a thread in front
  of his face and climbs back up. 8–10 spiders sit on the walls and ceilings of each level and scuttle now and then.
  None of it makes noise The Echo hears. (Birds were tried and removed at the owner's request.)
- Bad wiring (`RoomLightFlicker`): every hall and room light has its own random timer (every 14–40 s) and stutters for
  a few seconds when its turn comes and Alex is within 22 m (dips, brief blackouts, starter clicks); at most two at
  once, never one The Echo is already disturbing, never the red emergency lamps.
- Doors take 1.5 s to swing open and their creak is cut to the same 1.5 s. Doors are used within 1.6 m; notes, items and
  batteries within 1.4 m; machines, fuse boxes, switches and keypads keep the full 2.2 m.
- Level 1 outside: an old street light (pole on a base, arm over the path, lamp head with a glowing bowl; the light
  hangs under the bowl). Steady rain with distant thunder every 40–85 s (the old rain loop rumbled every few seconds).
- Tapes: the PLAY key clunks down, the tape hisses and whirs under the voices, and the key pops up at the end.
- Signs: board text keeps its line breaks and is shrunk until every line fits; the board is then sized to the text
  (a test checks every board and tag in all five levels).
- Water: the Level 2 flood and every puddle in every level are water zones; steps there splash (real wading
  recordings) and carry a little further.
- Level 1 ends with a real flight of nine concrete steps (about 1.5 m) down into the dark behind the basement door,
  with pipe handrails; Level 2 starts at the bottom.
- Distant flickering lights: three dying fluorescent tubes per level, far down the longest corridors and rooms.
  They sputter on with the click of a failing starter, hum while lit, die, and go dark when Alex comes within 5 m.
- Real recorded sounds (BigSoundBank, CC0) for footsteps on concrete and in water, creaking doors, lockers and
  wardrobes, creaking boards for the "someone upstairs" scare, Alex's frightened breathing and gasp, paper picked up
  and put down, mice squeaking, a rat gnawing inside a wall, fluorescent tube clicks and hum, and birds waking at
  sunrise in the ending. The editor tool
  `SoundCuts` (menu *The Last Signal/Audio/Cut BigSoundBank Sounds*) cuts them from the original takes: single steps
  and squeaks found at their peaks, the loudest stretch of long takes, seamless loops for breathing and the hum.
- Being caught: no punch — The Echo is suddenly in your face, towering; your view is wrenched up to it, its long hands
  close around your head, it shrieks and the picture tears into static and goes black.
- Scripted fear: footsteps on the floor above, falling objects, slamming doors, bursting lamps, whispers and radio
  voices, glimpses of Maya's ghost that vanish when looked at, The Echo's shadow, Maya beside the transmitter while
  her final recording plays.

## 6. Mobile and performance decisions

- URP **Low** and **Medium** presets (Android defaults to Medium). HDR off. Additional-light shadow atlas 512 (Low) / 1024 (Medium); soft shadows off.
- Compact indoor levels with doors and bends; few practical lights; most lights without shadows.
- Two skinned characters at most (Mixamo Remy for Alex and the Mixamo Echo character; vertex counts to be checked in the profiler on a phone). Smoke and ember particle systems are capped at 30–50 particles; the Level 1 outdoor rain uses up to 1,200 tiny stretched particles.
- Creatures are a few primitive shapes each (the mouse is the CC0 rat model), pooled (one of each, reused), with no colliders or shadows. Each level gets three extra point lights for the flickering tubes; they are disabled whenever the tube is dead (most of the time).
- Compressed audio (Vorbis; long ambiences streamed), 1K compressed textures.
- 30 FPS target (`AppBootstrap`), screen kept awake, landscape only, safe-area aware HUD.
- No per-frame allocations in gameplay code (cached strings, non-alloc physics queries).
- **Not yet done:** profiling on a real phone; baked lighting; removing unused template packages (kept at the owner's request).

## 7. Testing

### Automated (Unity Test Framework, Play Mode) — 25 tests, all passing

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
| Echo_SeesTorchLightFromLocker_AndFindsPlayerHoldingBreath | A lit torch in a locker is seen from in front; it inspects and finds the player despite the held breath |
| Sounds_BigSoundBankRecordingsArePresent | Every recorded sound the game asks for exists (footsteps, door creaks, water, paper, mice, breathing, tubes, dawn) |
| AmbientLife_MouseRatAndSpider_AppearAndLeave | Mouse, gnawing rat and dropping spider all start in a clear corridor and leave again; a tube goes dark when Alex is under it |
| Note_PickedUpReadInHand_AndPutBack | A desk note lifts to the face with its words on it, the hand holds it while paused, and it goes back exactly where it was |
| Signs_TextFitsOnEveryBoard | In all five levels, every hanging board and brass tag is big enough for all of its letters |
| Water_PuddleMakesStepsSplash | Standing in a puddle switches steps to splashes and makes them carry further |

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
| Alex's body (Remy) and The Echo (character + zombie animations), Alex's walk/run/crouch/torch animations | **Adobe Mixamo** (mixamo.com) — free to use in games under the Adobe/Mixamo terms; the raw files must not be redistributed on their own (keep the repository private) |
| Maya's ghost, fallback bodies (mesh + animations) | **Quaternius — Universal Animation Library (Standard)** — **CC0** (`Assets/Art/Models/Echo/Quaternius_UAL_License.txt`) |
| Props and furniture (38 models): boxes, crates, barrels, chairs, desks, tables, beds, sofa, shelves, cabinets, TV, wheelchair, trash bags, cans, rats, electrical cabinets, compressor, generator, pipes, propane tanks, radio, circuit boards, stove, mirror, ceiling fittings, wet-floor sign | **Poly Haven** models — **CC0** (`Assets/Art/Models/Props/CREDITS.txt`) |
| Fridge, bathtub, sink, safe, lockers and wardrobes (with swinging doors), reel-to-reel tape decks, machine vents/lights | Built in code from primitives and the CC0 textures (`Fixtures`, `BuildKit.Locker`) |
| Spiders, flickering tube fittings, sign boards, keypad, basement stairs | Built in code from primitives (`Wildlife`, `BuildKit`, `GamePrefabs`, `Level1Builder`) |
| Handwriting font for notes: Caveat | Google Fonts, The Caveat Project Authors — **SIL Open Font License 1.1** (`Assets/Art/Fonts/Caveat/OFL.txt`) |
| Aged paper, stains, drips, mould, cracks, cobwebs, soot, scratches, static | Generated in code by editor scripts (`PaperArt`, `Dressing`) |
| Impacts, switches, latches, pickups, sparks, generator, elevator, glitches | Kenney (kenney.nl) — **CC0** |
| Footsteps (concrete and water), creaking doors and boards, breathing, gasp, paper, mouse, rat, fluorescent tubes, birds at dawn, rain, distant thunder, cassette deck, The Echo's whispers and choking gasp | Joseph Sardin, **BigSoundBank.com** — **CC0** (originals in `Assets/Audio/Source/BigSoundBank/`, by sound id; not in the build) |
| Basement ambience | "Loopable Dungeon Ambience", OpenGameArt.org — **CC0** |
| The Echo ghostly voice, wind | Joseph Sardin (BigSoundBank / LaSonotheque) via Wikimedia Commons — **CC0** |
| Heartbeat | "Heartbeat.ogg" by HerbertBoland, Wikimedia Commons — **CC BY 3.0** (credited in game) |
| Electrical hum | "MainsBrum50Hz.ogg", Wikimedia Commons — **Public domain** |
| Spider skittering, fallback sounds when a recording is missing | Generated in code (`PlaceholderClips`) |
| Font: Liberation Sans SDF | TextMeshPro default font (SIL Open Font License) |
| Unity 6.6, URP, Input System, uGUI/TextMeshPro, AI Navigation, Test Framework | Unity packages (Unity Companion License / Unity terms) |
| Unity MCP for Unity (CoplayDev, v10.3.0) | Editor-only development tool (MIT) — not part of the game build |
| Template content in `Assets/SourceFiles`, `Assets/Tutorials`, `GetStarted_Scene` | Unity "Get Started" template; not used by the game |

## 9. Known limitations

- Maya's ghost still uses the stylised CC0 mannequin. The spiders are simple shapes (fine in the dark and in motion,
  but not detailed up close). The hand holding a note is posed by IK on the idle animation, not a hand-made grip.
- No voice acting; all story speech is subtitled. Breathing is a generated sound.
- Not yet played end-to-end by a person after the five-level update; not tested on a physical Android device; FPS unmeasured.
- Lighting is real-time only; baked lighting is a future optimisation.
- The brief specified three levels; the five-level structure is the owner's decision and should be confirmed with the assessor.
