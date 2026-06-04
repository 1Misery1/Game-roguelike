using System.Collections.Generic;
using UnityEngine;
using Game.Data;
namespace Game.AI
{
    // A* pathfinding grid built from the active map layout.
    // Call NavGrid.Build() whenever a new map is loaded.
    public static class NavGrid
    {
        const int W = MapDims.TileW;
        const int H = MapDims.TileH;

        static readonly bool[,] _w        = new bool[W, H]; // _w[col, row]
        static readonly int [,] _hazard   = new int [W, H]; // 危险代价（A* 加成；可走但成本高）

        // 静态地形危险（'t' 陷阱 / 'l' 岩浆）：穿越一格代价极高 → A* 几乎总会绕开
        const int StaticHazardPenalty  = 40;
        // 动态危险（LavaPool / IceSpike / VoidRift / FlamePillar 等运行时生成）：每格 +28
        const int DynamicHazardPenalty = 28;

        /// 是否已构建过有效地图（未建图的场景如训练场不应阻挡敌人移动）
        public static bool HasMap { get; private set; }

        public static void Build(string[] rows)
        {
            HasMap = true;
            for (int r = 0; r < H; r++)
            {
                string row = r < rows.Length ? rows[r] : "";
                if (row.Length < W) row = row.PadRight(W, '#');
                for (int c = 0; c < W; c++)
                {
                    char ch = row[c];
                    _w[c, r] = ch == '.' || ch == 'd' || ch == 't' || ch == 'l' || ch == 'x';
                    // 't' 陷阱 → 仍可走（防卡死）但代价极高；'l' 小熔岩已废弃，按普通地板处理
                    _hazard[c, r] = (ch == 't') ? StaticHazardPenalty : 0;
                }
            }
        }

        /// 在 worldCenter 周围 worldRadius 范围内增加动态危险代价（A* 会绕开）。
        /// 配套：在物体销毁时调用 RemoveDynamicHazard 还原。
        public static void AddDynamicHazard(Vector2 worldCenter, float worldRadius)
            => ApplyDynamic(worldCenter, worldRadius, +DynamicHazardPenalty);

        public static void RemoveDynamicHazard(Vector2 worldCenter, float worldRadius)
            => ApplyDynamic(worldCenter, worldRadius, -DynamicHazardPenalty);

        static void ApplyDynamic(Vector2 worldCenter, float worldRadius, int delta)
        {
            var center = WorldToCell(worldCenter);
            int rad    = Mathf.CeilToInt(worldRadius);
            float rsq  = worldRadius * worldRadius + 0.001f;
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                if (dx * dx + dy * dy > rsq) continue;
                int x = center.x + dx, y = center.y + dy;
                if (x < 0 || x >= W || y < 0 || y >= H) continue;
                _hazard[x, y] = Mathf.Max(0, _hazard[x, y] + delta);
            }
        }

        /// 调试用：查询某格的当前危险代价
        public static int HazardAt(int c, int r)
            => (c < 0 || c >= W || r < 0 || r >= H) ? 0 : _hazard[c, r];

        public static Vector2Int WorldToCell(Vector2 pos)
        {
            int c = Mathf.FloorToInt(pos.x + W * 0.5f);
            int r = Mathf.FloorToInt(H * 0.5f - pos.y);
            return new Vector2Int(Mathf.Clamp(c, 0, W - 1), Mathf.Clamp(r, 0, H - 1));
        }

        public static Vector2 CellToWorld(Vector2Int cell)
            => new Vector2(cell.x - W * 0.5f + 0.5f, H * 0.5f - cell.y - 0.5f);

        public static bool IsWalkable(int c, int r)
            => c >= 0 && c < W && r >= 0 && r < H && _w[c, r];

        // Returns world-space waypoints from 'from' to 'to', excluding the start position.
        public static List<Vector2> FindPath(Vector2 from, Vector2 to)
        {
            var start = WorldToCell(from);
            var goal  = WorldToCell(to);
            if (!IsWalkable(goal.x,  goal.y))  goal  = NearestWalkable(goal);
            if (!IsWalkable(start.x, start.y)) start = NearestWalkable(start);
            if (start == goal) return new List<Vector2>();

            var open   = new List<(float f, Vector2Int n)>();
            var gScore = new Dictionary<Vector2Int, float>();
            var came   = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();

            gScore[start] = 0f;
            open.Add((Heuristic(start, goal), start));

            int budget = W * H + 1;
            while (open.Count > 0 && budget-- > 0)
            {
                int bi = 0;
                for (int i = 1; i < open.Count; i++)
                    if (open[i].f < open[bi].f) bi = i;

                var (_, cur) = open[bi];
                open.RemoveAt(bi);

                if (cur == goal) return Reconstruct(came, start, goal);
                closed.Add(cur);

                foreach (var nb in Neighbors(cur))
                {
                    if (closed.Contains(nb)) continue;
                    float g = gScore[cur] + MoveCost(cur, nb);
                    if (!gScore.TryGetValue(nb, out float og) || g < og)
                    {
                        gScore[nb] = g;
                        came[nb]   = cur;
                        open.Add((g + Heuristic(nb, goal), nb));
                    }
                }
            }
            return new List<Vector2>();
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static float MoveCost(Vector2Int a, Vector2Int b)
        {
            float baseCost = (a.x != b.x && a.y != b.y) ? 1.414f : 1f;
            return baseCost + _hazard[b.x, b.y];
        }

        static IEnumerable<Vector2Int> Neighbors(Vector2Int c)
        {
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = c.x + dx, ny = c.y + dy;
                if (!IsWalkable(nx, ny)) continue;
                // Block diagonal movement through wall corners
                if (dx != 0 && dy != 0 &&
                    (!IsWalkable(c.x + dx, c.y) || !IsWalkable(c.x, c.y + dy)))
                    continue;
                yield return new Vector2Int(nx, ny);
            }
        }

        static Vector2Int NearestWalkable(Vector2Int cell)
        {
            for (int rad = 1; rad < 8; rad++)
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                if (Mathf.Abs(dx) != rad && Mathf.Abs(dy) != rad) continue;
                var c = new Vector2Int(cell.x + dx, cell.y + dy);
                if (IsWalkable(c.x, c.y)) return c;
            }
            return cell;
        }

        static List<Vector2> Reconstruct(
            Dictionary<Vector2Int, Vector2Int> came, Vector2Int start, Vector2Int goal)
        {
            var cells = new List<Vector2Int>();
            var cur   = goal;
            while (cur != start)
            {
                cells.Add(cur);
                if (!came.TryGetValue(cur, out cur)) break;
            }
            cells.Reverse();
            var result = new List<Vector2>(cells.Count);
            foreach (var cell in cells) result.Add(CellToWorld(cell));
            return result;
        }
    }
}
