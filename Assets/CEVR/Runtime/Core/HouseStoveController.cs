using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class HouseStoveController : MonoBehaviour
    {
        private bool isOn;
        private Renderer[] burners;
        private Material offMat;
        private Material onMat;

        public bool IsOn => isOn;

        public void Configure(Material offMaterial, Material onMaterial)
        {
            offMat = offMaterial;
            onMat = onMaterial;
            if (!TryBounds(out Bounds b)) return;

            GameObject root = new GameObject("CEVR_StoveControls");
            root.transform.SetParent(transform, true);

            burners = new Renderer[4];
            float dx = Mathf.Max(0.14f, b.extents.x * 0.45f);
            float dz = Mathf.Max(0.12f, b.extents.z * 0.38f);
            Vector3[] p = {
                new Vector3(-dx,0,-dz), new Vector3(dx,0,-dz),
                new Vector3(-dx,0,dz),  new Vector3(dx,0,dz)
            };
            for (int i=0;i<4;i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                g.name = "StoveBurner_" + (i+1);
                g.transform.SetParent(root.transform, true);
                g.transform.position = new Vector3(b.center.x+p[i].x, b.max.y+0.025f, b.center.z+p[i].z);
                g.transform.localScale = new Vector3(0.18f,0.015f,0.18f);
                Collider c = g.GetComponent<Collider>(); if (c != null) c.enabled = false;
                burners[i] = g.GetComponent<Renderer>(); if (burners[i] != null) burners[i].sharedMaterial = offMat;
            }

            GameObject hit = new GameObject("StoveInteractionSurface");
            hit.transform.SetParent(root.transform, true);
            hit.transform.position = new Vector3(b.center.x, Mathf.Lerp(b.min.y,b.max.y,0.55f), b.min.z-0.18f);
            hit.AddComponent<BoxCollider>().size = new Vector3(Mathf.Max(0.7f,b.size.x),0.55f,0.10f);
            hit.AddComponent<HouseStoveButton>().Configure(this);
        }

        public void Toggle()
        {
            isOn = !isOn;
            Material m = isOn ? onMat : offMat;
            if (burners != null) foreach (Renderer r in burners) if (r != null) r.sharedMaterial = m;
        }

        private bool TryBounds(out Bounds b)
        {
            b = default; bool found=false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.enabled) continue;
                if (!found) { b=r.bounds; found=true; } else b.Encapsulate(r.bounds);
            }
            return found;
        }
    }

    public sealed class HouseStoveButton : MonoBehaviour
    {
        private HouseStoveController stove;
        public string Prompt => stove != null && stove.IsOn ? "TURN STOVE OFF" : "TURN STOVE ON";
        public void Configure(HouseStoveController controller) => stove = controller;
        public void Press() { if (stove != null) stove.Toggle(); }
        private void OnMouseDown() => Press();
    }
}
