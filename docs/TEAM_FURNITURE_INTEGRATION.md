# Team furniture integration

The FBX models contributed through `assets/furniture-models` are integrated by `FurnitureSceneDressing` at runtime. This avoids editing the large scene YAML files and protects the existing gameplay setup.

| Team model | House | Engineering tutorial | Preserved behavior |
|---|---|---|---|
| DiningChair | All four movable chairs | All four movable chairs | Rigidbody, grab ownership, displacement logging, earthquake response, collision |
| DiningTable | Sturdy cover table | Sturdy cover table | Existing tabletop/leg collision and cover trigger |
| Bed_Pillow | Protective pillow and bed | Protective pillow | Grabbing and independent head protection |
| Fridge | Left toppling cabinet | Not used | Toppling, damage and hazard reset |
| Wandrobe | Right toppling wardrobe | Unsecured lab storage | Toppling, damage and hazard reset |
| Sofa | Living room | Waiting area | Existing/static collision |
| Sofa_Pillows | Living-room sofa | Waiting-area sofa | Visual decoration only |
| Bed | Condo sleeping area | Not relevant | Static bounds collision |
| Vase | Coffee table | Not relevant | Rigidbody, inertial shaking and impact sound |

## Safety rules

- Imported FBX colliders and meshes named `Collision` are not allowed to replace the tested gameplay collision.
- The original gameplay object remains the owner of Rigidbody, interaction, hazard and logging components.
- Placeholder renderers are hidden only after the corresponding FBX instantiates successfully.
- If an FBX cannot load, the previous generated/Kenney visual stays visible and gameplay remains functional.
- Runtime materials are converted to the active render-pipeline shader to prevent pink error materials.

## Unity acceptance

1. Open the main menu and load each scene.
2. Verify Console has zero red errors and no missing-team-furniture warnings.
3. Inspect all four chairs, the cover table, protective pillow, sofa and tall furniture in each scene.
4. Move every chair and confirm it remains grabbable and collides with the floor.
5. Crawl below the table and confirm its visible top and legs align with collision and the green cover area.
6. During shaking, confirm the fridge/wardrobe topple, the vase moves and falls, and the protective pillow still reduces damage.
7. Verify the House bed and sofa do not obstruct the spawn-to-cover or cover-to-exit route.
8. Run `TeamFurnitureAssetTests` and the complete EditMode suite.
