# Start Here — CEVR 0.3.1

This repository is the complete Unity project. Version 0.3.1 can play the committed tutorial scene immediately; it repairs an older generated scene in memory before tutorial validation.

## Fastest first run

1. Open the repository root with Unity `6000.3.20f1`.
2. Wait for package import and script compilation to finish.
3. Stop if the Console contains a red error.
4. Open `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity`.
5. Press Play.
6. Click inside the Game view, then turn 180 degrees toward the sturdy brown table.
7. Confirm that the HUD is readable and a bright pink chair is beside the table.

At startup, `RuntimeStageRepair` fixes the exact legacy-scene problems automatically: mirrored oversized wall text, mirrored desktop HUD, missing desktop grab component, missing pink chair, and missing generated-stage version. The repair is idempotent and happens before `GameFlowController` validates the scene.

## Optional persistent stage rebuild

To write the newest generated structure into the scene file instead of relying on runtime compatibility repair:

1. Stop Play mode.
2. Select `Tools > CEVR > 1. Build Chula Engineering Tutorial Stage` and select **Build**.
3. Select `Tools > CEVR > 2. Validate Open Tutorial Scene`.
4. Save the project after validation passes.

## Desktop controls

- `WASD`: move
- Mouse: look
- `E` or left click: grab/release an item or the pink chair
- Mouse wheel: move a held item nearer/farther
- `C` or left `Ctrl`: crouch
- `Esc`: release the cursor
- Left click: capture the cursor again
- `F12` or `Backspace`: emergency stop

The initial camera faces away from the cover table. Turn 180 degrees toward the brown strong table. The pink chair sits partly underneath it. Place the center `+` on the chair until **GRAB AND SLIDE CHAIR** appears, press `E` or left click, move it aside, release it, crouch, and enter the cover zone.

## Before sharing or merging

Run `python3 Tools/verify_repo.py`, run all EditMode tests, validate the generated scene, and complete the desktop checklist in `docs/TEST_PLAN.md`. VR testing additionally requires the gates in `docs/XR_SETUP.md`.
