using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChulaEarthquakeVR
{
    /// <summary>Saved editor furnishings. Runtime systems bind to these saved poses and colliders.</summary>
    public sealed class HouseAuthoredFurniture : MonoBehaviour
    {
        [Serializable]
        public struct Binding
        {
            public Transform furnishing;
            public string anchorName;
            public int anchorIndex;
        }
        public Binding[] bindings = Array.Empty<Binding>();

        public void BindRuntimeAnchors()
        {
            // Cache before reparenting so duplicate names cannot change subsequent lookups.
            var anchors = FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene == gameObject.scene && !t.IsChildOf(transform))
                .OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToArray();
            foreach (Binding entry in bindings)
            {
                if (entry.furnishing == null || string.IsNullOrEmpty(entry.anchorName)) continue;
                Transform target = anchors.Where(t => t.name == entry.anchorName).Skip(entry.anchorIndex).FirstOrDefault();
                if (target == null)
                {
                    Debug.LogError("Missing House gameplay anchor: " + entry.anchorName, this);
                    continue;
                }
                foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                foreach (Collider collider in target.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                target.SetPositionAndRotation(entry.furnishing.position, entry.furnishing.rotation);
                target.localScale = entry.furnishing.lossyScale;
                entry.furnishing.SetParent(target, true);
                XRGrabInteractable grab = target.GetComponent<XRGrabInteractable>();
                if (grab != null)
                {
                    // Refresh the collider list cached when the runtime anchor was created.
                    grab.enabled = false;
                    grab.colliders.Clear();
                    grab.colliders.AddRange(target.GetComponentsInChildren<Collider>().Where(c => c.enabled));
                    grab.enabled = true;
                }
            }
        }
    }
}
