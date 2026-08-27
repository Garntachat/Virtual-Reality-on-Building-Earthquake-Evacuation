# Start Here — CEVR 0.3.0

This repository is the complete Unity project. The tutorial scene is generated from source so the scene and builder must remain synchronized.

## First run or after pulling an upgrade

1. Open the repository root with Unity `6000.3.20f1`.
2. Wait for package import and script compilation to finish.
3. Stop if the Console contains a red error.
4. Select `Tools > CEVR > 1. Build Chula Engineering Tutorial Stage`.
5. Select **Build**. The newly generated scene remains open.
6. Select `Tools > CEVR > 2. Validate Open Tutorial Scene`.
7. Press Play only after validation passes.

An old generated scene cannot run silently: the game checks `GeneratedStageInfo` and displays a rebuild instruction when the committed scene is stale.

## Desktop controls

- `WASD`: move
- Mouse: look
- `E` or left click: grab/release an item or the pink chair
- Mouse wheel: move a held item nearer/farther
- `C` or left `Ctrl`: crouch
- `Esc`: release the cursor
- Left click: capture the cursor again
- `F12` or `Backspace`: emergency stop

Turn toward the brown strong table. The pink chair sits partly underneath it. Place the center `+` on the chair until **GRAB AND SLIDE CHAIR** appears, move it aside, crouch, and enter the cover zone.

## Before sharing or merging

Run `python3 Tools/verify_repo.py`, run all EditMode tests, validate the generated scene, and complete the desktop checklist in `docs/TEST_PLAN.md`. VR testing additionally requires the gates in `docs/XR_SETUP.md`.
