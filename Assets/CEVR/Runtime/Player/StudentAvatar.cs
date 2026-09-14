using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Licensed skinned character, with an edited student uniform and imported animation.
    public sealed class StudentAvatar : MonoBehaviour
    {
        private Material uniform;
        private Texture2D outfitTexture;
        private Animation animationPlayer;
        private Transform model;
        private Vector3 previousPosition;
        private Vector3 standingPosition;
        private Vector3 modelScale;
        private Quaternion modelRotation;
        private CharacterController controller;
        private bool moving;
        private bool wasCrawling;
        private bool wasAirborne;
        private readonly List<Material> fallbackMaterials = new List<Material>();
        private Transform fallbackLeftArm;
        private Transform fallbackRightArm;
        private Transform fallbackLeftLeg;
        private Transform fallbackRightLeg;
        private float fallbackStride;
        private float crouchBlend;
        private float crawlBlend;
        private float jumpBlend;
        private float crawlCycle;

        private const float CrouchHeightThreshold = 1.20f;
        private const float CrawlHeightThreshold = 0.72f;
        private const float CrouchTransitionSeconds = 0.16f;
        private const float CrawlTransitionSeconds = 0.22f;
        private const float JumpTransitionSeconds = 0.10f;
        private const float CrouchPitchDegrees = 8f;
        private const float PronePitchDegrees = 84f;
        private static readonly Vector3 CrouchPositionOffset = new Vector3(0f, -0.18f, 0.02f);
        private static readonly Vector3 PronePositionOffset = new Vector3(0f, 0.18f, -0.72f);

        public static Transform Build(Transform player, string objectName)
        {
            Transform existing = player.Find(objectName);
            if (existing != null) return existing;
            var root = new GameObject(objectName);
            root.transform.SetParent(player, false);
            root.AddComponent<StudentAvatar>().CreateUniform();
            return root.transform;
        }

        private void CreateUniform()
        {
            GameObject source = Resources.Load<GameObject>("Student/Student");
            Texture2D texture = Resources.Load<Texture2D>("Student/StudentUniform");
            if (source == null || texture == null)
            {
                string missing = source == null && texture == null ? "model and uniform texture" : source == null ? "model" : "uniform texture";
                Debug.LogWarning($"CEVR student {missing} unavailable. Using the built-in student fallback so the player remains visible.");
                CreateFallbackUniform();
                return;
            }

            model = Instantiate(source, transform, false).transform;
            model.name = "StudentMesh";
            Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("Student shader unavailable for the active render pipeline. Using the built-in student fallback.");
                Destroy(model.gameObject);
                model = null;
                CreateFallbackUniform();
                return;
            }

            uniform = new Material(shader) { mainTexture = texture, color = Color.white };
            try
            {
                outfitTexture = StudentAppearance.CreateOutfitTexture(texture);
                if (outfitTexture != null) uniform.mainTexture = outfitTexture;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Student outfit conversion unavailable; retaining original texture. " + exception.Message);
            }

            if (uniform.HasProperty("_Smoothness")) uniform.SetFloat("_Smoothness", 0.15f);
            if (uniform.HasProperty("_Glossiness")) uniform.SetFloat("_Glossiness", 0.15f);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("Student model has no renderers. Using the built-in student fallback.");
                Destroy(model.gameObject);
                model = null;
                CreateFallbackUniform();
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                var slots = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int i = 0; i < slots.Length; i++) slots[i] = uniform;
                renderer.sharedMaterials = slots;
                if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
            }

            float scale = 1.72f / Mathf.Max(0.01f, bounds.size.y);
            model.localScale *= scale;
            model.localPosition = Vector3.up * (transform.position.y - bounds.min.y) * scale;
            standingPosition = model.localPosition;
            modelScale = model.localScale;
            modelRotation = model.localRotation;

            foreach (Collider collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (Animator animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;

            animationPlayer = model.GetComponent<Animation>();
            if (animationPlayer == null) animationPlayer = model.gameObject.AddComponent<Animation>();
            animationPlayer.playAutomatically = false;
            animationPlayer.cullingType = AnimationCullingType.AlwaysAnimate;
            AddClip("Student/Idle", "idle");
            AddClip("Student/Run", "move");
            if (animationPlayer["idle"] != null) animationPlayer.Play("idle");

            controller = transform.parent.GetComponent<CharacterController>();
            previousPosition = transform.parent.position;
        }

        private void CreateFallbackUniform()
        {
            Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("CEVR could not find any compatible shader for the fallback student avatar.");
                return;
            }

            model = new GameObject("StudentMeshFallback").transform;
            model.SetParent(transform, false);

            Material shirt = CreateFallbackMaterial(shader, "Student Shirt", StudentAppearance.ShirtColor(StudentAppearance.Selected));
            Material trousers = CreateFallbackMaterial(shader, "Student Navy Trousers", new Color(0.035f, 0.07f, 0.13f));
            Material skin = CreateFallbackMaterial(shader, "Student Skin", new Color(0.62f, 0.39f, 0.25f));
            Material hair = CreateFallbackMaterial(shader, "Student Hair", new Color(0.035f, 0.025f, 0.02f));
            Material accent = CreateFallbackMaterial(shader, "Chula Pink Accent", new Color(0.86f, 0.08f, 0.38f));

            CreateFallbackPart("Torso", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0f), new Vector3(0.48f, 0.58f, 0.25f), shirt);
            CreateFallbackPart("Collar", PrimitiveType.Cube, new Vector3(0f, 1.43f, -0.13f), new Vector3(0.26f, 0.08f, 0.035f), shirt);
            CreateFallbackPart("UniformBadge", PrimitiveType.Cube, new Vector3(-0.13f, 1.25f, -0.132f), new Vector3(0.07f, 0.09f, 0.025f), accent);
            CreateFallbackPart("Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.49f, 0f), new Vector3(0.10f, 0.08f, 0.10f), skin);
            CreateFallbackPart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.67f, 0f), new Vector3(0.27f, 0.31f, 0.25f), skin);
            CreateFallbackPart("Hair", PrimitiveType.Sphere, new Vector3(0f, 1.80f, 0.015f), new Vector3(0.275f, 0.13f, 0.255f), hair);

            fallbackLeftArm = CreateFallbackPart("LeftArm", PrimitiveType.Capsule, new Vector3(-0.31f, 1.09f, 0f), new Vector3(0.105f, 0.33f, 0.105f), skin);
            fallbackRightArm = CreateFallbackPart("RightArm", PrimitiveType.Capsule, new Vector3(0.31f, 1.09f, 0f), new Vector3(0.105f, 0.33f, 0.105f), skin);
            CreateFallbackPart("LeftSleeve", PrimitiveType.Sphere, new Vector3(-0.30f, 1.31f, 0f), new Vector3(0.16f, 0.17f, 0.15f), shirt);
            CreateFallbackPart("RightSleeve", PrimitiveType.Sphere, new Vector3(0.30f, 1.31f, 0f), new Vector3(0.16f, 0.17f, 0.15f), shirt);

            fallbackLeftLeg = CreateFallbackPart("LeftLeg", PrimitiveType.Capsule, new Vector3(-0.14f, 0.47f, 0f), new Vector3(0.135f, 0.43f, 0.145f), trousers);
            fallbackRightLeg = CreateFallbackPart("RightLeg", PrimitiveType.Capsule, new Vector3(0.14f, 0.47f, 0f), new Vector3(0.135f, 0.43f, 0.145f), trousers);
            CreateFallbackPart("LeftShoe", PrimitiveType.Sphere, new Vector3(-0.14f, 0.08f, -0.07f), new Vector3(0.17f, 0.09f, 0.28f), hair);
            CreateFallbackPart("RightShoe", PrimitiveType.Sphere, new Vector3(0.14f, 0.08f, -0.07f), new Vector3(0.17f, 0.09f, 0.28f), hair);

            standingPosition = Vector3.zero;
            modelScale = Vector3.one;
            modelRotation = Quaternion.identity;
            controller = transform.parent == null ? null : transform.parent.GetComponent<CharacterController>();
            if (transform.parent != null) previousPosition = transform.parent.position;
        }

        private Material CreateFallbackMaterial(Shader shader, string name, Color color)
        {
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.24f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.24f);
            fallbackMaterials.Add(material);
            return material;
        }

        private Transform CreateFallbackPart(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(model, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                partCollider.enabled = false;
                Destroy(partCollider);
            }
            return part.transform;
        }

        private void AddClip(string resource, string name)
        {
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(resource))
            {
                if (clip.name.StartsWith("__preview__") || !clip.legacy) continue;
                animationPlayer.AddClip(clip, name);
                animationPlayer[name].wrapMode = WrapMode.Loop;
                return;
            }
            Debug.LogWarning("CEVR missing legacy student animation: " + resource + ". The avatar will remain visible without that clip.");
        }

        private void OnEnable()
        {
            if (transform.parent != null) previousPosition = transform.parent.position;
        }

        private void LateUpdate()
        {
            if (model == null || transform.parent == null) return;

            Vector3 current = transform.parent.position;
            Vector3 frameDelta = current - previousPosition;
            float verticalSpeed = frameDelta.y / Mathf.Max(0.001f, Time.deltaTime);
            Vector3 planarDelta = frameDelta;
            planarDelta.y = 0f;
            previousPosition = current;
            float speed = planarDelta.magnitude / Mathf.Max(0.001f, Time.deltaTime);

            bool crawling = controller != null && controller.height <= CrawlHeightThreshold;
            bool crouching = controller != null && !crawling && controller.height <= CrouchHeightThreshold;
            bool airborne = controller != null && !controller.isGrounded && !crawling && Mathf.Abs(verticalSpeed) > 0.12f;
            bool nextMoving = speed > (moving ? 0.05f : 0.12f);

            if (animationPlayer != null)
            {
                if (crawling)
                {
                    if (!wasCrawling && animationPlayer["idle"] != null) animationPlayer.CrossFade("idle", 0.12f);
                }
                else if (airborne)
                {
                    if (!wasAirborne && animationPlayer["idle"] != null) animationPlayer.CrossFade("idle", 0.08f);
                }
                else if (nextMoving != moving || wasCrawling || wasAirborne)
                {
                    string state = nextMoving ? "move" : "idle";
                    if (animationPlayer[state] != null) animationPlayer.CrossFade(state, 0.16f);
                }
            }

            moving = nextMoving;
            wasCrawling = crawling;
            wasAirborne = airborne;

            if (animationPlayer != null && animationPlayer["move"] != null && !crawling && !airborne)
            {
                float stanceReferenceSpeed = crouching ? 1.65f : 2.4f;
                animationPlayer["move"].speed = Mathf.Clamp(speed / stanceReferenceSpeed, 0.35f, 1.5f);
            }

            crouchBlend = Mathf.MoveTowards(crouchBlend, crouching ? 1f : 0f, Time.deltaTime / CrouchTransitionSeconds);
            crawlBlend = Mathf.MoveTowards(crawlBlend, crawling ? 1f : 0f, Time.deltaTime / CrawlTransitionSeconds);
            jumpBlend = Mathf.MoveTowards(jumpBlend, airborne ? 1f : 0f, Time.deltaTime / JumpTransitionSeconds);

            float crouchPose = Mathf.SmoothStep(0f, 1f, crouchBlend);
            float crawlPose = Mathf.SmoothStep(0f, 1f, crawlBlend);
            float jumpPose = Mathf.SmoothStep(0f, 1f, jumpBlend);

            fallbackStride += speed * Time.deltaTime * (crawling ? 3.1f : crouching ? 4.7f : 5.5f);
            if (crawling && moving) crawlCycle += speed * Time.deltaTime * 2.65f;

            if (animationPlayer == null && fallbackLeftLeg != null)
            {
                float swing = moving ? Mathf.Sin(fallbackStride) * 24f : 0f;
                float crouchStep = moving ? Mathf.Sin(fallbackStride) * 8f : 0f;
                float crawlStroke = crawling && moving ? Mathf.Sin(crawlCycle) * 7f : 0f;

                Quaternion standingLeftLeg = Quaternion.Euler(swing, 0f, 0f);
                Quaternion standingRightLeg = Quaternion.Euler(-swing, 0f, 0f);
                Quaternion standingLeftArm = Quaternion.Euler(-swing * 0.65f, 0f, 0f);
                Quaternion standingRightArm = Quaternion.Euler(swing * 0.65f, 0f, 0f);

                Quaternion crouchLeftLeg = Quaternion.Euler(24f - crouchStep, 0f, -5f);
                Quaternion crouchRightLeg = Quaternion.Euler(24f + crouchStep, 0f, 5f);
                Quaternion crouchLeftArm = Quaternion.Euler(-24f + crouchStep * 0.6f, 0f, -6f);
                Quaternion crouchRightArm = Quaternion.Euler(-24f - crouchStep * 0.6f, 0f, 6f);

                Quaternion crawlLeftLeg = Quaternion.Euler(28f - crawlStroke, 0f, -10f);
                Quaternion crawlRightLeg = Quaternion.Euler(28f + crawlStroke, 0f, 10f);
                Quaternion crawlLeftArm = Quaternion.Euler(-38f + crawlStroke, 0f, -18f);
                Quaternion crawlRightArm = Quaternion.Euler(-38f - crawlStroke, 0f, 18f);

                // Tuck the knees and raise the arms while airborne. On descent the same pose
                // relaxes smoothly into the landing/locomotion state.
                float ascent = Mathf.Clamp01((verticalSpeed + 1f) / 6f);
                Quaternion jumpLeftLeg = Quaternion.Euler(35f + ascent * 16f, 0f, -5f);
                Quaternion jumpRightLeg = Quaternion.Euler(35f + ascent * 16f, 0f, 5f);
                Quaternion jumpLeftArm = Quaternion.Euler(-55f - ascent * 22f, 0f, -8f);
                Quaternion jumpRightArm = Quaternion.Euler(-55f - ascent * 22f, 0f, 8f);

                fallbackLeftLeg.localRotation = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(standingLeftLeg, crouchLeftLeg, crouchPose), crawlLeftLeg, crawlPose), jumpLeftLeg, jumpPose);
                fallbackRightLeg.localRotation = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(standingRightLeg, crouchRightLeg, crouchPose), crawlRightLeg, crawlPose), jumpRightLeg, jumpPose);
                fallbackLeftArm.localRotation = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(standingLeftArm, crouchLeftArm, crouchPose), crawlLeftArm, crawlPose), jumpLeftArm, jumpPose);
                fallbackRightArm.localRotation = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(standingRightArm, crouchRightArm, crouchPose), crawlRightArm, crawlPose), jumpRightArm, jumpPose);
            }

            float crawlWave = crawling && moving ? Mathf.Sin(crawlCycle) : 0f;
            float crawlRoll = crawlWave * 3.2f;
            float crawlPitch = crawling && moving ? Mathf.Sin(crawlCycle * 2f) * 1.4f : 0f;
            float crawlBob = crawling && moving ? (Mathf.Sin(crawlCycle * 2f) + 1f) * 0.015f : 0f;
            float crawlPush = crawling && moving ? Mathf.Sin(crawlCycle) * 0.035f : 0f;

            Quaternion crouchRotation = modelRotation * Quaternion.Euler(CrouchPitchDegrees, 0f, 0f);
            Quaternion proneRotation = modelRotation * Quaternion.Euler(PronePitchDegrees + crawlPitch, 0f, crawlRoll);

            Vector3 crouchedPosition = standingPosition + CrouchPositionOffset;
            Vector3 stancePosition = Vector3.Lerp(standingPosition, crouchedPosition, crouchPose);
            Vector3 crawlPosition = standingPosition + PronePositionOffset + new Vector3(0f, crawlBob, crawlPush);
            Vector3 basePosition = Vector3.Lerp(stancePosition, crawlPosition, crawlPose);

            // No Jump.fbx exists in the project yet, so the imported student gets a clear
            // procedural takeoff/apex/landing silhouette instead of reusing the run animation.
            float rising = Mathf.Clamp(verticalSpeed / 6f, -1f, 1f);
            float jumpLift = jumpPose * (0.05f + Mathf.Max(0f, rising) * 0.05f);
            float landingCompression = jumpPose * Mathf.Max(0f, -rising) * 0.07f;
            model.localPosition = basePosition + Vector3.up * (jumpLift - landingCompression);

            Vector3 crouchedScale = new Vector3(modelScale.x, modelScale.y * 0.76f, modelScale.z);
            Vector3 stanceScale = Vector3.Lerp(modelScale, crouchedScale, crouchPose);
            Vector3 baseScale = Vector3.Lerp(stanceScale, modelScale, crawlPose);
            Vector3 airborneScale = new Vector3(baseScale.x * 0.98f, baseScale.y * (1.03f - Mathf.Max(0f, -rising) * 0.05f), baseScale.z * 0.98f);
            model.localScale = Vector3.Lerp(baseScale, airborneScale, jumpPose);

            Quaternion stanceRotation = Quaternion.Slerp(modelRotation, crouchRotation, crouchPose);
            Quaternion baseRotation = Quaternion.Slerp(stanceRotation, proneRotation, crawlPose);
            Quaternion jumpRotation = modelRotation * Quaternion.Euler(rising >= 0f ? -8f : 7f, 0f, 0f);
            model.localRotation = Quaternion.Slerp(baseRotation, jumpRotation, jumpPose);
            transform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            if (uniform != null) Destroy(uniform);
            if (outfitTexture != null) Destroy(outfitTexture);
            foreach (Material material in fallbackMaterials)
                if (material != null) Destroy(material);
        }
    }
}
