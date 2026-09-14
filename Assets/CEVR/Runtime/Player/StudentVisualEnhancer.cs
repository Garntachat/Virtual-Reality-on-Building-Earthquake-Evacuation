using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Visual-only polish layer for the student avatar. The preferred path remains the licensed
    /// skinned Student FBX. If Unity temporarily fails to import the uniform texture, this system
    /// prevents the old blocky emergency avatar from being shown by replacing its renderers with
    /// a proportioned human/student presentation while keeping StudentAvatar's movement logic.
    /// </summary>
    [DefaultExecutionOrder(12000)]
    public sealed class StudentVisualEnhancer : MonoBehaviour
    {
        private readonly HashSet<int> enhanced = new HashSet<int>();
        private readonly List<Material> ownedMaterials = new List<Material>();
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<StudentVisualEnhancer>() != null) return;
            var service = new GameObject("CEVR_StudentVisualEnhancer");
            DontDestroyOnLoad(service);
            service.AddComponent<StudentVisualEnhancer>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.35f;

            foreach (StudentAvatar avatar in FindObjectsByType<StudentAvatar>(FindObjectsSortMode.None))
            {
                if (avatar == null || enhanced.Contains(avatar.GetInstanceID())) continue;
                if (TryEnhance(avatar)) enhanced.Add(avatar.GetInstanceID());
            }
        }

        private bool TryEnhance(StudentAvatar avatar)
        {
            Transform imported = FindDeep(avatar.transform, "StudentMesh");
            if (imported != null)
            {
                PolishImportedStudent(imported);
                return true;
            }

            Transform fallback = FindDeep(avatar.transform, "StudentMeshFallback");
            if (fallback == null) return false;
            BuildHumanFallback(fallback);
            return true;
        }

        private void PolishImportedStudent(Transform imported)
        {
            // Keep the actual skinned mesh and its animations; only improve material response.
            foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = true;
                    skinned.quality = SkinQuality.Auto;
                }

                Material[] slots = renderer.materials;
                foreach (Material material in slots)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                    material.enableInstancing = true;
                }
            }
        }

        private void BuildHumanFallback(Transform fallback)
        {
            if (fallback.Find("EnhancedHumanStudent") != null) return;

            // Disable only visuals from the emergency primitive student. Its limb transforms remain
            // as animation pivots and are reused below.
            foreach (Renderer renderer in fallback.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            Transform visual = new GameObject("EnhancedHumanStudent").transform;
            visual.SetParent(fallback, false);

            Shader shader = FindLitShader();
            if (shader == null) return;

            Material skin = Mat(shader, "Student Skin Natural", new Color(0.72f, 0.50f, 0.36f), 0.30f);
            Material skinWarm = Mat(shader, "Student Skin Warm", new Color(0.66f, 0.43f, 0.30f), 0.27f);
            Material hair = Mat(shader, "Student Hair", new Color(0.025f, 0.018f, 0.015f), 0.20f);
            Material shirt = Mat(shader, "Student Uniform Shirt", StudentAppearance.ShirtColor(StudentAppearance.Selected), 0.18f);
            Material shirtShadow = Mat(shader, "Student Uniform Shadow", Color.Lerp(StudentAppearance.ShirtColor(StudentAppearance.Selected), new Color(0.55f, 0.58f, 0.62f), 0.18f), 0.15f);
            Material trousers = Mat(shader, "Student Navy Trousers", new Color(0.035f, 0.055f, 0.095f), 0.16f);
            Material belt = Mat(shader, "Student Belt", new Color(0.045f, 0.035f, 0.03f), 0.35f);
            Material shoe = Mat(shader, "Student Shoes", new Color(0.025f, 0.025f, 0.030f), 0.42f);
            Material eyeWhite = Mat(shader, "Student Eye White", new Color(0.92f, 0.92f, 0.90f), 0.25f);
            Material iris = Mat(shader, "Student Iris", new Color(0.055f, 0.035f, 0.025f), 0.35f);
            Material accent = Mat(shader, "Chula Accent", new Color(0.87f, 0.10f, 0.40f), 0.24f);

            // Human proportions: ~1.74m adult university student with narrower waist/shoulders than
            // the previous box avatar. Negative Z is treated as the character's front, matching the
            // existing uniform badge placement.
            Transform pelvis = Part("Pelvis", PrimitiveType.Sphere, visual, new Vector3(0f, 0.88f, 0f), new Vector3(0.38f, 0.25f, 0.25f), trousers);
            Transform torso = Part("Torso", PrimitiveType.Capsule, visual, new Vector3(0f, 1.22f, 0f), new Vector3(0.36f, 0.37f, 0.22f), shirt);
            torso.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part("Waist", PrimitiveType.Cylinder, visual, new Vector3(0f, 0.92f, 0f), new Vector3(0.29f, 0.08f, 0.20f), shirtShadow);
            Part("Belt", PrimitiveType.Cube, visual, new Vector3(0f, 0.865f, -0.005f), new Vector3(0.52f, 0.055f, 0.22f), belt);
            Part("Buckle", PrimitiveType.Cube, visual, new Vector3(0f, 0.865f, -0.125f), new Vector3(0.065f, 0.050f, 0.025f), accent);

            // Neck and a slightly oval head read much more naturally than the old sphere.
            Part("Neck", PrimitiveType.Cylinder, visual, new Vector3(0f, 1.48f, 0f), new Vector3(0.095f, 0.09f, 0.095f), skinWarm);
            Transform head = Part("Head", PrimitiveType.Sphere, visual, new Vector3(0f, 1.66f, -0.006f), new Vector3(0.245f, 0.285f, 0.225f), skin);
            Part("LeftEar", PrimitiveType.Sphere, visual, new Vector3(-0.246f, 1.66f, -0.005f), new Vector3(0.040f, 0.065f, 0.025f), skinWarm);
            Part("RightEar", PrimitiveType.Sphere, visual, new Vector3(0.246f, 1.66f, -0.005f), new Vector3(0.040f, 0.065f, 0.025f), skinWarm);
            Part("Nose", PrimitiveType.Sphere, visual, new Vector3(0f, 1.655f, -0.218f), new Vector3(0.040f, 0.055f, 0.045f), skinWarm);

            Part("LeftEyeWhite", PrimitiveType.Sphere, visual, new Vector3(-0.083f, 1.706f, -0.207f), new Vector3(0.047f, 0.026f, 0.018f), eyeWhite);
            Part("RightEyeWhite", PrimitiveType.Sphere, visual, new Vector3(0.083f, 1.706f, -0.207f), new Vector3(0.047f, 0.026f, 0.018f), eyeWhite);
            Part("LeftIris", PrimitiveType.Sphere, visual, new Vector3(-0.083f, 1.706f, -0.225f), new Vector3(0.017f, 0.017f, 0.010f), iris);
            Part("RightIris", PrimitiveType.Sphere, visual, new Vector3(0.083f, 1.706f, -0.225f), new Vector3(0.017f, 0.017f, 0.010f), iris);
            Part("Mouth", PrimitiveType.Cube, visual, new Vector3(0f, 1.565f, -0.222f), new Vector3(0.085f, 0.012f, 0.010f), skinWarm);

            // Layered short black hair: cap + fringe + side pieces, avoiding the helmet look.
            Part("HairCap", PrimitiveType.Sphere, visual, new Vector3(0f, 1.80f, 0.025f), new Vector3(0.255f, 0.145f, 0.235f), hair);
            Part("HairFringeL", PrimitiveType.Sphere, visual, new Vector3(-0.095f, 1.775f, -0.185f), new Vector3(0.10f, 0.080f, 0.045f), hair);
            Part("HairFringeR", PrimitiveType.Sphere, visual, new Vector3(0.075f, 1.785f, -0.190f), new Vector3(0.12f, 0.075f, 0.045f), hair);
            Part("HairSideL", PrimitiveType.Sphere, visual, new Vector3(-0.218f, 1.715f, 0f), new Vector3(0.045f, 0.11f, 0.16f), hair);
            Part("HairSideR", PrimitiveType.Sphere, visual, new Vector3(0.218f, 1.715f, 0f), new Vector3(0.045f, 0.11f, 0.16f), hair);

            // University shirt details.
            Transform collarL = Part("CollarL", PrimitiveType.Cube, visual, new Vector3(-0.07f, 1.445f, -0.185f), new Vector3(0.14f, 0.10f, 0.025f), shirtShadow);
            collarL.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Transform collarR = Part("CollarR", PrimitiveType.Cube, visual, new Vector3(0.07f, 1.445f, -0.185f), new Vector3(0.14f, 0.10f, 0.025f), shirtShadow);
            collarR.localRotation = Quaternion.Euler(0f, 0f, 18f);
            Part("ShirtPlacket", PrimitiveType.Cube, visual, new Vector3(0f, 1.235f, -0.220f), new Vector3(0.028f, 0.36f, 0.020f), shirtShadow);
            for (int i = 0; i < 4; i++)
                Part("Button" + i, PrimitiveType.Sphere, visual, new Vector3(0f, 1.38f - i * 0.10f, -0.238f), new Vector3(0.014f, 0.014f, 0.008f), belt);
            Part("StudentBadge", PrimitiveType.Cube, visual, new Vector3(-0.135f, 1.30f, -0.235f), new Vector3(0.060f, 0.075f, 0.012f), accent);

            // Reuse the old animated pivot transforms for arms/legs so locomotion/crouch/crawl/jump
            // continue to move the enhanced human rather than a rigid mannequin.
            Transform leftArmPivot = fallback.Find("LeftArm");
            Transform rightArmPivot = fallback.Find("RightArm");
            Transform leftLegPivot = fallback.Find("LeftLeg");
            Transform rightLegPivot = fallback.Find("RightLeg");

            BuildArm(leftArmPivot != null ? leftArmPivot : visual, "Left", -1f, shirt, skin, skinWarm);
            BuildArm(rightArmPivot != null ? rightArmPivot : visual, "Right", 1f, shirt, skin, skinWarm);
            BuildLeg(leftLegPivot != null ? leftLegPivot : visual, "Left", -1f, trousers, shoe);
            BuildLeg(rightLegPivot != null ? rightLegPivot : visual, "Right", 1f, trousers, shoe);

            Debug.Log("CEVR enhanced the emergency student fallback into a proportioned human university-student avatar. The licensed skinned FBX remains preferred when its texture import is available.");
        }

        private void BuildArm(Transform pivot, string side, float sign, Material shirt, Material skin, Material skinWarm)
        {
            bool animatedPivot = pivot.name == "LeftArm" || pivot.name == "RightArm";
            Vector3 baseOffset = animatedPivot ? Vector3.zero : new Vector3(sign * 0.34f, 1.26f, 0f);
            Transform sleeve = Part(side + "Sleeve", PrimitiveType.Capsule, pivot, baseOffset + new Vector3(0f, animatedPivot ? 0.16f : 0f, 0f), new Vector3(0.13f, 0.17f, 0.13f), shirt);
            Transform forearm = Part(side + "Forearm", PrimitiveType.Capsule, pivot, baseOffset + new Vector3(0f, animatedPivot ? -0.10f : -0.30f, 0f), new Vector3(0.095f, 0.19f, 0.095f), skin);
            Part(side + "Hand", PrimitiveType.Sphere, pivot, baseOffset + new Vector3(0f, animatedPivot ? -0.32f : -0.51f, -0.005f), new Vector3(0.095f, 0.115f, 0.070f), skinWarm);
        }

        private void BuildLeg(Transform pivot, string side, float sign, Material trousers, Material shoe)
        {
            bool animatedPivot = pivot.name == "LeftLeg" || pivot.name == "RightLeg";
            Vector3 baseOffset = animatedPivot ? Vector3.zero : new Vector3(sign * 0.14f, 0.50f, 0f);
            Part(side + "TrouserLeg", PrimitiveType.Capsule, pivot, baseOffset + new Vector3(0f, animatedPivot ? 0.02f : 0f, 0f), new Vector3(0.135f, 0.39f, 0.145f), trousers);
            Part(side + "Shoe", PrimitiveType.Capsule, pivot, baseOffset + new Vector3(0f, animatedPivot ? -0.39f : -0.43f, -0.10f), new Vector3(0.15f, 0.11f, 0.25f), shoe).localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private Material Mat(Shader shader, string name, Color color, float smoothness)
        {
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
            ownedMaterials.Add(material);
            return material;
        }

        private static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return part.transform;
        }

        private static Shader FindLitShader()
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null || !shader.isSupported) shader = Shader.Find("Unlit/Color");
            if (shader == null || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            return shader;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
                if (material != null) Destroy(material);
            ownedMaterials.Clear();
        }
    }
}
