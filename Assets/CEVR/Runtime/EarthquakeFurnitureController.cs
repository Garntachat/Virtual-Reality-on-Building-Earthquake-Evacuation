using UnityEngine;

// Retained for older team scenes that already reference this script.
// The Tutorial uses GroundMotionPlayer + one editable InertialRigidbody per item.
[DisallowMultipleComponent]
public class EarthquakeFurnitureController : MonoBehaviour
{
    [Header("Legacy scene fallback (no GroundMotionPlayer)")]
    [Tooltip("Only used by old standalone scenes. Modern CEVR scenes use their shared ground motion.")]
    public bool isEarthquakeActive;

    [Min(0f)] public float baseForce = 15f;
    [Min(0f)] public float baseTorque = 5f;

    private Rigidbody[] furnitureBodies;
    private bool hasSharedMotion;

    private void Awake()
    {
        hasSharedMotion = FindFirstObjectByType<ChulaEarthquakeVR.GroundMotionPlayer>() != null;
        furnitureBodies = GetComponentsInChildren<Rigidbody>(true);
    }

    private void FixedUpdate()
    {
        // Never stack a separate shaker on top of the normal CEVR earthquake.
        if (!isEarthquakeActive || hasSharedMotion) return;

        // Simple repeatable sine-wave fallback. No randomness and no per-object
        // secret multipliers. In normal Tutorial/House scenes this is not used.
        float phase = Time.fixedTime * Mathf.PI * 4f;
        Vector3 direction = new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase));
        foreach (Rigidbody body in furnitureBodies)
        {
            if (body == null || body.isKinematic ||
                body.GetComponent<ChulaEarthquakeVR.InertialRigidbody>() != null) continue;
            body.AddForce(direction * baseForce, ForceMode.Force);
            body.AddTorque(new Vector3(direction.z, 0f, -direction.x) * baseTorque, ForceMode.Force);
        }
    }
}
