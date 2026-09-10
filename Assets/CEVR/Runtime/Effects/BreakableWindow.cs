using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class BreakableWindow : MonoBehaviour
    {
        [SerializeField] private string windowId = "window-01";
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField, Range(0.05f, 1f)] private float crackThreshold = 0.32f;
        [SerializeField] private SessionLogger logger;
        private readonly List<Material> crackMaterials = new List<Material>();

        public bool IsCracked { get; private set; }

        private void Start()
        {
            if (motion == null) motion = FindFirstObjectByType<GroundMotionPlayer>();
            if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
        }

        private void Update()
        {
            if (!IsCracked && motion != null && motion.IsPlaying && motion.NormalizedIntensity >= crackThreshold)
                Crack();
        }

        public void Configure(string id, GroundMotionPlayer source, SessionLogger sessionLogger)
        {
            windowId = string.IsNullOrWhiteSpace(id) ? name : id;
            motion = source;
            logger = sessionLogger;
        }

        public void Crack()
        {
            if (IsCracked) return;
            IsCracked = true;
            Vector3[][] paths =
            {
                new[] { new Vector3(0f, 0f, -0.56f), new Vector3(0.16f, 0.22f, -0.56f), new Vector3(0.3f, 0.48f, -0.56f) },
                new[] { new Vector3(0f, 0f, -0.56f), new Vector3(-0.2f, 0.18f, -0.56f), new Vector3(-0.42f, 0.3f, -0.56f) },
                new[] { new Vector3(0f, 0f, -0.56f), new Vector3(0.24f, -0.16f, -0.56f), new Vector3(0.48f, -0.28f, -0.56f) },
                new[] { new Vector3(0f, 0f, -0.56f), new Vector3(-0.12f, -0.25f, -0.56f), new Vector3(-0.28f, -0.5f, -0.56f) },
                new[] { new Vector3(0.16f, 0.22f, -0.56f), new Vector3(-0.2f, 0.18f, -0.56f), new Vector3(-0.12f, -0.25f, -0.56f) }
            };
            for (int i = 0; i < paths.Length; i++) BuildCrackLine(i + 1, paths[i]);
            BuildFloorDebris();
            logger?.LogEvent("window_cracked", $"{{\"windowId\":\"{windowId}\"}}");
        }

        private void BuildFloorDebris()
        {
            var debris = new GameObject("BrokenGlassFloorHazard");
            debris.transform.position = new Vector3(transform.position.x, 0.035f, transform.position.z - 0.7f);
            BoxCollider trigger = debris.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.7f, 0.08f, 1.25f);
            debris.AddComponent<BrokenGlassHazard>();

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null) return;
            Material shardMaterial = new Material(shader) { color = new Color(0.55f, 0.86f, 1f, 0.82f) };
            crackMaterials.Add(shardMaterial);
            for (int i = 0; i < 7; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"SafeGlassShard_{i + 1}";
                shard.transform.SetParent(debris.transform, false);
                shard.transform.localPosition = new Vector3(-0.68f + i * 0.22f, 0f, ((i * 37) % 5 - 2) * 0.17f);
                shard.transform.localRotation = Quaternion.Euler(0f, i * 29f, 0f);
                shard.transform.localScale = new Vector3(0.12f + (i % 3) * 0.035f, 0.012f, 0.08f);
                Collider shardCollider = shard.GetComponent<Collider>();
                if (shardCollider != null) Destroy(shardCollider);
                shard.GetComponent<Renderer>().sharedMaterial = shardMaterial;
            }
        }

        private void BuildCrackLine(int index, Vector3[] points)
        {
            var lineObject = new GameObject($"Crack_{index}");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.widthMultiplier = 0.018f;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null) return;
            var material = new Material(shader) { color = new Color(0.72f, 0.9f, 1f, 0.96f) };
            line.sharedMaterial = material;
            crackMaterials.Add(material);
        }

        private void OnDestroy()
        {
            foreach (Material material in crackMaterials)
                if (material != null) Destroy(material);
            crackMaterials.Clear();
        }
    }
}
