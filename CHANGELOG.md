# Changelog

## Scene-wide realistic earthquake response

- Connected every safe active Rigidbody in House and Tutorial to the same deterministic floor-acceleration signal; light objects respond more readily while heavy objects retain restrained motion.
- Added automatic tall-object rocking/toppling, continuous collision handling, wake-up tuning, and impact audio without random torque.
- Added deterministic visual response for non-physics décor including books, screens, lamps, windows, cabinets, plants, cushions, tables, and other relevant dressing.
- Differentiated loose sliding, tall rocking, hanging swing, mounted vibration, and heavy-furniture response.
- Preserved a stable player camera, XR rig, floors, walls, HUD, assembly zones, and crawl/safety geometry for comfort and navigation.
- Limited floor-number amplification to the synthetic training preview; recorded research profiles continue to use their calibrated floor-response data unchanged.

## Editable Scene-view material repair

- Added an editor-only repair for unsupported or missing shaders in the House and Tutorial scenes, fixing magenta ProBuilder surfaces before Play mode.
- Reuses the project-owned Built-in Render Pipeline wall and concrete materials without changing meshes, transforms, XR hierarchy, or gameplay components.
- Rechecks newly created ProBuilder objects and provides a manual **CEVR > Repair Pink Materials In Open Scenes** command.
- Marks repaired scenes dirty so the corrected material references can be saved normally with Cmd/Ctrl+S.

## Visible exterior windows and reliable falling impacts

- Made tutorial and house windows transparent and added a lightweight exterior skyline view so players can clearly look outside.
- Added swept physics contact detection to falling hazards so fast objects striking a stationary player still deduct health.
- Preserved collision-based damage, one-hit protection, invulnerability timing, and pillow cover damage reduction.

## Team-authored furniture integration

- Integrated all nine FBX models contributed through `assets/furniture-models` without editing the committed scene YAML.
- Replaced chairs, sturdy tables, protective pillows, sofas and relevant tall furniture while preserving tested gameplay objects and collision.
- Added a condo bed area, sofa cushions, and a physics-enabled vase that responds to earthquake acceleration and produces impact audio.
- Added render-pipeline-safe materials, hidden imported collision proxies, visual fallbacks, deterministic import settings, asset tests and static verification.
- Embedded the small FBX files directly so a normal clone works without Git LFS.

## Full-size prone crawl pose

- Removed the collider-height scale shortcut that visibly squashed the student when crawling.
- Added a smooth full-body transition between standing and a floor-level prone pose for both desktop players.
- Reused locomotion limb motion at a crawl cadence and added subtle body roll while moving prone.
- Added static regression checks and a third-person crawl acceptance test.

## Main menu and selectable student outfits

- Added a committed main-menu scene, first in Build Settings, with Tutorial, House and Exit actions.
- Added guarded asynchronous loading, loading feedback, and return-to-menu controls.
- Added saved white, burgundy and blue shirt choices using the existing animated student model and UV atlas.
- Excluded the menu from the earthquake gameplay installer; kept direct gameplay-scene Play available.
- Added EditMode tests and static menu contracts. Unity runtime/visual validation remains required.

## Avatar and desktop HUD recovery

- Added an automatic student-resource reimport check and a visible uniformed fallback avatar, so a missing FBX or texture can no longer leave either player invisible.
- Restored the labelled health bar at full health and kept it visible throughout the run.
- Added an on-screen first-/third-person button while retaining the `T` shortcut and VR-safe first-person behavior.
- Prevented the desktop cursor-capture logic from swallowing clicks on the view button.

## Complete procedural gameplay audio

- Added escalating earthquake rumble and structural creaks driven by the same smooth motion envelope as the visuals.
- Added procedural footsteps, chair grab/release/scrape/impact, window crack, footwear, pillow, bulb-break, damage, and quake transition cues.
- Installed the audio director in both supported scenes and suppressed the older duplicate tutorial rumble.
- All clips are synthesized at runtime, so collaborators do not need missing audio files or additional downloads.


## Cover-access and footwear repair

- Replaced the block footwear pickup with a shaped low-poly shoe pair including soles, uppers, tongues, and laces.
- Increased both desktop players' crawl speed from 42% to 68% of walking speed.
- Enlarged the house table opening and cover trigger, moved approach chairs outward, and added green crawl-area markers.
- Reduced the tutorial health bar and hide it at full health; house health text appears only after damage.
- Repository structural and furniture checks pass; Unity Editor and headset acceptance remain required.


## Skinned student avatar

- Replaced primitive body assembly with a licensed skinned FBX and idle/run clips.
- Added edited student uniform texture and scoped automatic importer.
- Replaced task-item and falling-object visuals with existing authored assets.
- Added Unity import/animation-binding tests; execution requires Unity.


## Scene collision and co-op camera review

- Added collision to large decorative furniture and warm house fill lighting.
- Added player 2 crawl (N) with overhead clearance checks.
- Fixed wall checks for player 2 camera and near-wall minimum distance for player 1.
- Repository/mesh checks passed; Unity runtime acceptance remains pending.


## Authored furniture and simple training flow

- Added 22 CC0 Kenney furniture meshes with source provenance and reproducible import.
- Runtime dressing replaces generated chairs, tables, cabinets, pillow and workstation decorations in House and tutorial.
- Training begins with 30 seconds of exploration; lab tasks are optional.
- Short cover/exit direction cue, reduced HUD clutter, and third-person grab reach repair.
- Visual ceiling sway leaves camera tracking and architectural collision stable.
- Asset checks and repository checks passed. Unity and headset gates remain pending.


## Student avatar and gameplay clarity repair

- Replaced both capsule avatars with a stylized white-shirt, dark-trouser student character and walking motion.
- Fixed kinematic velocity warnings during hazard reset.
- Corrected third-person self-collider targeting and added shoulder camera mouse aiming.
- Added explicit next-task instructions, labelled placement trays, and accurate task-wait countdowns.
- Static checks passed; Unity Play-mode and headset validation remain pending.


## 0.5.0 - 2026-09-10

- Preserved the new 2.3 MB hand-built `House.unity` scene and enabled it in Build Settings
- Added a runtime cross-scene installer for the engineering tutorial and house
- Added a complete house loop with 30-second preparation, 20-second quake, damage/failure, and 60-second evacuation
- Added four uniquely logged grabbable chairs to the house while retaining four in the engineering tutorial
- Added deterministic shaking, four falling objects, and two toppling furniture hazards to the house
- Added wearable safety shoes and a grabbable protective pillow to every supported scene
- Added independent cover and pillow protection sources so one cannot incorrectly cancel the other
- Added deterministic visual window cracking without dangerous glass-fragment simulation
- Added desktop first-/third-person switching with camera collision avoidance
- Added optional two-player local split-screen with independent Player 2 movement
- Added cross-scene documentation, static contracts, and protection-source regression testing
- Made footwear functional through deterministic cracked-window floor-debris damage, with two shoe pairs for local co-op
- Corrected cover ownership for two simultaneous players and registered installation for subsequent supported scene loads

## 0.4.1 - 2026-08-28

- Expanded the scene to four uniquely identified, independently grabbable, earthquake-responsive chairs
- Kept the marked primary chair as the deliberate obstacle that must be moved before crawling under the sturdy table
- Added dual monitor workstations, keyboards, manuals, tools, first-aid equipment, fire extinguisher, emergency-stop station, plant, storage, and whiteboard details
- Made every decorative object collider-free so visual improvements cannot obstruct movement, cover access, or evacuation
- Strengthened orientation and normal-routine guidance for the two-minigame, 30-second earthquake-onset loop
- Expanded scene validation, static verification, gameplay specifications, and acceptance tests for all chairs and decoration safety

## 0.4.0 - 2026-08-28

- Added a unified runtime visual-polish layer that upgrades both the committed legacy scene and rebuilt scenes on Play
- Rebalanced ambient, directional, laboratory, cover, and exit lighting to remove the washed-out graybox appearance
- Applied a cohesive charcoal, warm concrete, wood, Chula-pink, safety-green, hazard-amber, and window-blue material system
- Rebuilt the desktop HUD as a compact high-contrast overlay with clearer typography, health styling, control guidance, and quake indicator
- Added a pink cover outline, chair beacon, green evacuation chevrons, amber cabinet boundary, and visible ceiling light panels
- Added green/pink interaction feedback to the desktop crosshair and prompt panel
- Updated earthquake instructions to explicitly teach moving the chair and using `Z` to crawl under the sturdy table
- Added static verification for the final visual, wayfinding, HUD, interaction, and runtime-preparation contracts

## 0.3.2 - 2026-08-28

- Added `Z` toggle crawling with a 0.58-metre controller stance that fits beneath the sturdy table
- Added slower crawl locomotion, low camera placement, and overhead clearance checks that prevent standing through the tabletop
- Updated HUD, prompts, setup instructions, acceptance testing, and static verification for crawling
- Made the English-only verifier tolerate non-UTF-8 files instead of crashing before a push

## 0.3.1 - 2026-08-28

- Added runtime repair for the committed legacy scene so Play no longer depends on a manual rebuild
- Automatically hides the oversized mirrored wall disclaimer and converts the desktop HUD to a readable screen overlay
- Automatically creates the missing pink physics chair and attaches desktop/XR grab support before tutorial validation
- Added clearer initial navigation guidance and static regression coverage for the exact legacy-scene failure

## 0.3.0 - 2026-08-28

- Added generated-scene versioning and clear stale-scene rejection
- Corrected mirrored/oversized wall text and rebuilt the world-space HUD layout
- Reworked the table obstacle into a recognizable pink movable chair
- Added a desktop crosshair, context interaction prompts, hold-distance control, and bounded grab velocity
- Added safer cursor capture and standing-clearance checks
- Made hazard resets restore their original poses and effect shutdown restore lights/audio
- Added head orientation and tracking-availability telemetry without identity fields
- Enabled linear color, enhanced physics determinism, frame timing statistics, background operation, and privacy-conscious project defaults
- Added stage-integrity tests and stronger static repository verification

## 0.2.0 - 2026-08-24

- Replaced all Thai-language repository documentation and GitHub templates with complete English editions
- Added a 21-page ICE pre-project proposal report in DOCX and PDF formats
- Added course-deadline, Turnitin, experimental-method, ethics, test, risk, schedule, and appendix coverage
- Added a reproducible report builder and visual render verification workflow
- Updated documentation filenames, links, verification requirements, and binary-report LFS rules

## 0.1.0 — 2026-08-24

- Added deterministic tutorial flow and fictional engineering-lab stage builder
- Added normal-activity tasks, cover, hazards, evacuation, health and stop controls
- Added recorded CSV ground-motion import and tutorial-only preview motion
- Added JSONL event/pose logging and Training/Research separation
- Added EditMode tests, static verification, GitHub templates and Thai setup documentation
