using System.Collections.Generic;
using UnityEngine;

namespace Game.AI
{
    // Attach to any enemy that should navigate around walls.
    // Caches an A* path and updates it every repathInterval seconds.
    public class EnemyNavigator : MonoBehaviour
    {
        public float repathInterval = 0.4f;
        public float waypointRadius = 0.7f;   // distance to advance to next waypoint

        readonly List<Vector2> _path = new List<Vector2>();
        int   _idx;
        float _nextRepath;

        // Returns a normalised move direction toward the given world-space target,
        // routing around walls if a path exists. Falls back to direct direction.
        public Vector2 GetMoveDirection(Vector2 target)
        {
            Vector2 myPos = transform.position;
            float dist = Vector2.Distance(myPos, target);

            // Skip pathfinding when very close AND no hazard between us and the target.
            // Sampling a few interpolated cells catches lava/traps in the direct line.
            if (dist < 1.5f && !DirectLineHasHazard(myPos, target))
            {
                _path.Clear();
                Vector2 d = target - myPos;
                return d.sqrMagnitude > 0.001f ? d / dist : Vector2.zero;
            }

            if (Time.time >= _nextRepath)
            {
                _nextRepath = Time.time + repathInterval;
                var newPath = NavGrid.FindPath(myPos, target);
                _path.Clear();
                _path.AddRange(newPath);
                _idx = 0;
            }

            // Advance waypoint index as we reach each one
            while (_idx < _path.Count &&
                   Vector2.Distance(myPos, _path[_idx]) < waypointRadius)
                _idx++;

            if (_idx < _path.Count)
            {
                Vector2 toWp = _path[_idx] - myPos;
                return toWp.sqrMagnitude > 0.001f ? toWp.normalized : Vector2.zero;
            }

            // No path or reached end — go direct
            Vector2 delta = target - myPos;
            return delta.sqrMagnitude > 0.001f ? delta.normalized : Vector2.zero;
        }

        // Force a repath on the next frame (call after teleporting enemy)
        public void InvalidatePath() => _nextRepath = 0f;

        // 敌人身体半径：用于检测「前缘」是否撞墙，避免运动学刚体穿模
        public const float BodyRadius = 0.34f;

        /// 把期望落点 to 钳制为「不穿墙/柱、不主动踩陷阱」的安全落点（轴分离贴墙滑行）。
        /// 敌人是 Kinematic 刚体，MovePosition 不会被静态墙挡住，故在此手动按格判定。
        /// 未建导航图的场景（如训练场）直接放行，保持原始移动。
        public static Vector2 Resolve(Vector2 from, Vector2 to, bool avoidHazards = true)
        {
            if (!NavGrid.HasMap) return to;

            Vector2 result = from;

            float dx = to.x - from.x;
            if (Mathf.Abs(dx) > 1e-5f)
            {
                float edgeX = to.x + Mathf.Sign(dx) * BodyRadius;     // 目标 X 方向前缘
                if (CellOk(new Vector2(edgeX, from.y), from, avoidHazards))
                    result.x = to.x;
            }

            float dy = to.y - from.y;
            if (Mathf.Abs(dy) > 1e-5f)
            {
                float edgeY = to.y + Mathf.Sign(dy) * BodyRadius;     // 目标 Y 方向前缘
                if (CellOk(new Vector2(result.x, edgeY), from, avoidHazards))
                    result.y = to.y;
            }

            return result;
        }

        static bool CellOk(Vector2 worldPos, Vector2 from, bool avoidHazards)
        {
            var cell = NavGrid.WorldToCell(worldPos);
            if (!NavGrid.PathWalkable(cell.x, cell.y)) return false;        // 墙/柱：硬挡（IgnoreObstacles 时穿过）
            if (!NavGrid.IgnoreObstacles && avoidHazards && NavGrid.HazardAt(cell.x, cell.y) > 0)
            {
                // 仅当自己当前不在危险格时才避让（已身处危险中则允许移动以逃出）
                var fc = NavGrid.WorldToCell(from);
                if (NavGrid.HazardAt(fc.x, fc.y) == 0) return false;
            }
            return true;
        }

        // 沿直线采样 4 个点，若任一格有危险代价，则走 A* 绕开
        static bool DirectLineHasHazard(Vector2 from, Vector2 to)
        {
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 5f;
                var s   = Vector2.Lerp(from, to, t);
                var cell = NavGrid.WorldToCell(s);
                if (NavGrid.HazardAt(cell.x, cell.y) > 0) return true;
            }
            return false;
        }
    }
}
