using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Connects the active ground-motion signal to every safe, relevant scene object.
    /// Physics props receive inertial acceleration; static scene dressing receives a
    /// small deterministic visual response. Player/camera/UI/navigation objects are excluded.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(800)]
    public sealed class EarthquakeSceneResponseInstaller : MonoBehaviour
    {
        private static readonly string[] ResponseKeywords =
        {
            "book", "monitor", "screen", "television", "keyboard", "laptop",
            "vase", "plant", "pillow", "cushion", "lamp", "light", "chair",
            "table", "desk", "cabinet", "shelf", "bookcase", "fridge",
            "sofa", "bed", "picture", "frame", "bottle", "cup", "mug",
            "window", "door", "curtain", "decor"
        };

        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField, Range(1, 50)] private int trainingFloorLevel = 3;
        private bool subscribed;

        public int PhysicsObjectsBound { get; private set; }
        public int DecorativeObjectsBound { get; private set; }

        public void Configure(GroundMotionPlayer source, int floorLevel)
        {
            motion = source;
            trainingFloorLevel = Mathf.Clamp(floorLevel, 1, 50);
        }

        private void Start()
        {
            if (motion == null) motion = FindFirstObjectByType<GroundMotionPlayer>();
            Subscribe();
            BindSceneResponses();
        }

        private void OnDestroy()
        {
            if (subscribed && motion != null) motion.QuakeStarted -= BindSceneResponses;
            subscribed = false;
        }

        private void Subscribe()
        {
            if (subscribed || motion == null) return;
            motion.QuakeStarted += BindSceneResponses;
            subscribed = true;
        }

        public void BindSceneResponses()
        {
            if (motion == null) return;

            // Artist-authored furniture may not match the legacy name keywords.
            // Connect ALL saved visual response components before discovering
            // new decor; never reset the values saved in the Inspector.
            foreach (DecorativeQuakeResponse response in
                FindObjectsByType<DecorativeQuakeResponse>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (response.gameObject.scene == gameObject.scene)
                    response.ConnectMotionIfMissing(motion);

            // Saved scene furniture may have a serialized Rigidbody/InertialRigidbody
            // but no root collider until the Editor opens the scene for its
            // one-time bake. Make direct Play from the Main Menu equally usable.
            EnsureSavedFurnitureColliders();
            PhysicsObjectsBound = BindPhysicsObjects();
            DecorativeObjectsBound = BindDecorativeObjects();
            Debug.Log($"CEVR SIMPLE Rigidbody earthquake: {PhysicsObjectsBound} physics object(s), " +
                      $"{DecorativeObjectsBound} visual-only decoration(s). " +
                      "Mass, Linear/Angular Damping and per-object Force/Torque remain editable.");
        }

        private void EnsureSavedFurnitureColliders()
        {
            foreach (InertialRigidbody item in
                FindObjectsByType<InertialRigidbody>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
            {
                if (item == null || item.gameObject.scene != gameObject.scene) continue;
                if (item.GetComponent<Rigidbody>() == null) continue;
                // Disabled imported colliders do not stop a falling Rigidbody.
                // Ensure each physics root has at least one enabled solid
                // collider, otherwise add a root box from visual bounds.
                bool hasSolidCollider = false;
                foreach (Collider candidate in item.GetComponentsInChildren<Collider>(true))
                    if (candidate != null && candidate.enabled && !candidate.isTrigger &&
                        candidate.gameObject.activeInHierarchy)
                    {
                        hasSolidCollider = true;
                        break;
                    }
                if (hasSolidCollider) continue;
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
                Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                bool found = false;
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    Bounds bounds = renderer.bounds;
                    foreach (float x in new[] {bounds.min.x, bounds.max.x})
                    foreach (float y in new[] {bounds.min.y, bounds.max.y})
                    foreach (float z in new[] {bounds.min.z, bounds.max.z})
                    {
                        Vector3 local = item.transform.InverseTransformPoint(new Vector3(x, y, z));
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                        found = true;
                    }
                }
                if (!found) continue;
                BoxCollider box = item.gameObject.AddComponent<BoxCollider>();
                box.center = (min + max) * 0.5f;
                Vector3 size = max - min;
                box.size = new Vector3(Mathf.Max(0.08f, size.x),
                    Mathf.Max(0.08f, size.y), Mathf.Max(0.08f, size.z));
            }
        }

        private int BindPhysicsObjects()
        {
            int bound = 0;
            Rigidbody[] bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (Rigidbody body in bodies)
            {
                if (!IsSafePhysicsTarget(body)) continue;

                InertialRigidbody inertial = body.GetComponent<InertialRigidbody>();
                if (inertial == null)
                {
                    inertial = body.gameObject.AddComponent<InertialRigidbody>();
                    inertial.Configure(motion, 1f, IncludeVertical(body));
                }
                else inertial.ConnectMotionIfMissing(motion);

                // Never override the designer's Rigidbody Mass, Damping,
                // Interpolation, Constraints, or Collision Detection settings.

                if (body.GetComponentInChildren<Collider>() != null &&
                    body.GetComponent<FurnitureImpactAudio>() == null)
                    body.gameObject.AddComponent<FurnitureImpactAudio>();

                // Tall unanchored furniture can tip, but the resulting component
                // remains visible and adjustable in the saved scene Inspector.
                TryAddToppleResponse(body);
                bound++;
            }
            return bound;
        }

        private int BindDecorativeObjects()
        {
            var roots = new HashSet<Transform>();
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                if (!IsSafeDecorativeRenderer(renderer)) continue;
                Transform root = FindResponseRoot(renderer.transform);
                if (root != null) roots.Add(root);
            }

            int bound = 0;
            foreach (Transform root in roots)
            {
                DecorativeQuakeResponse response = root.GetComponent<DecorativeQuakeResponse>();
                if (response == null)
                {
                    response = root.gameObject.AddComponent<DecorativeQuakeResponse>();
                    response.Configure(motion, Classify(root.name, CalculateBounds(root.gameObject)), 1f);
                }
                else response.ConnectMotionIfMissing(motion);
                // Never reset a saved object's shake strength or sway type at Play.
                bound++;
            }
            return bound;
        }

        private bool IsSafePhysicsTarget(Rigidbody body)
        {
            if (body == null || body.gameObject.scene != gameObject.scene) return false;
            if (body.GetComponentInParent<PlayerHealth>() != null) return false;
            if (body.GetComponentInParent<Camera>() != null) return false;
            if (body.GetComponentInChildren<Camera>(true) != null) return false;
            if (body.GetComponentInParent<Canvas>() != null) return false;
            if (body.GetComponentInParent<GroundMotionPlayer>() != null) return false;
            if (body.GetComponent<ConfigurableJoint>() == null && body.isKinematic &&
                body.GetComponent<FallingHazard>() == null &&
                body.GetComponent<MovableFurniture>() == null) return false;
            return !ExcludedName(HierarchyPath(body.transform));
        }

        private bool IsSafeDecorativeRenderer(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject.scene != gameObject.scene) return false;
            if (renderer.GetComponentInParent<Rigidbody>() != null) return false;
            // Saved House fridge/wardrobe/overhead props follow the ONE
            // Rigidbody on their gameplay proxy. Never add a second visual
            // sway to the already-following rendered object.
            if (renderer.GetComponentInParent<HouseProxyEarthquakeSettings>() != null ||
                renderer.GetComponentInParent<HouseVisualRuntimeFollower>() != null) return false;
            if (renderer.GetComponentInParent<PlayerHealth>() != null) return false;
            if (renderer.GetComponentInParent<Camera>() != null) return false;
            if (renderer.GetComponentInParent<Canvas>() != null) return false;
            string path = HierarchyPath(renderer.transform);
            return HasResponseKeyword(path) && !ExcludedName(path);
        }

        private static Transform FindResponseRoot(Transform leaf)
        {
            Transform candidate = null;
            Transform current = leaf;
            while (current != null)
            {
                if (current.GetComponent<Rigidbody>() != null) return null;
                if (current.GetComponent<VisualQuakeSway>() != null) return null;
                if (IsGroupingRoot(current)) break;
                // Prefer the nearest authored object, not an outer group.
                if (current.GetComponent<DecorativeQuakeResponse>() != null) return current;
                if (HasResponseKeyword(current.name)) candidate = current;
                if (current.parent == null ||
                    current.parent.GetComponent<GroundMotionPlayer>() != null) break;
                current = current.parent;
            }
            return candidate;
        }

        private static bool IsGroupingRoot(Transform candidate)
        {
            string name = candidate.name;
            return name == "Tutorial Editable Furniture" ||
                name == "Tutorial Editable Props" ||
                name == "Condo Gameplay" ||
                name == "DynamicProps" ||
                name == "CEVR_FinalVisualPolish" ||
                name == "TutorialObjects" ||
                name == "Environment_ChulaEngineeringInspired" ||
                candidate.GetComponent<FurnitureSceneDressing>() != null ||
                candidate.GetComponent<CondoGameplayLayout>() != null;
        }

        private void TryAddToppleResponse(Rigidbody body)
        {
            // Existing and hand-authored toppling settings are never overwritten.
            // The automatic fallback applies only to the tutorial preview.
            if (motion == null || !motion.IsUsingPreview ||
                body.isKinematic || body.GetComponent<ToppleableFurniture>() != null ||
                body.GetComponent<MovableFurniture>() != null ||
                body.GetComponent<ConfigurableJoint>() != null) return;

            Bounds bounds = CalculateBounds(body.gameObject);
            float footprint = Mathf.Max(0.05f, Mathf.Max(bounds.size.x, bounds.size.z));
            if (bounds.size.y < 1.1f || bounds.size.y / footprint < 1.35f) return;

            body.gameObject.AddComponent<ToppleableFurniture>()
                .Configure(motion, 1f, Mathf.Max(0.45f, bounds.size.y * 0.35f));
        }

        private static bool IncludeVertical(Rigidbody body)
        {
            return body.mass < 12f &&
                   (body.constraints & RigidbodyConstraints.FreezePositionY) == 0;
        }

        private static DecorativeQuakeMode Classify(string objectName, Bounds bounds)
        {
            if (ContainsAny(objectName, "lamp", "light", "pendant", "ceiling"))
                return DecorativeQuakeMode.Hanging;
            if (ContainsAny(objectName, "window", "door", "frame", "picture", "screen", "monitor", "television"))
                return DecorativeQuakeMode.Mounted;
            if (ContainsAny(objectName, "sofa", "bed", "table", "desk"))
                return DecorativeQuakeMode.Heavy;

            float footprint = Mathf.Max(0.05f, Mathf.Max(bounds.size.x, bounds.size.z));
            if (bounds.size.y / footprint > 1.2f ||
                ContainsAny(objectName, "cabinet", "shelf", "bookcase", "fridge", "plant"))
                return DecorativeQuakeMode.Rocking;
            return DecorativeQuakeMode.Loose;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static bool HasResponseKeyword(string value)
        {
            for (int i = 0; i < ResponseKeywords.Length; i++)
                if (value.IndexOf(ResponseKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static bool ExcludedName(string value)
        {
            return ContainsAny(value, "player", "camera", "xr origin", "xrorigin", "hud", "canvas",
                "safetyzone", "safety zone", "assembly", "crawlheremarker", "floor",
                "ground", "plane", "wall");
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            for (int i = 0; i < fragments.Length; i++)
                if (value.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static string HierarchyPath(Transform current)
        {
            string result = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                result = current.name + "/" + result;
            }
            return result;
        }
    }
}
