using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    [DefaultExecutionOrder(-10000)]
    public sealed class UniversalSceneGameplayBootstrap : MonoBehaviour
    {
        private const string RootName = "CEVR_UniversalGameplay";
        private readonly List<Material> materials = new List<Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneInstaller()
        {
            SceneManager.sceneLoaded -= InstallAfterSceneLoad;
            SceneManager.sceneLoaded += InstallAfterSceneLoad;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene() => InstallAfterSceneLoad(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        private static void InstallAfterSceneLoad(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name.ToLowerInvariant();
            if (!sceneName.Contains("cevr") && !sceneName.Contains("tutorial") && !sceneName.Contains("house")) return;
            if (GameObject.Find(RootName) != null) return;
            var root = new GameObject(RootName);
            root.AddComponent<UniversalSceneGameplayBootstrap>().Install(sceneName.Contains("house"));
        }

        private void Install(bool houseScene)
        {
            if (houseScene) InstallHouseScenario();
            else InstallTutorialFeatures();
        }

        private void InstallTutorialFeatures()
        {
            RuntimeStageRepair.EnsurePlayableStage();
            DesktopDebugRig rig = FindFirstObjectByType<DesktopDebugRig>();
            PlayerHealth health = rig == null ? FindFirstObjectByType<PlayerHealth>() : rig.GetComponent<PlayerHealth>();
            Camera camera = rig == null ? FindFirstObjectByType<Camera>() : rig.GetComponentInChildren<Camera>();
            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = FindFirstObjectByType<SessionLogger>();
            if (rig != null && camera != null) EnsurePlayerFeatures(rig.gameObject, camera);

            EnsureShoes("WearableSafetyShoes_P1", "protective-shoes-p1", new Vector3(0.8f, 0.09f, 2.45f), logger);
            EnsureShoes("WearableSafetyShoes_P2", "protective-shoes-p2", new Vector3(1.35f, 0.09f, 2.45f), logger);
            EnsurePillow(new Vector3(1.25f, 1.12f, 0.72f), motion, logger);
            AttachWindowCracking(motion, logger);
            GameObject cabinet = GameObject.Find("UnsecuredTallCabinet");
            if (cabinet != null && cabinet.GetComponent<ToppleableFurniture>() == null)
                cabinet.AddComponent<ToppleableFurniture>().Configure(motion, 1.25f, 1.2f);
            EnsureMultiplayer(camera, rig == null ? health?.transform : rig.transform);
        }

        private void InstallHouseScenario()
        {
            Camera camera = FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("House scene requires one camera before CEVR gameplay can be installed.");
                return;
            }

            GameObject player = PrepareHousePlayer(camera);
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            var systems = new GameObject("HouseGameplaySystems");
            systems.transform.SetParent(transform);
            GroundMotionPlayer motion = systems.AddComponent<GroundMotionPlayer>();
            SessionLogger logger = systems.AddComponent<SessionLogger>();
            HazardDirector hazardDirector = systems.AddComponent<HazardDirector>();

            Material pink = MaterialFor("House Pink", new Color(0.88f, 0.08f, 0.40f));
            Material charcoal = MaterialFor("House Charcoal", new Color(0.08f, 0.11f, 0.15f));
            Material wood = MaterialFor("House Wood", new Color(0.42f, 0.22f, 0.10f));
            Material teal = MaterialFor("House Teal", new Color(0.02f, 0.50f, 0.55f));
            Material green = MaterialFor("House Safety Green", new Color(0.05f, 0.66f, 0.30f));
            Material amber = MaterialFor("House Amber", new Color(1f, 0.55f, 0.04f));
            Material blueGlass = MaterialFor("House Glass", new Color(0.10f, 0.36f, 0.55f));
            Material cream = MaterialFor("House Cream", new Color(0.88f, 0.84f, 0.76f));

            Vector3 playerSpawn = new Vector3(0f, 0.03f, -5.2f);
            player.transform.position = playerSpawn;
            BuildInvisibleSafetyFloor();
            BuildHouseTable(new Vector3(0f, 0f, -2.7f), wood, charcoal);
            CreateChair("HouseChair_CoverObstacle", "house-chair-cover-01", new Vector3(0f, 0f, -3.7f), 0f,
                pink, charcoal, motion, logger);
            CreateChair("HouseChair_DiningLeft", "house-chair-left-01", new Vector3(-1.65f, 0f, -2.7f), 90f,
                teal, charcoal, motion, logger);
            CreateChair("HouseChair_DiningRight", "house-chair-right-01", new Vector3(1.65f, 0f, -2.7f), -90f,
                teal, charcoal, motion, logger);
            CreateChair("HouseChair_Spare", "house-chair-spare-01", new Vector3(2.8f, 0f, -4.7f), -45f,
                cream, charcoal, motion, logger);

            EnsureShoes("WearableSafetyShoes_P1", "protective-shoes-p1",
                playerSpawn + new Vector3(-0.85f, 0.06f, 1.0f), logger);
            EnsureShoes("WearableSafetyShoes_P2", "protective-shoes-p2",
                playerSpawn + new Vector3(-0.25f, 0.06f, 1.0f), logger);
            EnsurePillow(playerSpawn + new Vector3(0.95f, 0.55f, 1.4f), motion, logger);

            var staged = new List<Rigidbody>();
            staged.Add(CreateTopplingCabinet("HouseTallCabinet_Left", new Vector3(-4.55f, 1.15f, -1.0f),
                charcoal, amber, motion, "house-cabinet-left"));
            staged.Add(CreateTopplingCabinet("HouseBookcase_Right", new Vector3(4.55f, 1.15f, -0.4f),
                wood, amber, motion, "house-bookcase-right"));
            Vector3[] overhead =
            {
                new Vector3(-1.8f, 3.15f, -2.0f), new Vector3(1.7f, 3.25f, -1.1f),
                new Vector3(-0.4f, 3.35f, 0.5f), new Vector3(3.1f, 3.05f, 1.2f)
            };
            for (int i = 0; i < overhead.Length; i++)
                staged.Add(CreateFallingProp($"HouseFallingObject_{i + 1}", overhead[i],
                    i % 2 == 0 ? amber : charcoal, motion, $"house-overhead-{i + 1}"));
            hazardDirector.Configure(staged);

            BuildHouseWindows(motion, logger, blueGlass, charcoal);
            BuildHouseDecoration(wood, cream, teal, green, charcoal);
            Transform assembly = BuildAssemblyMarker(new Vector3(0f, 0.025f, -8.7f), green).transform;
            BuildCoverZone(new Vector3(0f, 0.42f, -2.7f));
            EnsurePlayerFeatures(player, camera);
            EnsureMultiplayer(camera, player.transform);

            HouseScenarioController scenario = systems.AddComponent<HouseScenarioController>();
            scenario.Configure(motion, hazardDirector, health, player.transform, assembly, logger);
            Debug.Log("CEVR installed the complete house tutorial gameplay without modifying the house mesh.");
        }

        private GameObject PrepareHousePlayer(Camera camera)
        {
            DesktopDebugRig existing = camera.GetComponentInParent<DesktopDebugRig>();
            if (existing != null) return existing.gameObject;
            var player = new GameObject("HouseDesktopPlayer");
            player.tag = "Player";
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.24f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            player.AddComponent<PlayerHealth>();
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0f, 1.63f, 0f);
            camera.transform.localRotation = Quaternion.identity;
            camera.nearClipPlane = 0.05f;
            player.AddComponent<DesktopDebugRig>().Configure(camera);
            player.AddComponent<DesktopGrabInteractor>().Configure(camera);
            return player;
        }

        private static void EnsurePlayerFeatures(GameObject player, Camera camera)
        {
            DesktopGrabInteractor grab = player.GetComponent<DesktopGrabInteractor>();
            if (grab == null) grab = player.AddComponent<DesktopGrabInteractor>();
            grab.Configure(camera);
            ThirdPersonViewController thirdPerson = player.GetComponent<ThirdPersonViewController>();
            if (thirdPerson == null) thirdPerson = player.AddComponent<ThirdPersonViewController>();
            thirdPerson.Configure(camera);
        }

        private void EnsureMultiplayer(Camera camera, Transform player)
        {
            if (camera == null || player == null || GetComponent<LocalMultiplayerManager>() != null) return;
            LocalMultiplayerManager manager = gameObject.AddComponent<LocalMultiplayerManager>();
            manager.Configure(camera, player);
        }

        private void EnsureShoes(string objectName, string footwearId, Vector3 position, SessionLogger logger)
        {
            if (GameObject.Find(objectName) != null) return;
            var shoes = new GameObject(objectName);
            shoes.transform.SetParent(transform);
            shoes.transform.position = position;
            Material sole = MaterialFor("Shoe Sole", new Color(0.06f, 0.07f, 0.08f));
            Material upper = MaterialFor("Shoe Upper", new Color(0.95f, 0.62f, 0.08f));
            CreateChildVisual("LeftShoe_Sole", shoes.transform, new Vector3(-0.18f, 0.05f, 0f),
                new Vector3(0.25f, 0.10f, 0.55f), sole, true);
            CreateChildVisual("RightShoe_Sole", shoes.transform, new Vector3(0.18f, 0.05f, 0f),
                new Vector3(0.25f, 0.10f, 0.55f), sole, true);
            CreateChildVisual("LeftShoe_Upper", shoes.transform, new Vector3(-0.18f, 0.13f, 0.06f),
                new Vector3(0.23f, 0.16f, 0.38f), upper, false);
            CreateChildVisual("RightShoe_Upper", shoes.transform, new Vector3(0.18f, 0.13f, 0.06f),
                new Vector3(0.23f, 0.16f, 0.38f), upper, false);
            shoes.AddComponent<WearableShoes>().Configure(footwearId, logger);
        }

        private void EnsurePillow(Vector3 position, GroundMotionPlayer motion, SessionLogger logger)
        {
            if (GameObject.Find("ProtectivePillow") != null) return;
            GameObject pillow = Primitive("ProtectivePillow", PrimitiveType.Cube, position,
                new Vector3(0.9f, 0.24f, 0.64f), MaterialFor("Pillow Fabric", new Color(0.74f, 0.86f, 0.94f)), true);
            Rigidbody body = pillow.AddComponent<Rigidbody>();
            body.mass = 0.65f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            pillow.AddComponent<InertialRigidbody>().Configure(motion, 0.8f, true);
            pillow.AddComponent<ProtectivePillow>().Configure("protective-pillow-01", logger);
            TryAddXrGrabInteractable(pillow);
        }

        private void BuildHouseTable(Vector3 center, Material top, Material frame)
        {
            Primitive("HouseSturdyTableTop", PrimitiveType.Cube, center + Vector3.up * 0.86f,
                new Vector3(2.8f, 0.18f, 1.5f), top, true);
            foreach (float x in new[] { -1.15f, 1.15f })
            foreach (float z in new[] { -0.58f, 0.58f })
                Primitive("HouseSturdyTableLeg", PrimitiveType.Cube, center + new Vector3(x, 0.42f, z),
                    new Vector3(0.18f, 0.84f, 0.18f), frame, true);
        }

        private void BuildInvisibleSafetyFloor()
        {
            GameObject floor = Primitive("HouseGameplaySafetyFloor", PrimitiveType.Cube,
                new Vector3(0f, -0.12f, 0f), new Vector3(12f, 0.2f, 19f), null, true);
            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private void CreateChair(
            string objectName, string id, Vector3 position, float yaw, Material seat, Material frame,
            GroundMotionPlayer motion, SessionLogger logger)
        {
            var chair = new GameObject(objectName);
            chair.transform.SetParent(transform);
            chair.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            CreateChildVisual("ChairSeat", chair.transform, new Vector3(0f, 0.48f, 0f),
                new Vector3(0.9f, 0.12f, 0.9f), seat, true);
            foreach (float x in new[] { -0.35f, 0.35f })
            foreach (float z in new[] { -0.35f, 0.35f })
                CreateChildVisual("ChairLeg", chair.transform, new Vector3(x, 0.23f, z),
                    new Vector3(0.1f, 0.46f, 0.1f), frame, true);
            CreateChildVisual("ChairBack", chair.transform, new Vector3(0f, 0.95f, 0.4f),
                new Vector3(0.75f, 0.38f, 0.1f), seat, true);
            Rigidbody body = chair.AddComponent<Rigidbody>();
            body.mass = 7.5f;
            body.linearDamping = 2.5f;
            body.angularDamping = 4f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            chair.AddComponent<InertialRigidbody>().Configure(motion, 0.65f, false);
            chair.AddComponent<MovableFurniture>().Configure(id, logger, motion);
            TryAddXrGrabInteractable(chair);
        }

        private Rigidbody CreateTopplingCabinet(
            string objectName, Vector3 position, Material bodyMaterial, Material warningMaterial,
            GroundMotionPlayer motion, string hazardId)
        {
            GameObject cabinet = Primitive(objectName, PrimitiveType.Cube, position,
                new Vector3(1.05f, 2.3f, 0.72f), bodyMaterial, true);
            CreateChildVisual("WarningStripe", cabinet.transform, new Vector3(0f, 0f, -0.54f),
                new Vector3(0.82f, 0.10f, 0.03f), warningMaterial, false);
            Rigidbody body = cabinet.AddComponent<Rigidbody>();
            body.mass = 34f;
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            cabinet.AddComponent<InertialRigidbody>().Configure(motion, 1f, false);
            cabinet.AddComponent<ToppleableFurniture>().Configure(motion, 1.45f, 1.15f);
            cabinet.AddComponent<FallingHazard>().Configure(hazardId, 42f);
            return body;
        }

        private Rigidbody CreateFallingProp(
            string objectName, Vector3 position, Material material,
            GroundMotionPlayer motion, string hazardId)
        {
            GameObject item = Primitive(objectName, PrimitiveType.Cube, position,
                new Vector3(0.58f, 0.24f, 0.42f), material, true);
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.mass = 2.2f;
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            item.AddComponent<InertialRigidbody>().Configure(motion, 1f, true);
            item.AddComponent<FallingHazard>().Configure(hazardId, 26f);
            return body;
        }

        private void BuildHouseWindows(
            GroundMotionPlayer motion, SessionLogger logger, Material glass, Material frame)
        {
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -3.1f : 3.1f;
                GameObject window = Primitive($"HouseWindow_{i + 1}", PrimitiveType.Cube,
                    new Vector3(x, 1.65f, 2.2f), new Vector3(2.0f, 1.45f, 0.08f), glass, false);
                window.AddComponent<BreakableWindow>().Configure($"house-window-{i + 1}", motion, logger);
                CreateChildVisual("WindowTopFrame", window.transform, new Vector3(0f, 0.53f, 0f),
                    new Vector3(1.08f, 0.07f, 1.4f), frame, false);
                CreateChildVisual("WindowBottomFrame", window.transform, new Vector3(0f, -0.53f, 0f),
                    new Vector3(1.08f, 0.07f, 1.4f), frame, false);
            }
        }

        private void AttachWindowCracking(GroundMotionPlayer motion, SessionLogger logger)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int index = 0;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.name.StartsWith("Window", StringComparison.OrdinalIgnoreCase)) continue;
                BreakableWindow window = renderer.GetComponent<BreakableWindow>();
                if (window == null) window = renderer.gameObject.AddComponent<BreakableWindow>();
                window.Configure($"tutorial-window-{++index}", motion, logger);
            }
        }

        private void BuildHouseDecoration(
            Material wood, Material cream, Material teal, Material green, Material frame)
        {
            Primitive("HouseSofaBase", PrimitiveType.Cube, new Vector3(-3.5f, 0.36f, -4.6f),
                new Vector3(2.2f, 0.55f, 0.85f), cream, true);
            Primitive("HouseSofaBack", PrimitiveType.Cube, new Vector3(-3.5f, 0.92f, -4.98f),
                new Vector3(2.2f, 0.85f, 0.16f), teal, true);
            Primitive("HouseCoffeeTable", PrimitiveType.Cube, new Vector3(-3.45f, 0.42f, -3.25f),
                new Vector3(1.7f, 0.12f, 0.9f), wood, true);
            Primitive("HouseRug", PrimitiveType.Cube, new Vector3(-3.45f, 0.018f, -3.7f),
                new Vector3(3f, 0.025f, 2.5f), teal, false);
            Primitive("HousePlantPot", PrimitiveType.Cylinder, new Vector3(4.6f, 0.28f, -4.8f),
                new Vector3(0.38f, 0.28f, 0.38f), wood, false);
            for (int i = 0; i < 3; i++)
                Primitive($"HousePlantLeaf_{i + 1}", PrimitiveType.Sphere,
                    new Vector3(4.4f + i * 0.2f, 0.78f + (i % 2) * 0.12f, -4.8f),
                    new Vector3(0.30f, 0.58f, 0.24f), green, false);
            for (int i = 0; i < 4; i++)
                Primitive($"HouseShelfBook_{i + 1}", PrimitiveType.Cube,
                    new Vector3(4.25f + i * 0.18f, 1.1f, -0.7f),
                    new Vector3(0.12f, 0.42f + i * 0.04f, 0.32f), i % 2 == 0 ? teal : cream, false);
            Primitive("HousePhotoFrame", PrimitiveType.Cube, new Vector3(0f, 1.75f, 2.28f),
                new Vector3(1.05f, 0.72f, 0.06f), frame, false);
            Primitive("HousePhoto", PrimitiveType.Cube, new Vector3(0f, 1.75f, 2.23f),
                new Vector3(0.88f, 0.56f, 0.025f), cream, false);
        }

        private GameObject BuildAssemblyMarker(Vector3 position, Material material)
        {
            GameObject marker = Primitive("HouseAssemblyPoint", PrimitiveType.Cylinder, position,
                new Vector3(1.5f, 0.025f, 1.5f), material, false);
            for (int i = 0; i < 3; i++)
                Primitive($"HouseExitChevron_{i + 1}", PrimitiveType.Cube,
                    position + new Vector3(0f, 0.02f, 1.0f + i * 1.0f),
                    new Vector3(0.38f, 0.025f, 0.65f), material, false);
            return marker;
        }

        private void BuildCoverZone(Vector3 position)
        {
            var zone = new GameObject("HouseCoverZone");
            zone.transform.SetParent(transform);
            zone.transform.position = position;
            BoxCollider trigger = zone.AddComponent<BoxCollider>();
            trigger.size = new Vector3(2.25f, 0.76f, 1.15f);
            trigger.isTrigger = true;
            zone.AddComponent<CoverZone>().Configure("house-sturdy-table-cover");
        }

        private GameObject Primitive(
            string objectName, PrimitiveType type, Vector3 position, Vector3 size,
            Material material, bool colliderEnabled)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = objectName;
            item.transform.SetParent(transform);
            item.transform.position = position;
            item.transform.localScale = size;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider itemCollider = item.GetComponent<Collider>();
            if (itemCollider != null) itemCollider.enabled = colliderEnabled;
            return item;
        }

        private static void CreateChildVisual(
            string objectName, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool colliderEnabled)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = objectName;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            Collider itemCollider = item.GetComponent<Collider>();
            if (itemCollider != null) itemCollider.enabled = colliderEnabled;
        }

        private Material MaterialFor(string materialName, Color color)
        {
            foreach (Material existing in materials)
                if (existing != null && existing.name == materialName) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var material = new Material(shader) { name = materialName, color = color };
            materials.Add(material);
            return material;
        }

        private static void TryAddXrGrabInteractable(GameObject target)
        {
            Type type = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (type != null && target.GetComponent(type) == null) target.AddComponent(type);
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }
    }
}
