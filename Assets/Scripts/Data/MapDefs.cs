using UnityEngine;

namespace Game.Data
{
    // 地图尺寸常量（每格 1×1 世界单位，地图中心在 (0,0)）
    public static class MapDims
    {
        public const int TileW = 32;
        public const int TileH = 20;
    }

    // 单张房间的运行时信息（由建图/加载房间预制体产出，供 Bootstrap 使用）
    public struct MapInfo
    {
        public float   HalfW;
        public float   HalfH;
        public Vector3 PlayerSpawn;
        public Vector3 DoorPos;
    }
}
