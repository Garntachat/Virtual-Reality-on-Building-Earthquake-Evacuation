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

            if (body != null && body.isKinematic)
                EditorGUILayout.HelpBox("Kinematic is enabled: the object is held by gameplay and will not shake until it is released.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    "One shared earthquake wave + one force/torque per object. " +
                    "Edit outside Play mode and save. Unity's actual Rigidbody Mass, " +
                    "Linear Damping and Angular Damping control the response.", MessageType.Info);

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
