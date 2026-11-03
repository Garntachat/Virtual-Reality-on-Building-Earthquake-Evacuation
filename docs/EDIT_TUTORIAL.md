# Editable tutorial furniture

Stop Play Mode. Open only Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity.
Choose CEVR > Tutorial > Make furniture editable (one time).

This runs the same repair, prop and furniture setup used at startup, without advancing physics or the earthquake timer. Generated meshes, materials and textures are saved as assets. Inspect the result and save the scene with Cmd/Ctrl+S. A timestamped pre-bake scene backup is kept alongside it.

Edit furniture root objects (including their colliders), not just their visual mesh children. Existing repaired objects stay in their original hierarchy; new decorations are under Tutorial Editable Furniture, and shoes/pillow under Tutorial Editable Props. Lighting and markers are under CEVR_FinalVisualPolish.

The saved dressing flag prevents runtime dressing from scaling/replacing furniture a second time. Repeating the editor command preserves your edits. Do not run the original stage generator afterward: it rebuilds the scene.

Runtime gameplay, player/avatar, audio and HUD remain runtime systems; this is not a snapshot of an in-progress earthquake. Cover-zone triggers and guidance targets must be moved with the table if you change its location. Deleting named gameplay props can cause runtime setup to recreate them.

Commit the edited scene and new TutorialBake asset folder with all .meta files. Check scene reload, unchanged chair scale across two Play sessions, grabbing, pillow/shoes, window cracking, cover detection and shaking. Unity testing is required: this change has only been source-reviewed, not executed in Unity here. The repository's existing English-only check fails on Thai text in EarthquakeFurnitureController.cs.
