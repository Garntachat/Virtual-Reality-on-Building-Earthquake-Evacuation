using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChulaEarthquakeVR
{
    /// <summary>Self-contained tabby model with owned meshes/materials and articulated feet/tail.</summary>
    public sealed class HousePetModel : MonoBehaviour
    {
        private readonly List<Object> owned = new List<Object>();

        public static HousePetController Create(Transform parent, Vector3 position, GroundMotionPlayer motion)
        {
            var root = new GameObject("HousePet_Mali");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var builder = root.AddComponent<HousePetModel>();
            builder.Build(motion);
            return root.GetComponent<HousePetController>();
        }

        private Material Fur(string label, Color color)
        {
            Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = label, color = color };
            owned.Add(material);
            return material;
        }

        private Transform Joint(string label, Transform parent, Vector3 position)
        {
            var joint = new GameObject(label).transform;
            joint.SetParent(parent, false);
            joint.localPosition = position;
            return joint;
        }

        private void Build(GroundMotionPlayer motion)
        {
            Material orange = Fur("Mali tabby fur", new Color(0.64f, 0.32f, 0.12f));
            Material cream = Fur("Mali cream markings", new Color(0.93f, 0.83f, 0.64f));
            Material stripe = Fur("Mali dark stripes", new Color(0.25f, 0.12f, 0.055f));
            Material black = Fur("Mali pupils", new Color(0.025f, 0.028f, 0.025f));
            Material green = Fur("Mali eyes", new Color(0.46f, 0.64f, 0.20f));
            Material pink = Fur("Mali nose and ears", new Color(0.72f, 0.37f, 0.34f));
            Transform visual = Joint("MaliModel", transform, Vector3.zero);
            Oval("Body", visual, new Vector3(0,.29f,0), new Vector3(.28f,.28f,.55f), orange);
            Oval("Chest", visual, new Vector3(0,.30f,.20f), new Vector3(.25f,.30f,.20f), cream);
            Oval("Head", visual, new Vector3(0,.43f,.28f), new Vector3(.28f,.25f,.24f), orange);
            for (int side = -1; side <= 1; side += 2)
            {
                Ear(visual, side, orange);
                Oval("InnerEar", visual, new Vector3(side*.105f,.568f,.30f), new Vector3(.043f,.067f,.013f), pink);
                Oval("Muzzle", visual, new Vector3(side*.048f,.397f,.387f), new Vector3(.105f,.072f,.065f), cream);
                Oval("Eye", visual, new Vector3(side*.077f,.466f,.377f), new Vector3(.064f,.051f,.027f), green);
                Oval("Pupil", visual, new Vector3(side*.077f,.467f,.391f), new Vector3(.016f,.040f,.008f), black);
                Oval("EyeGlint", visual, new Vector3(side*.077f-.007f,.478f,.396f), new Vector3(.009f,.009f,.006f), cream);
                for (int i=0;i<3;i++)
                    Oval("FlankStripe", visual, new Vector3(side*.133f,.32f,-.13f+i*.11f), new Vector3(.014f,.16f,.035f), stripe);
            }
            Oval("Nose", visual, new Vector3(0,.413f,.420f), new Vector3(.041f,.027f,.022f), pink);
            Transform[] feet = new Transform[4];
            for (int i=0;i<4;i++)
            {
                feet[i] = Joint("Leg"+i, visual, new Vector3(i%2==0 ? -.085f:.085f,.24f,i<2 ? .17f:-.18f));
                Oval("Shin", feet[i], new Vector3(0,-.10f,0), new Vector3(.075f,.23f,.09f), orange);
                Oval("Paw", feet[i], new Vector3(0,-.205f,.023f), new Vector3(.085f,.067f,.12f), cream);
            }
            Transform tail = Joint("Tail", visual, new Vector3(0,.31f,-.24f));
            for (int i=0;i<8;i++)
                Oval("TailSegment"+i, tail, new Vector3(.018f*i,.022f*i,-.043f*i),
                    Vector3.one*(.064f-i*.004f), i%3==0 ? stripe:orange);
            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.direction=2; collider.center=new Vector3(0,.16f,0); collider.radius=.16f; collider.height=.64f;
            var body = gameObject.AddComponent<Rigidbody>();
            body.mass=3.5f; body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.constraints=RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            var grab = gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach=false;
            grab.movementType=XRBaseInteractable.MovementType.VelocityTracking;
            grab.selectMode=InteractableSelectMode.Single;
            gameObject.AddComponent<HousePetController>().Configure(motion,visual,feet,tail);
        }

        private void Oval(string label, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            const int rings=10, sides=16;
            var vertices=new Vector3[(rings+1)*(sides+1)];
            var triangles=new List<int>();
            for(int y=0;y<=rings;y++)
            for(int x=0;x<=sides;x++)
            {
                float a=Mathf.PI*y/rings, b=2*Mathf.PI*x/sides;
                vertices[y*(sides+1)+x]=Vector3.Scale(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)),size)*.5f;
                if(y==rings || x==sides) continue;
                int k=y*(sides+1)+x;
                triangles.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});
            }
            RenderMesh(label,parent,position,vertices,triangles.ToArray(),material);
        }

        private void Ear(Transform parent,int side,Material material)
        {
            Vector3[] v={new Vector3(-.06f,0,-.045f),new Vector3(.06f,0,-.045f),new Vector3(.04f,0,.045f),new Vector3(-.04f,0,.045f),new Vector3(side*.025f,.14f,0)};
            RenderMesh("PointedEar",parent,new Vector3(side*.092f,.515f,.255f),v,
                new[]{0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3},material);
        }

        private void RenderMesh(string label,Transform parent,Vector3 position,Vector3[] vertices,int[] triangles,Material material)
        {
            var mesh=new Mesh { name="Mali_"+label,vertices=vertices,triangles=triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            Transform part=Joint(label,parent,position);
            part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        private void OnDestroy()
        {
            foreach(Object item in owned) if(item!=null) Destroy(item);
        }
    }
}
