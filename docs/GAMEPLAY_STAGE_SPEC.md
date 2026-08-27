# Tutorial Gameplay and Stage Specification

## 1. Purpose

The tutorial teaches locomotion, object placement, protective cover, event response, post-event evacuation, and emergency stopping in a controlled VR laboratory. It also establishes a deterministic baseline that can later support an approved human-behavior study.

The stage is a fictional engineering teaching laboratory. Pink accents and general laboratory furniture suggest a Thai university context, but the layout is not copied from an actual Chulalongkorn University building and contains no official logo.

## 2. Functional layout

| Zone | Approximate location | Function |
|---|---|---|
| Spawn and orientation | Front-center of the room | Learn looking, movement, crouching, and stop control |
| Laboratory benches A and B | Left side | Place a circuit module and safety canister |
| Strong cover table | Center-right | Reduces hazard damage while the player body is inside the trigger |
| Movable chair | In front of the strong table | Blocks direct cover access; can slide on the floor but cannot be lifted |
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

- A chair blocks the direct approach to the strong-table cover zone.
- The participant can grab and slide the chair in desktop or XR mode, or choose to navigate around it.
- Rigidbody constraints keep the chair on the floor and upright while allowing horizontal translation and yaw rotation.
- The chair responds to horizontal earthquake acceleration at a reduced scale.
- `furniture_displaced` records the first movement of at least 0.15 metres; desktop interaction also records grab start and release.

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
- The HUD is a world-space panel, not a head-locked overlay.
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
- The chair blocks the direct cover approach, slides without lifting, and produces displacement telemetry.
- Every JSONL line parses, each run has a unique filename, and no identifying data is recorded.
- Two team members independently reproduce the clean-clone setup.
