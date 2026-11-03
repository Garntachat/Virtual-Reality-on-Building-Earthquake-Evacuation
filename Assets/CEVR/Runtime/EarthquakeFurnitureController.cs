using UnityEngine;

// Simple legacy room controller matching the familiar inspector fields.
// New Tutorial/House/Condo scenes use GroundMotionPlayer to start/stop the quake.
// For a standalone authored scene (such as New_House), check isEarthquakeActive
// to test the SAME Rigidbody force physics manually in Play mode.
[DisallowMultipleComponent]
public class EarthquakeFurnitureController : MonoBehaviour
{
    [Header("Earthquake Settings")]
    public bool isEarthquakeActive = false;

    [Tooltip("Force in Newtons for a 1.0 Shake Strength object.")]
    [Min(0f)] public float baseForce = 15f;
    [Tooltip("Torque in Newton-meters for a 1.0 Shake Strength object.")]
    [Min(0f)] public float baseTorque = 5f;

    [Header("Optional Building Level")]
    [Min(1)] public int floorLevel = 1;
    [Min(0f)] public float perFloorMultiplier = 0f;

    private Rigidbody[] furnitureBodies;
    private ChulaEarthquakeVR.GroundMotionPlayer sharedMotion;

    private void Awake()
    {
        furnitureBodies = GetComponentsInChildren<Rigidbody>(true);
        sharedMotion = FindFirstObjectByType<ChulaEarthquakeVR.GroundMotionPlayer>();
    }

    private void FixedUpdate()
    {
        if (!isEarthquakeActive) return;
        // Never stack a second controller on top of the real shared scenario.
        if (sharedMotion != null) return;
        sharedMotion = FindFirstObjectByType<ChulaEarthquakeVR.GroundMotionPlayer>();
        if (sharedMotion != null) return;

        // Smooth, repeatable horizontal wave; no random per-frame impulses.
        float phase = Time.fixedTime * Mathf.PI * 4f;
        Vector3 wave = new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase));
        float floorScale = 1f + (Mathf.Max(1, floorLevel) - 1) * Mathf.Max(0f, perFloorMultiplier);

        foreach (Rigidbody body in furnitureBodies)
        {
            if (body == null || body.isKinematic || !body.gameObject.activeInHierarchy) continue;

            ChulaEarthquakeVR.InertialRigidbody item =
                body.GetComponent<ChulaEarthquakeVR.InertialRigidbody>();
            if (item != null)
            {
                // Respect the per-object strength and the saved Rigidbody Mass/Damping.
                item.ApplySimpleWave(wave, floorScale * baseForce / 15f,
                    floorScale * baseTorque / 5f);
            }
            else
            {
                // Unconfigured legacy object: Unity still respects Mass and Damping.
                body.AddForce(wave * (baseForce * floorScale), ForceMode.Force);
                body.AddTorque(new Vector3(wave.z, 0f, -wave.x) *
                    (baseTorque * floorScale), ForceMode.Force);
            }
        }
    }
}
