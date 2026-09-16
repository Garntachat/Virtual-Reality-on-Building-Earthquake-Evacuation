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

        public static readonly Vector3 PlayerSpawn = OnFloor(-1.90f, -5.55f, 0.03f);

        // Compact Pleng dining set. The earlier chair/table spacing was based on oversized placeholder
        // dimensions and visually blocked the central route.
        public static readonly Vector3 DiningTable = OnFloor(-1.90f, -2.70f);
        public static readonly Vector3 CoverObstacleChair = OnFloor(-1.90f, -3.75f);
        public static readonly Vector3 DiningLeftChair = OnFloor(-2.95f, -2.70f);
        public static readonly Vector3 DiningRightChair = OnFloor(-0.85f, -2.70f);
        public static readonly Vector3 SpareChair = OnFloor(-0.45f, -4.75f);

        // Living room: sofa faces +X toward the TV wall.
        public static readonly Vector3 Sofa = OnFloor(-3.65f, 0.55f);
        public static readonly Vector3 CoffeeTable = OnFloor(-2.10f, 0.55f);
        public static readonly Vector3 Rug = OnFloor(-2.65f, 0.55f);
        public static readonly Vector3 Television = OnFloor(-0.55f, 0.55f);
        public static readonly Vector3 Plant = OnFloor(5.55f, 4.85f);

        // Analyzer measured the usable upper landing at y=4.0, x~4.25..6.25, z~0.25..3.0.
        // Use a proportionally scaled Pleng bed along Z, tight against +X, leaving a walk strip on
        // the x~4.25 side for the stair/landing route.
        public static readonly Vector3 Bed = new Vector3(5.55f, SecondFloorY, 1.72f);
        public static readonly Vector3 BedPillow = new Vector3(5.55f, SecondFloorY + 0.55f, 2.55f);

        public static readonly Vector3 WardrobeBottom = OnFloor(-3.70f, 2.80f);
        public static readonly Vector3 WardrobeCenter = OnFloor(-3.70f, 2.80f, 1.15f);

        public static readonly Vector3 KitchenCabinet = OnFloor(2.25f, 6.15f);
        public static readonly Vector3 KitchenSink = OnFloor(3.35f, 6.15f);
        public static readonly Vector3 KitchenStove = OnFloor(4.45f, 6.15f);
        public static readonly Vector3 FridgeBottom = OnFloor(5.60f, 6.05f);
        public static readonly Vector3 FridgeCenter = OnFloor(5.60f, 6.05f, 1.15f);

        // South-wall aperture from the analyzer: near x=-2.0 at z=-7.5, with a solid sill below.
        // Keep the fitted frame slightly inside the wall surface to prevent z-fighting.
        public static readonly Vector3 WindowCenter = new Vector3(-2.0f, 2.75f, -7.43f);
        public static readonly Vector3 WindowSize = new Vector3(0.96f, 1.18f, 0.045f);

        public static readonly Vector3 AssemblyPoint = OnFloor(-1.90f, -8.70f, 0.025f);
        public static readonly Vector3 CoverZone = OnFloor(-1.90f, -2.70f, 0.42f);

        public static readonly Vector3[] OverheadHazards =
        {
            OnFloor(-2.60f, -2.30f, 3.15f),
            OnFloor(-0.85f, -2.00f, 3.25f),
            OnFloor(-2.15f, 0.55f, 3.35f),
            OnFloor(3.35f, 5.75f, 3.05f)
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
