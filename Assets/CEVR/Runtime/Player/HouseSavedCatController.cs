using UnityEngine;
using System.Collections.Generic;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-mode movement for the cat already saved in House.unity.
    /// It never creates or replaces the cat visual, and it never writes back to the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HouseSavedCatController : MonoBehaviour
    {
        [Header("First-floor roaming (world X/Z bounds)")]
        [SerializeField] private Vector2 minimum = new Vector2(-4.4f, -6.6f);
        [SerializeField] private Vector2 maximum = new Vector2(6.4f, 6.6f);
        [SerializeField] private AudioClip barkClip;
        [SerializeField] private bool enableRoaming = false;
        private HouseScenarioController houseScenario;
        private Transform barkHead;
        private Transform chest;
        private Quaternion headRest, chestRest;
        private float barkTime = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterPet()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AttachPet;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += AttachPet;
        }
        private static void AttachPet(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name != MainMenuController.HouseScene) return;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == "HouseLayout_Cat" && child.gameObject.activeInHierarchy &&
                        child.GetComponent<HouseSavedCatController>() == null)
                        child.gameObject.AddComponent<HouseSavedCatController>();
        }
        private readonly Queue<Vector3> route = new Queue<Vector3>();
        private GroundMotionPlayer motion;
        private AudioSource voice;
        private AudioClip generatedBark;
        private bool wasShaking;
        private float nextBark;
        private float retryAt;
        private readonly List<Bounds> furnitureBounds = new List<Bounds>();
        private float boundsRefreshAt;

        [SerializeField, Min(0.05f)] private float walkSpeed = 0.42f;
        [SerializeField, Min(10f)] private float turnSpeed = 150f;

        private Rigidbody body;
        private Vector3 home;
        private Vector3 target;
        private float waitUntil;
        private Transform furnitureRoot;
        private Transform tailBase;
        private Transform tailTip;
        private Transform[] legs;
        private Quaternion tailBaseRest;
        private Quaternion tailTipRest;
        private Quaternion[] legRest;
        private bool configured;

        public void Configure()
        {
            if (configured) return;
            configured = true;

            furnitureRoot = transform.parent;
            home = transform.position;
            target = home;

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.center = new Vector3(0f, 0.30f, 0f);
            capsule.radius = 0.17f;
            capsule.height = 0.62f;

            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            tailBase = FindChild("Cat_TailBase");
            tailTip = FindChild("Cat_TailTip");
            tailBaseRest = tailBase == null ? Quaternion.identity : tailBase.localRotation;
            tailTipRest = tailTip == null ? Quaternion.identity : tailTip.localRotation;

            legs = new[]
            {
                FindChild("Cat_LegFrontLeft"),
                FindChild("Cat_LegFrontRight"),
                FindChild("Cat_LegBackLeft"),
                FindChild("Cat_LegBackRight")
            };
            legRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                legRest[i] = legs[i] == null ? Quaternion.identity : legs[i].localRotation;

            if (enableRoaming) PickNextTarget();
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            houseScenario = FindFirstObjectByType<HouseScenarioController>();
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0.65f;
            voice.minDistance = 2f;
            voice.maxDistance = 30f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.volume = 0.9f;
            if (barkClip == null) barkClip = Resources.Load<AudioClip>("PetAudio/DogBark");
            if (barkClip == null) generatedBark = MakeBark();
            Transform head = FindChild("Cat_Head");
            if (head != null)
            {
                barkHead = new GameObject("Pet Bark Head Pivot").transform;
                barkHead.SetParent(transform, false);
                barkHead.position = head.position - transform.up * 0.08f;
                var parts = new List<Transform>();
                foreach (Transform part in transform)
                    if (part.name.StartsWith("Cat_Head") || part.name.StartsWith("Cat_Ear") ||
                        part.name.StartsWith("Cat_Eye") || part.name.StartsWith("Cat_Muzzle") || part.name == "Cat_Nose")
                        parts.Add(part);
                foreach (Transform part in parts) part.SetParent(barkHead, true);
                headRest = barkHead.localRotation;
            }
            chest = FindChild("Cat_Chest");
            if (chest != null) chestRest = chest.localRotation;
            Debug.Log("CEVR House pet ready: idle + earthquake bark; recorded audio=" + (barkClip != null), this);
        }

        private void Awake() => Configure();

        private void FixedUpdate()
        {
            if (!configured) Configure();
            if (body == null) return;
            if ((motion == null || houseScenario == null) && Time.time >= retryAt)
            {
                motion = FindFirstObjectByType<GroundMotionPlayer>();
                houseScenario = FindFirstObjectByType<HouseScenarioController>();
                retryAt = Time.time + 1f;
            }
            bool shaking = houseScenario != null ? houseScenario.IsQuaking : motion != null && motion.IsPlaying;
            if (shaking)
            {
                if (!wasShaking || Time.time >= nextBark)
                {
                    voice.pitch = Random.Range(0.92f, 1.08f);
                    voice.PlayOneShot(barkClip != null ? barkClip : generatedBark);
                    barkTime = Time.time;
                    nextBark = Time.time + Random.Range(3f, 6f);
                }
                wasShaking = true;
                Animate(false);
                return;
            }
            if (wasShaking)
            {
                wasShaking = false;
                route.Clear();
                target = body.position;
                waitUntil = Time.time + Random.Range(2f, 4f);
            }
            if (Time.time < waitUntil) { Animate(false); return; }
            if (!enableRoaming) { Animate(false); return; }

            Vector3 position = body.position;
            Vector3 flat = target - position;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.08f)
            {
                if (route.Count > 0) target = route.Dequeue();
                else
                {
                    PickNextTarget();
                    waitUntil = Time.time + Random.Range(1.5f, 4f);
                }
                Animate(false);
                return;
            }

            Vector3 direction = flat.normalized;
            Vector3 next = position + direction * walkSpeed * Time.fixedDeltaTime;
            next.y = home.y;

            if (!HasGroundAt(next) || ObstacleAhead(position, direction) || !IsClearOfSavedFurniture(next))
            {
                PickNextTarget();
                waitUntil = Time.time + 0.8f;
                Animate(false);
                return;
            }

            Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion rotation = Quaternion.RotateTowards(
                body.rotation, wanted, turnSpeed * Time.fixedDeltaTime);

            body.MoveRotation(rotation);
            body.MovePosition(next);
            Animate(true);
        }

        private void PickNextTarget()
        {
            route.Clear();
            Vector3 origin = body.position;
            target = origin;
            // Flood-fill reachable floor cells, not a repeating circle around the spawn.
            const float step = 0.55f;
            var frontier = new Queue<Vector2Int>();
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var candidates = new List<Vector2Int>();
            var start = Vector2Int.zero;
            frontier.Enqueue(start);
            visited.Add(start);
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (frontier.Count > 0 && visited.Count < 700)
            {
                Vector2Int cell = frontier.Dequeue();
                foreach (var offset in directions)
                {
                    Vector2Int next = cell + offset;
                    if (!visited.Add(next)) continue;
                    Vector3 point = origin + new Vector3(next.x * step, 0, next.y * step);
                    if (point.x < minimum.x || point.x > maximum.x || point.z < minimum.y || point.z > maximum.y) continue;
                    if (!HasGroundAt(point) || !IsClearOfSavedFurniture(point)) continue;
                    Vector3 from = origin + new Vector3(cell.x * step, 0, cell.y * step);
                    if (ObstacleAhead(from, (point - from).normalized, step)) continue;
                    parents[next] = cell;
                    frontier.Enqueue(next);
                    if ((point - origin).sqrMagnitude > 9f) candidates.Add(next);
                }
            }
            if (candidates.Count == 0)
                foreach (var cell in parents.Keys)
                    if (cell.sqrMagnitude > 2) candidates.Add(cell);
            if (candidates.Count == 0) return;
            Vector2Int destination = candidates[Random.Range(0, candidates.Count)];
            var reverse = new Stack<Vector3>();
            while (destination != start)
            {
                reverse.Push(origin + new Vector3(destination.x * step, 0, destination.y * step));
                destination = parents[destination];
            }
            while (reverse.Count > 0) route.Enqueue(reverse.Pop());
            if (route.Count > 0) target = route.Dequeue();
        }

        private bool HasGroundAt(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * 0.65f;
            RaycastHit[] hits = Physics.RaycastAll(
                origin, Vector3.down, 1.30f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                // Reject stairs, tabletops, upper floors and drops; remain on spawn floor.
                if (hit.normal.y > 0.85f && Mathf.Abs(hit.point.y - home.y) < 0.12f) return true;
            }
            return false;
        }

        private bool ObstacleAhead(Vector3 position, Vector3 direction, float distance = 0.42f)
        {
            Vector3 origin = position + Vector3.up * 0.28f;
            RaycastHit[] hits = Physics.SphereCastAll(
                origin, 0.22f, direction, distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.normal.y > 0.75f) continue;
                return true;
            }
            return false;
        }

        private bool IsClearOfSavedFurniture(Vector3 position)
        {
            if (furnitureRoot == null) return true;
            if (Time.time >= boundsRefreshAt)
            {
                RefreshFurnitureBounds();
                boundsRefreshAt = Time.time + 1f;
            }
            foreach (Bounds bounds in furnitureBounds)
                if (position.x >= bounds.min.x && position.x <= bounds.max.x &&
                    position.z >= bounds.min.z && position.z <= bounds.max.z) return false;
            return true;
        }

        private void RefreshFurnitureBounds()
        {
            furnitureBounds.Clear();
            foreach (Transform sibling in furnitureRoot)
            {
                if (sibling == null || sibling == transform || !sibling.gameObject.activeInHierarchy) continue;
                string n = sibling.name.ToLowerInvariant();
                if (n.Contains("rug") || n.Contains("window") || n.Contains("light") || n.Contains("art") || n.Contains("hazardbooks")) continue;

                bool found = false;
                Bounds bounds = default;
                foreach (Renderer r in sibling.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || !r.enabled) continue;
                    if (!found) { bounds = r.bounds; found = true; }
                    else bounds.Encapsulate(r.bounds);
                }
                if (!found) continue;

                float catBottom = home.y + 0.05f, catTop = home.y + 0.65f;
                if (bounds.max.y < catBottom || bounds.min.y > catTop) continue;
                bounds.Expand(new Vector3(0.50f, 0f, 0.50f));

                furnitureBounds.Add(bounds);
            }
        }

        private void Animate(bool walking)
        {
            float age = Time.time - barkTime;
            float reaction = age >= 0f && age < 0.55f ? Mathf.Sin(age / 0.55f * Mathf.PI) : 0f;
            if (barkHead != null) barkHead.localRotation = headRest * Quaternion.Euler(-22f * reaction, 0f, 0f);
            if (chest != null) chest.localRotation = chestRest * Quaternion.Euler(-8f * reaction, 0f, 0f);
            float gait = walking ? Mathf.Sin(Time.time * 10f) * 20f : 0f;
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float phase = (i == 0 || i == 3) ? gait : -gait;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(phase, 0f, 0f);
            }

            float tail = Mathf.Sin(Time.time * 2.4f) * 14f;
            if (tailBase != null)
                tailBase.localRotation = tailBaseRest * Quaternion.Euler(0f, tail, 0f);
            if (tailTip != null)
                tailTip.localRotation = tailTipRest * Quaternion.Euler(0f, -tail * 0.7f, 0f);
        }

        private Transform FindChild(string exactName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == exactName) return t;
            return null;
        }

        // Self-contained two-syllable synthetic bark; a recorded clip can override it in Inspector.
        private static AudioClip MakeBark()
        {
            const int rate = 22050;
            var samples = new float[rate];
            var noise = new System.Random(1927);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float local = t < 0.42f ? t : t - 0.48f;
                if (local < 0 || local > 0.30f) continue;
                float envelope = Mathf.Min(1f, local / 0.012f) * Mathf.Exp(-local * 13f);
                float phase = 2f * Mathf.PI * (180f * local - 90f * local * local);
                float voiced = Mathf.Sin(phase) + 0.45f * Mathf.Sin(phase * 2f) + 0.25f * Mathf.Sin(phase * 5f);
                samples[i] = envelope * (voiced * 0.35f + ((float)noise.NextDouble() * 2 - 1) * 0.22f);
            }
            var clip = AudioClip.Create("House Pet Double Bark", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (generatedBark != null) Destroy(generatedBark);
        }
    }
}
