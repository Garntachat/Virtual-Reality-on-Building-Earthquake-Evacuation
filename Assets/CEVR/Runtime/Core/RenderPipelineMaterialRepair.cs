using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Repairs scene materials whose shaders are missing or unsupported in the active
    /// render pipeline. Unity renders those materials bright magenta/pink.
    ///
    /// The project currently uses the Built-in Render Pipeline, while some imported
    /// house assets may carry URP/HDRP shaders. This keeps the original meshes intact
    /// and only substitutes a compatible runtime material when necessary.
    /// </summary>
    [DefaultExecutionOrder(-20000)]
    internal static class RenderPipelineMaterialRepair
    {
        private static readonly Dictionary<int, Material> Replacements = new Dictionary<int, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RepairInitialScene()
        {
            RepairScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RepairScene(scene);
        }

        private static void RepairScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            Shader fallback = FindFallbackShader();
            if (fallback == null)
            {
                Debug.LogWarning("CEVR material repair could not find a compatible fallback shader.");
                return;
            }

            int repairedSlots = 0;
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != scene) continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (!NeedsRepair(source)) continue;

                    materials[i] = GetReplacement(source, fallback);
                    changed = true;
                    repairedSlots++;
                }

                if (changed) renderer.sharedMaterials = materials;
            }

            if (repairedSlots > 0)
            {
                Debug.Log($"CEVR repaired {repairedSlots} unsupported material slot(s) in scene '{scene.name}' to prevent pink/magenta rendering.");
            }
        }

        private static bool NeedsRepair(Material material)
        {
            if (material == null || material.shader == null) return true;

            Shader shader = material.shader;
            if (!shader.isSupported) return true;

            string shaderName = shader.name ?? string.Empty;
            return shaderName.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   shaderName.IndexOf("Hidden/InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Material GetReplacement(Material source, Shader fallback)
        {
            int key = source == null ? 0 : source.GetInstanceID();
            if (Replacements.TryGetValue(key, out Material cached) && cached != null) return cached;

            Color color = Color.white;
            Texture texture = null;

            if (source != null)
            {
                if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
                else if (source.HasProperty("_Color")) color = source.GetColor("_Color");

                if (source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap");
                else if (source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
            }

            var replacement = new Material(fallback)
            {
                name = source == null ? "CEVR_RepairedMaterial" : $"{source.name}_CEVR_Repaired"
            };

            if (replacement.HasProperty("_Color")) replacement.SetColor("_Color", color);
            if (replacement.HasProperty("_BaseColor")) replacement.SetColor("_BaseColor", color);

            if (texture != null)
            {
                if (replacement.HasProperty("_MainTex")) replacement.SetTexture("_MainTex", texture);
                if (replacement.HasProperty("_BaseMap")) replacement.SetTexture("_BaseMap", texture);
            }

            Replacements[key] = replacement;
            return replacement;
        }

        private static Shader FindFallbackShader()
        {
            bool usingScriptablePipeline = GraphicsSettings.currentRenderPipeline != null;

            if (usingScriptablePipeline)
            {
                Shader urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null && urp.isSupported) return urp;
            }

            Shader standard = Shader.Find("Standard");
            if (standard != null && standard.isSupported) return standard;

            Shader unlit = Shader.Find("Unlit/Texture");
            if (unlit != null && unlit.isSupported) return unlit;

            return Shader.Find("Sprites/Default");
        }
    }
}
