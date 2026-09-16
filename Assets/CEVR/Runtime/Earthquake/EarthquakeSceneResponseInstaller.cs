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
            PhysicsObjectsBound = BindPhysicsObjects();
            DecorativeObjectsBound = BindDecorativeObjects();
            Debug.Log($"CEVR earthquake response connected {PhysicsObjectsBound} physics object(s) and " +
                      $"{DecorativeObjectsBound} decorative object(s) on training floor {trainingFloorLevel}. " +
                      "The player camera and XR rig remain unshaken.");
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
                    inertial.Configure(motion, ResponseScale(body), IncludeVertical(body));
                }

                body.interpolation = RigidbodyInterpolation.Interpolate;
                if (!body.isKinematic)
                {
                    body.collisionDetectionMode = body.mass <= 5f
                        ? CollisionDetectionMode.ContinuousDynamic
                        : CollisionDetectionMode.Continuous;
                    body.sleepThreshold = Mathf.Min(body.sleepThreshold, 0.005f);
                }

                if (body.GetComponentInChildren<Collider>() != null &&
                    body.GetComponent<FurnitureImpactAudio>() == null)
                    body.gameObject.AddComponent<FurnitureImpactAudio>();

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

            float floorScale = TrainingFloorGain(1.35f);
            int bound = 0;
            foreach (Transform root in roots)
            {
                DecorativeQuakeResponse response = root.GetComponent<DecorativeQuakeResponse>();
                if (response == null) response = root.gameObject.AddComponent<DecorativeQuakeResponse>();
                Bounds bounds = CalculateBounds(root.gameObject);
                response.Configure(motion, Classify(root.name, bounds), floorScale);
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
                if (HasResponseKeyword(current.name)) candidate = current;
                if (current.parent == null || current.parent.GetComponent<GroundMotionPlayer>() != null) break;
                current = current.parent;
            }
            return candidate;
        }

        private void TryAddToppleResponse(Rigidbody body)
        {
            if (body.isKinematic || body.GetComponent<ToppleableFurniture>() != null ||
                body.GetComponent<MovableFurniture>() != null ||
                body.GetComponent<ConfigurableJoint>() != null) return;

            Bounds bounds = CalculateBounds(body.gameObject);
            float footprint = Mathf.Max(0.05f, Mathf.Max(bounds.size.x, bounds.size.z));
            float slenderness = bounds.size.y / footprint;
            if (slenderness < 1.35f || bounds.size.y < 1.1f) return;

            float height = Mathf.Clamp(bounds.size.y * 0.35f, 0.45f, 1.8f);
            float scale = Mathf.Lerp(1.05f, 1.55f, Mathf.Clamp01((slenderness - 1.35f) / 1.8f));
            body.gameObject.AddComponent<ToppleableFurniture>().Configure(motion, scale, height);
        }

        private float ResponseScale(Rigidbody body)
        {
            float massResponse = body.mass <= 1.5f ? 1.18f :
                body.mass <= 10f ? 1f :
                body.mass <= 40f ? 0.90f : 0.82f;
            return massResponse * TrainingFloorGain(1.28f);
        }

        private float TrainingFloorGain(float maximumGain)
        {
            if (motion == null || !motion.IsUsingPreview) return 1f;
            float normalizedFloor = Mathf.Clamp01((trainingFloorLevel - 1f) / 49f);
            return Mathf.Lerp(1f, maximumGain, Mathf.Sqrt(normalizedFloor));
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
