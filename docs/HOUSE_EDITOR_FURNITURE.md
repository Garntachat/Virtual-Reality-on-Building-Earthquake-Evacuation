# Editable House furniture

The original House scene saved only ProBuilder architecture. Furniture was generated during Play Mode; stopping Play removed it.

## First capture

1. Open `Assets/CEVR/Generated/Scenes/House.unity`, outside Play Mode.
2. Choose **CEVR > House > Save editable furniture into scene**.
3. The editor enters Play briefly, captures the initial furniture, then exits automatically.
4. Expand **House_EditableFurniture** in the Hierarchy. Move and rotate these objects in Edit Mode.
5. Save the scene with Cmd/Ctrl+S. Commit the scene and the newly created `Assets/CEVR/Generated/EditableHouse_*` folder, including all `.meta` files.

Meshes and materials are saved as project assets so furniture survives editor restart and cloning the repository. The House geometry is not rebuilt. Running the command again when an authored layout exists will preserve the existing edits.

## Runtime behavior

The saved furnishings remain visible in play. The runtime layout generator skips rebuilding them. Saved chair/cabinet/hazard poses and colliders attach to the existing gameplay anchors for grabbing, falling and shaking. Static furniture stays where it was saved. Keep the HouseAuthoredFurniture component and its bindings intact. Keep the table legs and tabletop aligned when editing; they are separate objects. The cover-zone and evacuation markers are separate gameplay settings and do not automatically move with the table.

## Validation status

Repository static verification passes. Unity compilation, capture, scene reload, and physics playtesting are pending because Unity is unavailable in the coding environment. Validate the capture on a copy of House before replacing the team's working scene. Check furniture visibility after stopping Play and reopening Unity, and check chair pickup/collisions after editing positions.
