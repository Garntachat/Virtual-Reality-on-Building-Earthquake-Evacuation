using UnityEngine;
using System.Collections.Generic;

public class EarthquakeFurnitureController : MonoBehaviour
{
    [Header("Earthquake Settings")]
    public bool isEarthquakeActive = false;
    public float baseForce = 15f;    // Base shaking force
    public float baseTorque = 5f;    // Base rotational force

    [Header("Building Level Settings")]
    [Tooltip("Current floor number (1 = ground floor)")]
    public int floorLevel = 1;

    [Tooltip("Shaking-force increase per floor")]
    public float perFloorMultiplier = 0.5f;

    // Collect all furniture rigidbodies in the room
    private List<Rigidbody> furnitureBodies = new List<Rigidbody>();

    void Start()
    {
        // Find rigidbodies below this room's furniture root
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            // Ignore kinematic objects such as fixed windows
            if (!rb.isKinematic)
            {
                furnitureBodies.Add(rb);
            }
        }
    }

    void FixedUpdate()
    {
        if (isEarthquakeActive)
        {
            ApplyEarthquakeForce();
        }
    }

    private void ApplyEarthquakeForce()
    {
        // Scale the force by floor level
        // Floor 1 uses baseline force
        // Floor 3 uses twice the baseline force with multiplier 0.5
        float currentFloorMultiplier = 1f + ((floorLevel - 1) * perFloorMultiplier);

        float actualForce = baseForce * currentFloorMultiplier;
        float actualTorque = baseTorque * currentFloorMultiplier;

        foreach (Rigidbody rb in furnitureBodies)
        {
            // Generate a random horizontal force with a small vertical component
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0f, 0.2f),
                Random.Range(-1f, 1f)
            ).normalized;

            Vector3 randomTorqueDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

            // Apply an acceleration force independent of mass
            rb.AddForce(randomDirection * actualForce, ForceMode.Acceleration);
            rb.AddTorque(randomTorqueDir * actualTorque, ForceMode.Acceleration);
        }
    }
}