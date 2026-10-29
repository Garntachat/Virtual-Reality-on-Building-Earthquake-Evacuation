using UnityEngine;
using System.Collections.Generic;

public class EarthquakeFurnitureController : MonoBehaviour
{
    [Header("Earthquake Settings")]
    public bool isEarthquakeActive = false;
    public float baseForce = 15f;    // แรงสั่นพื้นฐาน
    public float baseTorque = 5f;    // แรงบิดหมุนพื้นฐาน

    [Header("Building Level Settings")]
    [Tooltip("ระบุว่าตอนนี้อยู่ชั้นที่เท่าไหร่ของอาคาร (1 = ชั้นล่างสุด)")]
    public int floorLevel = 1;

    [Tooltip("ตัวคูณความแรงต่อชั้น (ยิ่งสูงยิ่งสั่นแรง)")]
    public float perFloorMultiplier = 0.5f;

    // เก็บ Rigidbody ของเฟอร์นิเจอร์ทั้งหมดในห้องนี้
    private List<Rigidbody> furnitureBodies = new List<Rigidbody>();

    void Start()
    {
        // ค้นหา Rigidbody ทั้งหมดที่อยู่ในลูกๆ ของ Object นี้ (HouseFurniture)
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            // ข้ามวัตถุที่เป็น Kinematic (เช่น พรม, หน้าต่างที่ติดผนัง)
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
        // คำนวณความแรงสุทธิ โดยอิงจากชั้นของอาคาร
        // เช่น ชั้น 1 = baseForce * (1 + 0) = แรงปกติ
        // ชั้น 3 = baseForce * (1 + (2 * 0.5)) = แรงขึ้น 2 เท่า
        float currentFloorMultiplier = 1f + ((floorLevel - 1) * perFloorMultiplier);

        float actualForce = baseForce * currentFloorMultiplier;
        float actualTorque = baseTorque * currentFloorMultiplier;

        foreach (Rigidbody rb in furnitureBodies)
        {
            // สุ่มทิศทางแรง (เน้นเขย่าแนวราบ X, Z และกระดอนขึ้น Y นิดหน่อย)
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

            // การใส่แรงแบบ Acceleration จะทำให้ของหนักและของเบาได้รับผลกระทบจากแผ่นดินไหวต่างกัน
            rb.AddForce(randomDirection * actualForce, ForceMode.Acceleration);
            rb.AddTorque(randomTorqueDir * actualTorque, ForceMode.Acceleration);
        }
    }
}