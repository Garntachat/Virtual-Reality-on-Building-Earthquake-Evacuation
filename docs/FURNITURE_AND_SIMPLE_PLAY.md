# Furniture and simple play update

The existing House and engineering tutorial receive their furniture dressing automatically on entering Play mode. No scene regeneration is required. The original House scene file is preserved.

## Play

1. Open either existing scene and press Play. Click the Game view.
2. Explore for 30 seconds. WASD moves; E picks up/releases objects or slides a chair. Lab placement tasks are optional in Training.
3. Locate the sturdy table before shaking. Move its chair, then press Z to crawl under it. The pillow remains an alternative head-cover interaction.
4. After shaking stops, follow the exit/assembly cue. Reach assembly alive to complete the run.
5. T toggles the desktop third-person view. F2 joins/leaves the existing same-keyboard local second player; their controls appear on screen.

## Visual assets

Twenty-two authored Kenney Furniture Kit meshes replace major generated furniture and add sofas, kitchen fittings, plants, monitors, books, and lamps. They are stylized low-poly models, not photorealistic scanned furniture. The student avatar now uses the licensed Kenney skinned character, edited student texture and idle/run clips. Architecture and safety markers remain the existing scene geometry.

The imported meshes retain original material colors. Runtime renderers use the active built-in/URP pipeline. Existing interactive object colliders, rigidbodies and scripts are retained; large added decorative furniture has static mesh collision; small accessories remain visual-only. Collision shapes approximate the furniture and need in-editor alignment inspection. The house dressing uses the coordinates of the existing generated gameplay area; hand-authored rooms must be inspected for intersections.

Ceiling motion is visual-only, with 3 cm target translation at maximum intensity. It does not shake the tracked camera or represent a calibrated building response. Existing falling objects and window-crack mechanics remain separate.

## Verification status

Passed here: repository structural checks, 22 mesh data checks (7,060 triangles, indices, finite coordinates, normalized bounds, materials and metadata), whitespace check, and an offline mesh preview inspection.

Not run here: Unity C# compilation, EditMode tests, Play mode, standalone build, headset comfort/performance, or a two-player end-to-end run. Unity is unavailable in this workspace. Added an EditMode regression test for the Training timing configuration; it still needs execution in Unity.

Before a lab session, run the Unity Test Runner, inspect both scenes in Play, slide every chair, crawl beneath each cover table, test pillow/shoes/window damage, complete evacuation, then repeat with F2. Inspect readable HUD at the actual display size and verify all new meshes appear in a standalone build. Desktop direction boxes are not VR UI; headset UI and multiplayer require separate lab validation. Existing multiplayer is local split screen, not networked multi-headset play.

Only House and the engineering tutorial are covered; school and office scenes are future work. Research mode should use its approved protocol, rather than Training guidance, when measuring unprompted responses.

## Reproduce

Run `python3 Tools/verify_repo.py` and `python3 Tools/verify_furniture.py`.
The importer accepts the official Kenney Furniture Kit ZIP: `python3 Tools/import_kenney_furniture.py /path/to/kenney_furniture-kit.zip`.
See `third-party/FURNITURE_SOURCE.md` and the included CC0 license for provenance.

## Gradual shaking

Both default 20-second previews rise smoothly for 14 seconds, hold maximum envelope for four seconds, and fade for two seconds. Furniture acceleration follows that envelope. Ceiling sway, the tutorial intensity indicator and configured audio/light effects follow the same progression, with frame-rate-independent smoothing for presentation. The maximum is the configured preview amplitude, not a guarantee that every loose object topples. Recorded research time histories remain unchanged. Unity runtime and headset smoothness still require testing.

## Collision and visual review follow-up

- Added static mesh collision to added sofas, coffee tables, bookcases, TV cabinets and kitchen units. Interactive chair/pillow collision remains attached to its original physics body.
- Added two warm, shadow-free house fill lights and visible ceiling fixtures, which use the gradual sway progression.
- Player 2 now crawls with N, moves more slowly while crawling, and cannot stand into overhead collision.
- Player 2 camera now checks walls; removed the minimum camera distance that could push player 1 camera through a nearby wall.

Acceptance checks still requiring Unity: walk around all large furniture in both scenes; inspect table/seat collision alignment; press N under the house table as player 2 and confirm standing is blocked; test both cameras against walls; confirm a full 30-second preparation, shaking, and evacuation run with no Console errors. Check the house ceiling fixture positions against the hand-built mesh. No zero-bug or runtime performance claim is made.

## Skinned student and remaining limits

The primitive student assembly has been replaced by a CC0 Kenney skinned FBX character. White shirt, grey collar and dark trousers come from an edited source SVG skin. The runtime crossfades idle/run clips and keeps root motion separate from player collision. Import is configured automatically by StudentAssetImporter. No package installation or manual Animator setup is needed. The original gameplay CharacterController remains the collision body.

Run StudentAssetTests in the Unity EditMode Test Runner to check skinned mesh, texture, legacy clip import and every animation binding path. This test has been added but could not be executed here. Inspect the character facing direction and floor alignment before a build. Crawl currently adapts visual height; it is not an authored crawl animation. Shoe pickups, architecture and safety markers still include primitive geometry. Small lab placeholders are now a laptop/book bundle and falling hazards use book meshes, retaining the old physics/IDs. Research task labels should be reviewed before an experiment.

This update does not certify a finished or bug-free VR release. Unity compilation/import, animation playback, scene screenshots, complete gameplay and headset performance/comfort are remaining release gates. The source skin and license are included.

## Team feedback update

Safe-cover outlines in both current scenes are green. Chairs and their compound colliders are scaled together to approximately 60 cm wide and 1 m tall. Loose shelves remain affected by acceleration and toppling torque; the anchoredToStructure setting keeps built-in furniture kinematic. This is a training approximation, not structural engineering validation.

Training lights receive a per-run seeded schedule: unaffected, flickering, outage, or bulb-burst particles with outage. Seeds and outcomes are logged. Directional/exit lights remain available for orientation. Bulb particles are visual effects, not simulated glass fracture or extra damage. Recorded research and tutorial Research mode do not receive these random events.

The requested condo layout is the next map revision. House is preserved: no condo, corridor, stairwell or high-rise escape simulation is claimed in this update.
