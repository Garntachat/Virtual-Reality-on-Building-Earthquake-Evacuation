using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Stylized student uniform, without an official university emblem.
    public sealed class StudentAvatar : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Transform leftLeg, rightLeg, leftArm, rightArm;
        private Vector3 previousPosition;
        private float gait;

        public static Transform Build(Transform player, string objectName)
        {
            Transform existing = player.Find(objectName);
            if (existing != null) return existing;
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(player, false);
            root.AddComponent<StudentAvatar>().CreateUniform();
            return root.transform;
        }

        private Material Fabric(Color color)
        {
            Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader) { color = color };
            materials.Add(material);
            return material;
        }

        private Transform Part(string label, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = label;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Collider>().enabled = false;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        private void CreateUniform()
        {
            Material white = Fabric(new Color(0.96f, 0.96f, 0.92f));
            Material navy = Fabric(new Color(0.055f, 0.065f, 0.10f));
            Material skin = Fabric(new Color(0.72f, 0.48f, 0.32f));
            Material black = Fabric(new Color(0.025f, 0.025f, 0.03f));
            Part("WhiteStudentShirt", PrimitiveType.Cube, new Vector3(0, 1.15f, 0), new Vector3(0.43f, 0.52f, 0.25f), white);
            Part("BlackBelt", PrimitiveType.Cube, new Vector3(0, 0.91f, 0), new Vector3(0.44f, 0.045f, 0.26f), black);
            Part("Head", PrimitiveType.Sphere, new Vector3(0, 1.56f, 0), new Vector3(0.27f, 0.32f, 0.27f), skin);
            Part("ShortBlackHair", PrimitiveType.Sphere, new Vector3(0, 1.67f, -0.025f), new Vector3(0.28f, 0.16f, 0.27f), black);
            for (int i = 0; i < 4; i++)
                Part("ShirtButton", PrimitiveType.Sphere, new Vector3(0, 1.30f - i * 0.10f, 0.13f), Vector3.one * 0.018f, navy);
            foreach (float side in new[] { -1f, 1f })
            {
                Transform arm = Part("ShirtSleeve", PrimitiveType.Capsule, new Vector3(side * 0.29f, 1.23f, 0), new Vector3(0.15f, 0.12f, 0.16f), white);
                Part("Forearm", PrimitiveType.Capsule, new Vector3(side * 0.30f, 0.98f, 0), new Vector3(0.12f, 0.13f, 0.12f), skin);
                Transform leg = Part("DarkTrousers", PrimitiveType.Capsule, new Vector3(side * 0.12f, 0.49f, 0), new Vector3(0.18f, 0.38f, 0.20f), navy);
                Part("BlackShoe", PrimitiveType.Cube, new Vector3(side * 0.12f, 0.08f, 0.055f), new Vector3(0.19f, 0.12f, 0.32f), black);
                if (side < 0) { leftLeg = leg; leftArm = arm; }
                else { rightLeg = leg; rightArm = arm; }
            }
            previousPosition = transform.parent.position;
        }

        private void OnEnable()
        {
            if (transform.parent != null) previousPosition = transform.parent.position;
        }

        private void LateUpdate()
        {
            if (leftLeg == null || transform.parent == null) return;
            Vector3 current = transform.parent.position;
            Vector3 delta = current - previousPosition;
            delta.y = 0;
            previousPosition = current;
            gait += delta.magnitude * 8f;
            float swing = delta.sqrMagnitude > 0.000001f ? Mathf.Sin(gait) * 18f : 0f;
            leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
            leftArm.localRotation = Quaternion.Euler(-swing, 0, 0);
            rightArm.localRotation = Quaternion.Euler(swing, 0, 0);
            CharacterController controller = transform.parent.GetComponent<CharacterController>();
            transform.localScale = new Vector3(1f, controller == null ? 1f : Mathf.Clamp(controller.height / 1.75f, 0.3f, 1f), 1f);
        }

        private void OnDestroy()
        {
            foreach (Material material in materials) if (material != null) Destroy(material);
        }
    }
}
