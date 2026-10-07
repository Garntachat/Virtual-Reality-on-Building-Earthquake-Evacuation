using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Measured coordinates for the authored House ProBuilder scene.
    /// Ground floor is y=1.0 m; upper walkable landing is y=4.0 m.
    /// </summary>
    public static class HouseSceneLayout
    {
        public const float FloorY = 1.0f;
        public const float SecondFloorY = 4.0f;
        public const float FirstFloorCeilingY = SecondFloorY;
        public const float FirstFloorCeilingUndersideY = FirstFloorCeilingY - 0.04f;

        public static readonly Vector3 PlayerSpawn = OnFloor(-1.90f, -5.55f, 0.03f);

        public static readonly Vector3 DiningTable = OnFloor(-1.90f, -2.70f);
        public static readonly Vector3 DiningTableSize = new Vector3(1.50f, 0.734f, 0.85f);
        public const float DiningTableYaw = 0f;
        public static readonly Vector3 DiningChairSize = new Vector3(0.421f, 0.90f, 0.401f);
        public static readonly Vector3 CoverObstacleChair = OnFloor(-1.90f, -3.48f);
        public static readonly Vector3 SpareChair = OnFloor(-1.90f, -1.92f);
        public static readonly Vector3 DiningLeftChair = OnFloor(-2.93f, -2.70f);
        public static readonly Vector3 DiningRightChair = OnFloor(-0.87f, -2.70f);

        public static readonly Vector3 Sofa = OnFloor(-3.65f, 0.55f);
        public static readonly Vector3 SofaSize = new Vector3(1.90f, 0.811f, 0.942f);
        public const float SofaYaw = 90f;
        public static readonly Vector3 SofaPillows = Sofa + new Vector3(0f, 0.46f, 0f);
        public static readonly Vector3 SofaPillowsSize = new Vector3(1.546f, 0.210f, 0.246f);
        public static readonly Vector3 CoffeeTable = OnFloor(-2.10f, 0.55f);
        public static readonly Vector3 CoffeeTableSize = new Vector3(1.35f, 0.45f, 0.72f);
        public static readonly Vector3 Rug = OnFloor(-2.65f, 0.55f);
        public static readonly Vector3 RugSize = new Vector3(2.50f, 0.02f, 3.00f);
        public static readonly Vector3 Television = OnFloor(-0.55f, 0.55f);
        public static readonly Vector3 TelevisionCabinetSize = new Vector3(2.00f, 0.65f, 0.50f);
        public static readonly Vector3 TelevisionSize = new Vector3(1.45f, 0.85f, 0.22f);
        public const float TelevisionYaw = 90f;
        public static readonly Vector3 Plant = OnFloor(5.55f, 4.85f);

        // Analyzer: second-floor landing x=4.25..6.25, z=0.25..3.0. The upper stair spans
        // approximately x=1..5 and z=1.5..3.0. The bed is a compact single-bed footprint placed
        // entirely inside x=4.35..6.20 and z=0.225..1.375, so it does not sit on the stair.
        public static readonly Vector3 Bed = new Vector3(5.275f, SecondFloorY, 0.80f);
        public static readonly Vector3 BedSize = new Vector3(1.15f, 0.72f, 1.85f);
        public const float BedYaw = 90f;
        public static readonly Vector3 BedPillow = new Vector3(4.58f, SecondFloorY + 0.54f, 0.80f);
        public static readonly Vector3 BedPillowSize = new Vector3(0.58f, 0.10f, 0.28f);

        public static readonly Vector3 WardrobeBottom = OnFloor(-3.70f, 2.80f);
        public static readonly Vector3 WardrobeCenter = OnFloor(-3.70f, 2.80f, 1.15f);

        public static readonly Vector3 KitchenCabinet = OnFloor(2.25f, 6.15f);
        public static readonly Vector3 KitchenSink = OnFloor(3.35f, 6.15f);
        public static readonly Vector3 KitchenStove = OnFloor(4.45f, 6.15f);
        public static readonly Vector3 FridgeBottom = OnFloor(5.60f, 6.05f);
        public static readonly Vector3 FridgeCenter = OnFloor(5.60f, 6.05f, 1.15f);

        // Exterior-ray audit found the south-wall aperture centered around x=-2.0. Samples at
        // x=-2.5,-2.0,-1.5 pass through the opening at upper heights, with a solid sill below.
        public static readonly Vector3 WindowCenter = new Vector3(-2.0f, 2.75f, -7.43f);
        public static readonly Vector3 WindowSize = new Vector3(1.42f, 1.50f, 0.045f);

        public static readonly Vector3 AssemblyPoint = OnFloor(-1.90f, -8.70f, 0.025f);
        public static readonly Vector3 CoverZone = OnFloor(-1.90f, -2.70f, 0.42f);

        // 0.24 m-high hazard body: center at 3.84 => top at 3.96, flush beneath y=4.0 slab.
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
