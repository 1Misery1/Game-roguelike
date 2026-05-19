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

            // Skip pathfinding when very close — direct path is always fine
            if (dist < 1.5f)
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
    }
}
