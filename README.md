> Latest update: the House scene now uses measured floor/window geometry and room-appropriate placement for every team furniture model.

# CEVR - Chula Engineering Earthquake VR Tutorial Stage

CEVR is a Unity prototype for studying human decisions during a simulated earthquake in a virtual engineering teaching laboratory. The environment is fictional and only inspired by a Thai university engineering context. It is not an official Chulalongkorn University product, architectural digital twin, structural-analysis tool, or emergency guidance system.

> Status: CEVR 0.5.0 cross-scene gameplay candidate. The repository preserves the new hand-built house scene and adds reusable gameplay to both the engineering tutorial and house: deterministic shaking, falling furniture, four grabbable chairs per scenario, wearable shoes, protective pillow cover, visual window cracking, desktop third-person view, local split-screen multiplayer, crawl-under-table interaction, health, and evacuation outcomes. Unity compilation, OpenXR validation, headset performance, ethics approval, and human-participant pilot testing remain mandatory machine-side gates.

**Simple Rigidbody shaking:** Physical furniture now uses one predictable ForceMode.Force + torque response to the shared earthquake signal. Unity Mass, Linear/Angular Damping, Force (N), Torque (N m) and per-item Shake Strength are editable on each saved Rigidbody. Open House or New_House in Edit mode once to attach controls to existing furniture. See [Simple Earthquake Physics](docs/SIMPLE_EARTHQUAKE_PHYSICS.md) for the quick tuning and gameplay test steps.

## Implemented gameplay

1. A six-second orientation phase introduces the room and controls.
2. The participant performs two ordinary laboratory setup tasks.
3. The earthquake begins after at least 30 seconds once the tasks are complete, or after a 120-second watchdog timeout.
4. A deterministic 20-second motion profile applies inertial acceleration to physical objects without moving the camera or XR Origin.
5. Four overhead objects and one unsecured cabinet are released in a repeatable sequence.
6. Four pink, floor-constrained chairs can be grabbed independently; the clearly marked primary chair sits partly under the strong table and must be moved before entering cover.
7. Entering the strong-table cover zone reduces hazard damage by 80 percent.
8. Reaching the assembly point during shaking is logged as an unsafe early-exit attempt and does not count as success.
9. After shaking stops, reaching the outdoor assembly point completes the tutorial. Health depletion or evacuation timeout causes failure.
10. Semantic events, chair displacement, and head/hand poses are recorded as anonymous JSONL data using a participant code rather than a name.
11. Every supported scene receives two wearable-shoe pairs, a protective pillow, deterministic window cracking with a footwear-sensitive debris zone, and a desktop `T` third-person option.
12. `House.unity` receives a complete 30-second preparation, 20-second earthquake, hazard/health, cover, and post-quake assembly flow without altering the original house mesh.
13. `F2` enables same-device local split-screen evaluation; Player 2 uses `IJKL`, `U`/`O`, and right `Shift` to interact.

## Fastest clean-clone setup

1. Install Git, Git LFS, Unity Hub, and Unity Editor `6000.3.20f1`.
2. Clone the repository and run `git lfs pull`.
3. Open the repository root in Unity Hub and wait for package resolution.
4. Confirm that the Console has no compilation errors.
5. Open either `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity` or `Assets/CEVR/Generated/Scenes/House.unity`.
6. Press Play. Version 0.5.0 installs the correct scenario before gameplay starts.
7. Click the Game view and turn 180 degrees to find the sturdy brown table and bright pink chair.
8. Controls: `WASD`, mouse look, `E` or left click to wear shoes or grab/drop an item, `Z` to crawl, `C` or `Ctrl` to crouch, `T` for third person, `F2` for local split-screen, and `F12` or `Backspace` for emergency stop.

Follow [Unity Setup](docs/UNITY_SETUP.md) from the beginning if this is the first time opening the project. Complete [XR Setup](docs/XR_SETUP.md) before using a headset.

**Condo gameplay:** `Assets/CEVR/Generated/Scenes/Condo.unity` now supports the House-style preparation/earthquake/evacuation loop with editable Condo-specific table, chairs, cover trigger, pillow, safety shoes, cabinet hazards and assembly target. Its existing architectural mesh/prefabs are preserved. Open the Condo scene in **Edit mode** once so the editor can save the new `Condo Gameplay` hierarchy; use **CEVR > Condo > Prepare editable gameplay (once)** if necessary. The Main Menu has **Play Condo** next to Tutorial and House. Read [Edit Condo](docs/EDIT_CONDO.md) for the placement and test checklist. This is a prototype setup: confirm the furniture/exit positions in Unity and commit the saved scene and `CondoBake_*` assets afterward.

**Editing the Tutorial furniture:** open `Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity` in Edit mode. On first opening, CEVR automatically bakes and saves its Play-mode furniture, props, lighting and decoration as editable Unity scene objects. Look under `Tutorial Editable Furniture`, `DynamicProps` and `CEVR_FinalVisualPolish`, edit the **root** GameObjects and save. This first bake also creates a scene backup. Commit the updated `.unity` file and `Assets/CEVR/Generated/TutorialBake_*` assets with `.meta` files so collaborators can see your changes. Read [Edit Tutorial](docs/EDIT_TUTORIAL.md) for verification.

**Do not run** `Tools > CEVR > 1. Build Chula Engineering Tutorial Stage` after editing the scene: that generator **replaces** the Tutorial and discards your edits. Use `CEVR > Tutorial > Check saved editable furniture` to inspect the layout.

## Repository structure

| Path | Responsibility | Primary owner |
|---|---|---|
| `Assets/CEVR/Runtime` | Gameplay, motion, hazards, logging, UI, and player adapters | Programmer |
| `Assets/CEVR/Editor` | Stage builder, CSV importer, and scene validation | Programmer / technical designer |
| `Assets/CEVR/Tests` | EditMode tests | Programmer / reviewer |
| `Assets/CEVR/SampleData` | Importer demonstration data, never research evidence | Programmer |
| `Assets/CEVR/Generated` | Scene, materials, and config created by Unity | Level owner; commit after diff review |
| `Packages` | Pinned direct package versions | Maintainer |
| `ProjectSettings` | Unity and XR project configuration | Maintainer and reviewer |
| `docs` | Design, setup, testing, research, and collaboration guidance | Whole team |
| `Tools/verify_repo.py` | Fast static preflight outside Unity | Every pull request |

## Verification command

```bash
python3 Tools/verify_repo.py
```

Then open `Window > General > Test Runner > EditMode > Run All` in Unity and complete [Test Plan](docs/TEST_PLAN.md). A passing static verifier does not prove that Unity compilation, OpenXR, or a headset build works.

## Training and Research modes

- **Training mode** displays direct safety prompts and may use the seeded tutorial preview motion.
- **Research mode** uses neutral prompts and rejects startup unless a validated recorded `QuakeProfile` is assigned.

Change the mode in `Assets/CEVR/Generated/Data/CEVR_TutorialScenario.asset` after generating the stage. Read [Research and Data Governance](docs/RESEARCH_AND_DATA.md) before collecting data from any person.

## Documentation

- [Gameplay and stage specification](docs/GAMEPLAY_STAGE_SPEC.md)
- [Cross-scene feature guide](docs/SCENE_FEATURES.md)
- [Unity setup](docs/UNITY_SETUP.md)
- [OpenXR and headset setup](docs/XR_SETUP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Test plan and acceptance gates](docs/TEST_PLAN.md)
- [GitHub workflow](docs/GITHUB_WORKFLOW.md)
- [Research and data governance](docs/RESEARCH_AND_DATA.md)
- [Ground-motion data format](docs/GROUND_MOTION.md)
- [ICE pre-project proposal report](report/CEVR_ICE_PreProject_Proposal_Report.pdf)

## Deliberate MVP boundaries

- The system studies participant response; it does not predict structural damage.
- Floor-level conditions require calibrated `FloorResponseProfile` assets. A generic square-root-of-floor formula is not accepted as research evidence.
- Eye tracking, crowds, full glass fracture, and forced camera motion are excluded from the MVP.
- Primitive graybox geometry is intentional so that interaction logic can be verified before licensed art assets are introduced.

## Official technical references

- Unity XR Interaction Toolkit 3.1: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/
- XRI general setup: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/general-setup.html
- GitHub Git LFS: https://docs.github.com/en/repositories/working-with-files/managing-large-files/configuring-git-large-file-storage
