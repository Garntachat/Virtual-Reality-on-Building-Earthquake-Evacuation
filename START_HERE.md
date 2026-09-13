> Latest update: [authored furniture and simple 30-second training flow](docs/FURNITURE_AND_SIMPLE_PLAY.md). Lab tasks are optional in Training. Runtime/VR testing remains pending.

# Start Here — CEVR 0.5.0 Cross-Scene Gameplay Candidate

This repository is the complete Unity project. Version 0.5.0 supports both the engineering tutorial and the new hand-built house scene. Gameplay is installed safely at runtime, so the house geometry remains untouched.

## Fastest first run

1. Open the repository root with Unity `6000.3.20f1`.
2. Wait for package import and script compilation to finish.
3. Stop if the Console contains a red error.
4. Open either `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity` or `Assets/CEVR/Generated/Scenes/House.unity`.
5. Press Play.
6. Click inside the Game view, then turn 180 degrees toward the sturdy brown table.
7. Confirm that the HUD is readable, four pink chairs exist, and the marked primary chair is beside the table.

At startup, `RuntimeStageRepair` fixes the exact legacy-scene problems automatically: mirrored oversized wall text, mirrored desktop HUD, missing desktop grab component, missing chairs, and missing generated-stage version. `TutorialVisualPolish` then applies the final palette, balanced lighting, compact HUD, cover outline, chair beacon, exit chevrons, hazard boundary, ceiling panels, engineering workstations, safety equipment, and collider-free room decoration. Preparation happens before `GameFlowController` validates the scene.

## Optional persistent stage rebuild

To write the newest generated structure into the scene file instead of relying on runtime compatibility repair:

1. Stop Play mode.
2. Select `Tools > CEVR > 1. Build Chula Engineering Tutorial Stage` and select **Build**.
3. Select `Tools > CEVR > 2. Validate Open Tutorial Scene`.
4. Save the project after validation passes.

## Desktop controls

- `WASD`: move
- Mouse: look
- `E` or left click: wear shoes or grab/release an item, pillow, or chair
- Mouse wheel: move a held item nearer/farther
- `C` or left `Ctrl`: crouch
- `Z`: toggle crawl mode for moving under the table
- `T`: toggle first-person and third-person desktop view
- On-screen **THIRD-PERSON VIEW** button: press `Esc` to release the mouse, then click it
- `F2`: join/leave local split-screen Player 2 (`IJKL`, `U`/`O`, right `Shift` interact)
- `Esc`: release the cursor
- Left click: capture the cursor again
- `F12` or `Backspace`: emergency stop

The initial camera faces away from the cover table. Turn 180 degrees toward the brown strong table. The pink chair sits partly underneath it. Place the center `+` on the chair until **GRAB AND SLIDE CHAIR** appears, press `E` or left click, move it aside, and release it. Press `Z` to crawl, move beneath the table, and press `Z` again after leaving cover. The controller remains low automatically if the table blocks standing clearance.

In the house, use the 30-second preparation period to wear the shoes, locate the pillow, move the marked chair, and identify the sturdy table. Hold the pillow over your head or crawl under the table during shaking. Evacuate to the green marker only after the earthquake ends.

## Before sharing or merging

Run `python3 Tools/verify_repo.py`, run all EditMode tests, validate the generated scene, and complete the desktop checklist in `docs/TEST_PLAN.md`. VR testing additionally requires the gates in `docs/XR_SETUP.md`.
