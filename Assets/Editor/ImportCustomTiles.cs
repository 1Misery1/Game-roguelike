using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// 将 Assets/Tiles/ 下的自定义贴图导入为 Sprite 并分配给对应 Tile 资源。
/// 执行：Tools > Import Custom Tiles
public static class ImportCustomTiles
{
    private const string TileDir = "Assets/Tiles";

    // PNG文件名 → Tile资源路径
    private static readonly (string png, string tileAsset)[] Map =
    {
        ("熔岩地板.png",   "Tile_Floor.asset"),      // Floor 1 地板
        ("冰面地板.png",   "Tile_Floor2.asset"),     // Floor 2 地板（新建）
        ("虚空地板.png",   "Tile_Floor3.asset"),     // Floor 3 地板（新建）
        ("熔岩墙.png",     "Tile_Wall.asset"),       // Floor 1 墙
        ("冰霜墙.png",     "Tile_WallFrost.asset"),  // Floor 2 墙
        ("混沌墙.png",     "Tile_WallChaos.asset"),  // Floor 3 墙
        ("石柱.png",       "Tile_Pillar.asset"),     // 石柱（全层）
        ("熔岩危险格.png", "Tile_Lava.asset"),       // Floor 1 危险格
        ("冰面危险格.png", "Tile_Ice.asset"),        // Floor 2 危险格
        ("虚空危险格.png", "Tile_Void.asset"),       // Floor 3 危险格
    };

    [MenuItem("Tools/Import Custom Tiles")]
    public static void Execute()
    {
        int ok = 0;

        foreach (var (png, tileFile) in Map)
        {
            string texPath  = $"{TileDir}/{png}";
            string tilePath = $"{TileDir}/{tileFile}";

            // ── 1. 设置纹理导入参数 ────────────────────────────────────────────
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[ImportCustomTiles] 找不到文件：{texPath}");
                continue;
            }

            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode       = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;

            // PPU = 纹理宽度，使精灵恰好填满 1×1 Tilemap 格
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            importer.spritePixelsPerUnit = Mathf.Max(w, 1);

            importer.SaveAndReimport();

            // ── 2. 加载精灵 ────────────────────────────────────────────────────
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[ImportCustomTiles] 无法加载精灵：{texPath}");
                continue;
            }

            // ── 3. 确保 Tile 资源存在（不存在则新建）──────────────────────────
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = System.IO.Path.GetFileNameWithoutExtension(tileFile);
                AssetDatabase.CreateAsset(tile, tilePath);
                Debug.Log($"[ImportCustomTiles] 新建 Tile：{tilePath}");
            }

            // ── 4. 分配精灵 ────────────────────────────────────────────────────
            var so = new SerializedObject(tile);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tile);
            ok++;
            Debug.Log($"[ImportCustomTiles] {tileFile} ← {png}  ({w}×{h}px, PPU={w})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ImportCustomTiles] 完成，共更新 {ok}/{Map.Length} 个 Tile 资源。");
    }
}
