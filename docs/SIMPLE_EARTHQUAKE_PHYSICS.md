# Simple, editable earthquake physics in Unity

The goal is **one understandable physics system** for Tutorial, House, Condo and standalone New_House furniture. The shared `GroundMotionPlayer` still controls the beginning/end of shaking, timed hazards, warning cues, gameplay outcomes and recorded research profiles. Only the force applied to individual Rigidbody objects has changed.

## Every House furniture object has an understandable control

The saved `House.unity` currently has 43 immediate children under `HouseFurniture`. The **40 furniture/decorative roots** (everything except the dedicated front door, window assembly and animated cat) receive one appropriate editable setting after the one-time editor setup:

- **Four dining chairs:** real `Rigidbody` + `InertialRigidbody` + `BoxCollider` on each saved chair parent. Edit **Mass**, **Linear Damping**, **Angular Damping**, **Force**, **Torque** and **Shake Strength**.
- **Fridge, wardrobe and four overhead falling-book hazards:** `House Proxy Earthquake Settings` on each saved *visible* root, with the same Mass/Damping/Force/Torque controls. In Play, these values configure the **one existing physics Rigidbody** for that object. That collision body remains separate so the authored visual meshes and hazard-release mechanics remain intact. A duplicate visual shaker is blocked.
- **Other standalone furniture and decorations** (such as sofa, beds, books, plants, paintings, fixtures and rugs): `Decorative Quake Response` with a clear **Visual Shake Strength** slider. They are not independent physical falling bodies, so Mass and Damping would not be meaningful. Stationary cover furniture and other collidable large items default to strength 0 to keep their visuals aligned with safety colliders; you can adjust the visual setting manually.
- **Building structure, door, window and animated cat:** left under their existing static/gameplay controllers, instead of adding physics that would break the house.

Clicking a small mesh such as **Seat** automatically selects its nearest editable furniture parent in Edit mode. The toggle is **CEVR > Earthquake > Select furniture earthquake controls from mesh parts**. Run **CEVR > Earthquake > Check all House furniture earthquake controls** to check the whole saved hierarchy (the baseline scene should report at least 40/40, unless you have added or removed objects). The setup creates a timestamped backup and never replaces your saved furniture locations.

The same simple native Rigidbody force system is used by physical items in **Tutorial, Condo and New_House**. Existing Tutorial/Condo safety shoes and the stationary Condo cover table are given visual-only shake controls without rebuilding the scene.

## Tutorial and Condo: editable settings on every furniture root

**Tutorial**: Open `CEVR_ChulaEngineering_Tutorial.unity` in Edit mode. Its original and baked furniture roots (chairs, loose items, cabinet, cover table, lab benches, Kenney/team models, monitors, books, sofas, lighting and other furnishings) receive one simple, persistent control. **Condo**: Open `Condo.unity` in Edit mode. Its baked chairs, pillow, cabinet, falling props, protective table, shoes, and any additional identifiable furniture get the same treatment.

The one-time scene bakers now save these controls directly, and **already-baked local scenes** are retrofitted on next opening. To rerun safely, use **CEVR > Earthquake > Add simple physics settings to scene**. To count and locate missing settings, use **CEVR > Earthquake > Check Tutorial or Condo furniture controls**. Click a mesh child and Unity will select its nearest furniture parent with the editable controls.

**Movable or falling objects** with a Rigidbody use actual **Mass, Linear Damping, Angular Damping, Force (N), Torque (N m)** and Shake Strength. **Fixed tables, sofas, workstations, lamps and decorative models** use **Visual Shake Strength** only. They do **not** have physics mass because they are not Rigidbody objects; adding fake Rigidbodies to floors, anchored tables and safety props would break the scenario. Heavy cover tables start with visual strength 0 to keep the cover collider aligned, but the strength is editable.

The shared runtime binder now connects **saved per-item visual responses** even if their names do not contain furniture keywords. It does not attach a second shaker to the entire `Tutorial Editable Furniture` or `Condo Gameplay` hierarchy. Existing Inspector values are preserved on later Play runs. No furniture location, scale, generated mesh, colliders or cover-zone triggers are changed by the editor coverage pass.

**Manual validation after updating**: Stop Play, open Tutorial and run its coverage check; then open Condo and run the check there. Save both scenes. Set one chair's Mass and Damping, and change one lamp's Visual Shake Strength. Run each scenario twice and check that objects do not duplicate and the same edits remain after stopping Play. For hazard props, check that `Is Kinematic` is true before release—Mass will affect them after the hazard director releases them.

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

**Quick House check:** After opening House in Edit mode, choose **CEVR > Earthquake > Check all House furniture earthquake controls**. If any items are missing, run **CEVR > Earthquake > Add simple physics settings to scene**. Select a chair Seat again and check that the Inspector shows both **Rigidbody** and **Inertial Rigidbody** on its parent.

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
