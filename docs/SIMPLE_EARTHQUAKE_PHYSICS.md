# Simple, editable earthquake physics in Unity

The goal is **one understandable physics system** for Tutorial, House, Condo and standalone New_House furniture. The shared `GroundMotionPlayer` still controls the beginning/end of shaking, timed hazards, warning cues, gameplay outcomes and recorded research profiles. Only the force applied to individual Rigidbody objects has changed.

## One Rigidbody, one shaker

Select a chair, cabinet, vase or other physical object in the **Hierarchy in Edit mode**, not during Play. It should have Unity's normal `Rigidbody` and a small `Inertial Rigidbody` component. The latter now shows the real Rigidbody properties at the top:

| Inspector property | Simple meaning |
| --- | --- |
| **Mass (kg)** | Heavier items accelerate less for the same force |
| **Linear Damping** | How quickly side-to-side movement slows |
| **Angular Damping** | How quickly rotation/tipping slows |
| **Force (N)** | Horizontal force applied with `Rigidbody.AddForce(..., ForceMode.Force)` (default 15 N) |
| **Torque (N m)** | Rotational force applied with `Rigidbody.AddTorque(..., ForceMode.Force)` (default 5 N m) |
| **Shake Strength** | 0 = off; 1 = normal; 2 = twice the force/torque |
| **Allow Vertical Shake** | Off to keep the vibration horizontal |

All forces come from the *same* repeatable shaking signal; there are **no random forces every frame**, no extra hidden mass multiplier, and no second force from `ToppleableFurniture`. That component only tells the single shaker to apply its force higher on a tall object, creating an understandable tipping effect. Existing hazard release, grabbing, collision, cover protection and evacuation gameplay are retained.

For example, with **15 N Force** and **Shake Strength 1**, an object of mass **5 kg** receives about **3 m/s² peak horizontal acceleration** before damping/collisions, while one of mass **15 kg** receives about **1 m/s²**. The earthquake envelope rises and falls, so these are illustrative peak values, not constant motion.

**Important:** `Is Kinematic` means Unity physics will not move the Rigidbody until gameplay releases it. The hazard director deliberately keeps falling hazards kinematic before they are released. Freeze Rotation/Position constraints still work and are never silently changed.

## Getting the editable components into saved scenes

**House:** Open `House.unity` in Unity Edit mode after pulling the update. On first open, the tool attaches a **real Rigidbody, BoxCollider and InertialRigidbody to each of the four existing `HouseLayout_DiningChair_*` parent objects**, then saves the scene with a backup. It **does not create furniture or change any saved transform**. It preserves any existing authored Mass, Damping, collider shape or Shake Strength and adds defaults only when components are missing. Click a child mesh such as **Seat** and the Editor will automatically select the physical chair parent so you see its Mass and Damping controls. If you need to select a mesh child specifically, turn off **CEVR > Earthquake > Select chair physics when clicking a chair part**.

**New_House:** The standalone scene already has many Rigidbodies. On first open, the tool adds `Inertial Rigidbody` to eligible saved physics objects without changing Mass or Damping. It also creates a scene backup.

**Tutorial and Condo:** Their editable-scene tools already add this component to generated furniture. For any additional manually created Rigidbody objects, run **CEVR > Earthquake > Add simple physics settings to scene** (outside Play mode). This command is safe to repeat; it skips objects that already have the component and does not reset Inspector values.

**Quick House check:** After opening House in Edit mode, choose **CEVR > Earthquake > Check House chair physics**. It should report **4/4**. If not, run **CEVR > Earthquake > Add simple physics settings to scene**. Select a chair seat again and check that the Inspector shows both **Rigidbody** and **Inertial Rigidbody** on its parent.

**Testing any scene directly:** Unity's normal Play button opens the Main Menu, so use **CEVR > Earthquake > Test current scene (not Main Menu)** to start from the open House or New_House scene. When you stop Play, the normal Main Menu startup is restored.

**New_House standalone:** The older `EarthquakeFurnitureController` can still be tested by checking `Is Earthquake Active` during Play mode when no shared `GroundMotionPlayer` exists. It now uses a smooth repeatable wave with `Base Force` and `Base Torque` instead of random impulses, and also respects the per-object `Inertial Rigidbody` settings. Do not turn it on alongside the scenario quake; it automatically avoids stacking forces if a shared source exists.

**Decorations without a Rigidbody:** Lamps, monitors, and other static scene art can use a simple `Visual Shake Strength` to sway visually. They do **not** have Mass or Damping, and should not be made dynamic unless the object has appropriate Colliders. **Walls, floors, cameras, the player and safety-zone triggers remain fixed.**

## Test checklist

1. Open the real House or New_House scene in Edit mode. Select the **root** of the chair with a Rigidbody. Note its Mass and Damping and find the `Inertial Rigidbody` fields (or apply the menu command).
2. Set Mass to 5, Linear Damping to 1, Angular Damping to 0.5, Force to 15 and Shake Strength to 1. Save.
3. Play the scenario, or for standalone New_House use **CEVR > Earthquake > Test current scene (not Main Menu)** and turn on the old scene's `Is Earthquake Active` checkbox. The chair should move and respond naturally to collisions.
4. Stop Play; in Edit mode increase Mass to 15 and Linear Damping to 4. Save, then Play again. The same force should result in less translational motion. Repeat with Force = 30 or Shake Strength = 2 for a stronger response.
5. Test a moving chair (E grab, Z crawl), dropping a pillow, falling hazards, health/cover logic and leaving after the quake stops. Confirm no duplicate furniture and that Inspectors retain the saved values after two Play runs.
6. Commit your modified `.unity` file and accompanying `.meta` files so collaborators receive the same settings.

The code passed GitHub's static checks but must still be compiled and play-tested in the actual Unity 6000.3.20f1 Editor. The physical feel will be different from the former acceleration-based system, **because mass now really affects shaking**; adjust Force/Damping per object to tune it.
