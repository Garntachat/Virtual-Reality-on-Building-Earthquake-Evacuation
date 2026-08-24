# OpenXR and XR Interaction Toolkit Setup

The generated scene starts with a `DesktopDebugPlayer` so development can continue without a headset. These steps replace it with one active XR Origin while preserving the existing gameplay systems.

## 1. Import required packages and samples

1. Open `Window > Package Manager`.
2. Confirm XR Interaction Toolkit `3.1.3` and OpenXR Plugin `1.18.0`.
3. Import the XRI **Starter Assets** sample.
4. Import **XR Interaction Simulator** for headset-free interaction testing.
5. If Unity asks to enable the Input System backend, accept and restart the Editor.

## 2. Enable OpenXR

1. Open `Edit > Project Settings > XR Plug-in Management`.
2. Select the actual build target.
3. Enable OpenXR only for that target.
4. Add the interaction profile for the controller model that will be tested.
5. Open OpenXR Project Validation. Apply only fixes the team understands, and review resulting ProjectSettings diffs.

## 3. Add one XR Origin

1. Open the generated tutorial scene.
2. Add `GameObject > XR > XR Origin (VR)` using the menu provided by the installed XRI version.
3. Confirm that it contains Camera Offset, Main Camera, Left Controller, and Right Controller.
4. Assign the XRI default input actions from Starter Assets.
5. Keep exactly one active XR Origin and one active AudioListener.

## 4. Connect the XR body to CEVR

On the XR Origin root:

1. Add `PlayerHealth`.
2. Add a non-trigger `CapsuleCollider`.
3. Add a `Rigidbody`; enable Is Kinematic, disable Use Gravity, and enable Interpolate.
4. Add `XRBodyColliderFollower`; assign the XR Main Camera to **Head**.
5. Set the root tag to `Player`.

On `GameplaySystems > GameFlowController`, replace the Health reference with the PlayerHealth on the XR Origin. Do not change the other references.

On `GameplaySystems > PoseTelemetrySampler`, assign:

- Logger: SessionLogger on GameplaySystems
- Head: XR Main Camera
- Left Hand: Left Controller
- Right Hand: Right Controller

Disable the entire `DesktopDebugPlayer` root to remove its camera, AudioListener, and controller.

## 5. Interaction checks

- Each task object has Rigidbody, Collider, and XRGrabInteractable.
- Direct or ray interactors use the scene's XR Interaction Manager.
- Both task objects can be grabbed and placed into their correct goals.
- The body collider enters CoverZone and ExitAssemblyZone triggers.
- Locomotion configuration follows the approved comfort protocol.
- No quake component moves the XR Origin or camera.

## 6. Headset emergency stop

Keyboard emergency stop is provided for the facilitator. Before pilot testing, create a controller Input Action that invokes `GameFlowController.AbortTutorial()` through a small action callback. Do not rely on unplugging the headset or terminating the application as the primary stop method.

## 7. Device acceptance gates

1. OpenXR Project Validation has no blocking issue.
2. Controller tracking and handedness are correct.
3. Real and virtual floors align; guardian or boundary protection is active.
4. The participant cannot collide with a real table, wall, cable, or observer.
5. Frame pacing is stable at the target headset refresh rate.
6. Participant and facilitator stop controls both work.
7. An internal team pilot passes before any external participant uses the system.

Official references:

- https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/general-setup.html
- https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/xr-interaction-simulator-overview.html

