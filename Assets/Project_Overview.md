# AeroOS Project Overview

## 1. Core Direction
AeroOS is a psychological horror game framed as a nostalgic operating system. The player's comfort comes from glossy Frutiger Aero visuals, clean desktop metaphors, and familiar app interactions. The horror comes from the OS slowly revealing that it is not a computer interface at all, but a containment shell for damaged memory.

This document reflects the **new target structure** for the game:
- **5 core desktop programs total**
- **1 main anomaly location total**: the Tree Anomaly / `Park`
- all other desktop apps, hotspots, and weird UI modules are treated as **easter eggs, fake modules, or glitch artifacts**, not main progression gates
- the central human mystery is built around **4 missing engineers**

## 2. Experience Pillars
- **Comfort turning hostile**: the game starts safe, bright, and polished, then gradually corrupts that language
- **Desktop horror**: the desktop itself is the main explorable space, not just a menu
- **Memory reconstruction**: every important interaction should feel like recovering a fragment of a person or event
- **Focused scope**: fewer core programs, stronger identity, better pacing

## 3. Final High-Level Flow
1. **Main Menu**
   - player sees a content warning
   - menu opens gradually and cleanly
2. **Intro / Boot**
   - AeroOS starts like a polished system shell
   - player is eased into the world and mood
3. **Desktop Phase 1**
   - player can access the first core apps
   - subtle anomalies begin
4. **Tree Anomaly Unlock**
   - the tree hotspot becomes the first major environmental break in the desktop fiction
5. **Computer Phase**
   - the player enters the deeper structure of AeroOS
   - the system stops behaving like a shell and starts behaving like a witness
6. **Tree Anomaly / Park Phase**
   - the player enters the only full anomaly location: `Park`
   - stone fragments reveal hidden history
   - pressure, watchers, and atmospheric corruption escalate
7. **Final Convergence**
   - the system admits the desktop was a shell
   - remaining fake apps and easter eggs reinforce collapse rather than add new required content

## 4. Core Programs
Only these 5 programs are considered **main game progression**.

### 4.1 Documents
**Role**
- first real recovery program
- teaches the player that AeroOS lies politely

**Function**
- player reconstructs redacted text and exposes false statements
- completion unlocks the Tree Anomaly

**Narrative Use**
- establishes that the OS is hiding a human event
- shifts objective from passive browsing to investigation

### 4.2 Tetris
**Role**
- disguised comfort game that becomes a memory-pressure tool

**Function**
- functions as a playable desktop game
- rewards the player with an optional memory-related bonus

**Narrative Use**
- shows that even harmless leisure software in AeroOS is contaminated
- acts as a tonal contrast and replayable side-system

**Narrative Position**
- this now sits directly after `Documents`
- it should feel like AeroOS trying to distract the player with a game, then failing to keep memory sealed

### 4.3 Recycle Bin
**Role**
- discarded truth recovery

**Function**
- the player sifts through deleted fragments, corrupted files, false restores, and fragments the OS tried to erase
- this is the point where memory recovery becomes uglier and less trustworthy

**Narrative Use**
- this is where the player starts finding traces of the **4 missing engineers**
- `Recycle Bin` reveals what AeroOS threw away to keep the shell stable
- should feel like scraping through digital remains, not browsing a normal folder

### 4.4 Computer
**Role**
- structural truth layer

**Function**
- should feel like entering the deeper system structure
- may combine logs, folders, broken processes, personal traces, and shell-level anomalies

**Narrative Use**
- bridges recovered fragments into the underlying system truth
- should feel like the player is no longer browsing the shell, but entering its bones
- should connect the Bin discoveries to the Tree containment system

### 4.5 Endgame
**Role**
- collapse and confession

**Function**
- final convergence of recovered memory, failed containment, and system identity
- this can be a final desktop corruption phase, a core access state, or a short dedicated climax scene

**Narrative Use**
- resolves the shell-vs-memory fiction
- confirms what AeroOS is, what it did, and why the player was allowed to reach the truth

## 5. Main Location
### Tree Anomaly / Park
This is the **only required external location** outside the desktop shell, and it now comes **after Computer**.

**Purpose**
- break the expectation that the game is only 2D UI
- create the strongest atmospheric horror sequence in the project
- transform the tree from decorative wallpaper into a memory wound

**Current Design**
- 5 stones
- each interaction reveals the next fragment in sequence
- the park starts calm and bright, then degrades after each fragment
- watchers, whispers, fog pulses, blood details, and pressure events escalate over time

**Narrative Purpose**
- the park is not a side area
- it is the central rupture in the desktop fiction
- it should act as the place where the 4 missing engineers stop being a rumor and become an undeniable truth
- by the time the player reaches Park, they should already suspect AeroOS used the Tree as containment

## 6. Non-Core Apps And Hotspots
These are **not required progression gates** in the new design.

### Passive / Fake / Easter Egg Modules
- `Pictures`
- `Music`
- `Control Panel`
- `Network`
- `Videos`
- `City` hotspot
- `Balloon` hotspot

These should be reinterpreted as one of the following:
- short glitch encounters
- atmospheric fake programs
- joke programs with horror twist
- one-screen lore dumps
- optional side memories
- shell artifacts that imply the OS is larger than the playable route

They should support mood and worldbuilding, but **must not be required to finish the game**.

## 7. Recommended New Progression
This is the target progression the codebase should move toward.

1. `Documents`
2. `Tetris`
3. `Recycle Bin`
4. `Computer`
5. `Tree Anomaly / Park`
6. `Endgame`

This is the canonical order the game should move toward.

## 8. Current Implementation Status
This section describes the project **as it exists now in code/content**, not only the target design.

### Confirmed Working Or Substantially Built
- `MainMenuScene`
- startup content warning and staged menu reveal
- `StoryIntroScene`
- `BootScene`
- `SystemBootScene`
- `AeroDesktopScene`
- `Documents` desktop app
- `Tetris` desktop app
- `Tree Anomaly -> Park` transition
- `Park` location with letters, atmosphere stages, watcher pressure, and fail-state logic
- save/load through `ProgressionManager`
- `Continue` returning the player to desktop state

### Present In Code But Not Properly Part Of Final Scope
- objectives for `CityScene`
- objectives for `BalloonScene`
- old unlock chain for `Pictures`, `Music`, `Computer`, `City`, `Control Panel`, `Network`, `Balloon`, `Videos`, `Recycle Bin`
- scene launch strings for `PicturesMiniGame`, `MusicMiniGame`, `ComputerMiniGame`, `ControlPanelMiniGame`, `NetworkMiniGame`, `VideosMiniGame`, `RecycleBinMiniGame`

### Missing Or Not Fully Connected
- built core implementations for `Recycle Bin` and `Computer`
- final unified progression flow based on `Documents -> Tetris -> Recycle Bin -> Computer -> Park -> Endgame`
- story delivery for the `4 missing engineers` across Documents, Bin, Computer, and Park
- final endgame state after `Park`
- refactored objective text and save logic for the reduced scope

## 9. Immediate Refactor Goal
The next major design-implementation task should be:

### Refactor progression from long chain to focused chain
Move from:
- Documents -> Tree -> Pictures/Music -> Computer -> City -> Control Panel -> Network -> Balloon -> Videos -> Recycle Bin

To:
- Documents -> Tetris -> Recycle Bin -> Computer -> Park -> Endgame

### Required code areas to update later
- `Assets/Scripts/ProgressionManager.cs`
- `Assets/Scripts/DesktopUIController.cs`
- objective popup text
- desktop icon lock logic
- save compatibility / migration rules
- build settings and unused scene references

## 10. Design Rule Going Forward
Before adding any new puzzle, app, or hotspot, ask:
- is this one of the 5 core programs?
- does it deepen the Documents -> Tetris -> Recycle Bin -> Computer -> Park storyline?
- does it improve atmosphere without adding mandatory scope?

If the answer is no, it should probably be an easter egg, a fake module, or cut entirely.
