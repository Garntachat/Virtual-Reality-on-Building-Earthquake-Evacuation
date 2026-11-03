using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// The saved, editable props for Condo.unity. Does not touch or replace any
    /// existing condo mesh, imported prefab, material, or camera.
    /// Generated once in Edit mode; Play only attaches the gameplay systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CondoGameplayLayout : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform assemblyPoint;
        [SerializeField] private Transform coverZone;
        [SerializeField] private Rigidbody[] stagedHazards = new Rigidbody[0];

        public Vector3 PlayerSpawn => playerSpawn == null
            ? new Vector3(0f, 0.05f, -10f) : playerSpawn.position;
        public Transform AssemblyPoint => assemblyPoint;
        public Transform CoverTarget => coverZone;
        public Rigidbody[] StagedHazards => stagedHazards;

        public bool IsPrepared =>
            playerSpawn != null && assemblyPoint != null && coverZone != null &&
            stagedHazards != null && stagedHazards.Length >= 3;

        public void Build(Material dark, Material pink, Material green, Material amber)
        {
            if (IsPrepared) return;
            if (transform.childCount > 0)
            {
                Debug.LogError("CEVR Condo: Existing incomplete layout found; refusing to rebuild and overwrite edits.", this);
                return;
            }

            // These are explicit starting positions near the scene's original
            // camera (0, 1, -10), not the House scene's unrelated coordinates.
            // Move the ROOT objects in Edit mode to fit your final condo design.
            playerSpawn = new GameObject("Player Spawn").transform;
            playerSpawn.SetParent(transform, false);
            playerSpawn.localPosition = new Vector3(0f, 0.05f, -10f);

            // Invisible safety collision in the gameplay area. Existing condo
            // architecture and its own collision are preserved untouched.
            GameObject floor = new GameObject("Gameplay Safety Floor", typeof(BoxCollider));
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.16f, -8f);
            floor.GetComponent<BoxCollider>().size = new Vector3(9f, 0.25f, 15f);

            GameObject table = new GameObject("Condo Sturdy Table");
            table.transform.SetParent(transform, false);
            table.transform.localPosition = new Vector3(-1f, 0f, -6f);
            Part("Table Top", table.transform, new Vector3(0f, 0.79f, 0f),
                new Vector3(1.95f, 0.15f, 1.25f), dark);
            foreach (float x in new[] { -0.82f, 0.82f })
            foreach (float z in new[] { -0.48f, 0.48f })
                Part("Table Leg", table.transform, new Vector3(x, 0.37f, z),
                    new Vector3(0.11f, 0.74f, 0.11f), dark);

            GameObject safe = new GameObject("Condo Cover Zone", typeof(BoxCollider), typeof(CoverZone));
            safe.transform.SetParent(table.transform, false);
            safe.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            BoxCollider cover = safe.GetComponent<BoxCollider>();
            cover.isTrigger = true;
            cover.size = new Vector3(1.7f, 0.72f, 1.02f);
            safe.GetComponent<CoverZone>().Configure("condo-under-sturdy-table");
            coverZone = safe.transform;
            GameObject marker = Part("Crawl Under Here", table.transform,
                new Vector3(0f, 0.015f, 0f), new Vector3(1.5f, 0.02f, 0.95f), green);
            marker.GetComponent<Collider>().enabled = false;

            Chair("Condo Chair - Cover Approach", "condo-chair-cover",
                new Vector3(-1f, 0f, -7.3f), pink, dark);
            Chair("Condo Chair - Left", "condo-chair-left",
                new Vector3(-3.1f, 0f, -6f), pink, dark);
            Chair("Condo Chair - Right", "condo-chair-right",
                new Vector3(1.1f, 0f, -6f), pink, dark);
            Chair("Condo Chair - Spare", "condo-chair-spare",
                new Vector3(1.75f, 0f, -9.1f), pink, dark);

            GameObject shoes1 = new GameObject("Condo Safety Shoes P1", typeof(WearableShoes));
            shoes1.transform.SetParent(transform, false);
            shoes1.transform.localPosition = new Vector3(-0.8f, 0.09f, -9.2f);
            GameObject shoes2 = new GameObject("Condo Safety Shoes P2", typeof(WearableShoes));
            shoes2.transform.SetParent(transform, false);
            shoes2.transform.localPosition = new Vector3(0.1f, 0.09f, -9.2f);

            GameObject pillow = Part("Condo Protective Pillow", transform,
                new Vector3(-1f, 0.92f, -6f), new Vector3(0.64f, 0.20f, 0.5f), pink);
            Rigidbody pillowBody = pillow.AddComponent<Rigidbody>();
            pillowBody.mass = 0.7f;
            pillowBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            pillow.AddComponent<InertialRigidbody>().Configure(null, 0.8f, true);
            pillow.AddComponent<ProtectivePillow>().Configure("condo-protective-pillow", null);

            var hazards = new List<Rigidbody>();
            GameObject cabinet = Part("Condo Unsecured Cabinet", transform,
                new Vector3(2.9f, 1.0f, -5.7f), new Vector3(0.72f, 2.0f, 0.7f), dark);
            hazards.Add(HazardBody(cabinet, "condo-cabinet", 35f, 22f));
            cabinet.AddComponent<ToppleableFurniture>().Configure(null, 1.2f, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                GameObject overhead = Part("Condo Falling Object " + (i + 1), transform,
                    new Vector3(-2.4f + i * 1.5f, 2.7f, -5.1f + (i % 2) * 1.25f),
                    new Vector3(0.30f, 0.27f, 0.27f), amber);
                hazards.Add(HazardBody(overhead, "condo-falling-" + (i + 1), 25f, 1.2f));
            }
            stagedHazards = hazards.ToArray();

            GameObject exit = Part("Condo Assembly Point", transform,
                new Vector3(0f, 0.04f, -12.4f), new Vector3(1.5f, 0.06f, 1.5f), green);
            exit.GetComponent<Collider>().enabled = false;
            assemblyPoint = exit.transform;
        }

        public void ConnectForPlay(GroundMotionPlayer motion, SessionLogger logger)
        {
            foreach (MovableFurniture chair in GetComponentsInChildren<MovableFurniture>(true))
            {
                chair.Configure(chair.FurnitureId, logger, motion);
                InertialRigidbody shaker = chair.GetComponent<InertialRigidbody>();
                if (shaker != null) shaker.ConnectMotionIfMissing(motion);
            }
            foreach (InertialRigidbody shaker in GetComponentsInChildren<InertialRigidbody>(true))
                shaker.ConnectMotionIfMissing(motion);
            foreach (ToppleableFurniture topper in GetComponentsInChildren<ToppleableFurniture>(true))
                topper.ConnectMotionIfMissing(motion);
            foreach (WearableShoes shoes in GetComponentsInChildren<WearableShoes>(true))
                shoes.Configure(shoes.name, logger);
            foreach (ProtectivePillow pillow in GetComponentsInChildren<ProtectivePillow>(true))
                pillow.Configure("condo-protective-pillow", logger);
        }

        private void Chair(string objectName, string id, Vector3 localPosition,
            Material seat, Material frame)
        {
            var chair = new GameObject(objectName);
            chair.transform.SetParent(transform, false);
            chair.transform.localPosition = localPosition;
            Part("Seat", chair.transform, new Vector3(0f, 0.47f, 0f),
                new Vector3(0.66f, 0.12f, 0.66f), seat);
            Part("Back", chair.transform, new Vector3(0f, 0.83f, 0.29f),
                new Vector3(0.62f, 0.65f, 0.10f), seat);
            foreach (float x in new[] { -0.27f, 0.27f })
            foreach (float z in new[] { -0.27f, 0.27f })
                Part("Leg", chair.transform, new Vector3(x, 0.20f, z),
                    new Vector3(0.075f, 0.4f, 0.075f), frame);

            Rigidbody body = chair.AddComponent<Rigidbody>();
            body.mass = 7.5f;
            body.linearDamping = 2.5f;
            body.angularDamping = 4f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            chair.AddComponent<InertialRigidbody>().Configure(null, 0.65f, false);
            chair.AddComponent<MovableFurniture>().Configure(id, null, null);
        }

        private static Rigidbody HazardBody(GameObject obj, string id, float damage, float mass)
        {
            Rigidbody body = obj.AddComponent<Rigidbody>();
            body.mass = mass;
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            obj.AddComponent<InertialRigidbody>().Configure(null, 1f, false);
            obj.AddComponent<FallingHazard>().Configure(id, damage);
            return body;
        }

        private static GameObject Part(string name, Transform parent,
            Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
    }
}
