# Condo gameplay: editable scene and House-style earthquake tutorial

The existing `Assets/CEVR/Generated/Scenes/Condo.unity` is preserved, including its architectural mesh, prefabs, materials and Main Camera. **Do not rebuild or replace the condo with the House geometry.** The new scene-specific gameplay layout is placed near the Condo scene's existing camera at approximately (0, 1, -10); House world coordinates do not transfer to Condo.

## First open in Unity 6000.3.20f1

1. Pull the GitHub update, stop Play mode and open **only** `Assets/CEVR/Generated/Scenes/Condo.unity`.
2. The editor automatically creates a one-time **Condo Gameplay** root, backs up the original scene, builds the visible gameplay props and saves the scene. If it is absent, select **CEVR > Condo > Prepare editable gameplay (once)**.
3. Expand **Condo Gameplay** in the Hierarchy. The four movable chairs, sturdy table, cover-zone trigger, protective pillow, two safety-shoe pairs, cabinet, overhead hazards, player spawn and green assembly marker are editable objects. The original building objects remain where they were.
4. Select a **chair root** (not its mesh child). Under the **Inertial Rigidbody** component, adjust **Mass (kg)** or **Shake Strength** in the Inspector. Move that root in Scene view and save with Cmd/Ctrl+S. To relocate the whole training setup, move its parent root carefully; to relocate the safe table, move its parent table including the cover trigger.
5. Press Play from Main Menu and select **Play Condo**. The shared House-style scenario has 30 seconds preparation, 20 seconds of shaking, falling hazards and 60 seconds to reach the assembly marker. Test WASD/mouse, E for chairs/shoes/pillow, Z crawl, T third-person, F2 local player, and F12 abort.
6. Stop Play, reopen Condo, and repeat Play to check that the edited chairs, masses and shake strengths are unchanged and no duplicate furniture appears.
7. Commit the changed `Condo.unity` and all `Assets/CEVR/Generated/CondoBake_*` files **with .meta files** so teammates see the same Edit-mode scene.

The default starting positions are only a **prototype layout** because this repository's Condo geometry has different dimensions and interior obstacles from House. Inspect the Play-mode position of each object in your actual Unity scene, especially the cover area, cabinet, ceiling hazards, player spawn and evacuation marker, and adjust them in Edit mode. The green assembly marker is a prototype evacuation target; its default position has not been verified to be outdoors. The invisible safety-floor collider provides a small stable training area without editing imported architecture. Move or resize it in the Inspector if it interferes with your actual floor.

## Main Menu

The default menu now has **Play Tutorial**, **Play House**, **Play Condo** and **Exit**. `Condo.unity` is enabled in Unity build settings. If you use the editor-authored Canvas rather than the default fallback menu, run **CEVR > Main Menu > Open editable UI** once to add the button to an existing Canvas without deleting your other UI elements.

## Limitations

The editor bake persists the **initial** furniture positions and meshes. Animations, earthquake progression, HUD information and physics are gameplay effects and change only during Play. GitHub static checks cannot compile or simulate Unity here. Manual Unity verification remains necessary before claiming that the condo placement is physically correct or playable without collisions.
