# Team furniture models

These nine low-poly FBX models were authored by Pleng for the CEVR project and originally contributed through `assets/furniture-models`.

`FurnitureSceneDressing` loads them as visual replacements at runtime. Existing scene objects retain their colliders, Rigidbody components, interaction IDs, hazard scripts, logging, cover zones, and earthquake response. Imported collision meshes are deliberately hidden so they do not duplicate the tested gameplay collision.

The files are stored directly because each is small. A standard Git clone therefore includes working FBX data without requiring Git LFS.

The source filename `Wandrobe.fbx` is retained for history and compatibility; it represents the wardrobe model.
