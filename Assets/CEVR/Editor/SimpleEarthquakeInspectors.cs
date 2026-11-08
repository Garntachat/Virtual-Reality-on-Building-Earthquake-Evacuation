using UnityEditor;
using UnityEngine;

namespace ChulaEarthquakeVR.Editor
{
    // Same settings users already understand from Unity's native Rigidbody Inspector.
    // Editing here changes the actual Rigidbody; there is no hidden override at Play.
    [CustomEditor(typeof(InertialRigidbody))]
    public sealed class PhysicsEarthquakeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            InertialRigidbody item = (InertialRigidbody)target;
            Rigidbody body = item.GetComponent<Rigidbody>();
            serializedObject.Update();

            EditorGUILayout.LabelField("Simple Earthquake - Physical Object", EditorStyles.boldLabel);
            if (body != null)
            {
                EditorGUI.BeginChangeCheck();
                float mass = EditorGUILayout.FloatField(
                    new GUIContent("Mass (kg)", "Higher mass means less acceleration for the same force."),
                    body.mass);
                float linearDamping = EditorGUILayout.FloatField(
                    new GUIContent("Linear Damping", "Slows sideways movement."),
                    body.linearDamping);
                float angularDamping = EditorGUILayout.FloatField(
                    new GUIContent("Angular Damping", "Slows rotation and toppling."),
                    body.angularDamping);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(body, "Edit simple earthquake Rigidbody");
                    body.mass = Mathf.Max(0.01f, mass);
                    body.linearDamping = Mathf.Max(0f, linearDamping);
                    body.angularDamping = Mathf.Max(0f, angularDamping);
                    EditorUtility.SetDirty(body);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forceNewtons"),
                new GUIContent("Force (N)", "Linear force applied with Rigidbody.AddForce using ForceMode.Force."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("torqueNewtonMeters"),
                new GUIContent("Torque (N m)", "Rotation applied with Rigidbody.AddTorque using ForceMode.Force."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("responseScale"),
                new GUIContent("Shake Strength", "0 = off, 1 = normal, 2 = double force."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("includeVertical"),
                new GUIContent("Allow Vertical Shake"));

            EditorGUILayout.Space(4f);
            SerializedProperty houseMode = serializedObject.FindProperty("houseMotionMode");
            EditorGUILayout.PropertyField(houseMode,
                new GUIContent("House Motion Mode", "Standard = existing chair physics; Anchored Sway = heavy furniture rocks at the original pivot; Gravity Loose = small objects fall to the floor."));
            if (houseMode.enumValueIndex == (int)HouseFurnitureMotionMode.AnchoredSway)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anchoredSwayDegrees"),
                    new GUIContent("Anchored Rocking (degrees)", "Maximum small tilt; the object's center stays at the saved position."));
                EditorGUILayout.HelpBox(
                    "Large furniture stays kinematic and rooted at its saved position, but rocks gently with the shared quake. Force/Torque do not launch it. Edit this angle to change its visual shake.",
                    MessageType.Info);
            }
            else if (houseMode.enumValueIndex == (int)HouseFurnitureMotionMode.GravityLoose)
            {
                EditorGUILayout.HelpBox(
                    "Small loose object: stays parked before the earthquake, then moves with the shared quake and falls naturally under gravity. It keeps falling/settling after shaking ends.",
                    MessageType.Info);
            }
            else if (body != null && body.isKinematic)
                EditorGUILayout.HelpBox(
                    "Parked until the earthquake starts. Table chairs can still be moved deliberately using the player grab control.",
                    MessageType.Info);
            else
                EditorGUILayout.HelpBox(
                    "Standard shared earthquake Force/Torque response. Edit Rigidbody Mass and Damping outside Play mode.",
                    MessageType.Info);
            EditorGUILayout.Space(4f);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("motionSource"),
                new GUIContent("Shared Earthquake Source (advanced)"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(HouseProxyEarthquakeSettings))]
    public sealed class HouseProxyEarthquakeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Simple Earthquake - Moving House Furniture", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This furniture has a detailed saved mesh and ONE invisible gameplay " +
                "Rigidbody used for safe falling collisions. These saved values are " +
                "applied directly to that Rigidbody during Play. No second shaker.",
                MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("mass"),
                new GUIContent("Mass (kg)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("linearDamping"),
                new GUIContent("Linear Damping"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("angularDamping"),
                new GUIContent("Angular Damping"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forceNewtons"),
                new GUIContent("Force (N)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("torqueNewtonMeters"),
                new GUIContent("Torque (N m)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeStrength"),
                new GUIContent("Shake Strength"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("includeVertical"),
                new GUIContent("Allow Vertical Shake"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(DecorativeQuakeResponse))]
    public sealed class DecorativeEarthquakeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Simple Earthquake - Visual Only", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("responseScale"),
                new GUIContent("Visual Shake Strength", "0 = no movement, 1 = normal sway, 2 = double."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("responseMode"),
                new GUIContent("Visual Sway Type", "Only affects decoration, not Rigidbody physics."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visualDamping"),
                new GUIContent("Visual Damping", "Higher = slower/smoother sway. Visual only; not Rigidbody damping."));
            EditorGUILayout.HelpBox(
                "This is a static decoration without a Rigidbody, so it has no " +
                "Mass or Damping. To simulate physical falling, add a Collider, " +
                "Rigidbody and InertialRigidbody instead. Do not put physics on walls/floors.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("motion"),
                new GUIContent("Shared Earthquake Source (advanced)"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
