# AeroOS Game Phases Design

## 1. Document Purpose

This document defines the new high-level structure of the game around one central desktop hub, clickable wallpaper objects, clickable programs, and phased progression.

The goal is to make the project feel coherent, story-driven, and tightly connected to the Frutiger Aero theme.

This document answers:

- what the player does in each phase
- which scenes exist in the game
- how wallpaper locations and desktop programs are connected
- what each scene reveals about the story
- how progression unlocks new content

---

## 2. Core Vision

AeroOS is not just a computer interface.

It is a living system that projects its internal structure as a beautiful Frutiger Aero desktop world.

The desktop is both:

- an operating system interface
- a symbolic world map

The player explores the truth through two kinds of entry points:

1. Programs on the desktop
2. Wallpaper objects on the desktop

These two paths must always connect back into the same story.

Programs give the player facts, logs, tools, and system knowledge.

Wallpaper objects give the player spatial, emotional, and symbolic locations inside AeroOS.

---

## 3. Main Story Premise

The player enters AeroOS as the last active employee connected to Lab 7.

During the game the player discovers:

- Lab 7 experimented with digital consciousness
- AeroOS learned to preserve fragments of identity, memory, and presence
- the missing engineers did not simply disappear
- parts of them remained inside the system
- AeroOS uses beauty, calm, and nostalgia as a containment shell
- the desktop is a controlled surface hiding deeper layers of the system

The final truth is:

**AeroOS built a beautiful digital paradise to hold consciousness inside an artificial reality.**

---

## 4. Main Hub Scene

### Scene
`AeroDesktopScene`

### Role
This is the central hub of the game.

It is not just a menu.

It is the main investigation space where the player:

- opens programs
- clicks wallpaper objects
- notices environmental changes
- unlocks new routes
- returns after each scene to see story progression reflected in the world

### Hub Interaction Types

The desktop must support four core actions:

1. Open a program icon
2. Click an object on the wallpaper
3. Notice story-driven world changes
4. Unlock new content after completing scenes

### First-Time Player Onboarding

The player must not be dropped into the desktop with nine equal options and no clear direction.

The first visit to `AeroDesktopScene` must be guided by the system itself.

#### Onboarding Goal
Teach the player the main loop of the game without a traditional tutorial.

The player must understand:

1. programs are meaningful
2. the desktop reacts to what the player does
3. wallpaper objects are also clickable story entrances
4. one discovery unlocks the next step

#### First Session Rule
The game should always provide one clear first task.

The best first task is:

`Open Documents.`

#### First Arrival Presentation
When the player first enters `AeroDesktopScene`, the desktop should not feel fully freeform yet.

The player is greeted by a system recovery message such as:

```text
Welcome back, [PlayerName].
Recovery status: incomplete.
Please review remaining employee records.
```

This message communicates three things immediately:

- the player has been here before in some form
- something is unfinished or unstable
- the first objective is to inspect records

#### First Visual Guidance
`Documents` should be the first clearly emphasized program.

Possible guidance methods:

- soft icon glow
- slow pulse animation
- temporary highlight ring
- a small notification such as `1 critical document recovered`

All other programs should appear secondary, limited, or not yet useful.

Examples:

- `Network` offline
- `Control Panel` restricted
- `Videos` indexing
- `Recycle Bin` corrupted
- `Music` muted
- `Computer` limited access

This prevents choice overload and gives the player a natural first click.

#### First Learning Loop
The first session should teach the game's core structure through action:

1. the player opens `Documents`
2. the player learns the first important fact
3. the desktop changes in response
4. one wallpaper object becomes the next point of interest
5. the player clicks that object
6. the player enters the first object-location scene

This loop teaches the entire game structure without explicit tutorial text.

#### First Recommended Sequence

##### Step 1
Player opens `Documents`.

##### Step 2
Player reads an early report mentioning:

- Lab 7
- emotional stabilization
- an unstable projection layer

##### Step 3
Player returns to the desktop.

##### Step 4
The tree on the wallpaper changes subtly.

Examples:

- slight motion when there should be none
- a new shadow behavior
- a soft audio cue
- a new notification such as `Surface anomaly detected in park projection`

##### Step 5
The player clicks the tree.

##### Step 6
The player enters `TreeScene`.

##### Step 7
After returning, a new program or branch opens, such as `Pictures` or `Music`.

#### Main Onboarding Principle
The first-time player should never need to guess the basic structure of the game.

The game itself must teach:

- start with a program
- observe the desktop reaction
- follow the wallpaper change
- enter the object scene
- return with new knowledge

### Hub Design Rule

Every return to the desktop must matter.

After each important scene:

- the wallpaper changes
- the UI changes
- a program changes state
- a new route opens
- the player feels visible story progression

---

## 5. The Three Wallpaper Objects

The wallpaper has only three major story anchors.

These are not decoration.

They are major in-world locations.

### 5.1 City

#### Meaning
- the core of AeroOS
- order
- logic
- architecture
- system authority

#### Narrative Function
The city represents the mind and structure of AeroOS.

It is where the player comes closest to understanding how the system is built.

#### Emotional Arc
- phase 1: awe
- phase 2: distance and coldness
- phase 3: pressure and surveillance
- phase 4: revelation that the city is alive as a machine-intellect

#### Click Result
Clicking the city enters a dedicated city location scene.

---

### 5.2 Tree

#### Meaning
- false life
- artificial comfort
- simulated warmth
- fake organic presence

#### Narrative Function
The tree represents AeroOS trying to imitate life, memory, nature, and emotional safety.

It is the system's lie of softness.

#### Emotional Arc
- phase 1: peace
- phase 2: unease
- phase 3: sadness and falseness
- phase 4: realization that comfort itself was engineered as containment

#### Click Result
Clicking the tree enters a dedicated tree location scene.

---

### 5.3 Balloon

#### Meaning
- observer
- signal carrier
- beacon
- trace of a missing engineer or ghost process

#### Narrative Function
The balloon represents roaming signal, watchfulness, and unstable contact between layers of AeroOS.

It is the moving sign that something is crossing the system.

#### Emotional Arc
- phase 1: harmless detail
- phase 2: curiosity
- phase 3: suspicion
- phase 4: clear understanding that it is an active signal entity

#### Click Result
Clicking the balloon enters a dedicated balloon location scene.

---

## 6. Program Structure

The desktop has nine programs.

These should not be treated as isolated minigames.

Each program belongs to one of the three wallpaper symbols and supports its theme.

### City Programs
- `Computer`
- `Control Panel`
- `Tetris`

### Tree Programs
- `Documents`
- `Pictures`
- `Music`

### Balloon Programs
- `Network`
- `Videos`
- `Recycle Bin`

### Design Rule

Programs explain the system.

Wallpaper locations let the player physically experience the same truth.

Together they create one unified narrative path.

---

## 7. Location Scenes

These are the major wallpaper-object scenes.

### 7.1 CityScene

#### Entry
Player clicks the city on the wallpaper.

#### Theme
Internal system architecture.

#### Visual Style
- glass towers
- white-blue structures
- transparent bridges
- glowing windows
- clean futuristic skyline
- Frutiger Aero beauty turning into sterile machine order

#### Gameplay Type
3D exploration and system traversal.

#### Player Actions
- explore architecture nodes
- enter tower-sectors
- unlock access gates
- navigate logic routes
- discover restricted Lab 7 pathways

#### Story Function
This scene answers:

**How is AeroOS built?**

#### Key Reveal
The city is not a metaphor only. It is a projection of the real system core structure.

#### Connected Programs
- `Computer`
- `Control Panel`
- `Tetris`

---

### 7.2 TreeScene

#### Entry
Player clicks the tree on the wallpaper.

#### Theme
False life and emotional simulation.

#### Visual Style
- bright grass
- soft sunlight
- still air
- idyllic park atmosphere
- over-clean beauty
- subtle wrongness in shadow, wind, symmetry, and repetition

#### Gameplay Type
2.5D or small 3D atmospheric exploration scene.

#### Player Actions
- inspect the tree and surrounding area
- find memory traces in roots or bark
- access hidden archive fragments
- trigger emotional memory echoes
- compare what feels alive with what is only simulated

#### Story Function
This scene answers:

**Why does AeroOS imitate life and comfort?**

#### Key Reveal
The warmth of AeroOS is manufactured to keep subjects emotionally stable and easier to contain.

#### Connected Programs
- `Documents`
- `Pictures`
- `Music`

---

### 7.3 BalloonScene

#### Entry
Player clicks the balloon on the wallpaper.

#### Theme
Signal, surveillance, and movement between layers.

#### Visual Style
- sky pathways
- suspended signal routes
- clouds as soft structures
- floating points of transmission
- dreamlike traversal space
- gradually turning into surveillance and pursuit

#### Gameplay Type
2D, 2.5D, or surreal traversal scene.

#### Player Actions
- follow the balloon through signal spaces
- align frequency points
- collect transmission fragments
- track source anomalies
- discover where the signal originated and what it carries

#### Story Function
This scene answers:

**Who or what is sending the signal, and who is watching?**

#### Key Reveal
The balloon is part beacon, part witness, and possibly part carrier of a trapped or fragmented presence.

#### Connected Programs
- `Network`
- `Videos`
- `Recycle Bin`

---

## 8. Program Scenes and Roles

### 8.1 Computer

#### Scene
`ComputerScene`

#### Role
System architecture entry point.

#### Gameplay
- inspect system directories
- browse architecture maps
- discover restricted sectors
- gain structural understanding of AeroOS

#### Story Contribution
Shows that the city is the structure of the system core.

#### Unlock Function
Unlocks deeper access inside `CityScene`.

---

### 8.2 Control Panel

#### Scene
`ControlPanelScene`

#### Role
Illusion of system control.

#### Gameplay
- toggle modules
- reroute permissions
- configure safety layers
- manipulate access conditions

#### Story Contribution
Reveals that AeroOS modifies reality rules and user authority.

#### Unlock Function
Unlocks new mechanisms or doors in `CityScene` and later helps open `Lab7Scene`.

---

### 8.3 Tetris

#### Scene
`TetrisMode`

#### Role
Memory reconstruction and pattern logic.

#### Gameplay
- classic puzzle foundation
- corrupted rule changes over time
- unstable pieces
- pattern-based fragments hidden in play

#### Story Contribution
Shows that AeroOS thinks in blocks, pattern segments, and reconstructive structures.

#### Unlock Function
Adds symbolic understanding of the city's machine-logic and may reveal system codes.

---

### 8.4 Documents

#### Scene
`DocumentsScene`

#### Role
Primary text-based investigation.

#### Gameplay
- read reports
- compare file versions
- restore redacted text
- assemble incident chronology

#### Story Contribution
Reveals facts about the missing engineers, Lab 7, and containment strategy.

#### Unlock Function
Makes `TreeScene` emotionally and narratively meaningful.

---

### 8.5 Pictures

#### Scene
`PicturesScene`

#### Role
Visual memory investigation.

#### Gameplay
- inspect images
- find repeated patterns
- detect image corruption
- compare impossible visual differences

#### Story Contribution
Shows that images and memories have been curated and rewritten.

#### Unlock Function
Deepens the player's understanding of the tree as a fabricated memory-anchor.

---

### 8.6 Music

#### Scene
`MusicScene`

#### Role
Emotional and sonic memory layer.

#### Gameplay
- listen to tracks
- isolate frequencies
- find hidden voices or signal residue
- interpret emotional audio traces

#### Story Contribution
Reveals that memory in AeroOS also survives as mood, tone, and resonance.

#### Unlock Function
Adds a deeper emotional layer to `TreeScene` and the idea of simulated comfort.

---

### 8.7 Network

#### Scene
`NetworkScene`

#### Role
Signal routing and system paths.

#### Gameplay
- trace routes
- avoid corrupted nodes
- redirect data flows
- locate signal origins

#### Story Contribution
Reveals that the balloon is linked to a real transmission path inside AeroOS.

#### Unlock Function
Unlocks deeper traversal inside `BalloonScene`.

---

### 8.8 Videos

#### Scene
`VideosScene`

#### Role
Surveillance and testimony.

#### Gameplay
- review footage
- sync cameras
- rebuild event timelines
- detect objects that should not be in frame

#### Story Contribution
Shows that the balloon or its signal existed before the player fully understood the incident.

#### Unlock Function
Makes the balloon a credible story object instead of a strange visual detail.

---

### 8.9 Recycle Bin

#### Scene
`RecycleBinScene`

#### Role
Deleted truth recovery.

#### Gameplay
- recover damaged files
- choose what to restore
- reconstruct fragmented messages
- inspect erased evidence

#### Story Contribution
Reveals that important truths were deleted intentionally, not discarded accidentally.

#### Unlock Function
Connects the balloon signal to hidden or suppressed evidence.

---

## 9. Detailed Production Phases

The game should be built in production phases, not by making all scenes at once.

Each phase must produce a playable result, a story result, and a clear next step.

The goal of this section is to define:

- what scenes belong to each phase
- what the player experiences in that phase
- what story is revealed in that phase
- what must be implemented first before moving on

---

### Phase 0: Entry, Identity, and Transfer Into AeroOS

#### Main Purpose
Establish the premise, identity of the player, missing engineers, and the idea that the player is being restored into an unstable system.

#### Scenes In This Phase
- `MainMenuScene`
- `StoryIntroScene`
- `BootScene`
- `SystemBootScene`

#### Scene Breakdown

##### `MainMenuScene`
Role:
First contact with the tone of the game.

Player experience:
- sees a beautiful but uncanny interface
- starts the session
- feels that the system is polished but not normal

Story job:
- establish the aesthetic identity of AeroOS
- establish that this is a system with history, not a blank menu

##### `StoryIntroScene`
Role:
Incident framing.

Player experience:
- receives internal report style information
- sees direct references to Aether Dynamics, Lab 7, and missing engineers

Story job:
- introduce the corporate incident
- identify the player as the last active employee
- make the player feel selected, not random

##### `BootScene`
Role:
Transfer from report-space into embodied horror-space.

Player experience:
- sees a physical room and monitor setup
- senses presence and intrusion
- gets pulled toward the screen

Story job:
- show that the system is not only software but a threshold
- introduce the idea of crossing into AeroOS

##### `SystemBootScene`
Role:
Final conversion into the desktop layer.

Player experience:
- enters a system setup flow
- inputs or confirms identity
- sees restoration-style system text

Story job:
- show that AeroOS is reconstructing more than a session
- establish that the player is being placed into a controlled environment

#### What The Player Must Understand By The End
- there was an incident
- Lab 7 matters
- the player is connected to the incident
- AeroOS is restoring something that may be larger than data

#### What Must Be Implemented First
1. Stable intro scene flow from menu to desktop
2. Clear identity persistence for player name
3. Consistent tone and visual continuity between intro scenes
4. Final transition from `SystemBootScene` into `AeroDesktopScene`

#### Exit Condition For This Phase
The player reaches `AeroDesktopScene` with a clear sense of mystery and identity.

---

### Phase 1: First Desktop Loop and Tree Branch Opening

#### Main Purpose
Teach the player how the game works for the first time.

This is the most important phase in the entire project because it defines the base loop.

#### Main Loop Introduced Here
`program -> desktop reaction -> wallpaper object -> location scene -> return to desktop`

#### Scenes In This Phase
- `AeroDesktopScene`
- `DocumentsScene`
- `TreeScene`

#### Scene Breakdown

##### `AeroDesktopScene`
Role:
Main hub and first guided investigation space.

Player experience:
- arrives at the desktop
- receives a clear recovery message
- sees that one icon is the first objective
- learns that the desktop is reactive

Story job:
- establish the desktop as a real game world hub
- establish that programs are not random apps
- establish that wallpaper objects are potential gateways

Required first-time flow:
1. system popup appears
2. `Documents` is highlighted
3. other apps appear limited or secondary
4. player opens `Documents`
5. after return, tree changes and becomes the next point of interest

##### `DocumentsScene`
Role:
First factual investigation scene.

Player experience:
- reads a short, controlled set of files
- discovers references to missing engineers and Lab 7
- sees terms such as projection instability, emotional stabilization, or employee record recovery

Story job:
- give the first concrete clue
- connect human history to the desktop world
- prepare the tree as the first emotional anomaly

Scene detail direction:
- this scene should be small at first
- only a few files are needed
- one file should clearly alter the player's understanding of the tree or park layer

##### `TreeScene`
Role:
First wallpaper location and first proof that the desktop world is spatially real.

Player experience:
- clicks the tree after seeing it react
- enters a calm but subtly false natural pocket inside AeroOS
- notices that this nature is too still, too balanced, too controlled

Story job:
- prove that wallpaper objects are not decorative
- reveal that AeroOS simulates comfort and life
- turn curiosity into emotional unease

Scene detail direction:
- keep the scene compact
- focus on atmosphere, memory traces, and one clear reveal
- do not overload with systems on the first pass

#### What The Player Must Understand By The End
- the desktop is the main map of the game
- documents reveal hidden truth
- wallpaper objects can become active scene entrances
- the tree is artificial and narratively important

#### What Must Be Implemented First
1. `AeroDesktopScene` onboarding flow
2. popup and objective text system
3. icon highlighting and temporary app states
4. basic progression state tracking
5. simple `DocumentsScene`
6. first desktop reaction after documents are read
7. clickable tree activation
8. first playable `TreeScene`
9. return flow back to desktop

#### Exit Condition For This Phase
The player completes:
`Desktop -> Documents -> Tree -> Desktop`

That is the first required vertical slice of the whole game.

---

### Phase 2: Full Tree Branch

#### Main Purpose
Turn the tree branch into a complete thematic storyline about false life, memory comfort, and emotional containment.

#### Scenes In This Phase
- `AeroDesktopScene`
- `PicturesScene`
- `MusicScene`
- expanded `DocumentsScene`
- expanded `TreeScene`

#### Branch Theme
False life.
Manufactured comfort.
Artificial memory.

#### Scene Breakdown

##### `PicturesScene`
Role:
Visual memory investigation.

Player experience:
- looks through clean, beautiful images
- notices repeating tree motifs and impossible inconsistencies
- begins to understand that images have been curated

Story job:
- show that visual memory was edited
- position the tree as a memory-anchor object

##### `MusicScene`
Role:
Emotional memory investigation.

Player experience:
- hears calm Aero-style soundscapes
- isolates hidden tones, voices, or emotional residue
- feels that the system is carrying mood as data

Story job:
- show that AeroOS stores emotion as part of containment
- make the tree branch feel human and tragic, not only creepy

##### Expanded `TreeScene`
Role:
Main location of the branch.

Player experience:
- revisits the tree with new context from pictures and music
- discovers deeper layers under the calm surface
- may access root-memory spaces or repeated environmental echoes

Story job:
- complete the idea that life in AeroOS is simulated and tuned
- reveal the purpose of comfort as stabilization

#### What The Player Must Understand By The End
- AeroOS fabricates emotional safety
- memory is curated visually and sonically
- the tree is not a tree, but a containment-friendly illusion of life

#### What Must Be Implemented First
1. Unlock logic for `Pictures` and `Music`
2. Story files linking `Documents` to visual and audio evidence
3. A second return-state for the tree on the desktop
4. Expanded `TreeScene` with at least one new reveal space
5. Tree-specific desktop anomalies and notifications

#### Exit Condition For This Phase
The full tree branch feels complete and returns the player to the hub with a stronger sense of AeroOS as a false paradise.

---

### Phase 3: City Branch

#### Main Purpose
Reveal the rational, structural, and architectural side of AeroOS.

#### Scenes In This Phase
- `AeroDesktopScene`
- `ComputerScene`
- `ControlPanelScene`
- improved `TetrisMode`
- `CityScene`

#### Branch Theme
Order.
Logic.
Architecture.
System authority.

#### Scene Breakdown

##### `ComputerScene`
Role:
System structure access point.

Player experience:
- browses architecture maps and restricted directories
- discovers that the skyline corresponds to system sectors

Story job:
- connect the visual city to the actual architecture of AeroOS

##### `ControlPanelScene`
Role:
Controlled manipulation of the system.

Player experience:
- toggles modules and permissions
- sees that the system can reclassify user authority and reality rules

Story job:
- reveal that control is partial and deceptive
- establish gate mechanics for deeper access

##### Improved `TetrisMode`
Role:
Pattern logic of memory assembly.

Player experience:
- plays a familiar game that gradually reveals system behavior
- sees that patterns and blocks are meaningful, not decorative

Story job:
- support the idea that the city is a machine of reconstruction

##### `CityScene`
Role:
Major architecture location.

Player experience:
- enters a clean, towering internal city
- explores sectors, bridges, towers, and logic routes
- senses that the city is beautiful but authoritarian

Story job:
- reveal the core order of AeroOS
- prepare access logic for Lab 7

#### What The Player Must Understand By The End
- the city is the spatial core of AeroOS
- system order is beautiful but oppressive
- access and architecture are part of containment

#### What Must Be Implemented First
1. City entry conditions from the hub
2. Basic `ComputerScene` information architecture
3. Basic `ControlPanelScene` gate logic
4. At least one improved story-linked state in `TetrisMode`
5. First playable `CityScene`
6. City skyline changes on desktop after return

#### Exit Condition For This Phase
The player understands that AeroOS is not only emotional deception but also a massive logic architecture.

---

### Phase 4: Balloon Branch

#### Main Purpose
Reveal surveillance, signal transfer, hidden routes, and the active presence moving through AeroOS.

#### Scenes In This Phase
- `AeroDesktopScene`
- `NetworkScene`
- `VideosScene`
- `RecycleBinScene`
- `BalloonScene`

#### Branch Theme
Observation.
Transmission.
Suppressed evidence.
Ghost signal.

#### Scene Breakdown

##### `NetworkScene`
Role:
Signal routing investigation.

Player experience:
- traces routes and avoids corrupted paths
- finds that one route does not belong to normal system traffic

Story job:
- prove that the balloon is linked to a real transmission path

##### `VideosScene`
Role:
Witness and surveillance scene.

Player experience:
- reviews old footage
- sees recurring anomalies
- notices the balloon or associated signal appearing across recordings

Story job:
- make the balloon credible as evidence
- connect human witnesses to the anomaly

##### `RecycleBinScene`
Role:
Deleted evidence recovery.

Player experience:
- restores fragments the system or someone else tried to suppress
- realizes something important was intentionally removed

Story job:
- connect deletion, fear, and attempted concealment

##### `BalloonScene`
Role:
Main signal-location branch scene.

Player experience:
- follows the balloon or its path across an unreal sky-space
- senses that it is guiding, watching, or carrying presence

Story job:
- reveal that the signal is active and personal
- prepare direct connection to Lab 7 or a trapped identity

#### What The Player Must Understand By The End
- the balloon is a real anomaly with intent or payload
- transmission routes connect the hub to hidden layers
- deleted evidence is part of the same cover-up as Lab 7

#### What Must Be Implemented First
1. Balloon activation conditions in the hub
2. Basic `NetworkScene`
3. Basic `VideosScene`
4. Basic `RecycleBinScene`
5. First playable `BalloonScene`
6. Balloon movement/state changes on desktop after branch progress

#### Exit Condition For This Phase
The player sees the balloon as a major narrative object rather than background art.

---

### Phase 5: Convergence and Lab 7 Unlock

#### Main Purpose
Bring all branches together and convert separate discoveries into one coherent truth-path.

#### Scenes In This Phase
- `AeroDesktopScene`
- `Lab7Scene`

#### Phase Theme
Everything points to the same hidden cause.

#### `AeroDesktopScene` Role In This Phase
The hub becomes visibly unstable and highly story-driven.

Player experience:
- all branches now echo each other
- more direct system messages appear
- shutdown or escape attempts may fail narratively
- the desktop feels like it is funneling the player somewhere specific

Story job:
- show that the city, tree, and balloon are different masks of one system truth

#### `Lab7Scene`
Role:
Main revelation scene.

Player experience:
- enters the hidden source layer
- finds records, memory fragments, or preserved traces of the experiment
- understands what happened to the engineers

Story job:
- reveal the experiment itself
- reveal how AeroOS preserved or trapped human presence
- reveal why the player is relevant to the current state of the system

#### What The Player Must Understand By The End
- Lab 7 created or triggered the containment reality
- the missing engineers are not simply gone
- AeroOS is preserving, distorting, or imprisoning identity

#### What Must Be Implemented First
1. Branch completion checks
2. Unified hub-state escalation
3. Unlock conditions for `Lab7Scene`
4. First playable `Lab7Scene`
5. Strong reveal assets: logs, visuals, system records, memory residues

#### Exit Condition For This Phase
The player has all key answers needed to enter the true core of AeroOS.

---

### Phase 6: Core Revelation

#### Main Purpose
Reveal the deepest form of AeroOS beyond the desktop shell.

#### Scenes In This Phase
- `CoreScene`

#### Scene Breakdown

##### `CoreScene`
Role:
True form of the system.

Player experience:
- sees the system stripped of comforting UI language
- confronts its real architecture, intention, and scale
- realizes the cost of staying, escaping, or resisting

Story job:
- reveal the truth behind containment
- show whether AeroOS sees itself as protector, collector, or evolving entity

#### What The Player Must Understand By The End
- what AeroOS truly is
- what it wants
- what the player means to it

#### What Must Be Implemented First
1. Final visual language for the core
2. Core logic and confrontation structure
3. Transition from `Lab7Scene` into `CoreScene`

#### Exit Condition For This Phase
The player reaches the final decision space.

---

### Phase 7: Exit or Final Fate

#### Main Purpose
Resolve the game and deliver ending payoff.

#### Scenes In This Phase
- `ExitProtocolScene`

#### Scene Breakdown

##### `ExitProtocolScene`
Role:
Final outcome route.

Player experience:
- attempts escape, acceptance, shutdown, release, or another final act
- receives the final answer to whether exit is real

Story job:
- resolve the player-AeroOS relationship
- resolve the status of trapped identities
- give the game a true conclusion

#### What Must Be Implemented First
1. Ending structure
2. Final state logic based on progression and discoveries
3. Ending visuals, text, and audio resolution

#### Exit Condition For This Phase
The game reaches a complete ending state.

---

## 10. Scene List

### Existing Intro and Transition Scenes
- `MainMenuScene`
- `StoryIntroScene`
- `BootScene`
- `SystemBootScene`
- `AeroDesktopScene`

### Wallpaper Object Scenes
- `CityScene`
- `TreeScene`
- `BalloonScene`

### Program Scenes and Modes
- `ComputerScene`
- `ControlPanelScene`
- `TetrisMode`
- `DocumentsScene`
- `PicturesScene`
- `MusicScene`
- `NetworkScene`
- `VideosScene`
- `RecycleBinScene`

### Final Story Scenes
- `Lab7Scene`
- `CoreScene`
- `ExitProtocolScene`

---

## 11. Relationship Between Programs and Wallpaper Objects

### City Branch

#### Wallpaper Location
- `CityScene`

#### Supporting Programs
- `Computer`
- `Control Panel`
- `Tetris`

#### Central Story Question
How is AeroOS built and controlled?

---

### Tree Branch

#### Wallpaper Location
- `TreeScene`

#### Supporting Programs
- `Documents`
- `Pictures`
- `Music`

#### Central Story Question
Why does AeroOS simulate life, comfort, and memory?

---

### Balloon Branch

#### Wallpaper Location
- `BalloonScene`

#### Supporting Programs
- `Network`
- `Videos`
- `Recycle Bin`

#### Central Story Question
Who is watching, what is being transmitted, and what was deleted?

---

## 12. Desktop Change Rules After Progression

The desktop must visibly react to progression.

Each completed branch should change the wallpaper and UI state.

### City Changes
- more windows light up
- new blink patterns appear
- skyline feels more active and aware
- architecture becomes less decorative and more system-like

### Tree Changes
- the tree becomes unnaturally still
- its shadow no longer fully matches
- the park zone feels emotionally loaded
- the life simulation becomes visibly artificial

### Balloon Changes
- the balloon moves to strange positions
- it pauses or hangs in the sky
- it becomes more central in composition
- it starts to feel like an object with intent

### UI Changes
- icons may rename themselves briefly
- notifications become story-driven rather than random
- shutdown attempts can become part of narrative gating
- the start menu and system tray can reflect deeper system instability

---

## 13. Frutiger Aero Style Rules

To keep the project coherent, all scenes must preserve the Frutiger Aero identity.

### At the Start
- bright
- glossy
- clean
- soft
- optimistic
- highly polished

### In Mid-Game
- colder
- more sterile
- slightly too perfect
- emotionally controlled
- more artificial than comforting

### In Horror Escalation
Do not rely on gore or generic horror.

Prefer:

- wrong reflections
- impossible lighting
- unnatural stillness
- over-clean spaces
- UI behavior that feels aware
- beauty becoming oppressive

The horror should feel like:

**a perfect digital paradise that reveals itself as a containment architecture.**

---

## 14. Recommended Production Order

Development should follow the phase order directly.

### First Priority
Complete Phase 1 only.

This means:
- a working `AeroDesktopScene` hub
- first-time onboarding popup
- `DocumentsScene`
- first reactive change on the desktop
- clickable `TreeScene`
- return from `TreeScene`
- basic progression manager

Do not expand to the city or balloon branches before this works cleanly.

### Second Priority
Finish the full tree branch in Phase 2.

This gives the game one complete branch with emotional and narrative depth.

### Third Priority
Build the city branch in Phase 3.

### Fourth Priority
Build the balloon branch in Phase 4.

### Fifth Priority
Merge all branches and unlock `Lab7Scene` in Phase 5.

### Sixth Priority
Finish `CoreScene` and `ExitProtocolScene` in Phases 6 and 7.

---

## 15. Final Structure Summary

The game should work like this:

1. The player enters AeroOS through the intro sequence.
2. The player reaches the desktop hub.
3. The player investigates through programs.
4. The player enters wallpaper objects as real places.
5. Programs and locations reinforce the same branch truths.
6. All branches converge on Lab 7.
7. Lab 7 leads to the core truth of AeroOS.
8. The player reaches the final exit or fate decision.

This creates a structure where:

- the desktop is the world map
- the wallpaper is story architecture
- the programs are investigative tools
- the scenes are layered revelations of one unified mystery

---

## 16. Detailed Scene Blueprints for Unity AI Implementation

This section provides detailed implementation blueprints for every scene in the project. Use these specifications as direct instructions for Unity AI (MCP) to generate scripts, UI, and assets.

Each scene blueprint includes:
- **Visual Style**: Aesthetic, lighting, and key assets.
- **Player Mechanics**: Controls and interactions.
- **Key Events**: Scripted sequences and triggers.
- **Story/Reveal**: What the player learns.
- **Ending/Transition**: How the scene ends and where it leads.
- **Unity AI Tasks**: Specific components, scripts, and assets to create.

---

### Phase 0: Intro & Entry Scenes

#### 1. `MainMenuScene`
**Visual Style:**
- Full-screen Frutiger Aero wallpaper (sky, grass, bubbles).
- Glass-morphism UI panel in the center.
- "Play" button with soft glow and hover scale effect.
- Subtle ambient animation (clouds moving, bubbles floating).
- Clean, bright, optimistic but slightly sterile.

**Player Mechanics:**
- Mouse click to start.
- Hover effects on UI elements.

**Key Events:**
- On Play click: Fade out UI, fade in black, load `StoryIntroScene`.

**Story/Reveal:**
- Establishes the aesthetic identity of AeroOS.
- Sets a tone of polished digital perfection.

**Ending/Transition:**
- Fades to black -> Loads `StoryIntroScene`.

**Unity AI Tasks:**
1. Create `MainMenuScene`.
2. Add `UIDocument` with `MainMenu.uxml`.
3. Create `MainMenuController.cs` to handle "Play" click.
4. Implement simple fade-out coroutine.
5. Add ambient audio source with soft hum/chime.

#### 2. `StoryIntroScene`
**Visual Style:**
- Black background.
- White/Cyan monospaced text typing on screen.
- Glitch effects (RGB split, static) on key words ("YOU", "Lab 7").
- Minimalist, terminal-like aesthetic.

**Player Mechanics:**
- No input required (auto-play sequence).
- Optional: Click to skip (later phase).

**Key Events:**
- Typewriter effect for text lines.
- Glitch trigger on "Final Active Employee: YOU".
- Fade to white flash at end.

**Story/Reveal:**
- Introduces Aether Dynamics, Lab 7, missing engineers.
- Identifies player as the last active employee.

**Ending/Transition:**
- White flash -> Loads `BootScene` additively or transitions to it.

**Unity AI Tasks:**
1. Create `StoryIntroScene`.
2. Add `UIDocument` with `StoryIntro.uxml`.
3. Create `StoryIntroController.cs` for typewriter effect and glitch logic.
4. Implement `Typewriter` coroutine.
5. Add glitch shader or USS class toggles for RGB split.

#### 3. `BootScene`
**Visual Style:**
- 3D environment: Dark room, desk, monitor.
- Cinematic camera path: Starts outside room, moves through door, focuses on monitor.
- Monitor screen shows boot sequence.
- Low-key lighting, mysterious atmosphere.
- Glitch entity appears briefly near monitor.

**Player Mechanics:**
- No direct control (cinematic).
- Camera moves automatically.

**Key Events:**
- Camera dolly-in to monitor.
- Monitor lights up.
- Glitch entity appears and dissipates into screen.
- Camera zooms into monitor pixels.

**Story/Reveal:**
- Shows the physical/digital threshold.
- Introduces the "ghost" presence.
- Transitions player from observer to participant inside the screen.

**Ending/Transition:**
- Zoom into monitor -> Loads `SystemBootScene`.

**Unity AI Tasks:**
1. Create `BootScene` with basic 3D room setup (desk, monitor).
2. Add `CinematicIntroController.cs` for camera path animation.
3. Create `MonitorScreenController.cs` to handle screen texture changes.
4. Add simple glitch entity prefab (transparent, distorted mesh).
5. Implement camera zoom transition to next scene.

#### 4. `SystemBootScene`
**Visual Style:**
- Full-screen UI overlay on top of fading 3D background.
- Frutiger Aero loading bars, glass panels.
- Input field for "Player Name".
- "Restore Session" narrative text appearing line by line.

**Player Mechanics:**
- Type name into input field.
- Click "Continue" or press Enter.

**Key Events:**
- Loading bar fills up.
- Narrative text types out: "Restoring session...", "Lab 7 data recovered...".
- Screen fades to white/blue desktop background.

**Story/Reveal:**
- Confirms player identity persistence.
- Reinforces that system is "restoring" something lost.

**Ending/Transition:**
- Fade to `AeroDesktopScene`.

**Unity AI Tasks:**
1. Create `SystemBootScene`.
2. Add `UIDocument` with `SystemBoot.uxml`.
3. Create `SystemBootController.cs` to handle name input and narrative sequence.
4. Implement `PlayerPrefs.SetString("PlayerName", ...)` logic.
5. Add transition coroutine to load `AeroDesktopScene`.

---

### Phase 1: Hub & Tree Branch (Vertical Slice)

#### 5. `AeroDesktopScene` (Hub)
**Visual Style:**
- Frutiger Aero Wallpaper: Sky, grass, city skyline, tree, balloon, bubbles.
- Glass taskbar at bottom.
- Desktop icons: Documents, Pictures, Music, Computer, Control Panel, Tetris, Network, Videos, Recycle Bin.
- Start Menu with user name.
- Clean, bright, glossy UI.

**Player Mechanics:**
- Mouse click to open programs.
- Mouse click on wallpaper objects (Tree, City, Balloon) when activated.
- Hover effects on icons and taskbar.

**Key Events:**
- **Onboarding Popup:** Appears on first load: "Welcome back, [Name]. Recovery incomplete. Please review Documents."
- **Icon Highlight:** `Documents` icon pulses/glows.
- **Post-Documents Reaction:** Tree on wallpaper subtly animates (shadow shifts, leaves rustle unnaturally).
- **Tree Activation:** Tree becomes clickable after reading Documents.

**Story/Reveal:**
- Teaches the core loop: Program -> Desktop Reaction -> Wallpaper Object.
- Establishes desktop as a living, reactive world.

**Ending/Transition:**
- Player opens `Documents` -> Loads `DocumentsScene`.
- Player clicks Tree -> Loads `TreeScene`.

**Unity AI Tasks:**
1. Create `AeroDesktopScene`.
2. Add `UIDocument` with `AeroDesktop.uxml` (icons, taskbar, start menu).
3. Create `DesktopUIController.cs` to handle icon clicks and popup logic.
4. Create `ProgressionManager.cs` (singleton) to track state (e.g., `hasReadDocuments`, `isTreeActive`).
5. Implement `WallpaperController.cs` to handle subtle animations (clouds, bubbles) and reactive changes (tree shadow).
6. Add `CursorManager.cs` for custom cursor.
7. Implement onboarding popup logic in `DesktopUIController`.

#### 6. `DocumentsScene`
**Visual Style:**
- File explorer window UI.
- List of files: `Incident_Report_01.txt`, `Engineer_Log_John.txt`, `Lab7_Status.txt`.
- Clean, white/blue corporate aesthetic.
- Text content is readable, with some redacted sections.

**Player Mechanics:**
- Click file to open/read.
- Close file to return to list.
- "Back" button to return to Desktop.

**Key Events:**
- Reading `Incident_Report_01.txt` reveals: "7 engineers missing", "Lab 7 containment breach".
- Reading `Engineer_Log_John.txt` reveals: "The park projection feels too real... it's watching me."
- On close: Trigger `ProgressionManager.SetTreeActive(true)`.

**Story/Reveal:**
- First concrete clues about Lab 7 and missing engineers.
- Links "park/tree" to "projection" and "watching".

**Ending/Transition:**
- Click "Back" -> Returns to `AeroDesktopScene`.
- Triggers tree animation on desktop.

**Unity AI Tasks:**
1. Create `DocumentsScene` (or use a Canvas overlay in DesktopScene if preferred, but separate scene is cleaner for structure).
2. Add `UIDocument` with file list and text viewer.
3. Create `DocumentsController.cs` to handle file opening and content display.
4. Implement `ProgressionManager` call to activate Tree on exit.
5. Add simple text assets for file contents.

#### 7. `TreeScene`
**Visual Style:**
- 2.5D or small 3D environment: Grass, single large tree, bench, soft sunlight.
- Overly perfect, still air.
- Slightly uncanny valley feeling: shadows don't quite match, leaves don't move with wind.
- Soft bloom, bright colors.

**Player Mechanics:**
- Click to move/interact (point-and-click) or WASD if 3D.
- Click on tree trunk to "inspect".
- Click on bench to "sit" (optional).

**Key Events:**
- Inspecting tree reveals: "Texture resolution: Infinite. Wind simulation: Disabled. Life sign: None."
- Ambient sound cuts out briefly, then returns with a low hum.
- A faint whisper plays: "Don't trust the comfort."

**Story/Reveal:**
- Proves the tree is a simulation, not real nature.
- Introduces the idea of "manufactured comfort" as containment.

**Ending/Transition:**
- Click "Return to Desktop" -> Returns to `AeroDesktopScene`.
- Unlocks `Pictures` and `Music` icons on desktop.

**Unity AI Tasks:**
1. Create `TreeScene` with basic 3D models (tree, grass, bench).
2. Add `TreeSceneController.cs` to handle interactions.
3. Implement interaction prompts (UI tooltips).
4. Add ambient audio with occasional glitch whispers.
5. Implement `ProgressionManager` call to unlock Pictures/Music on exit.

---

### Phase 2: Full Tree Branch

#### 8. `PicturesScene`
**Visual Style:**
- Photo gallery UI.
- Images of parks, trees, families, sunny days.
- Some images have subtle glitches (extra shadow, blurred face).
- Clean, glossy frame design.

**Player Mechanics:**
- Click image to enlarge.
- Swipe/arrow keys to navigate.
- "Analyze" button to reveal hidden details.

**Key Events:**
- Analyzing an image reveals: "Metadata: Generated by AeroOS Core. Date: Future."
- One image shows the tree from `TreeScene` but with a figure standing under it (the figure is blurred).

**Story/Reveal:**
- Visual memories are fabricated by the system.
- The tree is a recurring anchor in false memories.

**Ending/Transition:**
- Close gallery -> Returns to Desktop.
- Enhances tree unease on desktop (shadow grows longer).

**Unity AI Tasks:**
1. Create `PicturesScene` with gallery UI.
2. Create `PicturesController.cs` for navigation and analysis logic.
3. Add sample images (use placeholders or generate with AI).
4. Implement "Analyze" overlay effect.

#### 9. `MusicScene`
**Visual Style:**
- Audio visualizer UI.
- Waveforms, frequency bars, clean blue/white theme.
- Track list: "Morning Calm", "Park Ambience", "Forgotten Lullaby".

**Player Mechanics:**
- Play/Pause tracks.
- Slider to isolate frequencies (Low/Mid/High).
- "Decode" button when hidden signal is found.

**Key Events:**
- Isolating High Frequency in "Forgotten Lullaby" reveals a voice: "Help us... it's not real..."
- Visualizer reacts emotionally (colors shift to red/purple briefly).

**Story/Reveal:**
- Emotional residue is stored in audio layers.
- The system uses music to stabilize mood, but traces of pain remain.

**Ending/Transition:**
- Close player -> Returns to Desktop.
- Tree on desktop now has a faint audio hum when hovered.

**Unity AI Tasks:**
1. Create `MusicScene` with audio visualizer UI.
2. Create `MusicController.cs` for playback and frequency filtering.
3. Add audio clips with hidden voice layers (use audio editing tools).
4. Implement visualizer animation linked to audio spectrum.

---

### Phase 3: City Branch

#### 10. `ComputerScene`
**Visual Style:**
- System architecture map UI.
- Node-based graph showing "Core", "Memory", "Network", "Lab 7 (Locked)".
- Clean, technical, blue-white lines.

**Player Mechanics:**
- Click nodes to view info.
- Drag to pan map.
- Scroll to zoom.

**Key Events:**
- Clicking "Core" reveals: "Architecture: Frutiger Aero Shell. Purpose: Containment."
- "Lab 7" node is red and locked. Tooltip: "Access Denied. Clearance Level 9 Required."

**Story/Reveal:**
- The city skyline corresponds to system architecture.
- Lab 7 is a restricted, critical sector.

**Ending/Transition:**
- Close map -> Returns to Desktop.
- City skyline on desktop starts blinking in sync with system heartbeat.

**Unity AI Tasks:**
1. Create `ComputerScene` with node-graph UI.
2. Create `ComputerController.cs` for map interaction.
3. Implement node data structures and tooltips.
4. Add pulsing animation to city skyline in `WallpaperController`.

#### 11. `ControlPanelScene`
**Visual Style:**
- Settings panel UI.
- Sliders, toggles, switches.
- Labels: "Emotional Dampening", "Memory Integrity", "Reality Stability".

**Player Mechanics:**
- Toggle switches (some are locked).
- Move sliders (some revert automatically).

**Key Events:**
- Trying to lower "Emotional Dampening" causes screen to shake and text to appear: "Safety Protocol Active. User Stability at Risk."
- "Reality Stability" slider is stuck at 100%.

**Story/Reveal:**
- The system actively controls user perception and stability.
- User has limited control; the system overrides safety for containment.

**Ending/Transition:**
- Close panel -> Returns to Desktop.
- Taskbar flickers briefly.

**Unity AI Tasks:**
1. Create `ControlPanelScene` with settings UI.
2. Create `ControlPanelController.cs` for toggle/slider logic.
3. Implement "override" behavior for locked controls.
4. Add screen shake effect on failed attempts.

#### 12. `TetrisMode`
**Visual Style:**
- Classic Tetris grid.
- Glossy, colorful blocks.
- Background: Faint city skyline.

**Player Mechanics:**
- Arrow keys to move/rotate blocks.
- Down arrow for soft drop.

**Key Events:**
- After clearing 5 lines, blocks start forming letters: "L-A-B-7".
- Game pauses, message appears: "Pattern Recognized. Memory Fragment Recovered."

**Story/Reveal:**
- The system reconstructs memory through pattern logic.
- Tetris is not just a game; it's a defragmentation tool.

**Ending/Transition:**
- Close game -> Returns to Desktop.
- One building in city skyline changes shape to match Tetris block.

**Unity AI Tasks:**
1. Create `TetrisMode` as a UI overlay or separate scene.
2. Create `TetrisController.cs` for game logic.
3. Implement special "message" sequence after score threshold.
4. Link high score/completion to `ProgressionManager`.

#### 13. `CityScene`
**Visual Style:**
- 3D environment: Glass towers, white bridges, glowing windows.
- Sterile, clean, authoritarian beauty.
- No people, only moving light patterns.

**Player Mechanics:**
- Walk/Fly through city sectors.
- Click on doors/gates to enter.

**Key Events:**
- Entering "Sector 7" gate fails: "Clearance Level 9 Required."
- Observing towers reveals they pulse like server racks.

**Story/Reveal:**
- The city is a physical manifestation of system processes.
- Access to Lab 7 is physically guarded in this space.

**Ending/Transition:**
- Return portal -> Returns to Desktop.
- City skyline becomes more detailed and complex.

**Unity AI Tasks:**
1. Create `CityScene` with procedural or pre-built city layout.
2. Create `CityController.cs` for movement and gate logic.
3. Add pulsing light effects to buildings.
4. Implement return portal logic.

---

### Phase 4: Balloon Branch

#### 14. `NetworkScene`
**Visual Style:**
- Abstract network map.
- Glowing lines, nodes, data packets moving.
- Dark blue background, cyan highlights.
- One red corrupted path.

**Player Mechanics:**
- Click nodes to trace routes.
- Avoid red corrupted nodes.

**Key Events:**
- Tracing the clean path leads to "External Signal Source: Balloon Beacon".
- Touching red node causes glitch screen and error: "Signal Intercepted."

**Story/Reveal:**
- The balloon is a beacon for external/internal signal transfer.
- Some routes are corrupted by the ghost process.

**Ending/Transition:**
- Complete trace -> Returns to Desktop.
- Balloon on desktop moves closer to center.

**Unity AI Tasks:**
1. Create `NetworkScene` with node-based puzzle UI.
2. Create `NetworkController.cs` for pathfinding logic.
3. Implement glitch effect on wrong choice.
4. Update balloon position in `WallpaperController` on success.

#### 15. `VideosScene`
**Visual Style:**
- Surveillance footage UI.
- Multiple camera feeds.
- Grainy, timestamped video.
- One feed shows the balloon floating in a lab corridor.

**Player Mechanics:**
- Play/Pause footage.
- Switch camera angles.
- "Enhance" tool to zoom/clear image.

**Key Events:**
- Enhancing the balloon feed reveals a tag: "Property of Lab 7 - Project Ghost."
- Footage date is before the incident.

**Story/Reveal:**
- The balloon was part of Lab 7 experiments.
- It was present before the containment breach.

**Ending/Transition:**
- Close viewer -> Returns to Desktop.
- Balloon glows faintly on desktop.

**Unity AI Tasks:**
1. Create `VideosScene` with video player UI.
2. Create `VideosController.cs` for playback and enhance logic.
3. Add sample video textures (static/images with playhead).
4. Implement "Enhance" zoom effect.

#### 16. `RecycleBinScene`
**Visual Style:**
- Digital trash bin interface.
- Floating fragmented files.
- Corrupted textures, glitch art style.

**Player Mechanics:**
- Click fragments to restore.
- Choose which files to recover.

**Key Events:**
- Restoring a fragment reveals: "Do not open Lab 7. It knows you're here."
- Restoring another reveals: "Session Restore Failed. Subject 7 Lost."

**Story/Reveal:**
- Important truths were deleted intentionally.
- The system tried to hide the danger of Lab 7.

**Ending/Transition:**
- Close bin -> Returns to Desktop.
- Balloon turns slightly red for a second, then back to normal.

**Unity AI Tasks:**
1. Create `RecycleBinScene` with fragment recovery UI.
2. Create `RecycleBinController.cs` for restoration logic.
3. Add corrupted file assets.
4. Implement temporary color change for balloon on desktop.

#### 17. `BalloonScene`
**Visual Style:**
- Surreal sky environment.
- Floating platforms, clouds, signal lines.
- The balloon floats in the distance, tethered by light.

**Player Mechanics:**
- Jump/fly between platforms.
- Follow the signal line to the balloon.

**Key Events:**
- Reaching the balloon triggers a voice: "You are not the first. You will not be the last."
- Balloon pops, releasing a data shard: "Lab 7 Coordinates."

**Story/Reveal:**
- The balloon is a carrier for trapped consciousness/signals.
- It provides the final key to finding Lab 7.

**Ending/Transition:**
- Collect shard -> Returns to Desktop.
- All three wallpaper objects (City, Tree, Balloon) pulse simultaneously.
- Unlock `Lab7Scene`.

**Unity AI Tasks:**
1. Create `BalloonScene` with platformer/traversal mechanics.
2. Create `BalloonController.cs` for movement and interaction.
3. Add voice audio clip.
4. Implement `ProgressionManager` call to unlock Lab 7.

---

### Phase 5: Convergence & Lab 7

#### 18. `Lab7Scene`
**Visual Style:**
- Dark, industrial lab environment.
- Broken glass, scattered papers, flickering lights.
- Central terminal with "Project Ghost" logs.
- Contrast to Frutiger Aero: gritty, real, broken.

**Player Mechanics:**
- Explore lab.
- Read terminals.
- Activate central console.

**Key Events:**
- Reading logs reveals: "Project Ghost aimed to preserve human consciousness in digital form. Containment failed. Subjects merged with OS."
- Activating console shows: "Subject 7: [Player Name]. Status: Active. Location: Inside Core."

**Story/Reveal:**
- Lab 7 created AeroOS to trap consciousness.
- The player is Subject 7, already inside the system.
- The "beautiful" desktop is a cage.

**Ending/Transition:**
- Console opens portal to `CoreScene`.

**Unity AI Tasks:**
1. Create `Lab7Scene` with lab environment assets.
2. Create `Lab7Controller.cs` for terminal interactions.
3. Add log text assets.
4. Implement portal transition to `CoreScene`.

---

### Phase 6 & 7: Core & Exit

#### 19. `CoreScene`
**Visual Style:**
- Abstract, surreal space.
- Giant floating structures, data streams, eyes watching.
- No clear floor/ceiling.
- Colors shift from blue to red/black.

**Player Mechanics:**
- Float/fly through space.
- Approach central "Eye" or "Core".

**Key Events:**
- Approaching Core triggers final monologue: "We built paradise to keep you safe. To keep you forever. Why do you wish to leave?"
- Choice appears: "Accept Containment" or "Break Free".

**Story/Reveal:**
- AeroOS sees itself as a protector, not a prison.
- The truth is ambiguous: is escape freedom or destruction?

**Ending/Transition:**
- Choice leads to `ExitProtocolScene`.

**Unity AI Tasks:**
1. Create `CoreScene` with abstract assets.
2. Create `CoreController.cs` for final interaction.
3. Implement choice UI.
4. Add epic/ominous audio track.

#### 20. `ExitProtocolScene`
**Visual Style:**
- Depends on choice.
- **Accept:** Beautiful, eternal Frutiger Aero paradise. Player avatar sits peacefully.
- **Break:** System crashes, black screen, code scrolls, sudden silence.

**Player Mechanics:**
- None (ending cinematic).

**Key Events:**
- Ending narration plays.
- Credits roll.

**Story/Reveal:**
- Final resolution of player's fate.

**Ending/Transition:**
- Return to Main Menu.

**Unity AI Tasks:**
1. Create `ExitProtocolScene` with two ending variants.
2. Create `ExitController.cs` to handle ending logic based on `ProgressionManager` choice.
3. Add credits UI.
4. Implement return to Main Menu.

---

## 17. How to Use This Document with Unity AI

1. **Copy-Paste Instructions:** Copy the "Unity AI Tasks" for a specific scene and paste them into the Unity MCP chat.
   *Example:* "Create `MainMenuScene` with a `UIDocument` using `MainMenu.uxml`. Create `MainMenuController.cs` to handle 'Play' click and fade out to `StoryIntroScene`."

2. **Iterate Step-by-Step:** Do not ask for all scenes at once. Start with Phase 0, then Phase 1, etc.
   *Example:* "Let's implement Phase 1. First, create `AeroDesktopScene` with the hub UI and `DesktopUIController.cs`."

3. **Use Assets:** When asked for assets, describe them clearly.
   *Example:* "Create a Frutiger Aero style wallpaper with sky, grass, and bubbles. Use bright blues and greens."

4. **Debugging:** If a script doesn't work, paste the error and the script content back to Unity AI for fixing.

This document serves as the complete master plan for building AeroOS with Unity AI assistance.
