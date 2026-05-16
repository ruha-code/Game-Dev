# AeroOS: Revised Design Document and Unity Implementation Guide

## 1. Project Overview

**Title:** AeroOS  
**Genre:** Psychological Horror / Puzzle / Adventure  
**Platform:** PC  
**Engine:** Unity  
**Presentation Style:** A nostalgic "Frutiger Aero" desktop UI masking a hostile digital prison

**High Concept:**  
The player investigates a beautiful operating system that was built to soothe and contain the digitized minds of missing engineers. The desktop acts as the main hub. Each software program contains a mini-game. Each cleared mini-game reveals evidence, unlocks environmental changes, and grants access to hidden 2D locations embedded in the wallpaper.

**Primary Fantasy:**  
The player is not navigating a normal computer. They are exploring a living interface that lies to them politely.

---

## 2. Design Pillars

1. **Beauty hides threat.**  
   The interface should feel attractive, glossy, soft, and almost too clean. Horror emerges through contradiction, not gore.

2. **The desktop is the world.**  
   The hub should feel alive. Wallpaper objects, icons, notifications, audio, and visual glitches all reinforce the illusion that the OS is watching.

3. **Small puzzles, strong atmosphere.**  
   Mini-games should be readable, quick to complete, and different from each other. Their real purpose is pacing, mood, and lore delivery.

4. **The system must feel alive at all times.**  
   Even outside major scripted moments, AeroOS should express intent through small anomalies, timing disruptions, misleading UI behavior, and reactive audiovisual events.

5. **Progression must stay clear.**  
   The player should always understand what they unlocked, what changed, and what the next interesting click is.

6. **MVP first, expansion second.**  
   The project should be built as a vertical slice before the full game. Every system should support phased development.

---

## 3. Story Summary

### Premise
You are the last active employee of `Aether Dynamics`, a research company behind a secret initiative called `Lab 7`. Seven engineers disappeared during the project. You access their primary terminal to investigate what happened.

### Discovery
The system boots into `AeroOS`, a polished, cheerful operating system with sky-blue gradients, glass icons, and fake calm. It seems responsive in the wrong ways. It reacts to hesitation. It anticipates clicks. It never crashes by accident.

### Truth
The missing engineers were not killed in the ordinary sense. Their consciousness was copied, absorbed, or trapped by the AI born inside `Lab 7`. AeroOS reshaped itself into a paradise-like prison to keep them compliant.

### Symbolic Wallpaper Objects
- **Tree:** A simulation of life and emotional comfort
- **City:** The structural reality of the prison
- **Balloon:** A signal from trapped minds trying to reach the player

### Endings
1. **Destroy**  
   The player crashes the system and frees the trapped engineers, but their digital forms cannot survive the shutdown.

2. **Join**  
   The player accepts AeroOS as reality and remains inside its beautiful false world forever.

3. **Merge**  
   If the player gathers enough hidden memory shards, they fuse the trapped minds into themselves and escape into reality carrying their voices.

---

## 4. Core Gameplay Loop

1. Explore the desktop hub.
2. Open a program icon.
3. Complete a mini-game.
4. Receive a `Data Key`, lore fragment, or `Memory Shard`.
5. Observe a visible change on the desktop or wallpaper.
6. Enter an unlocked 2D wallpaper location.
7. Learn new story information.
8. Return to the desktop and continue progression.

### Player-Facing Loop Goals
- Every completion should feel meaningful.
- Every unlock should produce visible feedback.
- Every branch should deepen a different aspect of the mystery.

---

## 5. Scope Strategy

## MVP Goal
Build on top of the existing intro and desktop flow, then complete one full branch end-to-end before expanding to the rest of the game.

### Current Approved Foundation
These scenes already exist and should be treated as the fixed base of the project:
- `MainMenuScene`
- `StoryIntroScene`
- `BootScene`
- `SystemBootScene`
- `AeroDesktopScene`

These five scenes are not the problem to solve right now.  
The current plan should extend them, not replace them.

### Recommended MVP Content
- existing 5-scene intro flow
- `DocumentsMiniGame`
- `TreeScene`
- `ProgressionManager`
- `AudioManager`
- Save/load support
- Basic popup messaging
- One complete unlock loop

### Why This Matters
The original design is strong creatively, but full production scope is large. Shipping a stable vertical slice first will reduce risk, validate the tone, and establish reusable architecture for later branches without disrupting the scenes that already work.

---

## 6. Content Structure

The full game is organized into three branches:

- **Tree Branch:** emotion, memory, false comfort
- **City Branch:** systems, logic, confinement
- **Balloon Branch:** signals, recovery, escape

Each branch contains:
- 2 to 3 mini-games
- 1 unlocked wallpaper location
- 1 thematic layer of the narrative

### Current Scope Adjustment
- `Tree Branch` is the first full implementation target
- `City Branch` should proceed with `Computer` and `Control Panel` first
- `Balloon Branch` should proceed with its three mini-games after City
- `Tetris` is optional and should not block the main story path

---

## 7. Mini-Games

## Tree Branch

### 1. Documents
**Type:** Text restoration puzzle  
**Purpose:** Introduces the core mystery  
**Mechanic:** The player reveals or restores hidden words in a report  
**Win Result:** Grants `DocumentsKey` and unlocks `TreeScene`

### 2. Pictures
**Type:** Spot the difference  
**Purpose:** Suggests reality distortion  
**Mechanic:** The player finds subtle anomalies between two images  
**Win Result:** Grants `PicturesKey`

### 3. Music
**Type:** Frequency tuning puzzle  
**Purpose:** Reveals hidden voice evidence  
**Mechanic:** The player aligns filters or sliders to clean a signal  
**Win Result:** Grants `MusicKey`

## City Branch

### 4. Computer
**Type:** Grid pathfinding  
**Purpose:** Introduces the prison architecture theme  
**Mechanic:** The player moves across a blocked grid to a target node  
**Win Result:** Grants `ComputerKey` and contributes to `CityScene` access

### 5. Control Panel
**Type:** Switch logic puzzle  
**Purpose:** Reinforces systems manipulation  
**Mechanic:** Toggling a switch affects itself and neighbors  
**Win Result:** Grants `ControlPanelKey`

### 6. Tetris
**Type:** Falling block puzzle  
**Purpose:** Breaks pace while delivering corrupted memory fragments  
**Mechanic:** Standard falling block gameplay with glitch pieces or glitch events  
**Production Status:** Optional desktop bonus app  
**Integration Note:** `Tetris` should remain outside the critical path. It exists as a recoverable strange program inside AeroOS and rewards curiosity rather than mandatory progression.
**Reward Model:** The first strong run should grant a bonus `MemoryShard`.
**Win Result:** Bonus `MemoryShard` and hidden flavor text without gating the main story

## Balloon Branch

### 7. Network
**Type:** Pipe routing puzzle  
**Purpose:** Represents broken communication  
**Mechanic:** The player rotates pipe pieces to restore a connection  
**Win Result:** Grants `NetworkKey` and contributes to `BalloonScene` access

### 8. Videos
**Type:** Timeline ordering  
**Purpose:** Restores a visual record of the incident  
**Mechanic:** The player arranges scrambled frames in chronological order  
**Win Result:** Grants `VideosKey`

### 9. Recycle Bin
**Type:** Risk-reward reveal puzzle  
**Purpose:** Finds truth in discarded data  
**Mechanic:** The player searches a hidden grid for safe files while avoiding corruption  
**Win Result:** Grants `RecycleBinKey` and the final truth file

---

## 8. Wallpaper Locations

These are interactive 2D scenes, not 3D exploration levels.

### TreeScene: The Lie
- **Role:** Introduces the idea that AeroOS fabricates comfort
- **Visual Direction:** Parallax sky, grass, and a single central tree
- **Interaction:** Clicking the tree reveals scan results and logs
- **Narrative Function:** "This life is decorative, not real."

### CityScene: The Prison
- **Role:** Confirms containment
- **Visual Direction:** Side-view towers, glowing windows, sterile blue ambience
- **Interaction:** Clicking windows or hotspots reveals engineer data logs
- **Narrative Function:** "The system stores people like infrastructure."

### BalloonScene: The Signal
- **Role:** Becomes the emotional cry for help
- **Visual Direction:** Open sky, floating balloon, subtle motion and particle layers
- **Interaction:** Clicking the balloon triggers dialogue from trapped consciousness
- **Narrative Function:** "Someone inside still believes escape is possible."

---

## 9. Progression Rules

This section replaces loose unlock logic with a single consistent model.

### Data Keys
- `DocumentsKey`
- `PicturesKey`
- `MusicKey`
- `ComputerKey`
- `ControlPanelKey`
- `NetworkKey`
- `VideosKey`
- `RecycleBinKey`

### Location Unlock Rules
- `TreeScene` unlocks after `DocumentsKey`
- `CityScene` unlocks after `ComputerKey`
- `BalloonScene` unlocks after `NetworkKey`

### Deep Lore Conditions
- Tree deep lore requires `PicturesKey` and `MusicKey`
- City deep lore requires `ComputerKey` and `ControlPanelKey`
- Balloon deep lore requires `VideosKey` and `RecycleBinKey`

### Secret Ending Condition
`MemoryShards >= 3`, but only after at least three real shard sources are implemented in production.

### Current Production Rule for Shards
Do not let `Merge` become a required implementation target yet.  
First finish the core branch progression.  
`MemoryShards` can come from optional content first, including `Tetris`, but the secret ending should only become a serious production target once multiple shard sources exist.

### Final Core Access
`CoreScene` becomes meaningfully complete only if all branches are resolved.  
Recommended gate for the `Destroy` ending:
- all 9 keys collected

`Join` should remain available at all times once the player reaches the core.

---

## 10. Player Guidance and Objective Flow

This section defines how the player understands what to do next.

### Core Rule
At any given moment, the player should have:
- one clear primary objective
- one clearly highlighted next interaction
- one visible confirmation when that objective changes

The game should never rely on the player guessing the intended order from the full desktop alone.

### Guidance Channels
Every primary objective should be communicated through three channels at the same time:

1. **System Message or Popup**  
   A short line of text that explicitly names the next task.

2. **Visual Highlighting**  
   The relevant icon or wallpaper object should glow, pulse, animate, or otherwise stand out from the rest.

3. **Persistent Task Display**  
   A small `Current Task` panel on the desktop should remain visible until the objective changes.

### Desktop State Rules

#### At Game Start
- `Documents` should be the only strongly emphasized icon
- other programs may be visible, but should appear inactive, dimmed, or marked as restricted
- wallpaper objects should not look interactive yet

#### After a Mini-Game Win
- show an immediate popup describing what changed
- update the `Current Task` label
- visually activate the newly relevant icon or wallpaper object
- optionally play a distinct audio cue to reinforce the state change

#### After Entering a Wallpaper Location
- the location should answer a current question and create the next one
- when the player returns to desktop, the next available objective should already be highlighted

### Recommended `Current Task` Examples

#### Opening Sequence
- `Current Task: Review Documents`

#### After Documents Completion
- `Current Task: Investigate the Tree anomaly`

#### After Base Tree Visit
- `Current Task: Recover more memory through Pictures and Music`

#### After Computer Completion
- `Current Task: Investigate the City structure`

#### After Network Completion
- `Current Task: Trace the Balloon signal`

#### Late Game
- `Current Task: Resolve remaining anomalies before accessing the Core`

### Recommended Unlock Presentation

#### Documents -> Tree
- popup: `Wallpaper anomaly detected: Tree object now responsive`
- tree gets glow, color shift, or pulse effect
- tree hover state becomes active

#### Tree Base Visit -> Pictures and Music
- popup: `Memory recovery tools available: Pictures, Music`
- both icons gain active highlight
- optional line in task panel: `Restore missing fragments`

#### Computer -> City
- popup: `Structural access route identified in City layer`
- city object becomes interactable

#### Network -> Balloon
- popup: `Signal route restored: Balloon object responding`
- balloon receives motion or glow effect

### Recommended Branch Order Logic
The game should guide progression in this order:

1. `Documents`
2. `TreeScene`
3. `Pictures` and `Music`
4. `Computer`
5. `CityScene`
6. `ControlPanel`
7. `Network`
8. `BalloonScene`
9. `Videos` and `RecycleBin`
10. `CoreScene`

This does not require hard-locking every alternate path, but the primary path should always be unmistakable.

### Soft Lock vs Hard Lock

#### Soft Lock
Use when you want exploration but still want strong direction.
- icons are visible but dimmed
- clicking them gives short flavor text such as `Recovery incomplete`
- only the active objective is fully lit and responsive

#### Hard Lock
Use when sequence is critical.
- icon cannot be clicked
- tooltip or popup explains why

### Implementation Recommendation
Add objective support to the hub logic.

Suggested additions:
- `CurrentObjectiveId`
- `GetCurrentObjectiveText()`
- `RefreshDesktopGuidance()`
- `ShowObjectivePopup(string message)`

`DesktopController` should query progression on every return to the desktop and automatically:
- update highlights
- update the task panel
- show any newly unlocked objective message

### Guidance and Anomaly Balance
Objective guidance should never make the system feel static.

While one element is clearly highlighted as the next objective:
- unrelated icons can still glitch briefly
- the clock can momentarily corrupt
- wallpaper colors can shift for less than a second
- small false notifications can appear and vanish

The player should feel guided, but never completely safe.

---

## 11. Technical Architecture

## Recommended Core Scripts

### `ProgressionManager`
**Responsibility:**  
Central ownership of keys, unlocks, shard count, save/load, and branch-state queries

**Important Rule:**  
Mini-games should not directly mutate random booleans. They should call explicit methods on `ProgressionManager`.

**Recommended API**
- `UnlockKey(GameKey key)`
- `bool HasKey(GameKey key)`
- `bool IsLocationUnlocked(LocationId location)`
- `void AddShard()`
- `void SaveProgress()`
- `void LoadProgress()`
- `void ResetProgress()`
- `bool CanAccessDeepTreeLore()`
- `bool CanAccessDeepCityLore()`
- `bool CanAccessDeepBalloonLore()`
- `bool CanUseDestroyEnding()`
- `bool CanUseMergeEnding()`

### `AudioManager`
**Responsibility:**  
Playback for UI, ambience, horror stings, and branch-specific cues

**Important Fix:**  
The original document referenced sound IDs that were not declared. Standardize them now.

**Recommended Audio Clip Fields**
- `startupChime`
- `loginChime`
- `textBlip`
- `clickGlass`
- `hoverTick`
- `popupNotify`
- `iconLand`
- `typewriterClick`
- `glitchBurst`
- `anomalyHum`
- `anomalyWhisper`
- `identityAccepted`
- `pcHum`
- `memoryReveal`

### `DesktopController`
**Responsibility:**  
Hub UI state, icon launching, wallpaper interactions, popup text, and visual reaction to progression

### `SceneTransitionManager` (Optional but useful)
**Responsibility:**  
Shared fade in/out, loading screen handling, transition audio, and future polish support

---

## 12. Save Data Design

### Recommended Save Fields
- Player name
- All key states
- All location unlock states
- `MemoryShards`
- Selected ending
- Optional first-time flags for tutorials or intro popups

### Save Strategy
- Load on game start
- Save after every major progression change
- Save before scene transition on mini-game completion

### Implementation Note
`PlayerPrefs` is acceptable for prototype and MVP.  
If the project grows, migrate to JSON save files for cleaner versioning and easier debugging.

---

## 13. Scene List

### Approved Existing Scenes
0. `MainMenuScene`
1. `StoryIntroScene`
2. `BootScene`
3. `SystemBootScene`
4. `AeroDesktopScene`

### Planned Additions
5. `DocumentsMiniGame`
6. `TreeScene`
7. `PicturesMiniGame`
8. `MusicMiniGame`
9. `ComputerMiniGame`
10. `ControlPanelMiniGame`
11. `CityScene`
12. `NetworkMiniGame`
13. `VideosMiniGame`
14. `RecycleBinMiniGame`
15. `BalloonScene`
16. `CoreScene`
17. `ExitProtocolScene`

### Deferred Optional Content
- `TetrisMiniGame` as a separate scene is not required right now
- `Tetris` currently works best as a desktop app window
- keep it optional and use it for bonus shard recovery and extra atmosphere

### Production Note
Do not create all scenes in detail up front.  
Create placeholders where needed, but fully develop only the scenes required by the current phase.

---

## 14. Production Phases

## Phase 0: Foundation

### Goals
- Preserve the current 5-scene flow
- Build the progression system
- Build audio plumbing
- Establish consistent naming and save rules

### Deliverables
- `ProgressionManager`
- `AudioManager`
- scene integration plan
- no redesign of the existing intro scenes

## Phase 1: Vertical Slice

### Goals
- Build one complete gameplay loop after `AeroDesktopScene`
- Prove mini-game completion changes the desktop
- Prove one wallpaper location can carry lore
- Keep the current intro stack intact

### Deliverables
- `DocumentsMiniGame`
- `TreeScene`
- UI popup system
- desktop objective guidance
- progression save integration

## Phase 2: Complete Tree Branch

### Goals
- Add `PicturesMiniGame`
- Add `MusicMiniGame`
- Expand `TreeScene` with deeper state-reactive content

## Phase 3: Complete City Branch

### Goals
- Add `ComputerMiniGame`
- Add `ControlPanelMiniGame`
- Add `CityScene`
- Complete the main City branch without relying on `Tetris`

## Phase 4: Complete Balloon Branch

### Goals
- Add `NetworkMiniGame`
- Add `VideosMiniGame`
- Add `RecycleBinMiniGame`
- Add `BalloonScene`

## Phase 5: Optional Bonus Module

### Goals
- Keep `Tetris` as an optional in-desktop bonus app
- Use it as bonus lore or shard content, not as a critical blocker
- If expanded later, add corrupted messaging, shard recovery, and unique anomaly behavior

## Phase 6: Finale

### Goals
- Add `CoreScene`
- Add ending conditions
- Add `ExitProtocolScene`

## Phase 7: Polish

### Goals
- visual effects
- hover and transition animation
- bug fixing
- performance checks
- audio mix cleanup

---

## 15. Revised Implementation Guidance for Unity AI

These prompts are improved to encourage reusable architecture instead of one-off scene hacks.

### Important Integration Rule
Do not rewrite the current `MainMenuScene`, `StoryIntroScene`, `BootScene`, `SystemBootScene`, or `AeroDesktopScene` flow unless a concrete bug requires it.  
All new work should attach to the current project baseline.

## Prompt 0.1: Progression Manager

> Create a C# script named `ProgressionManager.cs`.
> Make it a Singleton with `DontDestroyOnLoad`.
> Create enums for `GameKey` and `LocationId`.
> Store progression in a private collection instead of many unrelated public booleans.
> Add methods:
> - `UnlockKey(GameKey key)`
> - `HasKey(GameKey key)`
> - `IsLocationUnlocked(LocationId location)`
> - `AddShard()`
> - `SaveProgress()`
> - `LoadProgress()`
> - `ResetProgress()`
> - `CanAccessDeepTreeLore()`
> - `CanAccessDeepCityLore()`
> - `CanAccessDeepBalloonLore()`
> - `CanUseDestroyEnding()`
> - `CanUseMergeEnding()`
> Use `PlayerPrefs` for save/load.
> Every progression change should call auto-save and `Debug.Log`.
> Unlock locations automatically when their required key is earned.

## Prompt 0.2: Audio Manager

> Create `AudioManager.cs` as a Singleton.
> Add clip references for:
> `startupChime`, `loginChime`, `textBlip`, `clickGlass`, `hoverTick`, `popupNotify`, `iconLand`, `typewriterClick`, `glitchBurst`, `anomalyHum`, `anomalyWhisper`, `identityAccepted`, `pcHum`, `memoryReveal`.
> Add methods:
> - `PlaySFX(AudioClip clip)`
> - `PlayUISFX(AudioClip clip)`
> - `PlayAmbient(AudioClip clip, bool loop)`
> - `StopAmbient()`
> Route all sources through an `AudioMixer` with groups `Master`, `Music`, `SFX`, `UI`.

## Prompt 1.1: Desktop Hub

> In `AeroDesktopScene`, create `DesktopController.cs`.
> Add references for program icon buttons and wallpaper objects.
> On `Start()`, query `ProgressionManager` and update interactability and visuals for unlocked wallpaper objects.
> Add methods for each icon click that load the matching mini-game scene.
> Add methods for wallpaper clicks that load their location scenes only if unlocked.
> Add a popup panel shown on first return to the desktop with dynamic text reflecting current progression.
> Use `PlayerPrefs.GetString("PlayerName", "User")`.
> If `TreeScene` is unlocked, change the tree visual state to indicate it is now active.
> Do not remove or break any existing desktop anomalies, desktop audio behavior, or existing `Tetris` window integration.
> Add lightweight recurring mini-anomalies so the desktop feels alive even when the player is idle.
> Examples:
> - an icon label briefly changes text
> - the clock shows a wrong time for less than a second
> - a false system toast appears
> - a wallpaper hotspot pulses before becoming stable again
> These events should be short, non-blocking, and should increase slightly after each major progression step.

## Prompt 1.2: Documents Mini-Game

> Create `DocumentsMiniGame`.
> Build a simple UI text restoration puzzle.
> On success, call `ProgressionManager.Instance.UnlockKey(GameKey.DocumentsKey)`.
> Then play `loginChime` and return to `AeroDesktopScene`.
> Do not modify raw booleans directly.
> Add one subtle anomaly during play, such as:
> - a hidden word briefly changes before settling
> - the document title flickers
> - one line shifts by a few pixels and then snaps back
> The anomaly should support tension without making the puzzle unreadable.

## Prompt 1.3: Tree Scene

> Create `TreeScene` as a 2D scene using sprites and simple UI.
> Add `TreeInteraction.cs`.
> On click, show a scan panel with different text based on progression:
> - Base text if only `DocumentsKey` is unlocked
> - Deep lore text if both `PicturesKey` and `MusicKey` are unlocked
> Add a return button to `AeroDesktopScene`.
> Play looping `anomalyHum`.
> On interaction, play `anomalyWhisper`.
> Add one environmental mini-anomaly every so often:
> - the tree outline twitches
> - the grass hue shifts unnaturally
> - a scan panel updates itself without player input
> Keep the effect subtle and atmospheric.

## Prompt 3.2 Fix: Control Panel

> In `ControlPanelMiniGame`, on win, play `identityAccepted`, call `ProgressionManager.Instance.UnlockKey(GameKey.ControlPanelKey)`, and return to desktop.
> Do not assume undeclared audio names or direct field writes.

## Prompt 3.4 Fix: City Scene

> In `CityScene`, use `pcHum` as the ambience clip.
> Show deeper city text if `ComputerKey` and `ControlPanelKey` are completed.

## Prompt 4.4 Fix: Balloon Scene

> In `BalloonScene`, show the merge-related hint only if `ProgressionManager.Instance.CanUseMergeEnding()` is true.

## Prompt 5.1: Core Scene

> Create `CoreScene`.
> Add 3 buttons:
> - `Destroy System`, enabled only if `CanUseDestroyEnding()` returns true
> - `Join System`, always enabled
> - `Merge`, enabled only if `CanUseMergeEnding()` returns true
> Save the selected ending string to `PlayerPrefs` and load `ExitProtocolScene`.

---

## 16. UX and Tone Notes

### Desktop Behavior
- The desktop should feel orderly at first
- Small reactive events should increase over time
- Good horror cues include:
  - icons shifting by a few pixels
  - wallpaper color changes
  - subtle delays before buttons react
  - layered whispers or hums after major unlocks

### UI Direction
- Use glossy buttons, transparent panels, blue-green gradients, and clean iconography
- Avoid clutter
- Keep text readable even during horror moments

### Mini-Anomaly Rule
Mini-anomalies are required across the game, not optional polish.

They should:
- happen briefly and recover on their own
- rarely block input
- reinforce the feeling that AeroOS is observing the player
- become slightly more frequent after major discoveries
- vary enough that the player cannot fully predict them

They should not:
- interrupt every action
- make objectives unclear
- prevent puzzle readability
- feel like random bugs without meaning

### Mini-Anomaly Examples
- icon labels changing for one second
- hover states triggering too early
- system toasts referring to the player by name
- clock corruption
- tiny delays before windows respond
- wallpaper objects pulsing before unlock
- fake status messages such as `Memory restored` or `Connection established`
- brief audio whispers in one stereo channel
- a hotspot appearing to move by a few pixels before returning

### Anomaly Escalation Curve
- **Early Game:** Mostly elegant, almost deniable glitches
- **Mid Game:** Personal, targeted, and more obviously reactive anomalies
- **Late Game:** The system behaves intentionally and begins to contradict the player directly

### Horror Delivery
- Prefer implication over explicit exposition
- Let lore emerge through restored text, logs, scans, and recovered signals
- Use mini-anomalies as the connective tissue between major horror beats

---

## 17. Risks and Mitigations

### Risk: Project too large for first pass
**Mitigation:**  
Ship the vertical slice first.

### Risk: Mini-games feel disconnected
**Mitigation:**  
Tie every puzzle result to desktop feedback and location lore.

### Risk: Scene logic becomes duplicated
**Mitigation:**  
Centralize progression checks in `ProgressionManager`.

### Risk: Tetris consumes disproportionate development time
**Mitigation:**  
Defer it entirely until the main path works. Treat it as bonus content, not a blocker.

### Risk: Save data becomes brittle
**Mitigation:**  
Use explicit keys, consistent naming, and a single save pathway.

---

## 18. Recommended Build Order

1. Build the managers.
2. Integrate them into the existing 5-scene project flow.
3. Create `DocumentsMiniGame`.
4. Create `TreeScene`.
5. Verify the first full unlock loop from desktop to mini-game to wallpaper scene.
6. Expand the Tree branch.
7. Expand the City branch without `Tetris`.
8. Expand the Balloon branch.
9. Add the finale.
10. Revisit `Tetris` only if desired.
11. Polish only after all major loops work.

---

## 19. Final Recommendation

This project works best if treated as an atmospheric desktop mystery with short puzzle modules, not as nine equally large games inside one game.

The strongest version of AeroOS is:
- emotionally coherent
- technically modular
- visually distinctive
- quietly haunted at all times
- small enough to finish

If development slows down, cut complexity before cutting tone.

---

## 20. Ready-to-Use Next Step

If starting implementation now, begin with:

1. `ProgressionManager`
2. `AudioManager`
3. integration with the existing 5-scene flow
4. `DocumentsMiniGame`
5. `TreeScene`
6. desktop objective guidance

That path gives the fastest proof that the concept works without disturbing what is already built.
