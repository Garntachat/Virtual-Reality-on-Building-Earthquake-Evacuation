using UnityEditor;
using UnityEngine;

namespace ChulaEarthquakeVR.Editor
{
    // These editors change no runtime physics. They just put the useful settings
    // in one obvious place on EACH saved scene object.
    [CustomEditor(typeof(InertialRigidbody))]
    public sealed class PhysicsEarthquakeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            InertialRigidbody item = (InertialRigidbody)target;
            Rigidbody body = item.GetComponent<Rigidbody>();
            serializedObject.Update();

            EditorGUILayout.LabelField("Earthquake - Physical Object", EditorStyles.boldLabel);
            if (body != null)
            {
                EditorGUI.BeginChangeCheck();
                float mass = EditorGUILayout.FloatField(
                    new GUIContent("Mass (kg)", "Real Unity Rigidbody mass, used for impacts and momentum."),
                    body.mass);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(body, "Change furniture mass");
                    body.mass = Mathf.Max(0.01f, mass);
                    EditorUtility.SetDirty(body);
                }
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("responseScale"),
                new GUIContent("Shake Strength", "0 = still; 1 = normal; 2 = strong; 3 = very strong."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("includeVertical"),
                new GUIContent("Vertical Shake"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "The same earthquake signal drives every item. Mass changes collisions and " +
                "momentum; Shake Strength changes shaking. A kinematic Rigidbody will not be " +
                "pushed by physics. Edit values outside Play mode and save the scene.", MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("motionSource"),
                new GUIContent("Earthquake Source (advanced)"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(DecorativeQuakeResponse))]
    public sealed class DecorativeEarthquakeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Earthquake - Visual Decoration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("responseScale"),
                new GUIContent("Shake Strength", "0 = still; 1 = normal; 2 = strong; 3 = very strong."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("responseMode"),
                new GUIContent("How It Moves", "Loose, rocking, hanging, mounted or heavy."));
            EditorGUILayout.HelpBox(
                "Visual sway only: no Rigidbody, mass or collisions. " +
                "For real physical movement, the object needs a Rigidbody and collider " +
                "with an InertialRigidbody instead. Static walls and floors stay fixed.",
                MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("motion"),
                new GUIContent("Earthquake Source (advanced)"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
