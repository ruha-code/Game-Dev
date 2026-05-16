# AeroOS Project Overview

## 1. Project Description
AeroOS is a psychological horror experience set within a simulated operating system environment. The project leverages the "Frutiger Aero" aesthetic—characterized by glossy textures, water motifs, and vibrant blues—to create a sense of nostalgic comfort that gradually decays into a surreal, glitch-filled nightmare. The core experience centers on environmental storytelling through a desktop interface, where the player interacts with a seemingly benign OS that begins to exhibit anomalous and haunting behavior.

**Core Pillars:**
*   **Aesthetic Subversion:** Using the clean, optimistic Frutiger Aero design language to mask horror elements.
*   **Operating System Meta-Fiction:** The entire gameplay loop takes place within the confines of a computer screen.
*   **Environmental Storytelling:** Narrative progression through OS anomalies, file discovery, and UI shifts.

## 2. Gameplay Flow / User Loop
1.  **Boot Sequence:** The user begins at the `MainMenuScene`, transitioning into the `SystemBootController` logic which simulates a hardware startup and OS initialization.
2.  **User Identification:** The user provides a "PlayerName" which is persisted via `PlayerPrefs` to personalize the "haunting" experience.
3.  **Desktop Interaction:** The player enters the `AeroDesktopScene`. Here, they interact with desktop icons, a functional taskbar, and a start menu managed by `DesktopUIController`.
4.  **Anomaly Induction:** As the player spends time on the desktop, the `DesktopController` and `ScreamerController` trigger "Anomalies"—procedural glitches, audio distortions, and visual hallucinations.
5.  **Horror Escalation:** The experience transitions from subtle UI glitches (shifting icons, clock flickering) to overt horror elements managed by `HallucinationPresence` and `ScreamerController`.

## 3. Architecture
The project follows a **Manager-Controller** pattern with a central `GameManager` acting as a persistent singleton for global state and scene management. Scene-specific logic is encapsulated in "Controllers" (e.g., `DesktopController`, `SystemBootController`) that handle local system orchestration.

*   **Global Management:** `GameManager` handles master settings and scene transitions.
*   **Decoupled Systems:** Horror elements and UI are separated; the UI triggers events that horror systems can listen to, or horror systems independently manipulate the UI via class manipulation.
*   **Event-Driven UI:** Uses the Unity UI Toolkit (UITK) with a CSS-like styling approach (USS) where state changes are driven by adding/removing class names (e.g., `AddToClassList("hidden")`).

**Location:** `Assets/Scripts/`

## 4. Game Systems & Domain Concepts

### OS Simulation System
Handles the visual and interactive representation of the AeroOS desktop environment, including parallax backgrounds and animated elements.
*   `DesktopController`: Manages desktop parallax layers, cloud movement, and random anomaly triggering.
*   `WallpaperController`: Controls the desktop background and potential visual shifts.
*   `MonitorFlicker`: Simulates hardware-level screen instability.
*   `CursorManager`: Customizes the software cursor to match the OS aesthetic.

**Location:** `Assets/Scripts/`

### Horror & Anomaly System
A collection of systems designed to trigger psychological horror events and visual/audio glitches.
*   `ScreamerController`: Handles high-intensity horror triggers (jump scares).
*   `HallucinationPresence`: Manages subtle environmental changes and "ghost" elements in the OS.
*   `BackgroundEffects`: Implements full-screen shaders or UI-based glitch overlays.

**Location:** `Assets/Scripts/`

### Audio System
Combines traditional SFX playback with procedural generation to create an unsettling atmosphere.
*   `ProceduralAudioGenerator`: Generates dynamic, non-repeating audio artifacts and hums.
*   `CinematicAudioController`: Orchestrates scripted audio sequences for intro/outro events.

**Location:** `Assets/Scripts/`

## 5. Scene Overview
*   **MainMenuScene:** The entry point where the initial game state is established.
*   **AeroDesktopScene:** The primary gameplay environment. It hosts the OS simulation, desktop icons, and is the stage for most horror anomalies.
*   **Cinematic/Intro Scenes:** Handled by `StoryIntroController` and `CinematicIntroController` to set the narrative context before reaching the desktop.

## 6. UI System
The project uses the **Unity UI Toolkit (UITK)** for all OS-related interfaces, providing a responsive and style-sheet-driven UI.
*   **Structure:** UI is defined in `.uxml` files and styled with `.uss`.
*   **Binding Logic:** `DesktopUIController` queries elements by name or class (e.g., `root.Q<Button>("start-button")`) and registers C# callbacks.
*   **State Management:** Visual states (visible, hidden, glitched) are toggled using USS class manipulation, allowing for smooth transitions via CSS transitions.
*   **Modification:** To add a new OS app or icon, add the element to the `UIDocument` and register its click event in `DesktopUIController.OnEnable`.

**Location:** `Assets/Scripts/DesktopUIController.cs`, `Assets/UI/` (Assumed UI folder)

## 7. Asset & Data Model
*   **Persistence:** Player data (like `PlayerName`) and game progress are stored using `PlayerPrefs`.
*   **Visual Assets:** High-gloss textures and "glass" materials (e.g., `AeroDesktop_BlueGlass.mat`) define the Frutiger Aero look.
*   **Input:** Uses the **New Input System** (`InputSystem_Actions.inputactions`) for modern peripheral support.
*   **Organization:** Scripts are centrally located in `Assets/Scripts`, while visual assets are categorized in `Materials`, `Textures`, and `UI`.

## 8. Notes, Caveats & Gotchas
*   **Class-Based UI:** The UI relies heavily on specific USS class names. Removing or renaming classes in the `.uss` files will break the `SystemBootController` and `DesktopUIController` visual transitions.
*   **Mouse Dependency:** `DesktopController` parallax logic requires `UnityEngine.InputSystem.Mouse.current`. If the mouse is not detected, parallax will fail silently.
*   **Singleton Pattern:** `GameManager` creates itself if not present in the scene, which is convenient for testing but requires caution during initialization to ensure all global systems are ready.
*   **Anomaly Randomization:** The `AnomalyRoutine` in `DesktopController` is a infinite coroutine; ensure it is properly stopped if the desktop environment needs to be "frozen" for cinematic events.