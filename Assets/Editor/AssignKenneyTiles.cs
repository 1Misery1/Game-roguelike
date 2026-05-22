using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// 把已下载的 Kenney Tiny Dungeon tilemap_packed.png 切片后
/// 分配给 Assets/Tiles/ 下各 Tile 资源。
/// 执行：Tools > Assign Kenney Tiles
public static class AssignKenneyTiles
{
    const string TilesetPath = "Assets/Resources/Tilesets/tiny_dungeon/Tilemap/tilemap_packed.png";
    const int    COLS = 12;
    const int    ROWS = 11;
    const int    SZ   = 16;

    // Kenney Tiny Dungeon 12×11 索引表（从左到右、从上到下，0起）
    // 行 0 ( 0-11)  : 顶部墙帽/装饰
    // 行 1 (12-23)  : 墙体正面（标准地下城墙）
    // 行 2 (24-35)  : 墙体变体
    // 行 3 (36-47)  : 地板/墙过渡
    // 行 4 (48-59)  : 石板地板（主要开放格）
    // 行 5 (60-71)  : 道具 A（火炬、骨骼…）
    // 行 6 (72-83)  : 道具 B
    // 行 7 (84-95)  : 道具 C
    // 行 8 (96-107) : 特殊地形 A
    // 行 9 (108-119): 特殊地形 B
    // 行10 (120-131): 特殊地形 C

    static readonly (string asset, int idx)[] Assignments =
    {
        ("Assets/Tiles/Tile_Floor.asset",      48),  // 石板地板
        ("Assets/Tiles/Tile_Wall.asset",        1),  // 地下城墙（Floor 1）
        ("Assets/Tiles/Tile_Pillar.asset",      7),  // 支柱/柱子
        ("Assets/Tiles/Tile_Lava.asset",       96),  // 危险地形 A（Floor 1）
        ("Assets/Tiles/Tile_WallFrost.asset",  25),  // 冰霜墙（Floor 2）
        ("Assets/Tiles/Tile_WallChaos.asset",  37),  // 混沌墙（Floor 3）
        ("Assets/Tiles/Tile_Ice.asset",        60),  // 冰地板（Floor 2）
        ("Assets/Tiles/Tile_Void.asset",       84),  // 虚空地板（Floor 3）
    };

    [MenuItem("Tools/Assign Kenney Tiles")]
    public static void Execute()
    {
        // ── 1. 设置 tilemap_packed.png 的导入参数（多精灵 16×16 切片）────────
        var importer = AssetImporter.GetAtPath(TilesetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[AssignKenneyTiles] 找不到 {TilesetPath}，请先下载 tilemap_packed.png");
            return;
        }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = SZ;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        // 构建 16×16 切片元数据
        var meta = new SpriteMetaData[COLS * ROWS];
        for (int i = 0; i < COLS * ROWS; i++)
        {
            int col = i % COLS;
            int row = i / COLS;
            // Unity Rect 原点在左下角，tileset 行 0 在顶部 → 翻转 Y
            float x = col * SZ;
            float y = (ROWS - 1 - row) * SZ;
            meta[i] = new SpriteMetaData
            {
                name      = $"tile_{i}",
                rect      = new Rect(x, y, SZ, SZ),
                pivot     = new Vector2(0.5f, 0.5f),
                alignment = (int)SpriteAlignment.Center,
            };
        }
        importer.spritesheet = meta;
        importer.SaveAndReimport();

        // ── 2. 加载所有子精灵，建立名称→Sprite 字典 ─────────────────────────
        var sprites = AssetDatabase.LoadAllAssetsAtPath(TilesetPath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name);

        // ── 3. 逐个 Tile asset 更新 Sprite 引用 ─────────────────────────────
        int updated = 0;
        foreach (var (assetPath, idx) in Assignments)
        {
            string key = $"tile_{idx}";
            if (!sprites.TryGetValue(key, out var sprite))
            {
                Debug.LogWarning($"[AssignKenneyTiles] 索引 {idx} ({key}) 不存在于 tileset，跳过 {assetPath}");
                continue;
            }

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                Debug.LogWarning($"[AssignKenneyTiles] Tile 资源未找到：{assetPath}");
                continue;
            }

            var so = new SerializedObject(tile);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tile);
            updated++;
            Debug.Log($"[AssignKenneyTiles] {System.IO.Path.GetFileName(assetPath)} → tile_{idx}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AssignKenneyTiles] 完成，更新了 {updated}/{Assignments.Length} 个 Tile 资源。");
    }
}
