# Test Plan and Acceptance Gates

## 1. Gate sequence

| Gate | Owner | Required evidence | Release blocker |
|---|---|---|---|
| Static | Every pull request | `verify_repo.py` PASS | Required file, metadata, manifest, or policy failure |
| Compile | Programmer | Console with zero errors | Any compile error or unexplained new warning |
| EditMode | Programmer | Test Runner all green | Rule, profile, or health test failure |
| Desktop flow | Reviewer | Checklist and screenshot/video | Incorrect success, failure, stop, or log behavior |
| XR simulator | XR owner | Interaction checklist | Grab, cover, body, or exit failure |
| Headset | XR owner and facilitator | Device test record | Tracking, frame, stop, or safety failure |
| Pilot | Research lead | Anonymous pilot report | Discomfort, data, or experimental-validity concern |

## 2. Functional cases

| ID | Procedure | Expected result |
|---|---|---|
| F01 | Start the scene | Orientation transitions to Normal Activity |
| F02 | Observe at 29.9 seconds | Earthquake has not started |
| F03 | Complete tasks and reach 30 seconds | Earthquake starts exactly once |
| F04 | Leave tasks incomplete until watchdog | Earthquake starts and watchdog event is logged |
| F05 | Enter cover and receive a hazard hit | Applied damage is 20 percent of raw damage |
| F06 | Enter assembly during shaking | Unsafe attempt is logged; no success |
| F07 | Enter assembly after shaking | Success transitions to Debrief |
| F08 | Reduce health to zero | Failure transitions to Debrief |
| F09 | Do not evacuate for 60 seconds | Failure reason is evacuation timeout |
| F10 | Activate emergency stop | Motion, hazards, and logging stop |
| F11 | Run two sessions | Log filenames are unique; neither overwrites the other |
| F12 | Use Research mode with preview | Startup is rejected with a clear reason |
| F13 | Use Research mode with valid recorded profile | Recorded motion starts successfully |
| F14 | Open the committed legacy generated scene | Runtime preparation repairs it before validation; no stale-scene failure blocks Play |
| F15 | Aim at both task items and all four pink chairs | Context prompt changes and every object can be grabbed/released independently |
| F16 | Press `Z`, crawl under the table, then press `Z` again beneath it | Player fits below the tabletop and remains low until there is standing clearance |
| F17 | Enable third-person view, press `Z`, move while low, then press `Z` after leaving the table | The full-size student smoothly rotates into a floor-level prone crawl with limb motion, never squashes vertically, and smoothly returns upright |
| F18 | Reset released hazards in the same scene | Every staged hazard returns to its captured pose and safe kinematic state |
| F19 | Start the committed scene and inspect the first frame | Oversized mirrored text is hidden; HUD is compact, readable, and not mirrored |
| F20 | Inspect cover, chair, exit, and cabinet areas | Pink cover outline/beacon, green exit chevrons, and amber hazard boundary are visible |
| F21 | Aim away from and then at the chair or task item | Crosshair/prompt feedback changes from white/navy to green; held feedback becomes pink |
| F22 | Observe Orientation, Earthquake, Evacuation, Success, and Failure | Phase heading uses distinct accessible status colors and quake indicator activates only during shaking |
| F23 | Compare camera during normal and earthquake phases | Environment, lights, and objects react; camera transform receives no scripted shake |
| F24 | Move each of the four chairs and inspect the session log | Each unique chair ID produces independent grab, release, and first-displacement events |
| F25 | Walk from spawn to both benches, cover table, exit, and assembly point | Workstation and safety decorations are visible but have no colliders and never block the route |
| F26 | Complete both routine tasks before 30 seconds | Earthquake waits until the 30-second minimum, then begins exactly once |
| F27 | Open `House.unity` and press Play | Player spawns inside the playable area; prompt, chairs, shoes, pillow, windows, cover table, hazards, and exit marker appear |
| F28 | Aim at each shoe pair and press the player's interact key | Shoes attach to that player, stop colliding, and log `footwear_equipped` once per pair |
| F29 | Grab the pillow during shaking | Pillow is held above the head, protection activates, and damage is reduced without cancelling table-cover protection |
| F30 | Observe both house windows, then walk across the visible debris with and without shoes | Each displays a deterministic crack pattern; visual shards have no collision, and the trigger inflicts substantially less damage when footwear is equipped |
| F31 | Observe the house cabinet and bookcase | Both release in order and receive deterministic overturning force from ground acceleration |
| F32 | Press `T` twice in desktop mode | Camera switches to collision-aware third person and returns to first person; movement remains responsive |
| F33 | Press `F2`, move and interact as Player 2, then press `F2` again | Split screen opens, Player 2 responds to `IJKL`, `U`/`O`, and right `Shift`; leaving restores Player 1 full screen |
| F34 | Repeat F32 and F33 with stereo XR active | Third-person/split-screen inputs do not replace the tracked stereo view |
| F35 | Complete the house quake, then reach the marker | Success is accepted only after shaking stops |
| F36 | Deplete health, then repeat and wait 60 seconds after shaking | Health depletion and evacuation timeout each produce failure and stop active hazards |

## 3. VR comfort and physical safety

- Earthquake code never translates or rotates the camera or XR Origin.
- No falling object starts at the initial head position.
- Third-person and split-screen desktop modes are unavailable while the primary camera is stereo-enabled.
- Window cracking is visual only; sharp glass fragments are never simulated.
- Guardian or boundary protection is active and the real floor is aligned.
- The facilitator can see the participant and reach a stop control.
- Stop immediately for dizziness, nausea, headache, imbalance, panic, breathing difficulty, or participant request.
- After stopping, help the participant sit and remove the headset safely; never pressure them to continue.

## 4. Performance record

For each device build, record device, operating system, headset runtime, refresh target, commit hash, mean frame time, 95th-percentile frame time, worst sustained drop, and thermal state. Editor FPS is not a substitute for an on-device measurement.

## 5. Log validation

1. `session_started` is the first event and `session_ended` is the last event.
2. Every line parses as JSON.
3. Participant code contains no name, email, or student ID.
4. Timestamps increase and phase order is valid.
5. Pose rate is approximately 10 Hz without degrading frame time.
6. Task, cover, damage, unsafe exit, assembly, and outcome events match the functional test cases.
7. Motion-profile ID, floor-response ID, duration, scenario ID, and build version are recorded.
8. Pose samples include head position and orientation plus explicit left/right tracking availability.

## 6. Bug severity

- **P0:** physical safety, privacy exposure, or stop failure. Stop all testing.
- **P1:** crash, blocked flow, or missing research data. Block the build.
- **P2:** functional, visual, or audio defect with a workaround. Resolve before pilot when it affects a variable.
- **P3:** cosmetic or documentation defect. Schedule against the milestone.
