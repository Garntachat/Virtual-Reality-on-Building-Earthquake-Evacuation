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

## 3. VR comfort and physical safety

- Earthquake code never translates or rotates the camera or XR Origin.
- No falling object starts at the initial head position.
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

## 6. Bug severity

- **P0:** physical safety, privacy exposure, or stop failure. Stop all testing.
- **P1:** crash, blocked flow, or missing research data. Block the build.
- **P2:** functional, visual, or audio defect with a workaround. Resolve before pilot when it affects a variable.
- **P3:** cosmetic or documentation defect. Schedule against the milestone.

