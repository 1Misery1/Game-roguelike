using UnityEngine;
using Game.Data;
namespace Game.Dungeon
{
    /// Attached to every Room Prefab. Tells GameBootstrap where to spawn the player,
    /// place the exit door, and the arena bounds for enemy AI clamping.
    public class RoomMetadata : MonoBehaviour
    {
        [Header("Arena Bounds")]
        public float halfW = 14f;
        public float halfH =  8f;

        [Header("Spawn Points")]
        public Transform playerSpawn;
        public Transform doorSpawn;

        [Tooltip("Enemy AI will pick random positions near these transforms.")]
        public Transform[] enemySpawnPoints;

        /// Converts to the MapInfo struct that GameBootstrap currently uses.
        public MapInfo ToMapInfo()
        {
            return new MapInfo
            {
                HalfW       = halfW,
                HalfH       = halfH,
                PlayerSpawn = playerSpawn != null ? playerSpawn.position : new Vector3(-halfW + 2f, 0f, 0f),
                DoorPos     = doorSpawn   != null ? doorSpawn.position   : new Vector3( halfW - 1f, 0f, 0f),
            };
        }
    }
}
