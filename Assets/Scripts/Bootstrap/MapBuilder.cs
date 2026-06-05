using System.Collections.Generic;
using Game.AI;
using UnityEngine;
using Game.Art;
using Game.Data;
using static Game.Data.MapDims;
namespace Game.Bootstrap
{
    // 程序化地图系统：3层×3种布局 = 9张地图
    // 每张地图 32×20 格，每格 = 1×1 世界单位，中心在 (0,0)
    //
    // 瓦片字符：
    //   '#' = 实墙       'p' = 石柱       '.' = 地板
    //   'd' = 出口位置   't' = 地形陷阱   'x' = 装饰道具
    //   'l' = 熔岩喷发格（第1层，周期性爆发 6 DPS 真实伤害）
    public static class MapBuilder
    {
        /// 返回指定楼层/变体的字符串地图（供 Editor 工具使用）
        public static string[] GetMap(int floor, int variant)
        {
            int fi  = Mathf.Clamp(floor - 1, 0, 2);
            int idx = fi * 3 + (variant % 3);
            return _maps[idx];
        }

        // ── 9 张地图布局 ─────────────────────────────────────────────────────
        static readonly string[][] _maps =
        {
            // ═══ Floor 1：炼狱石牢（岩浆池 'l'，火柱陷阱 't'，骸骨装饰 'x'）═
            new[] // F1-A "守卫大厅" — 四角哨塔 + 中央熔岩伏击区
            {
                "################################",
                "#..............................#",
                "#.pp...####.......####...pp....#",
                "#..............................#",
                "#.....####...tll...####........#",
                "#..............................#",
                "#.pp...........................#",
                "#..............................#",
                "#..............................d",
                "#...............p..............d",
                "#..............................#",
                "#..............................#",
                "#.pp...........................#",
                "#..............................#",
                "#.....####...tll...####........#",
                "#..............................#",
                "#.pp...####.......####...pp....#",
                "#...ll.........................#",
                "#..............................#",
                "################################",
            },
            new[] // F1-B "柱廊迷阵" — 多段墙体形成过道与伏击区
            {
                "################################",
                "#..............................#",
                "#..p....p...####...p....p......#",
                "#...t..........................#",
                "#......####.............####...#",
                "#...ll.........................#",
                "#..p....p....p....p....p......#",
                "#..............................#",
                "#..............................d",
                "#..............................d",
                "#..p....p....p....p....p......#",
                "#..............................#",
                "#...ll.........................#",
                "#......####.............####...#",
                "#...t..........................#",
                "#..p....p...####...p....p......#",
                "#..............................#",
                "#......................ll......#",
                "#..............................#",
                "################################",
            },
            new[] // F1-C "废墟神庙" — 双层遗迹墙 + 中庭支柱
            {
                "################################",
                "#..............................#",
                "#.p.....p.....x.....p.....p...#",
                "#.....####..........####.......#",
                "#.....####..........####.......#",
                "#.p.....p.................p....#",
                "#...t..........................#",
                "#.......##.............##......#",
                "#.......##.............##......d",
                "#...............p..............d",
                "#.......##.............##......#",
                "#.......##.............##......#",
                "#.p.....p.................p....#",
                "#.....####..........####.......#",
                "#.....####..........####.......#",
                "#.p.....p.....x.....p.....p...#",
                "#..................t...........#",
                "#......................ll......#",
                "#.......ll.....................#",
                "################################",
            },

            // ═══ Floor 2：霜境冰窟（冰刺陷阱 't'，冰晶装饰 'x'）═══════════
            new[] // F2-A "冰宫回廊" — 厚重冰墙 + 对称巡逻陷阱
            {
                "################################",
                "##............................##",
                "##..p.p....####...####....p.p.#",
                "#..............................#",
                "#.........t..........t.........#",
                "#.p.p..........................#",
                "#..............................#",
                "#.......####......####.........#",
                "#..............................d",
                "#...............p..............d",
                "#.......####......####.........#",
                "#..............................#",
                "#.p.p..........................#",
                "#.........t..........t.........#",
                "#..............................#",
                "##..p.p....####...####....p.p.#",
                "##............................##",
                "#.....x........................#",
                "#..............................#",
                "################################",
            },
            new[] // F2-B "冰晶柱阵" — 密集冰柱阵 + 核心堡垒
            {
                "################################",
                "#..............................#",
                "#.p..p..p..p..p..p..p..p......#",
                "#..............................#",
                "#..............................#",
                "#.p..p..p..p..p..p..p..p......#",
                "#..............t...............#",
                "#...........######.............#",
                "#...........######.............d",
                "#...........######.............d",
                "#...........######.............#",
                "#..............................#",
                "#.p..p..p..p..p..p..p..p......#",
                "#..............t...............#",
                "#..............................#",
                "#.p..p..p..p..p..p..p..p......#",
                "#....x.........................#",
                "#..............................#",
                "#..............................#",
                "################################",
            },
            new[] // F2-C "霜域石窟" — 钟乳石洞窟感 + 不规则冰壁
            {
                "################################",
                "###..........................###",
                "##.p.......................p..##",
                "#.......###......###...........#",
                "#..............................#",
                "#.p.....p.....t...........p....#",
                "##.............................#",
                "#..........##.....##...........#",
                "#..........##.....##...........d",
                "#...............p..............d",
                "#..........##.....##...........#",
                "#.............................##",
                "#.p.....p.................p....#",
                "#.......###......###...........#",
                "#...........t..................#",
                "##.p.......................p..##",
                "###..........................###",
                "#...x..........................#",
                "#..............................#",
                "################################",
            },

            // ═══ Floor 3：混沌深渊（虚空裂隙陷阱 't'，虚空符文装饰 'x'）═══
            new[] // F3-A "虚空裂隙" — 不对称虚空能量场
            {
                "################################",
                "####..........................##",
                "#.....p....................t....#",
                "#..............................#",
                "#.........######.......######..#",
                "#.....p........................#",
                "#..........t...................#",
                "#.........######.......######..#",
                "#..............................d",
                "#...............p..............d",
                "#.........######.......######..#",
                "#..............................#",
                "#.....p........................#",
                "#.........######.......######..#",
                "#..............................#",
                "#.....p..t.................p...#",
                "####..........................##",
                "#.....x........................#",
                "#..............................#",
                "################################",
            },
            new[] // F3-B "混沌祭台" — 中央祭坛 + 放射状通道
            {
                "################################",
                "#..............................#",
                "#.p..p..p..p..p..p..p..p......#",
                "#.........t..........t.........#",
                "#......####.......####.........#",
                "#..............t...............#",
                "#.p......####...####.......p...#",
                "#..........##...##.............#",
                "#..........##...##.............d",
                "#..........p.....p.............d",
                "#..........##...##.............#",
                "#.p......####...####.......p...#",
                "#..............................#",
                "#......####.......####.........#",
                "#..............................#",
                "#.p..p..p..p..p..p..p..p......#",
                "#....x.........................#",
                "#..............................#",
                "#..............................#",
                "################################",
            },
            new[] // F3-C "深渊长廊" — 交错暗墙 + 双轴陷阱走廊
            {
                "################################",
                "#.....##.........##............#",
                "#.....##.........##............#",
                "#.p..........................p.#",
                "#..................t...........#",
                "#.....##.........##............#",
                "#.p..........................p.#",
                "#.....##.........##............#",
                "#..............................d",
                "#...............p..............d",
                "#.....##.........##............#",
                "#.p..........................p.#",
                "#.....##.........##............#",
                "#...........t..................#",
                "#.p..........................p.#",
                "#.....##.........##............#",
                "#.....##.........##............#",
                "#........................x.....#",
                "#..............................#",
                "################################",
            },
        };

        // ══════════════════════════════════════════════════════════════════════
        //  运行时程序化布局生成（item 5：地图深度）
        //  以「世界种子 + 楼层 + 变体」确定性产出 string[] 布局，沿用同一套瓦片
        //  字符，因此渲染 / NavGrid / 门 / 出生点全部复用现有管线，零额外接线。
        //  连通性由构造保证：外圈 2 格恒为地板（敌人四壁刷怪带 + 周界环路），障碍
        //  仅落在核心点阵且彼此天然留 >=1 空地；末尾再洪泛验证，失败回退静态图。
        // ══════════════════════════════════════════════════════════════════════

        /// 是否启用运行时程序化布局（关掉则回退到 9 张手写静态图 / 房间预制体）
        public static bool Procedural = true;

        static int  _worldSeed;
        static bool _seeded;
        static int  _reseedCounter;

        /// 世界种子：同一局内恒定 → 每层确定性可复现；首次访问惰性随机
        public static int WorldSeed
        {
            get { if (!_seeded) ReseedWorld(); return _worldSeed; }
            set { _worldSeed = value; _seeded = true; }
        }

        /// 每局开始调用一次，刷新世界种子 → 新一局得到全新布局
        public static void ReseedWorld()
        {
            _worldSeed = unchecked((int)System.DateTime.Now.Ticks + (++_reseedCounter) * 0x6F4E3D2B);
            _seeded    = true;
        }

        // ── 生成器输出（异形房间需把出生/出门/刷怪点动态告知 Build/Bootstrap）──
        static bool          _genValid;          // 本次是否产出有效的异形布局
        static Vector3       _genSpawn;          // 玩家出生世界坐标
        static List<Vector3> _genEnemySpawns;    // 可达开放格（敌人刷怪候选）

        /// 程序化产出一张 string[] 布局（确定性：相同 WorldSeed/floor/variant → 相同图）。
        /// 优先生成「异形轮廓」房间，退化时回退矩形布局，再不行回退静态图。
        static string[] Generate(int floor, int variant, bool forceRect = false)
        {
            int fi  = Mathf.Clamp(floor - 1, 0, 2);
            int idx = fi * 3 + (variant % 3);
            int seed = unchecked(WorldSeed * 73856093 ^ (floor + 1) * 19349663 ^ ((variant % 3) + 1) * 83492791);
            var rng  = new System.Random(seed);

            // forceRect：商店房 / Boss 房强制规整矩形（GenerateRect 内置 _genValid=false → 固定出生 + 旧刷怪逻辑）
            if (forceRect) return GenerateRect(fi, idx, rng);

            var irregular = GenerateIrregular(fi, rng);
            return irregular ?? GenerateRect(fi, idx, rng);
        }

        // ── 矩形布局：外圈空 + 核心点阵障碍；连通性靠构造 + 洪泛验证 ────────────
        static string[] GenerateRect(int fi, int idx, System.Random rng)
        {
            _genValid = false;                       // 矩形房沿用固定出生 + 旧刷怪逻辑

            var g = new char[TileH][];
            for (int r = 0; r < TileH; r++)
            {
                g[r] = new char[TileW];
                for (int c = 0; c < TileW; c++)
                    g[r][c] = (r == 0 || r == TileH - 1 || c == 0 || c == TileW - 1) ? '#' : '.';
            }

            g[8][TileW - 1] = 'd';
            g[9][TileW - 1] = 'd';

            const int x0 = 5, y0 = 3, cellW = 5, cellH = 4;
            int x1 = TileW - 6, y1 = TileH - 4;
            for (int cy = y0; cy + cellH - 1 <= y1; cy += cellH)
            for (int cx = x0; cx + cellW - 1 <= x1; cx += cellW)
            {
                if (rng.Next(100) < 22) continue;
                StampFeature(g, rng, fi, cx + 1, cy + 1, cellW - 2, cellH - 2);
            }

            var rows = ToRows(g);
            return Verify(rows) ? rows : _maps[idx];
        }

        // ══ 异形房间：重叠矩形并集 + 横向主脊 → 非矩形轮廓；连通由主脊 + 验证保证 ══
        static string[] GenerateIrregular(int fi, System.Random rng)
        {
            // 1) 全墙
            var g = new char[TileH][];
            for (int r = 0; r < TileH; r++)
            {
                g[r] = new char[TileW];
                for (int c = 0; c < TileW; c++) g[r][c] = '#';
            }

            // 2) 横向主脊：横贯左右的走廊 → 保证出生(左)↔出门(右)连通
            int spineH   = 4 + rng.Next(3);                          // 4..6
            int spineTop = 4 + rng.Next(Mathf.Max(1, TileH - 8 - spineH + 1));
            Carve(g, 2, spineTop, TileW - 4, spineH);

            // 3) 若干重叠矩形「凸包」：与主脊纵向相交 → 连通且轮廓不规则
            int blobs = 3 + rng.Next(3);                             // 3..5
            for (int k = 0; k < blobs; k++)
            {
                int bw = 5 + rng.Next(8);                            // 5..12
                int bh = 4 + rng.Next(7);                            // 4..10
                int bx = 1 + rng.Next(Mathf.Max(1, TileW - 2 - bw));
                int byMin = Mathf.Max(1, spineTop - bh + 2);
                int byMax = Mathf.Min(TileH - 1 - bh, spineTop + spineH - 2);
                int by = byMax <= byMin ? byMin : byMin + rng.Next(byMax - byMin + 1);
                Carve(g, bx, by, bw, bh);
            }

            // 4) 主题化障碍（散点，要求四邻皆地板 → 不封口；末尾再洪泛验证）
            ScatterFeatures(g, rng, fi);

            // 5) 出生 / 出门：主脊中线最左 / 最右地板格
            int midRow = spineTop + spineH / 2;
            int sc = -1, dc = -1;
            for (int c = 1; c < TileW - 1; c++) if (g[midRow][c] == '.') { sc = c; break; }
            for (int c = TileW - 2; c > 0;     c--) if (g[midRow][c] == '.') { dc = c; break; }
            if (sc < 0 || dc < 0 || dc - sc < 6) return null;        // 退化 → 回退矩形
            g[midRow][dc] = 'd';

            var rows = ToRows(g);

            // 6) 连通验证 + 收集敌人可达刷怪格
            var seen = FloodFrom(rows, sc, midRow);
            if (!seen[dc, midRow]) return null;                      // 门不可达 → 回退

            Vector3 spawnW = ToWorld(sc, midRow);
            var enemies = new List<Vector3>();
            for (int r = 1; r < TileH - 1; r++)
            for (int c = 1; c < TileW - 1; c++)
            {
                if (!seen[c, r] || rows[r][c] != '.') continue;
                Vector3 w = ToWorld(c, r);
                if ((w - spawnW).sqrMagnitude >= 6f * 6f) enemies.Add(w);  // 远离出生点
            }
            if (enemies.Count < 4) return null;                      // 刷怪点太少 → 回退

            _genValid       = true;
            _genSpawn       = spawnW;
            _genEnemySpawns = enemies;
            return rows;
        }

        // 把 (x,y,w,h) 矩形雕成地板，永不破坏外框
        static void Carve(char[][] g, int x, int y, int w, int h)
        {
            for (int r = y; r < y + h; r++)
            for (int c = x; c < x + w; c++)
                if (c >= 1 && c < TileW - 1 && r >= 1 && r < TileH - 1)
                    g[r][c] = '.';
        }

        // 异形房散点障碍：随机地板格 + 四邻皆地板（避免贴边封口）
        static void ScatterFeatures(char[][] g, System.Random rng, int fi)
        {
            var th = _featureThresh[Mathf.Clamp(fi, 0, 2)];
            int want = 10 + rng.Next(8), attempts = 80;
            while (attempts-- > 0 && want > 0)
            {
                int x = 2 + rng.Next(TileW - 4), y = 2 + rng.Next(TileH - 4);
                if (g[y][x] != '.' ||
                    g[y - 1][x] != '.' || g[y + 1][x] != '.' ||
                    g[y][x - 1] != '.' || g[y][x + 1] != '.') continue;

                int roll = rng.Next(100);
                char t = roll < th[0] ? '#' : roll < th[1] ? 'p' : roll < th[2] ? 't' : 'x';
                g[y][x] = t;
                if ((t == '#' || t == 'p') && rng.Next(100) < 50)    // 偶尔扩成小簇
                {
                    int nx = x + (rng.Next(2) * 2 - 1);
                    if (g[y][nx] == '.' && g[y - 1][nx] == '.' && g[y + 1][nx] == '.') g[y][nx] = t;
                }
                want--;
            }
        }

        static Vector3 ToWorld(int c, int r)
        {
            var v = NavGrid.CellToWorld(new Vector2Int(c, r));
            return new Vector3(v.x, v.y, 0f);
        }

        static bool TileWalkable(string[] rows, int c, int r) =>
            c >= 0 && c < TileW && r >= 0 && r < TileH &&
            (rows[r][c] == '.' || rows[r][c] == 'd' || rows[r][c] == 't' ||
             rows[r][c] == 'l' || rows[r][c] == 'x');

        // 自 (sc,sr) 起 8 向洪泛，返回可达可走格集合
        static bool[,] FloodFrom(string[] rows, int sc, int sr)
        {
            var seen = new bool[TileW, TileH];
            if (!TileWalkable(rows, sc, sr)) return seen;
            var stack = new Stack<int>();
            seen[sc, sr] = true;
            stack.Push(sr * TileW + sc);
            while (stack.Count > 0)
            {
                int v = stack.Pop();
                int c = v % TileW, r = v / TileW;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nc = c + dx, nr = r + dy;
                    if (nc < 0 || nc >= TileW || nr < 0 || nr >= TileH) continue;
                    if (seen[nc, nr] || !TileWalkable(rows, nc, nr)) continue;
                    seen[nc, nr] = true;
                    stack.Push(nr * TileW + nc);
                }
            }
            return seen;
        }

        // 主题化特征权重（每行 = wall / pillar / trap 的累计阈值，余下为装饰）
        //   F1 炼狱→多墙   F2 霜境→多柱   F3 混沌→多陷阱
        static readonly int[][] _featureThresh =
        {
            new[] { 55, 72, 88 },   // fi=0 wall<55  pillar<72  trap<88  decor
            new[] { 22, 70, 86 },   // fi=1 wall<22  pillar<70  trap<86  decor
            new[] { 26, 48, 90 },   // fi=2 wall<26  pillar<48  trap<90  decor
        };

        // 在子格内部区域 (rx,ry,rw,rh) 盖一个特征；区域四周天然留 >=1 空地 → 保证连通
        static void StampFeature(char[][] g, System.Random rng, int fi, int rx, int ry, int rw, int rh)
        {
            var th   = _featureThresh[Mathf.Clamp(fi, 0, 2)];
            int roll = rng.Next(100);
            if (roll < th[0])         // 墙块
            {
                int w  = Mathf.Min(rw, 2 + rng.Next(2));
                int h  = Mathf.Min(rh, 1 + rng.Next(2));
                int ox = rx + rng.Next(rw - w + 1);
                int oy = ry + rng.Next(rh - h + 1);
                for (int y = oy; y < oy + h; y++)
                for (int x = ox; x < ox + w; x++) g[y][x] = '#';
            }
            else if (roll < th[1])    // 柱阵
            {
                int n = 1 + rng.Next(rw * rh / 2 + 1);
                for (int k = 0; k < n; k++)
                    g[ry + rng.Next(rh)][rx + rng.Next(rw)] = 'p';
            }
            else if (roll < th[2])    // 陷阱
            {
                int x = rx + rng.Next(rw), y = ry + rng.Next(rh);
                g[y][x] = 't';
                if (rng.Next(100) < 35 && x + 1 < rx + rw) g[y][x + 1] = 't';
            }
            else                      // 装饰
            {
                g[ry + rng.Next(rh)][rx + rng.Next(rw)] = 'x';
            }
        }

        static string[] ToRows(char[][] g)
        {
            var rows = new string[TileH];
            for (int r = 0; r < TileH; r++) rows[r] = new string(g[r]);
            return rows;
        }

        // 洪泛验证：自出生格 8 向连通；门 + 四壁刷怪带代表点须全部可达，否则判失败
        static bool Verify(string[] rows)
        {
            bool Walk(int c, int r) =>
                c >= 0 && c < TileW && r >= 0 && r < TileH &&
                (rows[r][c] == '.' || rows[r][c] == 'd' || rows[r][c] == 't' ||
                 rows[r][c] == 'l' || rows[r][c] == 'x');

            int sc = 2, sr = TileH / 2;          // 出生格 (2,10)
            if (!Walk(sc, sr)) return false;

            var seen  = new bool[TileW, TileH];
            var stack = new Stack<int>();
            seen[sc, sr] = true;
            stack.Push(sr * TileW + sc);
            while (stack.Count > 0)
            {
                int v = stack.Pop();
                int c = v % TileW, r = v / TileW;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nc = c + dx, nr = r + dy;
                    if (nc < 0 || nc >= TileW || nr < 0 || nr >= TileH) continue;
                    if (seen[nc, nr] || !Walk(nc, nr)) continue;
                    seen[nc, nr] = true;
                    stack.Push(nr * TileW + nc);
                }
            }

            // 门(双格) + 右/上/下三向刷怪带代表点（左向即出生格本身）
            return seen[TileW - 1, 8] && seen[TileW - 1, 9] &&
                   seen[TileW - 3, TileH / 2] &&
                   seen[TileW / 2, 2] && seen[TileW / 2, TileH - 3];
        }

        // ── 精灵缓存 ─────────────────────────────────────────────────────────
        static Sprite[] _wallSprites    = new Sprite[3];
        static Sprite[] _pillarSprites  = new Sprite[3];
        static Sprite[] _torchSprites   = new Sprite[3];
        static Sprite[] _trapSprites    = new Sprite[3];

        // ── 构建地图 ─────────────────────────────────────────────────────────
        public static MapInfo Build(int floor, int variant, Transform parent, bool createBackground = true, bool forceRect = false)
        {
            int fi  = Mathf.Clamp(floor - 1, 0, 2);
            int idx = fi * 3 + (variant % 3);
            var rows = Procedural ? Generate(floor, variant, forceRect) : _maps[idx];

            NavGrid.Build(rows);
            SetupPhysicsLayers();

            if (createBackground)
                FloorBackground.Create(floor, parent, TileW + 4f, TileH + 4f);

            float ox = -TileW * 0.5f;
            float oy =  TileH * 0.5f;

            var wallSpr   = GetWall(fi);
            var pillarSpr = GetPillar(fi);
            var torchSpr  = GetTorch(fi);
            var trapSpr   = GetTrap(fi);

            var playerSpawn = (Procedural && _genValid)
                ? _genSpawn
                : new Vector3(ox + 2.5f, 0f, 0f);
            var doorPos     = new Vector3(-ox - 0.5f, 0f, 0f);

            for (int r = 0; r < TileH; r++)
            {
                string row = r < rows.Length ? rows[r] : new string('#', TileW);
                if (row.Length < TileW) row = row.PadRight(TileW, '#');

                for (int c = 0; c < TileW; c++)
                {
                    char tile = row[c];
                    float wx = ox + c + 0.5f;
                    float wy = oy - r - 0.5f;
                    var pos = new Vector3(wx, wy, 0f);

                    switch (tile)
                    {
                        case '#':
                            SpawnFloor(pos, fi, r, c, parent, isUnderWall: true);
                            SpawnWall(pos, wallSpr, parent);
                            if (ShouldAddTorch(rows, r, c) && (c + r * 3) % 8 == 0)
                                SpawnTorch(pos, torchSpr, parent);
                            break;
                        case '.':
                            SpawnFloor(pos, fi, r, c, parent);
                            break;
                        case 'p':
                            SpawnFloor(pos, fi, r, c, parent);
                            SpawnPillar(pos, pillarSpr, parent);
                            break;
                        case 'd':
                            SpawnFloor(pos, fi, r, c, parent);
                            doorPos = pos;
                            break;
                        case 't':
                            SpawnFloor(pos, fi, r, c, parent);
                            SpawnTrap(pos, fi, trapSpr, parent);
                            break;
                        case 'l':
                            // 小熔岩格已废弃：只生成普通地板，不再放 LavaVent
                            SpawnFloor(pos, fi, r, c, parent);
                            break;
                        case 'x':
                            SpawnFloor(pos, fi, r, c, parent);
                            SpawnDecoration(pos, fi, parent);
                            break;
                    }
                }
            }

            return new MapInfo
            {
                HalfW       = TileW * 0.5f,
                HalfH       = TileH * 0.5f,
                PlayerSpawn = playerSpawn,
                DoorPos     = doorPos,
                EnemySpawns = (Procedural && _genValid && _genEnemySpawns != null)
                    ? _genEnemySpawns.ToArray()
                    : null,
            };
        }

        // ── 地板瓦片（Kenney 素材，有主题配色）──────────────────────────────
        static void SpawnFloor(Vector3 pos, int fi, int r, int c, Transform parent, bool isUnderWall = false)
        {
            if (isUnderWall) return; // 墙体下无需地板

            int variation = ((c * 7 + r * 13) & 0x7FFFFFFF) % 5; // 0-4
            int tileIndex = 48 + variation; // Kenney 确认的石地板组

            Sprite spr = TilesetLoader.IsAvailable
                ? TilesetLoader.Get(tileIndex)
                : MakeFallbackFloor(fi);

            if (spr == null) return;

            var go = new GameObject("F");
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 0;
            sr.color        = FloorTint(fi);
        }

        // ── 陷阱（含已有MonoBehaviour：FlamePillar / IceSpikeTrap / VoidRift）
        static void SpawnTrap(Vector3 pos, int fi, Sprite baseSpr, Transform parent)
        {
            var go = new GameObject("Trap");
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = baseSpr;
            sr.sortingOrder = 2;

            switch (fi)
            {
                case 0:
                    go.AddComponent<FlamePillar>();
                    go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 0.9f;
                    break;
                case 1:
                    go.AddComponent<IceSpikeTrap>();
                    go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 0.7f;
                    break;
                case 2:
                    go.AddComponent<VoidRift>();
                    go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 1.4f;
                    break;
            }
        }

        // ── 装饰道具（Kenney 素材，有主题着色）──────────────────────────────
        static void SpawnDecoration(Vector3 pos, int fi, Transform parent)
        {
            // Props rows 5-7 from TMX Objects layer analysis
            int[] decorIndices = { 61, 73, 75 }; // GIDs 62/74/76 confirmed in Objects layer
            int idx = decorIndices[((int)(pos.x * 13 + pos.y * 7) & 0x7FFFFFFF) % decorIndices.Length];

            Sprite spr = TilesetLoader.IsAvailable
                ? TilesetLoader.Get(idx)
                : MakeFallbackDecor(fi);

            if (spr == null) return;

            var go = new GameObject("D");
            go.transform.SetParent(parent, true);
            go.transform.position = pos + new Vector3(0f, 0.1f, 0f);
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 3;
            sr.color        = DecoTint(fi);
        }

        // ── 墙体 ─────────────────────────────────────────────────────────────
        static void SpawnWall(Vector3 pos, Sprite spr, Transform parent)
        {
            var go = new GameObject("W");
            go.layer = 9;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 1;
            go.AddComponent<BoxCollider2D>();
        }

        static void SpawnPillar(Vector3 pos, Sprite spr, Transform parent)
        {
            var go = new GameObject("P");
            go.layer = 9;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.72f, 0.88f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 2;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.85f);
        }

        static void SpawnTorch(Vector3 wallPos, Sprite spr, Transform parent)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, true);
            go.transform.position = wallPos + new Vector3(0f, -0.55f, 0f);
            go.transform.localScale = new Vector3(0.4f, 0.6f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 3;
        }

        static bool ShouldAddTorch(string[] rows, int r, int c)
        {
            if (r + 1 >= TileH) return false;
            string nextRow = r + 1 < rows.Length ? rows[r + 1] : new string('#', TileW);
            if (nextRow.Length <= c) return false;
            char below = nextRow[c];
            return below == '.' || below == 'p' || below == 'd' || below == 't' || below == 'x';
        }

        // ── 物理层 ───────────────────────────────────────────────────────────
        static bool _physicsSetup;
        public static void SetupPhysics() => SetupPhysicsLayers();
        static void SetupPhysicsLayers()
        {
            if (_physicsSetup) return;
            _physicsSetup = true;
            Physics2D.IgnoreLayerCollision(8, 8, true);
            Physics2D.IgnoreLayerCollision(0, 8, true);
            Physics2D.IgnoreLayerCollision(8, 9, false);
            Physics2D.IgnoreLayerCollision(0, 9, false);
        }

        // ── 配色方案 ─────────────────────────────────────────────────────────
        static Color FloorTint(int fi) => fi switch
        {
            0 => new Color(0.52f, 0.32f, 0.20f),  // 炼狱：暗红石
            1 => new Color(0.40f, 0.58f, 0.78f),  // 霜境：冰蓝石
            _ => new Color(0.28f, 0.18f, 0.42f),  // 混沌：深紫石
        };

        static Color DecoTint(int fi) => fi switch
        {
            0 => new Color(0.80f, 0.50f, 0.28f),  // 炼狱：焦棕
            1 => new Color(0.68f, 0.88f, 1.00f),  // 霜境：冰白
            _ => new Color(0.72f, 0.42f, 1.00f),  // 混沌：紫光
        };

        // ── 精灵缓存获取 ──────────────────────────────────────────────────────
        static Sprite GetWall(int fi)
        {
            if (_wallSprites[fi] == null) _wallSprites[fi] = MakeWall(fi);
            return _wallSprites[fi];
        }
        static Sprite GetPillar(int fi)
        {
            if (_pillarSprites[fi] == null) _pillarSprites[fi] = MakePillar(fi);
            return _pillarSprites[fi];
        }
        static Sprite GetTorch(int fi)
        {
            if (_torchSprites[fi] == null) _torchSprites[fi] = MakeTorch(fi);
            return _torchSprites[fi];
        }
        static Sprite GetTrap(int fi)
        {
            if (_trapSprites[fi] == null) _trapSprites[fi] = MakeTrapMark(fi);
            return _trapSprites[fi];
        }

        // ══════════════════════════════════════════════════════════════════════
        //  像素精灵绘制（32×32，每格 = 1 世界单位）
        // ══════════════════════════════════════════════════════════════════════
        const int SZ  = 32;
        const int PPU = 32;

        // ── 炼狱墙：暗红错缝砖墙 + 岩浆裂缝 + 深阴影 ───────────────────────
        static Sprite MakeWall(int fi)
        {
            var px = new Color32[SZ * SZ];
            switch (fi)
            {
                case 0: DrawInfernoWall(px); break;
                case 1: DrawFrostWall(px);   break;
                default: DrawVoidWall(px);   break;
            }
            return Bake(px);
        }

        static void DrawInfernoWall(Color32[] px)
        {
            var mortar = C(14, 7,  3);
            var brick1 = C(55, 26, 10);
            var brick2 = C(44, 20,  8);
            var hiEdge = C(82, 42, 18);
            var shadow = C( 8,  3,  1);
            var ember  = C(210, 72, 10);

            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int brickRow = y / 8;
                int shift    = (brickRow % 2 == 0) ? 0 : 8;
                int brickX   = (x + shift) % SZ;

                Color32 c;
                if (y % 8 == 0 || y % 8 == 1 || brickX % 16 == 0 || brickX % 16 == 1)
                    c = (y % 8 <= 1) ? mortar : shadow;
                else
                    c = (brickRow % 2 == 0) ? brick1 : brick2;

                int by = y % 8;
                if (by == 2) c = Blend(c, hiEdge, 0.35f);
                if (by == 3) c = Blend(c, hiEdge, 0.18f);
                if (by >= 6) c = Blend(c, shadow, 0.4f + (by - 6) * 0.2f);

                // 岩浆裂缝
                float n = Mathf.Abs(Mathf.Sin(x * 1.3f + y * 2.7f + brickRow * 1.9f));
                float n2 = Mathf.Abs(Mathf.Sin(x * 3.1f - y * 1.5f));
                if (n > 0.90f && n2 > 0.4f && y % 8 > 1) c = Blend(c, ember, (n - 0.90f) * 10f);

                if (x == 0 || y == SZ - 1) c = Blend(c, hiEdge, 0.45f);
                if (x == SZ - 1) c = Blend(c, shadow, 0.6f);
                px[y * SZ + x] = c;
            }
        }

        static void DrawFrostWall(Color32[] px)
        {
            var mortar = C( 5, 10, 22);
            var stone  = C(16, 30, 56);
            var ice    = C(48, 98, 168);
            var hiEdge = C(98,158, 220);
            var shadow = C( 3,  6, 14);
            var vein   = C(80,150, 210);

            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int brickRow = y / 8;
                int shift    = (brickRow % 2 == 0) ? 0 : 8;
                int brickX   = (x + shift) % SZ;

                Color32 c;
                if (y % 8 == 0 || y % 8 == 1 || brickX % 16 == 0 || brickX % 16 == 1)
                    c = mortar;
                else
                    c = (brickRow % 2 == 0) ? stone : Blend(stone, shadow, 0.3f);

                int by = y % 8;
                if (by == 2) c = Blend(c, hiEdge, 0.4f);
                if (by >= 6) c = Blend(c, shadow, 0.3f + (by - 6) * 0.15f);

                // 冰晶纹理
                float iv = Mathf.Abs(Mathf.Sin(x * 0.9f + y * 1.6f) * Mathf.Cos(x * 1.9f - y * 0.7f));
                if (iv > 0.84f && y % 8 > 1) c = Blend(c, vein, (iv - 0.84f) * 6f);

                if ((x == 0 || y == SZ - 1) && y % 8 > 1) c = Blend(c, hiEdge, 0.55f);
                if (x == SZ - 1) c = Blend(c, shadow, 0.5f);
                px[y * SZ + x] = c;
            }
        }

        static void DrawVoidWall(Color32[] px)
        {
            var mortar = C( 6,  3, 10);
            var void_  = C(10,  5, 18);
            var stone  = C(20, 10, 32);
            var vein   = C(75, 10,125);
            var glow   = C(155, 28,215);
            var shadow = C( 3,  1,  6);

            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int brickRow = y / 8;
                int shift    = (brickRow % 2 == 0) ? 0 : 8;
                int brickX   = (x + shift) % SZ;

                Color32 c;
                if (y % 8 == 0 || y % 8 == 1 || brickX % 16 == 0 || brickX % 16 == 1)
                    c = mortar;
                else
                    c = (brickRow % 3 == 0) ? stone : void_;

                int by = y % 8;
                if (by >= 6) c = Blend(c, shadow, 0.4f + (by - 6) * 0.2f);

                // 虚空符文裂缝
                float rune  = Mathf.Abs(Mathf.Sin(x * 1.6f - y * 2.4f));
                float rune2 = Mathf.Abs(Mathf.Cos(x * 2.8f + y * 1.1f));
                if (rune > 0.87f && rune2 > 0.5f && y % 8 > 1)
                    c = Blend(c, rune > 0.94f ? glow : vein, (rune - 0.87f) * 13f);

                if (x == SZ - 1) c = Blend(c, shadow, 0.65f);
                px[y * SZ + x] = c;
            }
        }

        // ── 石柱（32×32，圆柱感，主题装饰）─────────────────────────────────
        static Sprite MakePillar(int fi)
        {
            var px = new Color32[SZ * SZ];
            switch (fi)
            {
                case 0: DrawInfernoPillar(px); break;
                case 1: DrawFrostPillar(px);   break;
                default: DrawVoidPillar(px);   break;
            }
            return Bake(px);
        }

        static void DrawInfernoPillar(Color32[] px)
        {
            int cx = SZ / 2, cy = SZ / 2;
            var outer  = C(22, 11,  5);
            var mid    = C(46, 22,  9);
            var inner  = C(62, 32, 14);
            var hiTop  = C(88, 46, 20);
            var ember  = C(220, 85, 10);
            var chain  = C(90, 70, 50);

            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float r = 13.5f;
                if (d > r) { px[y * SZ + x] = C(0,0,0,0); continue; }

                float t = d / r;
                Color32 c = Blend(inner, mid, t);
                c = Blend(c, outer, Mathf.Pow(t, 1.8f));

                if (dy < -3 && d < r - 1.5f) c = Blend(c, hiTop, 0.4f * (1f - t));
                if (dy > 3) c = Blend(c, ember, 0.22f * (1f - t) * ((float)(dy + cy) / SZ));

                // 柱面纵向凹槽
                float groove = Mathf.Abs(Mathf.Sin(x * Mathf.PI * 4f / SZ));
                if (groove < 0.12f && d < r - 2f) c = Blend(c, outer, 0.45f);

                // 中部铁链纹路
                if (Mathf.Abs(y - cy) < 3 && d < r - 2f) c = Blend(c, chain, 0.55f);

                px[y * SZ + x] = c;
            }
        }

        static void DrawFrostPillar(Color32[] px)
        {
            int cx = SZ / 2, cy = SZ / 2;
            var outer  = C( 8, 20, 46);
            var mid    = C(22, 52, 98);
            var inner  = C(46, 98,158);
            var tip    = C(175,218,255);
            var crack  = C(78,148,218);
            var icicle = C(140,210,255);

            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float r = 13f;
                if (d > r) { px[y * SZ + x] = C(0,0,0,0); continue; }

                float t = d / r;
                Color32 c = Blend(inner, mid, t);
                c = Blend(c, outer, Mathf.Pow(t, 1.6f));

                if (dy < -2 && d < r - 1f) c = Blend(c, tip, 0.55f * (1f - t));

                float cf = Mathf.Abs(Mathf.Sin(x * 1.2f + y * 0.9f));
                if (cf > 0.84f && d < r - 1.5f) c = Blend(c, crack, (cf - 0.84f) * 6f);

                // 顶部冰锥
                if (dy < -8 && d < 6f) c = Blend(c, icicle, 0.7f * (1f - t));
                if (dy < -10 && d < 4f) c = Blend(c, tip, 0.9f);

                px[y * SZ + x] = c;
            }
        }

        static void DrawVoidPillar(Color32[] px)
        {
            int cx = SZ / 2, cy = SZ / 2;
            var outer  = C( 8,  4, 16);
            var mid    = C(25,  9, 46);
            var inner  = C(42, 17, 70);
            var rune   = C(115, 18,185);
            var glow   = C(188, 55,255);
            var eye    = C(255,180, 80);

            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int dx = x - cx, dy = y - cy;
                float dm = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                float r = 12.5f;
                if (dm > r) { px[y * SZ + x] = C(0,0,0,0); continue; }

                float t = dm / r;
                Color32 c = Blend(inner, mid, t);
                c = Blend(c, outer, Mathf.Pow(t, 1.4f));

                // 符文纹路
                float rn = Mathf.Abs(Mathf.Sin(y * 0.8f + x * 0.15f));
                if (rn > 0.87f && dm < r - 1.5f) c = Blend(c, rn > 0.95f ? glow : rune, (rn - 0.87f) * 12f);

                // 中心虚空眼
                float de = Mathf.Sqrt(dx * dx + dy * dy);
                if (de < 5f)
                {
                    float et = de / 5f;
                    c = Blend(eye, c, et);
                    if (de < 2.5f) c = Blend(C(0,0,0,255), c, de / 2.5f);
                }

                px[y * SZ + x] = c;
            }
        }

        // ── 火把 ─────────────────────────────────────────────────────────────
        static Sprite MakeTorch(int fi)
        {
            var px = new Color32[SZ * SZ];
            switch (fi)
            {
                case 0: DrawFlame(px, C(255,130,15), C(255,215,50)); break;
                case 1: DrawFlame(px, C(55,125,215), C(175,215,255)); break;
                default: DrawFlame(px, C(135,15,195), C(195,75,255)); break;
            }
            return Bake(px);
        }

        static void DrawFlame(Color32[] px, Color32 baseCol, Color32 tipCol)
        {
            int cx = SZ / 2;
            Fill(px, SZ, cx - 3, SZ - 9, 6, 8, C(55, 45, 36));
            Fill(px, SZ, cx - 2, SZ - 10, 4, 2, C(88, 72, 52));

            for (int y = 3; y < SZ - 9; y++)
            {
                float fy = (SZ - 9 - y) / (float)(SZ - 12);
                int hw = Mathf.Max(1, Mathf.RoundToInt(5f * (1f - fy * fy)));
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    if (x < 0 || x >= SZ) continue;
                    float t = fy;
                    Color32 fc = Blend(baseCol, tipCol, t);
                    byte alpha = (byte)(230 - (byte)(80 * fy));
                    fc.a = alpha;
                    int bidx = y * SZ + x;
                    if (px[bidx].a < alpha) px[bidx] = fc;
                }
            }
        }

        // ── 陷阱标记精灵（供 FlamePillar/IceSpikeTrap/VoidRift 使用）────────
        static Sprite MakeTrapMark(int fi)
        {
            var px = new Color32[SZ * SZ];
            switch (fi)
            {
                case 0: DrawLavaMark(px);   break;
                case 1: DrawIceMark(px);    break;
                default: DrawVoidMark(px);  break;
            }
            return Bake(px);
        }

        static void DrawLavaMark(Color32[] px)
        {
            int cx = SZ / 2, cy = SZ / 2;
            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float d = Mathf.Sqrt((x-cx)*(x-cx) + (y-cy)*(y-cy));
                if (d > 12f) { px[y*SZ+x] = C(0,0,0,0); continue; }
                float t = d / 12f;
                Color32 c = Blend(C(255,90,10), C(80,20,5), t);
                if (d < 5f) c = Blend(C(255,200,60), c, (5f-d)/5f);
                c.a = (byte)(200 * (1f - t * 0.5f));
                px[y*SZ+x] = c;
            }
        }

        static void DrawIceMark(Color32[] px)
        {
            int cx = SZ / 2, cy = SZ / 2;
            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                int dx = x-cx, dy = y-cy;
                float star = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy))
                           + Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) * 0.5f;
                if (star > 12f) { px[y*SZ+x] = C(0,0,0,0); continue; }
                float t = star / 12f;
                Color32 c = Blend(C(220,240,255), C(40,80,150), t);
                c.a = (byte)(180 * (1f - t * 0.5f));
                px[y*SZ+x] = c;
            }
        }

        static void DrawVoidMark(Color32[] px)
        {
            int cx = SZ / 2, cy = SZ / 2;
            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float dx = x-cx, dy = y-cy;
                float d = Mathf.Sqrt(dx*dx + dy*dy);
                if (d > 12f) { px[y*SZ+x] = C(0,0,0,0); continue; }
                float angle = Mathf.Atan2(dy, dx);
                float swirl = Mathf.Abs(Mathf.Sin(angle * 4f + d * 0.5f));
                float t = d / 12f;
                Color32 c = Blend(C(180,30,255), C(15,5,30), t);
                c = Blend(c, C(80,0,140), swirl * 0.4f);
                c.a = (byte)(190 * (1f - t * 0.4f));
                px[y*SZ+x] = c;
            }
        }

        // ── 备用精灵（TilesetLoader 不可用时）────────────────────────────────
        static Sprite MakeFallbackFloor(int fi)
        {
            var px = new Color32[SZ * SZ];
            Color32 a, b;
            switch (fi)
            {
                case 0: a = C(45,22,10); b = C(38,18,8);  break;
                case 1: a = C(14,28,52); b = C(10,22,42); break;
                default:a = C(16, 8,28); b = C(12, 6,22); break;
            }
            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                bool grid = (x == 0 || x == SZ-1 || y == 0 || y == SZ-1);
                px[y*SZ+x] = grid ? Blend(a, C(0,0,0), 0.5f) : ((x+y)%2==0 ? a : b);
            }
            return Bake(px);
        }

        static Sprite MakeFallbackDecor(int fi)
        {
            var px = new Color32[SZ * SZ];
            Color32 c = fi switch { 0 => C(120,80,40), 1 => C(160,200,255), _ => C(140,60,200) };
            for (int y = SZ/4; y < SZ*3/4; y++)
            for (int x = SZ/4; x < SZ*3/4; x++)
                px[y*SZ+x] = c;
            return Bake(px);
        }

        // ── 工具 ─────────────────────────────────────────────────────────────
        static Color32 C(int r, int g, int b, int a = 255)
            => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        static Color32 Blend(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t));
        }

        static void Fill(Color32[] px, int sz, int x0, int y0, int w, int h, Color32 c)
        {
            for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
                if (x >= 0 && x < sz && y >= 0 && y < sz)
                    px[y * sz + x] = c;
        }

        static Sprite Bake(Color32[] px)
        {
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), PPU);
        }
    }
}
