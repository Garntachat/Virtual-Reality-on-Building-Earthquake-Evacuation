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
    "Assets/CEVR/Runtime/Core/GeneratedStageInfo.cs",
    "Assets/CEVR/Runtime/Core/GameFlowController.cs",
    "Assets/CEVR/Runtime/Core/RuntimeStageRepair.cs",
    "Assets/CEVR/Runtime/Player/DesktopGrabInteractor.cs",
    "Assets/CEVR/Runtime/Player/MovableFurniture.cs",
    "Assets/CEVR/Tests/EditMode/StageIntegrityTests.cs",
    "START_HERE.md",
    "docs/UNITY_SETUP.md",
    "docs/TEST_PLAN.md",
    "docs/XR_SETUP.md",
    "docs/GAMEPLAY_STAGE_SPEC.md",
    "docs/GITHUB_WORKFLOW.md",
    "docs/RESEARCH_AND_DATA.md",
    "docs/GROUND_MOTION.md",
    "report/CEVR_ICE_PreProject_Proposal_Report.docx",
    "report/CEVR_ICE_PreProject_Proposal_Report.pdf",
)

EXPECTED_PACKAGES = {
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
        'new GameObject("MovableChair_StrongTableApproach")',
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
        if thai.search(path.read_text(encoding="utf-8")):
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
