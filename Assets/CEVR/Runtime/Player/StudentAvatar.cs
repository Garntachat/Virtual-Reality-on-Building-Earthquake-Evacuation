using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Licensed skinned character, with an edited student uniform and imported animation.
    public sealed class StudentAvatar : MonoBehaviour
    {
        private Material uniform;
        private Animation animationPlayer;
        private Transform model;
        private Vector3 previousPosition;
        private Vector3 standingPosition;
        private Vector3 modelScale;
        private Quaternion modelRotation;
        private CharacterController controller;
        private bool moving;

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
                Debug.LogError("CEVR student assets missing. Reimport Assets/CEVR/Resources/Student.");
                return;
            }
            model = Instantiate(source, transform, false).transform;
            model.name = "StudentMesh";
            Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("Student shader unavailable for active render pipeline."); return; }
            uniform = new Material(shader) { mainTexture = texture, color = Color.white };
            if (uniform.HasProperty("_Smoothness")) uniform.SetFloat("_Smoothness", 0.15f);
            if (uniform.HasProperty("_Glossiness")) uniform.SetFloat("_Glossiness", 0.15f);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Debug.LogError("Student model has no renderers."); return; }
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

        private void AddClip(string resource, string name)
        {
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(resource))
            {
                if (clip.name.StartsWith("__preview__") || !clip.legacy) continue;
                animationPlayer.AddClip(clip, name);
                animationPlayer[name].wrapMode = WrapMode.Loop;
                return;
            }
            Debug.LogError("CEVR missing legacy student animation: " + resource);
        }

        private void OnEnable()
        {
            if (transform.parent != null) previousPosition = transform.parent.position;
        }
        private void LateUpdate()
        {
            if (model == null || animationPlayer == null || transform.parent == null) return;
            Vector3 current = transform.parent.position;
            Vector3 delta = current - previousPosition; delta.y = 0f;
            previousPosition = current;
            float speed = delta.magnitude / Mathf.Max(0.001f, Time.deltaTime);
            bool nextMoving = speed > (moving ? 0.05f : 0.12f);
            if (nextMoving != moving)
            {
                moving = nextMoving;
                string state = moving ? "move" : "idle";
                if (animationPlayer[state] != null) animationPlayer.CrossFade(state, 0.18f);
            }
            if (animationPlayer["move"] != null) animationPlayer["move"].speed = Mathf.Clamp(speed / 2.4f, 0.35f, 1.5f);
            // Root motion never drives gameplay collision. A dedicated crawl clip is still needed.
            model.localPosition = standingPosition;
            model.localScale = modelScale;
            model.localRotation = modelRotation;
            transform.localScale = new Vector3(1f, controller == null ? 1f : Mathf.Clamp(controller.height / 1.75f, 0.3f, 1f), 1f);
        }
        private void OnDestroy() { if (uniform != null) Destroy(uniform); }
    }
}
