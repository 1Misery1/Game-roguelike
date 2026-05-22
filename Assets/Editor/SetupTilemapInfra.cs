using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// Editor utility: builds the Tilemap layer hierarchy in Test.unity and
/// generates colour-blocked Tile assets in Assets/Tiles/.
public class SetupTilemapInfra
{
    private const string TileDir = "Assets/Tiles";

    public static void Execute()
    {
        // ── 1. 确保 Tiles 目录存在 ──────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(TileDir))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        // ── 2. 生成基础 Tile 资源（纯色像素块）──────────────────────────
        CreateTile("Tile_Floor",      new Color(0.18f, 0.16f, 0.14f));   // 深石板地板
        CreateTile("Tile_Wall",       new Color(0.32f, 0.27f, 0.22f));   // 暗岩墙壁
        CreateTile("Tile_Pillar",     new Color(0.42f, 0.36f, 0.28f));   // 石柱
        CreateTile("Tile_Lava",       new Color(0.85f, 0.30f, 0.05f));   // 岩浆（第1层）
        CreateTile("Tile_Ice",        new Color(0.55f, 0.78f, 0.95f));   // 寒冰（第2层）
        CreateTile("Tile_Void",       new Color(0.22f, 0.05f, 0.30f));   // 虚空（第3层）
        CreateTile("Tile_WallFrost",  new Color(0.15f, 0.25f, 0.42f));   // 冰岩墙（第2层）
        CreateTile("Tile_WallChaos",  new Color(0.28f, 0.10f, 0.40f));   // 混沌岩墙（第3层）

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 3. 打开 Test 场景，插入 TilemapRoot 层级 ────────────────────
        string scenePath = "Assets/Scenes/Test.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        // 避免重复创建
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "TilemapRoot")
            {
                Debug.Log("[SetupTilemapInfra] TilemapRoot already exists in Test.unity — skipping.");
                EditorSceneManager.CloseScene(scene, false);
                return;
            }
        }

        // Grid 根节点
        var gridGO = new GameObject("TilemapRoot");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridGO, scene);
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // 地板层（sorting order 0）
        AddTilemapLayer(gridGO, "Floor",       0, false);
        // 墙壁层（sorting order 1，带碰撞）
        AddTilemapLayer(gridGO, "Walls",       1, true);
        // 装饰层（sorting order 2）
        AddTilemapLayer(gridGO, "Decorations", 2, false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, false);

        Debug.Log("[SetupTilemapInfra] TilemapRoot created in Test.unity. Tile assets saved to Assets/Tiles/.");
    }

    // ── 辅助：添加一个 Tilemap 子层 ──────────────────────────────────────

    private static void AddTilemapLayer(GameObject parent, string layerName, int sortOrder, bool withCollider)
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

            // 墙壁使用 Unity 物理层 9（与游戏代码中的 wall layer 对应）
            go.layer = 9;
        }
    }

    // ── 辅助：创建纯色 Tile 资源 ─────────────────────────────────────────

    private static void CreateTile(string name, Color color)
    {
        string path = $"{TileDir}/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(path) != null)
            return; // 已存在则跳过

        // 16×16 纯色像素贴图
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp,
        };
        var pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        string texPath = $"{TileDir}/{name}_tex.png";
        System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType         = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode          = FilterMode.Point;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        var tile   = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        tile.sprite = sprite;
        tile.color  = Color.white;

        AssetDatabase.CreateAsset(tile, path);
    }
}
