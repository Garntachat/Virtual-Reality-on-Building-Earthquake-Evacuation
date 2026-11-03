using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Suggested INITIAL values, not a runtime controller.
    /// Runs in Edit mode only; does not change authored values or run in Play.
    /// Values are training-game estimates, not measured furniture specifications.
    /// </summary>
    public static class EarthquakeStarterPresets
    {
        public readonly struct Values
        {
            public readonly float Mass;
            public readonly float LinearDamping;
            public readonly float AngularDamping;
            public readonly float ForceNewtons;
            public readonly float TorqueNewtonMeters;

            public Values(float mass, float linear, float angular, float force, float torque)
            {
                Mass = mass;
                LinearDamping = linear;
                AngularDamping = angular;
                ForceNewtons = force;
                TorqueNewtonMeters = torque;
            }
        }

        public static Values Suggest(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            // Order matters: a fridge/cabinet is much heavier than a loose book.
            if (n.Contains("fridge") || n.Contains("refrigerator"))
                return new Values(65f, 0.80f, 1.20f, 100f, 45f);
            if (n.Contains("wardrobe") || n.Contains("wandrobe"))
                return new Values(42f, 0.80f, 1.00f, 100f, 45f);
            if (n.Contains("cabinet") || n.Contains("bookcase") || n.Contains("shelf"))
                return new Values(38f, 0.80f, 1.10f, 95f, 40f);
            if (n.Contains("chair"))
                return new Values(6.50f, 1.20f, 1.80f, 22f, 6f);
            if (n.Contains("pillow") || n.Contains("cushion"))
                return new Values(0.75f, 1.20f, 1.10f, 3.5f, 1f);
            if (n.Contains("vase") || n.Contains("plantpot"))
                return new Values(1.10f, 0.45f, 0.65f, 6f, 1.8f);
            if (n.Contains("lamp") || n.Contains("fixture"))
                return new Values(3f, 0.70f, 1.20f, 7f, 2.5f);
            if (n.Contains("falling") || n.Contains("hazardbook") ||
                n.Contains("books") || n.Contains("overhead"))
                return new Values(2.20f, 0.30f, 0.50f, 7f, 2f);
            if (n.Contains("circuit") || n.Contains("canister") ||
                n.Contains("taskitem"))
                return new Values(1.20f, 0.65f, 0.90f, 5f, 2f);
            if (n.Contains("monitor") || n.Contains("screen") || n.Contains("television"))
                return new Values(5f, 0.80f, 1f, 12f, 3f);
            if (n.Contains("laptop") || n.Contains("keyboard"))
                return new Values(2f, 0.65f, 0.90f, 7f, 2f);
            if (n.Contains("table") || n.Contains("desk"))
                return new Values(28f, 1.50f, 2f, 45f, 12f);
            return new Values(2f, 0.70f, 1f, 8f, 2.5f);
        }

        [MenuItem("CEVR/Earthquake/Apply suggested starter presets (preserve edits)")]
        private static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path.Length == 0)
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int modified = ApplyOnce(scene);
            if (modified > 0)
                EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Earthquake starter presets",
                "Applied suggested starting values to " + modified + " unmodified physical/visual item(s). " +
                "Customized objects were preserved and will not be overwritten later. " +
                "You can now edit Mass, both Damping values, Force and Torque in the Inspector.",
                "OK");
        }

        public static bool HasPending(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (InertialRigidbody item in root.GetComponentsInChildren<InertialRigidbody>(true))
                    if (Eligible(item) && !item.StarterPresetProcessed) return true;
                foreach (HouseProxyEarthquakeSettings item in root.GetComponentsInChildren<HouseProxyEarthquakeSettings>(true))
                    if (!item.StarterPresetProcessed) return true;
                foreach (DecorativeQuakeResponse item in root.GetComponentsInChildren<DecorativeQuakeResponse>(true))
                    if (!item.StarterPresetProcessed) return true;
            }
            return false;
        }

        /// <summary>Call once after the scene's furniture components have been prepared.</summary>
        public static int ApplyOnce(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                EditorApplication.isPlayingOrWillChangePlaymode) return 0;
            int updated = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (InertialRigidbody item in root.GetComponentsInChildren<InertialRigidbody>(true))
                    if (Eligible(item) && !item.StarterPresetProcessed)
                        updated += PreparePhysical(item);
                foreach (HouseProxyEarthquakeSettings item in root.GetComponentsInChildren<HouseProxyEarthquakeSettings>(true))
                    if (!item.StarterPresetProcessed)
                        updated += PrepareHouseProxy(item);
                foreach (DecorativeQuakeResponse item in root.GetComponentsInChildren<DecorativeQuakeResponse>(true))
                    if (!item.StarterPresetProcessed)
                        updated += PrepareDecoration(item);
            }
            return updated;
        }

        private static bool Eligible(InertialRigidbody item)
        {
            if (item == null || item.GetComponent<Rigidbody>() == null) return false;
            if (item.GetComponentInParent<PlayerHealth>() != null ||
                item.GetComponentInParent<Camera>() != null ||
                item.GetComponentInParent<Canvas>() != null) return false;
            string n = item.name.ToLowerInvariant();
            return !n.Contains("floor") && !n.Contains("ground") &&
                !n.Contains("foundation") && !n.Contains("player") &&
                !n.Contains("assembly") && !n.Contains("wall");
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) <= 0.001f;

        private static bool FactoryBody(Rigidbody body, InertialRigidbody item)
        {
            if (!Near(item.ForceNewtons, 15f) || !Near(item.TorqueNewtonMeters, 5f))
                return false;

            // Any custom Shake Strength means the user has already tuned this item.
            float scale = item.ShakeStrength;
            string n = item.name.ToLowerInvariant();
            bool factoryScale = Near(scale, 1f) ||
                (n.Contains("chair") && Near(scale, 0.65f)) ||
                (n.Contains("pillow") && Near(scale, 0.8f)) ||
                (n.Contains("vase") && Near(scale, 1.05f));
            if (!factoryScale) return false;

            // Unity's default Rigidbody or exact values from the original CEVR
            // scene builders. A change to ANY value protects the whole object.
            bool unityDefault = Near(body.mass, 1f) &&
                Near(body.linearDamping, 0f) && Near(body.angularDamping, 0.05f);
            if (unityDefault) return true;
            bool chair = n.Contains("chair");
            if (chair) return Near(body.mass, 7.5f) &&
                Near(body.linearDamping, 2.5f) && Near(body.angularDamping, 4f);

            if (!Near(body.linearDamping, 0f) ||
                !Near(body.angularDamping, 0.05f)) return false;

            if (n.Contains("fridge") || n.Contains("wardrobe") ||
                n.Contains("wandrobe") || n.Contains("cabinet") ||
                n.Contains("bookcase") || n.Contains("shelf"))
                return Near(body.mass, 22f) || Near(body.mass, 34f) ||
                    Near(body.mass, 38f);
            if (n.Contains("falling") || n.Contains("hazardbook") || n.Contains("overhead"))
                return Near(body.mass, 1.2f) || Near(body.mass, 2.2f) ||
                    Near(body.mass, 2.5f);
            if (n.Contains("pillow"))
                return Near(body.mass, 0.65f) || Near(body.mass, 0.7f);
            if (n.Contains("vase")) return Near(body.mass, 1.1f);
            if (n.Contains("lamp")) return Near(body.mass, 3f);
            if (n.Contains("taskitem") || n.Contains("circuit") ||
                n.Contains("canister")) return Near(body.mass, 1.2f);
            return false;
        }

        private static int PreparePhysical(InertialRigidbody item)
        {
            Rigidbody body = item.GetComponent<Rigidbody>();
            bool untouched = FactoryBody(body, item);
            if (untouched)
            {
                Values value = Suggest(item.name);
                Undo.RecordObject(body, "Suggested earthquake mass and damping");
                body.mass = value.Mass;
                body.linearDamping = value.LinearDamping;
                body.angularDamping = value.AngularDamping;
                EditorUtility.SetDirty(body);
            }

            Undo.RecordObject(item, "Mark earthquake starter preset");
            SerializedObject fields = new SerializedObject(item);
            if (untouched)
            {
                Values value = Suggest(item.name);
                fields.FindProperty("forceNewtons").floatValue = value.ForceNewtons;
                fields.FindProperty("torqueNewtonMeters").floatValue = value.TorqueNewtonMeters;
            }
            fields.FindProperty("starterPresetProcessed").boolValue = true;
            fields.ApplyModifiedProperties();
            return untouched ? 1 : 0;
        }

        private static int PrepareHouseProxy(HouseProxyEarthquakeSettings item)
        {
            string n = item.name.ToLowerInvariant();
            bool looseBooks = n.Contains("hazardbooks");
            bool untouched = Near(item.Mass, looseBooks ? 2.2f : 34f) &&
                Near(item.LinearDamping, 0f) && Near(item.AngularDamping, 0.05f) &&
                Near(item.ForceNewtons, 15f) && Near(item.TorqueNewtonMeters, 5f) &&
                Near(item.ShakeStrength, 1f);

            Undo.RecordObject(item, "Set House furniture starter values once");
            SerializedObject fields = new SerializedObject(item);
            if (untouched)
            {
                Values value = Suggest(item.name);
                fields.FindProperty("mass").floatValue = value.Mass;
                fields.FindProperty("linearDamping").floatValue = value.LinearDamping;
                fields.FindProperty("angularDamping").floatValue = value.AngularDamping;
                fields.FindProperty("forceNewtons").floatValue = value.ForceNewtons;
                fields.FindProperty("torqueNewtonMeters").floatValue = value.TorqueNewtonMeters;
            }
            fields.FindProperty("starterPresetProcessed").boolValue = true;
            fields.ApplyModifiedProperties();
            return untouched ? 1 : 0;
        }

        private static int PrepareDecoration(DecorativeQuakeResponse item)
        {
            // Keep the authored Visual Shake Strength and Sway Type. Only
            // initialize damping when it is still the newly added default zero.
            bool untouched = Near(item.VisualDamping, 0f);
            string n = item.name.ToLowerInvariant();
            float damping = n.Contains("lamp") || n.Contains("light") ? 1.5f :
                n.Contains("table") || n.Contains("desk") || n.Contains("sofa") ||
                n.Contains("bed") || n.Contains("cabinet") ? 4f :
                n.Contains("books") || n.Contains("plant") || n.Contains("shoes") ? 1f : 1.8f;

            Undo.RecordObject(item, "Set visual earthquake damping once");
            SerializedObject fields = new SerializedObject(item);
            if (untouched) fields.FindProperty("visualDamping").floatValue = damping;
            fields.FindProperty("starterPresetProcessed").boolValue = true;
            fields.ApplyModifiedProperties();
            return untouched ? 1 : 0;
        }
    }
}
