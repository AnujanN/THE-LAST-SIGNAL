# THE LAST SIGNAL — Upgrade Plan (v2)

Requested after the first full play-through: harder, longer, smarter enemy, more fear,
realistic visuals, visible first-person body. Decisions (18 Nov 2026):

- **5 playable levels** (owner's decision; the original brief said exactly three — confirm with the lecturer).
- Free **CC0** art may be downloaded (Poly Haven textures/props, Quaternius humanoid + animations, OpenGameArt arms).
- Flashlight **real-time shadows on both** Low and Medium presets.

The core story does not change: Alex returns to the station, meets The Echo, learns Maya saved him during
the fire, broadcasts her final recording, escapes, sunrise memorial ending. New levels add evidence and
dread on the way to that truth — they never contradict it.

## New level order

| # | Level | Difficulty | New story content | Main puzzle (deduction, not fetch) |
|---|---|---|---|---|
| 1 | The Abandoned Station | Easy | (existing) visitor log, Maya's note | Breaker panel deduced from a circuit memo; control-room keypad code from the visitor log |
| 2 | The Underground Secrets | Medium | (existing) fire recording | Generator: parts + start-up order reconstructed from a torn card and gauge labels |
| 3 | **The Living Quarters** (new) | Medium-hard | The family's staff rooms: Maya's diary, Dad's logbook — The Echo first appeared in the static *before* the fire and learned voices | Safe combination assembled from three clues (photo date, calendar mark, diary) |
| 4 | **The Burned Wing** (new) | Hard | The fire-ravaged broadcasting corridor where Maya was trapped; scorched recorder holds a fragment of her final message | Re-route power through a junction box using a schematic (wire colours + labels, not colour-only) |
| 5 | The Last Transmission | Hardest | (existing) final recording, shutdown, escape | Frequency split across clues (one-oh-three from Level 2, ".7" from the diary) + antenna bearing |

## The Echo — smarter and scarier

- **Body:** animated humanoid (Quaternius CC0), tall and thin, black with smoke particles and pale eyes; creeps when
  searching, sprints when chasing, stops to listen.
- **Director ("menace"):** keeps The Echo in the player's general area without cheating — if it is far for too long it
  gets a vague hint of the player's zone; if it has been close for too long it backs off, so tension rises and falls.
- **Senses:** sight blocked by walls; flashlight beam makes the player visible from further; running/water/doors make noise;
  it remembers the last places it heard noise and checks them.
- **Behaviours:** stops and listens, checks nearby hiding spots during a search (player must **hold breath** while it is
  close), calls Alex in Maya's voice from a direction to lure him, flickers lights as it approaches, slams doors.
- Difficulty rises per level (sight, hearing, speed, how often it checks hiding spots) but stays fair: warnings before
  it arrives, safe restart distance and grace period after checkpoints.

## Fear

Light flicker near The Echo, whispers and radio voices, footsteps above, doors slamming, objects falling, short
scripted apparitions of Maya's silhouette, heartbeat and breathing that respond to danger, darker scenes where the
flashlight matters (battery pressure from Level 2).

## Visuals and first-person presence

- Photo-scanned PBR textures with world-scaled UVs (no more stretched grey boxes), real props where available.
- Much darker scenes: near-black ambient, fog, few practical lights, flashlight with shadows and a lens cookie.
- Light post-processing on both presets: tonemapping, cold colour grading, vignette (no motion blur/film grain by default).
- First-person arms holding the flashlight; hand reaches for pickups, pushes doors, places items; head bob synced to
  footsteps; breathing.

## Phases (each ends compiled, tested and committed)

1. Darkness, lighting, post-processing, PBR textures and world-scaled UVs on all levels.
2. The Echo: body, animations, smoke, smarter AI and director, hold-breath hiding.
3. First-person arms, interaction animations, head bob.
4. Deduction puzzles and keypad/combination systems in Levels 1–2.
5. New Levels 3 and 4, story documents and recordings; Tower becomes Level 5; scene flow.
6. Fear events and apparitions across all levels.
7. Tests for all five levels, report update, Android build.
