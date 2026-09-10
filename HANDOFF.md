# Handoff - CEVR Cross-Scene Gameplay Candidate 0.5.0

## Included deliverables

- Unity 6.3 project manifest with pinned direct package versions
- One-click fictional engineering-laboratory stage builder plus automatic legacy-scene preparation on Play
- Organized runtime C# systems separated into Core, Data, Earthquake, Effects, Hazards, Logging, Player, Tutorial, UI, and Zones
- Deterministic 30-second onset flow, two ordinary laboratory tasks, five staged hazards, strong-table cover, and assembly-point outcome logic
- Tutorial preview motion, recorded CSV importer, and reduced floor-response model
- Desktop grab/move interaction for two task objects and four uniquely logged chairs, clearance-safe crouch and crawl, XR integration instructions, emergency stop, procedural rumble, and lighting cues
- Final visual-polish layer with cohesive materials, balanced lighting, compact HUD, cover/chair guidance, exit chevrons, hazard marking, workstations, safety equipment, plant, storage, and tool decoration
- Anonymous JSONL semantic-event logging and 10 Hz pose sampling
- Three EditMode test classes, scene validator, and repository static verifier
- GitHub Actions, issue forms, pull-request template, Git LFS rules, and Unity ignore rules
- Complete English setup, architecture, testing, research, and collaboration documentation
- Preserved hand-built house scene with runtime-installed shake, falling furniture, two functional shoe pairs, pillow cover, cracked windows and floor debris, third-person desktop camera, and local split-screen
- ICE pre-project proposal report in editable DOCX and submission-friendly PDF formats

## Checks completed in the authoring environment

- `python3 Tools/verify_repo.py`: PASS
- Required files, package pins, and JSON syntax: PASS
- Unity `.meta` coverage for all hand-authored Assets: PASS
- Determinism and no-camera-motion policy scan: PASS
- Generated Unity folders and participant logs in the deliverable: none
- DOCX and PDF report rendering: visually inspected page by page

## Gates that must still run on the team machine

The authoring environment does not contain Unity Editor or a VR headset. Therefore, this handoff does not claim that Unity compilation, OpenXR validation, headset runtime, or device performance has passed. The receiving team must complete these gates in order:

1. Clean clone and Git LFS pull
2. Unity package resolution and Console compilation
3. Immediate committed-scene Play test, including chair grab, `Z` crawl, readable HUD, and visual wayfinding
4. Optional one-click persistent stage generation
5. Open-scene validator
6. EditMode Run All
7. Desktop functional and visual test suite
8. OpenXR Project Validation and XR simulator tests
9. Headset build, performance and safety tests, followed by an approved pilot

If a gate fails, open a GitHub bug using the included template and state the commit, Unity version, platform, and reproduction steps. Never attach raw participant logs or identifying information.

## Start here

Read `README.md`, then follow `docs/UNITY_SETUP.md` without skipping a gate. Read `docs/XR_SETUP.md` before connecting a headset. Before any human-participant activity, complete `docs/RESEARCH_AND_DATA.md` and `docs/TEST_PLAN.md` under the approved institutional protocol.
