using System.IO;
using Game.Dungeon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Game.Bootstrap;
using Game.Data;
/// Editor utility: generates 9 Room Prefabs (Floor1-3 × A-C) from MapBuilder map data.
/// Run via Tools > Generate Room Prefabs.
public static class GenerateRoomPrefabs
{
    private const string TileDir    = "Assets/Tiles";
    private const string PrefabRoot = "Assets/Resources/Rooms";

    private static readonly string[] VariantNames = { "A", "B", "C" };

    [MenuItem("Tools/Generate Room Prefabs")]
    public static void Execute()
    {
        // ── 1. 确保目录存在（逐级创建）────────────────────────────────────────
        EnsureDir("Assets/Resources");
        EnsureDir(PrefabRoot);
        for (int f = 1; f <= 3; f++)
            EnsureDir($"{PrefabRoot}/Floor{f}");

        // ── 2. 加载 Tile 资源 ────────────────────────────────────────────────
        // 地板：每层独立（熔岩 / 冰面 / 虚空）
        var floorTile1 = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Floor.asset");
        var floorTile2 = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Floor2.asset");
        var floorTile3 = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Floor3.asset");
        // 墙壁：每层独立（熔岩墙 / 冰霜墙 / 混沌墙）
        var wallTile      = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Wall.asset");
        var wallFrostTile = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_WallFrost.asset");
        var wallChaos     = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_WallChaos.asset");
        // 公共
        var pillarTile = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Pillar.asset");
        var lavaTile   = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Lava.asset");
        var iceTile    = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Ice.asset");
        var voidTile   = Load<UnityEngine.Tilemaps.Tile>($"{TileDir}/Tile_Void.asset");

        if (floorTile1 == null || wallTile == null)
        {
            Debug.LogError("[GenerateRoomPrefabs] 缺少 Tile 资源，请先执行 Tools > Import Custom Tiles。");
            return;
        }

        // ── 3. 为每张地图生成 Prefab ─────────────────────────────────────────
        int created = 0;
        for (int floor = 1; floor <= 3; floor++)
        {
            // 按楼层选瓦片
            var activeFloor  = floor == 1 ? floorTile1  : floor == 2 ? (floorTile2 ?? floorTile1) : (floorTile3 ?? floorTile1);
            var activeWall   = floor == 1 ? wallTile     : floor == 2 ? wallFrostTile              : wallChaos;
            var activeHazard = floor == 1 ? lavaTile     : floor == 2 ? iceTile                    : voidTile;

            for (int v = 0; v < 3; v++)
            {
                string name = $"Room_F{floor}{VariantNames[v]}";
                string path = $"{PrefabRoot}/Floor{floor}/{name}.prefab";

                // 始终覆写，确保参数修改（如出生点）能立即生效
                AssetDatabase.DeleteAsset(path);

                var go = BuildRoomGameObject(
                    name, floor, v,
                    activeFloor, activeWall, pillarTile, activeHazard);

                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                created++;
                Debug.Log($"[GenerateRoomPrefabs] Created {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateRoomPrefabs] Done — {created} prefabs generated.");
    }

    // ── 构建单个房间的 GameObject（未保存到磁盘）────────────────────────────

    private static GameObject BuildRoomGameObject(
        string name, int floor, int variant,
        UnityEngine.Tilemaps.Tile floorTile,
        UnityEngine.Tilemaps.Tile wallTile,
        UnityEngine.Tilemaps.Tile pillarTile,
        UnityEngine.Tilemaps.Tile hazardTile)
    {
        string[] rows = MapBuilder.GetMap(floor, variant);

        // Grid 根节点
        var root = new GameObject(name);
        var grid = root.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // Grid 左下角原点偏移（地图中心在 world 0,0；32×20 格）
        // MapBuilder：行 0 在上，row r 的世界 y = TileH/2 - r - 0.5
        // Tilemap cellPos 是整数格坐标，原点设在 (-16, -10) 使中心对齐
        root.transform.position = Vector3.zero;

        // ── 三个 Tilemap 层 ──────────────────────────────────────────────────
        var (tmFloor, tmWalls) = CreateLayers(root);

        // ── 扫描地图字符并绘制瓦片 ──────────────────────────────────────────
        Vector3Int doorCell   = Vector3Int.zero;
        Vector3Int playerCell = new Vector3Int(-MapDims.TileW / 2 + 2, -1, 0); // 默认 c=2, 中行

        for (int r = 0; r < MapDims.TileH; r++)
        {
            string row = r < rows.Length ? rows[r] : new string('#', MapDims.TileW);
            if (row.Length < MapDims.TileW) row = row.PadRight(MapDims.TileW, '#');

            for (int c = 0; c < MapDims.TileW; c++)
            {
                char ch = row[c];
                // Tilemap cell coords: col offset by -TileW/2, row flipped from top-down to bottom-up
                var cell = new Vector3Int(c - MapDims.TileW / 2, MapDims.TileH - 1 - r - MapDims.TileH / 2, 0);

                switch (ch)
                {
                    case '#':
                        tmFloor.SetTile(cell, floorTile);
                        tmWalls.SetTile(cell, wallTile);
                        break;
                    case 'p':
                        tmFloor.SetTile(cell, floorTile);
                        tmWalls.SetTile(cell, pillarTile);
                        break;
                    case '.':
                    case 't': // 陷阱：可视为地板（运行时 trap spawner 叠加）
                    case 'x': // 装饰：同上
                        tmFloor.SetTile(cell, floorTile);
                        break;
                    case 'l':
                        // 小熔岩危险格已废弃：当作普通地板（大危险由运行时 FlamePillar 等提供）
                        tmFloor.SetTile(cell, floorTile);
                        break;
                    case 'd':
                        tmFloor.SetTile(cell, floorTile);
                        doorCell = cell; // 记录最后一个 'd' 作为出口
                        break;
                }

                // 检测玩家出生点（列 2，与旧程序化系统 x=-13.5 对齐，远离左侧碰撞墙）
                if (ch == '.' && c == 2 && r >= MapDims.TileH / 2 - 1 && r <= MapDims.TileH / 2 + 1)
                    playerCell = cell;
            }
        }

        // ── RoomMetadata ────────────────────────────────────────────────────
        var meta = root.AddComponent<RoomMetadata>();
        meta.halfW = MapDims.TileW * 0.5f;
        meta.halfH = MapDims.TileH * 0.5f;

        // 出生点：以 child Transform 记录（world position 由 cell 换算）
        meta.playerSpawn = MakeSpawnTransform(root, "PlayerSpawn",
            CellToWorld(playerCell));
        meta.doorSpawn = MakeSpawnTransform(root, "DoorSpawn",
            CellToWorld(doorCell));

        // 敌人出生点：从地图字符 '.' 区域采样几个位置
        meta.enemySpawnPoints = BuildEnemySpawns(root, rows);

        return root;
    }

    // ── 创建 Floor 和 Walls 两个 Tilemap 子层 ────────────────────────────────

    private static (Tilemap floor, Tilemap walls) CreateLayers(GameObject parent)
    {
        var floor = MakeTilemapLayer(parent, "Floor",  0, false);
        var walls = MakeTilemapLayer(parent, "Walls",  1, true);
        MakeTilemapLayer(parent, "Decorations", 2, false);
        return (floor, walls);
    }

    private static Tilemap MakeTilemapLayer(GameObject parent, string layerName, int sortOrder, bool withCollider)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(parent.transform, false);

        var tm = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = sortOrder;

        if (withCollider)
        {
            var col = go.AddComponent<TilemapCollider2D>();
            col.usedByComposite = true;
            var rb  = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var comp = go.AddComponent<CompositeCollider2D>();
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            go.layer = 9; // Wall 物理层
        }

        return tm;
    }

    // ── 从地图数据采样敌人出生点 ──────────────────────────────────────────────

    private static Transform[] BuildEnemySpawns(GameObject root, string[] rows)
    {
        var parent = new GameObject("EnemySpawnPoints");
        parent.transform.SetParent(root.transform, false);

        int count = 0;
        // 均匀网格采样：每 6 列、5 行采一个空地格，最多 8 个
        for (int r = 2; r < MapDims.TileH - 2 && count < 8; r += 5)
        {
            for (int c = 3; c < MapDims.TileW - 3 && count < 8; c += 6)
            {
                char ch = (r < rows.Length && c < rows[r].Length) ? rows[r][c] : '#';
                if (ch != '.' && ch != 't' && ch != 'x') continue;

                var cell = new Vector3Int(c - MapDims.TileW / 2, MapDims.TileH - 1 - r - MapDims.TileH / 2, 0);
                var t = new GameObject($"Spawn_{count}").transform;
                t.SetParent(parent.transform, false);
                t.localPosition = CellToWorld(cell);
                count++;
            }
        }

        // 保底：若找不到足够格子，在中心附近补
        if (count == 0)
        {
            var t = new GameObject("Spawn_0").transform;
            t.SetParent(parent.transform, false);
            t.localPosition = Vector3.zero;
        }

        var result = new Transform[parent.transform.childCount];
        for (int i = 0; i < result.Length; i++)
            result[i] = parent.transform.GetChild(i);
        return result;
    }

    // ── 把 cell 坐标转换为 local world 位置（中心在 0,0，每格 1 单位）────────

    private static Vector3 CellToWorld(Vector3Int cell)
        => new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

    // ── 创建出生点 Transform ──────────────────────────────────────────────────

    private static Transform MakeSpawnTransform(GameObject root, string n, Vector3 localPos)
    {
        var go = new GameObject(n);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    // ── 工具方法 ─────────────────────────────────────────────────────────────

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);
}
