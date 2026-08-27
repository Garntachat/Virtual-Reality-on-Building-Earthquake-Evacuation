# Unity Setup and Stage-Build Tutorial

Follow every gate in order. Do not continue after a failed gate.

## A. Prepare the workstation

1. Install Git and Git LFS.
2. Run `git lfs install` once on the workstation.
3. Install Unity Hub.
4. Install Unity Editor `6000.3.20f1`.
5. Add Android Build Support, SDK, NDK, and OpenJDK for a standalone Android headset, or the relevant desktop module for PCVR.
6. Clone the repository with `git clone <repository-url>`.
7. Enter the repository and run `git lfs pull`.
8. Run `python3 Tools/verify_repo.py`; it must report `PASS`.

## B. Open the project for the first time

1. In Unity Hub, select **Add project from disk**.
2. Select the repository root, not the `Assets` folder.
3. Confirm that the version matches `ProjectSettings/ProjectVersion.txt`.
4. Open the project and wait for all package imports to finish.
5. Open `Window > General > Console`.
6. Do not continue if any compilation error remains.

**Gate B:** the Console has zero errors and Package Manager resolves Input System, OpenXR, XR Interaction Toolkit, Test Framework, and UGUI.

## C. Immediate desktop launch

1. Open `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity`.
2. Press Play.
3. Click inside the Game view to capture the pointer.
4. Confirm that the HUD is readable and no oversized mirrored wall text blocks the view.
5. Turn 180 degrees and confirm that the bright pink chair is beside the sturdy brown table.

The committed legacy scene is repaired in memory before tutorial validation. This compatibility path creates the missing chair and desktop grab component and corrects the HUD/text. It does not modify the scene asset on disk.

**Gate C:** Play mode begins without a stale-scene failure; the HUD is readable; the pink chair is visible after turning around; and aiming at it shows **GRAB AND SLIDE CHAIR**.

## D. Optional persistent stage rebuild

1. Select `Tools > CEVR > 1. Build Chula Engineering Tutorial Stage`.
2. Read the replacement warning and select **Build**.
3. Open `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity`.
4. Select `Tools > CEVR > 2. Validate Open Tutorial Scene`.
5. Save the project and inspect `git status`.

**Gate D:** the `GeneratedStageInfo` version is current; exactly one GameFlowController, GroundMotionPlayer, active PlayerHealth, AudioListener, and ExitAssemblyZone exist; exactly two TutorialTasks, two TaskItems, five FallingHazards, and at least one MovableFurniture exist. No camera is parented under an inertial object.

## E. Desktop smoke test

1. Press Play.
2. Click the Game view to capture the cursor. Use the mouse to look, `WASD` to move, `E` or left click to grab/drop the object at the center `+`, the mouse wheel to adjust hold distance, and `C` or `Ctrl` to crouch.
3. Turn toward the brown strong table and its pink chair. Aim until **GRAB AND SLIDE CHAIR** appears, grab it, move it sideways, and release it. Confirm it slides on the floor without being lifted.
4. Move both task objects into their green goals, or intentionally wait for the watchdog test.
5. Confirm that the earthquake does not start before 30 seconds.
6. During shaking, enter the strong-table cover zone and compare damage with an uncovered run.
7. Enter the assembly point while shaking; the run must not succeed.
8. After shaking ends, enter the assembly point; the run must reach Success and Debrief.
9. Start another run and press `F12` or `Backspace`; motion, hazards, and logging must stop.
10. Locate logs under `Application.persistentDataPath/CEVRLogs` and confirm `furniture_displaced` appears after moving the chair.

**Gate E:** chair movement, success, early-exit rejection, cover protection, and emergency stop work without exceptions. The camera does not shake.

## F. Automated tests

1. Open `Window > General > Test Runner`.
2. Select EditMode.
3. Select **Run All**.
4. Confirm that rule timing, evacuation success, failure conditions, quake interpolation, validity, and health tests pass.
5. Attach a screenshot of the result to the pull request.

## G. Safe editing rules

- Put reusable systems in `Assets/CEVR/Runtime`.
- Put generation and import tooling in `Assets/CEVR/Editor`.
- Prefer editing `ChulaTutorialStageBuilder.cs` over hand-editing the generated scene.
- If a generated scene is hand-edited, document the reason because rebuilding replaces it.
- Do not combine art changes with package, physics timestep, or XR-provider changes.
- Commit every asset with its `.meta` file.
- Track binary models, textures, audio, and video through Git LFS.

## Common problems

| Symptom | Check | Resolution |
|---|---|---|
| CEVR menu is missing | Compilation errors | Resolve all errors; Editor scripts do not load after a failed compile |
| Play mode says the generated scene is stale | Runtime repair script is missing or did not compile | Confirm `RuntimeStageRepair.cs` exists and clear all Console errors; a persistent rebuild is optional after Play works |
| VR object cannot be grabbed | XRI package, actions, interactors | Complete `XR_SETUP.md`, then rebuild the stage |
| Object passes through floor | Collider and collision mode | Use primitive or convex colliders and Continuous collision detection |
| HUD is missing or mirrored | Runtime repair did not execute | Clear Console errors and confirm the project is version 0.3.1; then rebuild and validate if a persistent scene update is desired |
| Research mode refuses to start | Preview profile still active | Import and assign a valid recorded QuakeProfile |
| References disappear for another teammate | Missing `.meta` files | Recover the original GUID from Git; do not generate unrelated replacement metadata |
