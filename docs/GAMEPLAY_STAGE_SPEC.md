# Tutorial Gameplay and Stage Specification

## 1. Purpose

The tutorial teaches locomotion, object placement, protective cover, event response, post-event evacuation, and emergency stopping in a controlled VR laboratory. It also establishes a deterministic baseline that can later support an approved human-behavior study.

The stage is a fictional engineering teaching laboratory. Pink accents and general laboratory furniture suggest a Thai university context, but the layout is not copied from an actual Chulalongkorn University building and contains no official logo.

The separate `House.unity` scene preserves the team's hand-built residential architecture. A cross-scene runtime installer supplies the same earthquake-response vocabulary plus a self-contained house preparation, cover, health, and evacuation loop. See [Cross-Scene Gameplay Features](SCENE_FEATURES.md).

## 2. Functional layout

| Zone | Approximate location | Function |
|---|---|---|
| Spawn and orientation | Front-center of the room | Learn looking, movement, crouching, crawling, and stop control |
| Visual guidance | Pink cover outline and chair beacon; green exit chevrons; amber cabinet boundary | Communicate affordances without forced camera motion |
| Laboratory benches A and B | Left side | Place a circuit module and safety canister among monitors, manuals, and tools |
| Strong cover table | Center-right | Reduces hazard damage while the player body is inside the trigger |
| Four movable pink chairs | Table approach, two lab benches, and spare-work area | All can be grabbed and moved; the marked primary chair blocks direct cover access |
| Hazard corridor | Center and right side | Four overhead objects and one unsecured cabinet |
| Exit opening | Front wall | Records an unsafe attempt if crossed before the event ends |
| Outdoor assembly point | Beyond the exit | Accepts success only during post-quake evacuation |

## 3. State machine

| Phase | Entry condition | Exit condition | Primary events logged |
|---|---|---|---|
| Orientation | Session starts | Six seconds elapse | Phase and pose samples |
| Normal Activity | Orientation completes | At least 30 seconds plus task completion, or 120-second watchdog | Task completion and watchdog continuation |
| Earthquake | Normal-activity gate passes | Motion ends, health reaches zero, or stop is requested | Onset, cover, damage, unsafe exit |
| Post-Quake Evacuation | Motion ends and player remains active | Assembly reached, health reaches zero, or 60-second timeout | Assembly entry and timeout |
| Success | Valid assembly entry | Three seconds | Remaining health |
| Failure | Health depletion, timeout, or stop | Three seconds | Failure reason |
| Debrief | Success or failure completes | Facilitator ends the session | Debrief-ready event |

## 4. Ordinary activity tasks

1. `task-circuit-module`: place the circuit module in the green test tray.
2. `task-safety-canister`: place the safety canister in the green storage slot.

Each placement goal checks `TaskItem.ItemId`; an incorrect object cannot complete a task. The builder attempts to add `XRGrabInteractable` when XR Interaction Toolkit is available. In desktop debug mode, `E` or left click grabs and releases a task object.

## 5. Movable chair interaction

- Four uniquely identified chairs are available; the marked primary chair blocks the direct approach to the strong-table cover zone.
- The participant can grab and slide every chair in desktop or XR mode, or choose to navigate around it.
- Rigidbody constraints keep each chair on the floor and upright while allowing horizontal translation and yaw rotation.
- Every chair responds to horizontal earthquake acceleration at a reduced scale.
- `furniture_displaced` records each chair's first movement of at least 0.15 metres; desktop interaction also records grab start and release.
- A center crosshair and context-sensitive desktop prompt expose the available interaction without a head-locked VR overlay.

## 6. Earthquake and hazard behavior

- `GroundMotionPlayer` outputs acceleration in metres per second squared.
- `InertialRigidbody` applies `-acceleration` with `ForceMode.Acceleration`.
- The tutorial preview uses an envelope, seeded Perlin noise, and sinusoidal components, so identical settings produce an identical motion request.
- Research mode requires a recorded `QuakeProfile` and refuses the preview profile.
- `HazardDirector` releases hazards in serialized order and uses no random torque.
- The main camera and XR Origin are never moved by earthquake code.
- A procedural low-frequency rumble and deterministic light flicker provide environmental cues without head motion.

## 7. Win, failure, and safety rules

- Cover reduces incoming hazard damage to 20 percent of raw damage.
- Early entry into the assembly zone is logged but rejected.
- Success requires the Post-Quake Evacuation phase, assembly-zone occupancy, and health greater than zero.
- Failure occurs when health is zero, evacuation exceeds 60 seconds, or the session is stopped.
- Emergency stop must stop the coroutine, motion, hazards, and log writer.

## 8. User-experience requirements

- No camera shake, artificial head roll, or forced locomotion.
- Desktop uses a compact screen overlay; XR retains a readable world-space panel and never uses scripted camera motion.
- The participant may stop at any time without penalty.
- Training prompts may teach Drop-Cover-Hold. Research prompts must remain neutral unless the protocol explicitly studies instruction.
- Audio volume must be calibrated on the actual headset and kept within the approved protocol.

## 9. Definition of Done

- The stage can be generated from a clean clone.
- Unity Console contains no errors.
- Scene validation and all EditMode tests pass.
- Success, early-exit, cover, damage, timeout, and emergency-stop paths pass.
- Headset frame pacing meets the device target without sustained drops.
- No earthquake component changes camera or XR Origin transforms.
- Four chairs can be grabbed independently; the primary chair blocks direct cover access, slides without lifting, and produces displacement telemetry.
- Monitors, safety equipment, plants, storage, and guidance decorations remain collider-free and cannot block the player or evacuation route.
- Every JSONL line parses, each run has a unique filename, and no identifying data is recorded.
- Two team members independently reproduce the clean-clone setup.
- The house mesh remains unchanged while its runtime-installed feature set appears exactly once.
- Shoes, pillow protection, window cracks, toppling furniture, third-person desktop view, and local split-screen pass cases F26-F35.
