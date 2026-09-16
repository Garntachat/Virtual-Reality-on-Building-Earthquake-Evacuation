using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Measured layout for the authored House ProBuilder scene.
    /// Ground-floor coordinates come from HouseProBuilderLayoutAnalyzer. The small upper landing
    /// at y=4.0 m is reserved for the bedroom so the bed never blocks the ground-floor hallway.
    /// Every anchor is a bottom-center position unless its name explicitly says Center.
    /// </summary>
    public static class HouseSceneLayout
    {
        public const float FloorY = 1.0f;
        public const float SecondFloorY = 4.0f;
        // The y=4.0 upper-floor slab is the ceiling of the ground floor. Hanging objects should
        // touch its underside rather than being centered at/above the slab.
        public const float FirstFloorCeilingY = SecondFloorY;
        public const float FirstFloorCeilingUndersideY = FirstFloorCeilingY - 0.04f;

        public static readonly Vector3 PlayerSpawn = OnFloor(-1.90f, -5.55f, 0.03f);

        public static readonly Vector3 DiningTable = OnFloor(-1.90f, -2.70f);
        public static readonly Vector3 CoverObstacleChair = OnFloor(-1.90f, -3.75f);
        public static readonly Vector3 DiningLeftChair = OnFloor(-2.95f, -2.70f);
        public static readonly Vector3 DiningRightChair = OnFloor(-0.85f, -2.70f);
        public static readonly Vector3 SpareChair = OnFloor(-0.45f, -4.75f);

        public static readonly Vector3 Sofa = OnFloor(-3.65f, 0.55f);
        public static readonly Vector3 CoffeeTable = OnFloor(-2.10f, 0.55f);
        public static readonly Vector3 Rug = OnFloor(-2.65f, 0.55f);
        public static readonly Vector3 Television = OnFloor(-0.55f, 0.55f);
        public static readonly Vector3 Plant = OnFloor(5.55f, 4.85f);

        public static readonly Vector3 Bed = new Vector3(5.55f, SecondFloorY, 1.72f);
        public static readonly Vector3 BedPillow = new Vector3(5.55f, SecondFloorY + 0.55f, 2.55f);

        public static readonly Vector3 WardrobeBottom = OnFloor(-3.70f, 2.80f);
        public static readonly Vector3 WardrobeCenter = OnFloor(-3.70f, 2.80f, 1.15f);

        public static readonly Vector3 KitchenCabinet = OnFloor(2.25f, 6.15f);
        public static readonly Vector3 KitchenSink = OnFloor(3.35f, 6.15f);
        public static readonly Vector3 KitchenStove = OnFloor(4.45f, 6.15f);
        public static readonly Vector3 FridgeBottom = OnFloor(5.60f, 6.05f);
        public static readonly Vector3 FridgeCenter = OnFloor(5.60f, 6.05f, 1.15f);

        public static readonly Vector3 WindowCenter = new Vector3(-2.0f, 2.75f, -7.43f);
        public static readonly Vector3 WindowSize = new Vector3(0.96f, 1.18f, 0.045f);

        public static readonly Vector3 AssemblyPoint = OnFloor(-1.90f, -8.70f, 0.025f);
        public static readonly Vector3 CoverZone = OnFloor(-1.90f, -2.70f, 0.42f);

        // Falling props are 0.24 m tall when spawned. Center at 3.84 m so the top face is at
        // ~3.96 m: flush to the measured underside of the y=4.0 first-floor ceiling slab.
        public static readonly Vector3[] OverheadHazards =
        {
            new Vector3(-2.60f, 3.84f, -2.30f),
            new Vector3(-0.85f, 3.84f, -2.00f),
            new Vector3(-2.15f, 3.84f, 0.55f),
            new Vector3(3.35f, 3.84f, 5.75f)
        };

        public static Vector3 OnFloor(float x, float z, float yOffset = 0f)
            => new Vector3(x, FloorY + yOffset, z);

        public static bool IsInsideGroundFloor(Vector3 point, float margin = 0f)
        {
            return point.x >= -4.25f + margin && point.x <= 6.25f - margin &&
                   point.z >= -6.75f + margin && point.z <= 6.75f - margin &&
                   point.y >= FloorY - 0.05f;
        }
    }
}
