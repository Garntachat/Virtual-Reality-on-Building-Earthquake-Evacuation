# Play House now uses the editable New_House scene

**Play House** in the main menu loads `Assets/CEVR/Generated/Scenes/New_House.unity`, **not** the old `House.unity`. The original scene is retained as a backup (disabled in Build Settings, not deleted). This is a non-destructive replacement of the **playable scene selection**, not an overwrite of your existing House scene file.

## Why

The committed `New_House.unity` already contains 35 scene-saved Rigidbody components on furniture such as the dining chairs, fridge, wardrobe, books, lamps, tables and other items. You can click their GameObject in the Unity hierarchy in **Edit mode** and adjust the native **Mass**, **Linear Damping** and **Angular Damping** fields. The previous `House.unity` had no saved Rigidbody components; gameplay attached many of them during Play.

## Test after updating

1. Preserve local scene edits before updating Git. Reopen Unity with the latest `main`.
2. Open `Assets/CEVR/Generated/Scenes/New_House.unity` (do not keep editing the archived `House.unity`). Allow Unity to attach `InertialRigidbody` settings to existing physics objects once and save. Adjust an object's **root with Rigidbody**, not the decorative `Seat` mesh child unless it has its own Rigidbody.
3. Run `CEVR > Earthquake > Review saved Mass and Damping (current scene)` to see a full list. Edit the existing Rigidbody values **outside Play mode** and save with Cmd+S.
4. Press Play (the menu opens first), then choose **Play House**. The scene should load **New_House** and keep those scene-saved furniture objects.
5. Verify that the camera can be controlled with WASD/mouse, and test grabbing (E), crawling (Z), hazards, cover and evacuation. The New_House scene did not originally have a Camera; the House gameplay installer supplies one in Play mode without altering the saved furniture.
6. Stop Play, reopen **New_House**, and check that your Mass, Damping and location changes remain. If they do not, capture the first red Unity Console error before applying further changes.

The scene's original physics includes some fixed furniture as non-kinematic Rigidbodies, so a realistic physics result and the old House gameplay's alignment still require a manual Unity test. The repository's GitHub Actions check is a structural check, **not** a Unity compiler or Play-mode simulation.

Avoid using `CEVR > House > RESTORE Furniture Colors + Kitchen` on New_House; that command was written for the original House hierarchy. Your original `House.unity` remains available as a separate asset if you need to compare or revert.
