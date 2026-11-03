# Edit the Tutorial exactly as it appears at the beginning of Play mode

Open `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity` in Unity **6000.3.20f1** in **Edit mode**. Close other loaded scenes.

When you open the original, unbaked tutorial scene, CEVR prepares its playable layout **once**, writes the generated meshes and materials into `Assets/CEVR/Generated/TutorialBake_*`, and **saves the .unity scene**. The furniture, visual polish, furniture replacements, authored safety-shoe models, props, window exteriors, lighting and markers that were previously built before/at Play mode are now ordinary, selectable hierarchy objects. This happens on first opening or after script compilation. If the scene was already baked, NOTHING is regenerated.

Look in the Hierarchy for:

- `Tutorial Editable Furniture` — sofas, desks, decoration and other generated models.
- `DynamicProps > MovableChair_*` — four grabbable chair ROOT objects.
- `SturdyCoverTableTop`, `UnsecuredTallCabinet` and the original scene environment — their baked visual child models.
- `Tutorial Editable Props` — shoe props (the pillow may be a separate root).
- `CEVR_FinalVisualPolish` — exit path, safe-zone visuals, additional lights and detailed decoration.

Select and move the **root object with the interaction/collider components**, not just its mesh child. If you move the table, adjust `CoverZone_StrongTable` and its visual indicator with it. Editing a transform while **in Play mode** is temporary; edit in the Scene view **outside Play mode**, then save with Cmd/Ctrl+S.

## How to verify the edit is preserved

1. After Unity finishes importing, choose `CEVR > Tutorial > Check saved editable furniture`. It must show the saved dressing and four chairs. If no saved dressing is reported, run `CEVR > Tutorial > Make furniture editable (one time)`.
2. Expand the four chairs and `Tutorial Editable Furniture`. They must have visible renderers and preserved colliders. Move one chair a small, obvious distance in Edit mode, then **save**.
3. Press Play. The chair must stay at your edited position, without duplicates or another scale change. Test E/left-click grabbing, Z crawling under the table, shoe/pillow interaction, shaking, cover detection, and broken windows.
4. Stop Play, reopen the tutorial scene and press Play again. The chair's new position and furniture size should remain unchanged after both runs. Undo your test position if desired, and save again.
5. Commit both the changed `.unity` scene and **all** new `TutorialBake_*` files **with their `.meta` files**. Your teammate must pull these assets to see exactly the same editable scene.

The first bake creates a timestamped `*_BeforeEditableBake_*.unity` scene backup **from the actual scene currently in memory**, including unsaved changes. A scene that is already baked is never rebuilt. On failure, do **not** save the partial scene: reopen the original or the backup and check the Console.

**Never run `Tools > CEVR > 1. Build Chula Engineering Tutorial Stage` after making custom edits.** That original scene generator replaces the scene and will erase your hand-authored furniture placement. Use the `CEVR > Tutorial` tools instead.

## What stays dynamic

The saved scene represents the **initial furnished room** of Play mode, not an earthquake already in progress. Per-item Rigidbody Mass and Shake Strength settings are editable in the Inspector. Gameplay controllers, split-screen player, audio, HUD updates, physically falling furniture, and later earthquake effects still run at play time. Runtime dressing recognizes `savedInEditor` and skips re-scaling or re-creating saved furniture.

This change can only be fully verified in Unity Editor on a machine with the project and imported models. Passing GitHub static checks is not a Unity compilation, play-test or VR-headset test.
