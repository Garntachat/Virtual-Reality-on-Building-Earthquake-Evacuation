#!/usr/bin/env python3
"""Fast repository checks that do not replace Unity compilation or device tests."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED = (
    ".gitattributes",
    ".gitignore",
    "Packages/manifest.json",
    "ProjectSettings/ProjectVersion.txt",
    "Assets/CEVR/Runtime/CEVR.Runtime.asmdef",
    "Assets/CEVR/Editor/CEVR.Editor.asmdef",
    "Assets/CEVR/Tests/EditMode/CEVR.Tests.asmdef",
    "Assets/CEVR/Editor/ChulaTutorialStageBuilder.cs",
    "Assets/CEVR/Editor/HouseProBuilderLayoutAnalyzer.cs",
    "Assets/CEVR/Editor/HouseProBuilderLayoutAnalyzer.cs.meta",
    "Assets/CEVR/Editor/EditableSceneMaterialRepair.cs",
    "Assets/CEVR/Editor/EditableSceneMaterialRepair.cs.meta",
    "Assets/CEVR/Runtime/Core/GeneratedStageInfo.cs",
    "Assets/CEVR/Runtime/Core/GameFlowController.cs",
    "Assets/CEVR/Runtime/Core/RuntimeStageRepair.cs",
    "Assets/CEVR/Runtime/Core/UniversalSceneGameplayBootstrap.cs",
    "Assets/CEVR/Runtime/Core/HouseScenarioController.cs",
    "Assets/CEVR/Runtime/Core/HouseSceneLayout.cs",
    "Assets/CEVR/Runtime/Core/HouseSceneLayout.cs.meta",
    "Assets/CEVR/Runtime/Core/RenderPipelineMaterialRepair.cs",
    "Assets/CEVR/Runtime/Core/RenderPipelineMaterialRepair.cs.meta",
    "Assets/CEVR/Runtime/Earthquake/GroundMotionPlayer.cs",
    "Assets/CEVR/Runtime/Earthquake/InertialRigidbody.cs",
    "Assets/CEVR/Runtime/Earthquake/EarthquakeSceneResponseInstaller.cs",
    "Assets/CEVR/Runtime/Earthquake/EarthquakeSceneResponseInstaller.cs.meta",
    "Assets/CEVR/Runtime/Effects/DecorativeQuakeResponse.cs",
    "Assets/CEVR/Runtime/Effects/DecorativeQuakeResponse.cs.meta",
    "Assets/CEVR/Runtime/Audio/GameplayAudioDirector.cs",
    "Assets/CEVR/Runtime/Audio/FurnitureImpactAudio.cs",
    "Assets/CEVR/Runtime/Effects/TutorialVisualPolish.cs",
    "Assets/CEVR/Runtime/Effects/BreakableWindow.cs",
    "Assets/CEVR/Runtime/Effects/WindowView.cs",
    "Assets/CEVR/Runtime/Hazards/ToppleableFurniture.cs",
    "Assets/CEVR/Runtime/Hazards/BrokenGlassHazard.cs",
    "Assets/CEVR/Runtime/Player/DesktopGrabInteractor.cs",
    "Assets/CEVR/Runtime/Player/ProtectivePillow.cs",
    "Assets/CEVR/Runtime/Player/WearableShoes.cs",
    "Assets/CEVR/Runtime/Player/ThirdPersonViewController.cs",
    "Assets/CEVR/Runtime/Player/LocalMultiplayerManager.cs",
    "Assets/CEVR/Runtime/Player/MovableFurniture.cs",
    "Assets/CEVR/Tests/EditMode/StageIntegrityTests.cs",
    "START_HERE.md",
    "docs/UNITY_SETUP.md",
    "docs/TEST_PLAN.md",
    "docs/XR_SETUP.md",
    "docs/GAMEPLAY_STAGE_SPEC.md",
    "docs/SCENE_FEATURES.md",
    "docs/GITHUB_WORKFLOW.md",
    "docs/RESEARCH_AND_DATA.md",
    "docs/GROUND_MOTION.md",
    "report/CEVR_ICE_PreProject_Proposal_Report.docx",
    "report/CEVR_ICE_PreProject_Proposal_Report.pdf",
)

EXPECTED_PACKAGES = {
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.inputsystem": "1.14.2",
    "com.unity.xr.interaction.toolkit": "3.1.3",
    "com.unity.xr.openxr": "1.18.0",
    "com.unity.test-framework": "1.6.0",
}

FORBIDDEN_TRACKED_DIRS = {"Library", "Temp", "Obj", "Build", "Builds", "Logs", "UserSettings"}


def fail(errors: list[str], message: str) -> None:
    errors.append(message)


def check_required(errors: list[str]) -> None:
    for relative in REQUIRED:
        if not (ROOT / relative).is_file():
            fail(errors, f"missing required file: {relative}")


def check_manifest(errors: list[str]) -> None:
    try:
        manifest = json.loads((ROOT / "Packages/manifest.json").read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(errors, f"invalid Packages/manifest.json: {exc}")
        return
    dependencies = manifest.get("dependencies", {})
    for package, expected in EXPECTED_PACKAGES.items():
        actual = dependencies.get(package)
        if actual != expected:
            fail(errors, f"package {package}: expected {expected}, found {actual!r}")


def check_json_files(errors: list[str]) -> None:
    for pattern in ("*.json", "*.asmdef"):
        for path in ROOT.rglob(pattern):
            try:
                json.loads(path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError) as exc:
                fail(errors, f"invalid JSON in {path.relative_to(ROOT)}: {exc}")


def check_project_version(errors: list[str]) -> None:
    version_file = ROOT / "ProjectSettings/ProjectVersion.txt"
    if version_file.is_file() and "6000.3.20f1" not in version_file.read_text(encoding="utf-8"):
        fail(errors, "ProjectVersion.txt does not declare Unity 6000.3.20f1")


def check_project_safety_defaults(errors: list[str]) -> None:
    settings_path = ROOT / "ProjectSettings/ProjectSettings.asset"
    dynamics_path = ROOT / "ProjectSettings/DynamicsManager.asset"
    time_path = ROOT / "ProjectSettings/TimeManager.asset"
    try:
        settings = settings_path.read_text(encoding="utf-8")
        dynamics = dynamics_path.read_text(encoding="utf-8")
        time_settings = time_path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read project safety settings: {exc}")
        return
    expected_settings = (
        "companyName: CEVR Student Research Team",
        "m_ActiveColorSpace: 1",
        "runInBackground: 1",
        "submitAnalytics: 0",
        "resizableWindow: 1",
        "bundleVersion: 0.3.1",
        "enableFrameTimingStats: 1",
    )
    for value in expected_settings:
        if value not in settings:
            fail(errors, f"required PlayerSettings value is missing: {value}")
    version_marker = (ROOT / "Assets/CEVR/Runtime/Core/GeneratedStageInfo.cs").read_text(encoding="utf-8")
    if 'CurrentVersion = "0.5.0"' not in version_marker:
        fail(errors, "CEVR release marker must declare version 0.5.0")
    logger = (ROOT / "Assets/CEVR/Runtime/Logging/SessionLogger.cs").read_text(encoding="utf-8")
    if "buildVersion = GeneratedStageInfo.CurrentVersion" not in logger:
        fail(errors, "session logs must use the CEVR release marker")
    if "m_EnableEnhancedDeterminism: 1" not in dynamics:
        fail(errors, "enhanced physics determinism must remain enabled")
    if "Maximum Allowed Timestep: 0.1" not in time_settings:
        fail(errors, "maximum allowed timestep must remain 0.1 seconds")


def check_meta_files(errors: list[str]) -> None:
    assets = ROOT / "Assets"
    if not assets.is_dir():
        fail(errors, "Assets directory is missing")
        return
    for path in assets.rglob("*"):
        if path.name.endswith(".meta"):
            continue
        meta = path.with_name(path.name + ".meta")
        if not meta.is_file():
            fail(errors, f"missing Unity meta: {meta.relative_to(ROOT)}")


def check_csharp_policies(errors: list[str]) -> None:
    earthquake_files = list((ROOT / "Assets/CEVR/Runtime/Earthquake").glob("*.cs"))
    for path in earthquake_files:
        text = path.read_text(encoding="utf-8")
        if "Camera.main.transform" in text or re.search(r"\bAddTorque\s*\(", text):
            fail(errors, f"camera motion or ad-hoc torque found in earthquake system: {path.relative_to(ROOT)}")
        if "Random.Range" in text:
            fail(errors, f"non-deterministic Random.Range found in earthquake system: {path.relative_to(ROOT)}")
    all_cs = list((ROOT / "Assets/CEVR").rglob("*.cs"))
    for path in all_cs:
        if "\t" in path.read_text(encoding="utf-8"):
            fail(errors, f"tab indentation found: {path.relative_to(ROOT)}")


def check_runtime_repair_contract(errors: list[str]) -> None:
    repair_path = ROOT / "Assets/CEVR/Runtime/Core/RuntimeStageRepair.cs"
    flow_path = ROOT / "Assets/CEVR/Runtime/Core/GameFlowController.cs"
    try:
        repair = repair_path.read_text(encoding="utf-8")
        flow = flow_path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read runtime repair contract: {exc}")
        return

    required_repairs = (
        'GameObject.Find("StageDisclaimer")',
        'GameObject.Find("ExitSign")',
        'GameObject.Find("DynamicProps")',
        "RenderMode.ScreenSpaceOverlay",
        "AddComponent<DesktopGrabInteractor>()",
        '"MovableChair_StrongTableApproach", "chair-strong-table-01"',
        '"MovableChair_LabBenchNorth", "chair-lab-north-01"',
        '"MovableChair_LabBenchSouth", "chair-lab-south-01"',
        '"MovableChair_Spare", "chair-spare-01"',
        "AddComponent<Rigidbody>()",
        "AddComponent<MovableFurniture>()",
        "TryAddXrGrabInteractable(chair)",
    )
    for fragment in required_repairs:
        if fragment not in repair:
            fail(errors, f"runtime legacy-scene repair is missing: {fragment}")

    repair_call = flow.find("RuntimeStageRepair.EnsurePlayableStage();")
    validation_call = flow.find("ValidateSetup(out string error)")
    if repair_call < 0 or validation_call < 0 or repair_call > validation_call:
        fail(errors, "GameFlowController must repair the committed scene before validating it")


def check_crawl_contract(errors: list[str]) -> None:
    path = ROOT / "Assets/CEVR/Runtime/Player/DesktopDebugRig.cs"
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read desktop crawl controller: {exc}")
        return
    required = (
        "Keyboard.current.zKey.wasPressedThisFrame",
        "crawlingHeight = 0.58f",
        "crawlMoveMultiplier = 0.68f",
        "HasClearance(requestedHeight)",
        "ApplyHeight(requestedHeight)",
    )
    for fragment in required:
        if fragment not in text:
            fail(errors, f"desktop crawl contract is missing: {fragment}")

    avatar_path = ROOT / "Assets/CEVR/Runtime/Player/StudentAvatar.cs"
    try:
        avatar = avatar_path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read student crawl pose: {exc}")
        return
    pose_required = (
        "CrawlHeightThreshold = 0.72f",
        "PronePitchDegrees = 84f",
        "PronePositionOffset = new Vector3(0f, 0.18f, -0.72f)",
        "crawlBlend = Mathf.MoveTowards",
        "Quaternion.Slerp(stanceRotation, proneRotation, crawlPose)",
        "model.localRotation = Quaternion.Slerp(baseRotation, jumpRotation, jumpPose)",
        "transform.localScale = Vector3.one",
    )
    for fragment in pose_required:
        if fragment not in avatar:
            fail(errors, f"student prone-pose contract is missing: {fragment}")
    if "controller.height / 1.75f" in avatar:
        fail(errors, "student crawl must not squash the avatar to match collider height")


def check_visual_polish_contract(errors: list[str]) -> None:
    path = ROOT / "Assets/CEVR/Runtime/Effects/TutorialVisualPolish.cs"
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read tutorial visual polish: {exc}")
        return
    required = (
        "ApplyScenePalette()",
        "ConfigureLightingAndCamera()",
        "BuildWayfindingAndSafetyMarkers()",
        "PolishHud()",
        "BuildCoverOutline()",
        "BuildExitPath()",
        "BuildChairBeacon()",
        "BuildHazardBoundary()",
        "BuildEngineeringWorkstations()",
        "BuildSafetyEquipment()",
        "BuildComfortAndStorageDecor()",
        "RenderMode.ScreenSpaceOverlay",
        "collider.enabled = false",
    )
    repair = (ROOT / "Assets/CEVR/Runtime/Core/RuntimeStageRepair.cs").read_text(encoding="utf-8")
    for fragment in required:
        if fragment not in text and fragment not in repair:
            fail(errors, f"final visual-polish contract is missing: {fragment}")
    if "TutorialVisualPolish.EnsureApplied()" not in repair:
        fail(errors, "runtime scene preparation must apply TutorialVisualPolish")
    if "playerCamera.transform" in text or "Camera.main.transform" in text:
        fail(errors, "visual polish must never move or rotate the participant camera")

    grab = (ROOT / "Assets/CEVR/Runtime/Player/DesktopGrabInteractor.cs").read_text(encoding="utf-8")
    for fragment in ("HasGrabbableTarget()", "targetAvailable", "GUI.backgroundColor"):
        if fragment not in grab:
            fail(errors, f"final interaction-feedback contract is missing: {fragment}")

    hud = (ROOT / "Assets/CEVR/Runtime/UI/TutorialHud.cs").read_text(encoding="utf-8")
    if "PhaseColor(phase)" not in hud:
        fail(errors, "final HUD must provide phase-specific status colors")


def check_cross_scene_feature_contract(errors: list[str]) -> None:
    bootstrap_path = ROOT / "Assets/CEVR/Runtime/Core/UniversalSceneGameplayBootstrap.cs"
    house_scene = ROOT / "Assets/CEVR/Generated/Scenes/House.unity"
    build_settings = ROOT / "ProjectSettings/EditorBuildSettings.asset"
    try:
        bootstrap = bootstrap_path.read_text(encoding="utf-8")
        settings = build_settings.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read cross-scene feature contract: {exc}")
        return
    required = (
        "RuntimeInitializeOnLoadMethod",
        'sceneName.Contains("house")',
        "InstallTutorialFeatures()",
        "InstallHouseScenario()",
        'CreateChair("HouseChair_CoverObstacle"',
        'CreateChair("HouseChair_DiningLeft"',
        'CreateChair("HouseChair_DiningRight"',
        'CreateChair("HouseChair_Spare"',
        "EnsureShoes(",
        "EnsurePillow(",
        "BuildHouseWindows(",
        "HouseSceneLayout.PlayerSpawn",
        "HouseSceneLayout.WindowCenter",
        "HouseSceneLayout.DiningTable",
        "CreateTopplingCabinet(",
        "CreateFallingProp(",
        "HouseScenarioController",
        "ThirdPersonViewController",
        "LocalMultiplayerManager",
        "WearableSafetyShoes_P2",
        "AddComponent<WindowView>()",
        "EarthquakeSceneResponseInstaller",
        "GameplayAudioDirector",
    )
    for fragment in required:
        if fragment not in bootstrap:
            fail(errors, f"cross-scene gameplay installer is missing: {fragment}")
    if not house_scene.is_file() or house_scene.stat().st_size < 1_000_000:
        fail(errors, "the hand-built House.unity scene is missing or unexpectedly replaced")
    if "Assets/CEVR/Generated/Scenes/House.unity" not in settings:
        fail(errors, "House.unity must be enabled in EditorBuildSettings")

    feature_contracts = {
        "Assets/CEVR/Editor/EditableSceneMaterialRepair.cs": ("EditorSceneManager.sceneOpened", "EditorApplication.hierarchyChanged", "Repair Pink Materials In Open Scenes", "NeedsRepair", "MarkSceneDirty"),
        "Assets/CEVR/Runtime/Earthquake/EarthquakeSceneResponseInstaller.cs": ("BindPhysicsObjects", "BindDecorativeObjects", "InertialRigidbody", "TryAddToppleResponse", "GetComponentInParent<PlayerHealth>()", "IsUsingPreview"),
        "Assets/CEVR/Runtime/Effects/DecorativeQuakeResponse.cs": ("CurrentFloorAccelerationMs2", "PresentationIntensity", "DecorativeQuakeMode.Hanging", "StablePhase", "OnDisable"),
        "Assets/CEVR/Runtime/Effects/BreakableWindow.cs": ("NormalizedIntensity", "window_cracked"),
        "Assets/CEVR/Runtime/Effects/WindowView.cs": ("ApplyTransparentGlass", "BuildExteriorView", "ExteriorView"),
        "Assets/CEVR/Runtime/Hazards/FallingHazard.cs": ("CheckSweptPlayerContact", "Physics.OverlapBox", "ApplyDamage"),
        "Assets/CEVR/Runtime/Hazards/ToppleableFurniture.cs": ("AddForceAtPosition", "IsPlaying"),
        "Assets/CEVR/Runtime/Audio/GameplayAudioDirector.cs": ("PresentationIntensity", "CEVR_Rumble", "CEVR_Footstep", "CEVR_WindowCrack", "GameplayAudioCue.Success"),
        "Assets/CEVR/Runtime/Audio/FurnitureImpactAudio.cs": ("FurnitureScrape", "FurnitureImpact", "OnCollisionEnter"),
        "Assets/CEVR/Runtime/Player/WearableShoes.cs": ("EnsureAuthoredVisual()", "CEVR_LowPolySafetyShoe", "Equip(", "footwear_equipped"),
        "Assets/CEVR/Runtime/Player/ProtectivePillow.cs": ("SetProtection", "pillow_cover_started"),
        "Assets/CEVR/Runtime/Player/ThirdPersonViewController.cs": ("tKey.wasPressedThisFrame", "SphereCast"),
        "Assets/CEVR/Runtime/Player/LocalMultiplayerManager.cs": ("f2Key.wasPressedThisFrame", "LocalPlayer2", "Configure(secondCamera, true)"),
        "Assets/CEVR/Runtime/Core/HouseScenarioController.cs": ("preparationSeconds = 30f", "house_tutorial_success"),
        "Assets/CEVR/Runtime/Core/HouseSceneLayout.cs": ("FloorY = 1.0f", "WindowCenter", "DiningTable", "WardrobeCenter", "FridgeCenter", "OverheadHazards"),
    }
    for relative, fragments in feature_contracts.items():
        try:
            text = (ROOT / relative).read_text(encoding="utf-8")
        except OSError as exc:
            fail(errors, f"could not read feature contract {relative}: {exc}")
            continue
        for fragment in fragments:
            if fragment not in text:
                fail(errors, f"feature contract {relative} is missing: {fragment}")

    health = (ROOT / "Assets/CEVR/Runtime/Player/PlayerHealth.cs").read_text(encoding="utf-8")
    if "HashSet<string> protectionSources" not in health:
        fail(errors, "cover and pillow protection must use independent protection sources")
    if "HasProtectiveFootwear" not in health:
        fail(errors, "player health must retain the equipped footwear state")

    glass = (ROOT / "Assets/CEVR/Runtime/Hazards/BrokenGlassHazard.cs").read_text(encoding="utf-8")
    for fragment in ("glass-barefoot", "glass-with-footwear", "HasProtectiveFootwear"):
        if fragment not in glass:
            fail(errors, f"functional broken-glass footwear contract is missing: {fragment}")
    grab = (ROOT / "Assets/CEVR/Runtime/Player/DesktopGrabInteractor.cs").read_text(encoding="utf-8")
    for fragment in ("rightShiftKey.wasPressedThisFrame", "ClaimedBodies.Contains(body)", "ClaimedBodies.Add(body)"):
        if fragment not in grab:
            fail(errors, f"local multiplayer interaction ownership is missing: {fragment}")


def check_team_furniture(errors: list[str]) -> None:
    folder = ROOT / "Assets/CEVR/Resources/PlengFurniture"
    models = (
        "Bed", "Bed_Pillow", "DiningChair", "DiningTable", "Fridge",
        "Sofa", "Sofa_Pillows", "Vase", "Wandrobe",
    )
    for model in models:
        path = folder / f"{model}.fbx"
        if not path.is_file():
            fail(errors, f"missing team furniture model: {model}.fbx")
            continue
        if path.stat().st_size < 4_000 or not path.read_bytes().startswith(b"Kaydara FBX Binary"):
            fail(errors, f"team furniture is not a real embedded FBX (possible LFS pointer): {model}.fbx")

    attributes = (ROOT / ".gitattributes").read_text(encoding="utf-8")
    if "Assets/CEVR/Resources/PlengFurniture/*.fbx -filter -diff -merge -text" not in attributes:
        fail(errors, "small team furniture must remain directly cloneable without Git LFS")

    dressing_path = ROOT / "Assets/CEVR/Runtime/Effects/FurnitureSceneDressing.cs"
    try:
        dressing = dressing_path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read team furniture integration: {exc}")
        return
    required = (
        'TeamFurniturePath = "PlengFurniture/"',
        "ReplaceWithTeamModel(",
        "ReplaceWithTeamModelAt(",
        "ConvertMaterialsForActivePipeline(",
        "IsCollisionVisual(",
        "CreateShakingVase(",
        '"DiningChair"', '"DiningTable"', '"Bed_Pillow"', '"Fridge"',
        '"Wandrobe"', '"Sofa"', '"Sofa_Pillows"', '"Bed"', '"Vase"',
    )
    for fragment in required:
        if fragment not in dressing:
            fail(errors, f"team furniture integration is missing: {fragment}")

    if re.search(r"\\bstring\\s+table\\b[\\s\\S]*\\bVector3\\s+table\\b", dressing):
        fail(errors, "FurnitureSceneDressing.Start has a C# CS0136 local variable collision for table")


def check_house_layout_analyzer(errors: list[str]) -> None:
    analyzer_path = ROOT / "Assets/CEVR/Editor/HouseProBuilderLayoutAnalyzer.cs"
    legacy_path = ROOT / "Assets/CEVR/Runtime/Effects/HouseResourceFurnitureLayout.cs"
    try:
        analyzer = analyzer_path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(errors, f"could not read House layout analyzer: {exc}")
        return

    required = (
        "OpenSceneMode.Additive",
        "EditorSceneManager.CloseScene(house, true)",
        "BelongsToScene(",
        "DetectHorizontalLevels(",
        "AnalyzeWalkableLevels(",
        "Physics.OverlapCapsule(",
        "AnalyzeExteriorOpenings(",
        "AnalyzePlengFurniture(",
        "ModelImporter importer",
        "Mode: READ-ONLY",
    )
    for fragment in required:
        if fragment not in analyzer:
            fail(errors, f"House layout analyzer safety contract is missing: {fragment}")

    for forbidden in ("SaveScene(", "SaveOpenScenes(", "SaveCurrentModifiedScenesIfUserWantsTo("):
        if forbidden in analyzer:
            fail(errors, f"House layout analyzer must remain read-only; found: {forbidden}")

    if legacy_path.exists():
        fail(errors, "unverified hard-coded HouseResourceFurnitureLayout.cs must not return")


def check_main_menu(errors: list[str]) -> None:
    scene_dir = "Assets/CEVR/Generated/Scenes/"
    expected = ["CEVR_MainMenu", "CEVR_ChulaEngineering_Tutorial", "House"]
    settings = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text()
    paths = re.findall(r"- enabled: 1\s+path: (.+)", settings)
    if paths[:3] != [scene_dir + name + ".unity" for name in expected]:
        fail(errors, "main menu must be first, followed by enabled Tutorial and House scenes")
    for name in expected:
        scene = ROOT / (scene_dir + name + ".unity")
        if not scene.is_file():
            fail(errors, "missing menu destination: " + name)
    menu_path = ROOT / "Assets/CEVR/Runtime/UI/MainMenuController.cs"
    appearance_path = ROOT / "Assets/CEVR/Runtime/Player/StudentAppearance.cs"
    for path in (menu_path, appearance_path):
        if not path.is_file():
            fail(errors, "missing menu implementation: " + str(path))
            return
    menu = menu_path.read_text()
    for token in ("LoadSceneAsync", "CanStreamedLevelBeLoaded", "if (loading) return", "Application.Quit", "isPlaying = false", "StudentAppearance.Select"):
        if token not in menu:
            fail(errors, "menu contract missing: " + token)
    bootstrap = (ROOT / "Assets/CEVR/Runtime/Core/UniversalSceneGameplayBootstrap.cs").read_text()
    if "if (scene.name == MainMenuController.MenuScene) return;" not in bootstrap:
        fail(errors, "menu must not install earthquake gameplay")


def check_git_hygiene(errors: list[str]) -> None:
    for name in FORBIDDEN_TRACKED_DIRS:
        if (ROOT / name).exists():
            fail(errors, f"generated Unity directory exists in deliverable: {name}/")
    attributes = (ROOT / ".gitattributes").read_text(encoding="utf-8") if (ROOT / ".gitattributes").is_file() else ""
    for extension in ("*.fbx", "*.png", "*.wav"):
        if extension not in attributes or "filter=lfs" not in attributes:
            fail(errors, f"Git LFS rule missing for {extension}")
    for log in ROOT.rglob("*.jsonl"):
        fail(errors, f"research/session log must not be in repository: {log.relative_to(ROOT)}")


def check_english_only(errors: list[str]) -> None:
    text_suffixes = {".md", ".cs", ".py", ".json", ".asmdef", ".yml", ".yaml", ".txt", ".csv"}
    thai = re.compile(r"[\u0E00-\u0E7F]")
    for path in ROOT.rglob("*"):
        if not path.is_file() or path.suffix.lower() not in text_suffixes:
            continue
        if thai.search(path.read_text(encoding="utf-8", errors="replace")):
            fail(errors, f"Thai text remains in English-only deliverable: {path.relative_to(ROOT)}")


def main() -> int:
    errors: list[str] = []
    check_required(errors)
    check_manifest(errors)
    check_json_files(errors)
    check_project_version(errors)
    check_project_safety_defaults(errors)
    check_meta_files(errors)
    check_csharp_policies(errors)
    check_runtime_repair_contract(errors)
    check_crawl_contract(errors)
    check_visual_polish_contract(errors)
    check_cross_scene_feature_contract(errors)
    check_team_furniture(errors)
    check_house_layout_analyzer(errors)
    check_main_menu(errors)
    check_git_hygiene(errors)
    check_english_only(errors)
    if errors:
        print(f"CEVR STATIC VERIFICATION: FAIL ({len(errors)} issue(s))")
        for error in errors:
            print(f" - {error}")
        return 1
    file_count = sum(1 for path in ROOT.rglob("*") if path.is_file())
    print(f"CEVR STATIC VERIFICATION: PASS ({file_count} files checked structurally)")
    print("Next gates: Unity compile, EditMode tests, scene validator, OpenXR validation, headset test.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
