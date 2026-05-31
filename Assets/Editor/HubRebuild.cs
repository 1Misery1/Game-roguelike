using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// 一次性重建大厅的地板/墙壁网格，并放大、重新摆放所有物件。
// 不删除带有 HubController 引用的物件(台座/伴侣/石碑行/封印)，仅重摆+缩放，保持接线不丢。
public static class HubRebuild
{
    const string FloorSpritePath = "Assets/Art/Hub/Floor.png";
    const string WallSpritePath  = "Assets/Art/Hub/BackWall.png";

    static readonly Vector2 Fire = new Vector2(0f, -1.9f);

    public static void Execute()
    {
        AssetDatabase.Refresh();

        var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorSpritePath);
        var wallSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(WallSpritePath);
        if (floorSprite == null || wallSprite == null)
        { Debug.LogError("[HubRebuild] missing floor/wall sprite"); return; }

        var floor = GameObject.Find("Floor");
        var wall  = GameObject.Find("BackWall");
        if (floor == null || wall == null)
        { Debug.LogError("[HubRebuild] Floor/BackWall not found"); return; }

        floor.transform.position = Vector3.zero;
        wall.transform.position  = Vector3.zero;
        ClearChildren(floor.transform);
        ClearChildren(wall.transform);

        // ── 墙壁(后景，最底层)：清晰横向铺满上 1/3，越往上越暗 ─────────
        float wallNativeH = wallSprite.bounds.size.y;   // ~3.46
        float wallScale   = 0.62f;
        Color wallBase    = new Color(0.32f, 0.36f, 0.50f);
        for (int r = 0; r < 4; r++)
        {
            float y = 1.6f + r * 1.7f;
            float vf = Mathf.Lerp(0.95f, 0.5f, r / 3f);   // 顶部更暗
            for (int c = 0; c < 7; c++)
            {
                float x = -10.8f + c * 3.6f;
                var t = MakeSprite(wall.transform, $"Wall_{c}_{r}", wallSprite,
                                   new Vector3(x, y, 0f), wallScale, -22,
                                   new Color(wallBase.r * vf, wallBase.g * vf, wallBase.b * vf, 1f));
            }
        }

        // ── 地板(前景平面)：小砖密铺下 2/3，按到篝火距离上暖光 ────────
        float floorNative = floorSprite.bounds.size.y;   // ~4.898
        float floorScale  = 2.2f / floorNative;
        Color warm = new Color(1.0f, 0.86f, 0.62f);
        Color cold = new Color(0.34f, 0.30f, 0.44f);
        int fi = 0;
        for (int r = 0; r < 5; r++)
        {
            float y = -6.4f + r * 1.8f;
            for (int c = 0; c < 12; c++)
            {
                float x = -11f + c * 2f;
                float d = Vector2.Distance(new Vector2(x, y), Fire);
                Color col = Color.Lerp(warm, cold, Mathf.Clamp01(d / 9f));
                MakeSprite(floor.transform, $"Tile_{fi++}", floorSprite,
                           new Vector3(x, y, 0f), floorScale, -19, col);
            }
        }

        // ── 重摆 + 放大所有物件(乘原 scale，保持各自比例) ──────────────
        SetWorld("Camp", Vector3.zero, 1f);
        SetWorld("Camp/Rug",          new Vector3(0f,  -2.3f, 0f), 1.5f);
        SetWorld("Camp/FireGlow",     new Vector3(0f,  -1.9f, 0f), 1.4f);
        SetWorld("Camp/FireGlowCore", new Vector3(0f,  -1.9f, 0f), 1.4f);
        SetWorld("Camp/Campfire",     new Vector3(0f,  -1.7f, 0f), 1.6f);
        SetWorld("Camp/Dust",         new Vector3(0f,  -1.6f, 0f), 1f);
        SetWorld("Camp/Embers",       new Vector3(0f,  -1.6f, 0f), 1f);

        SetWorld("Camp/Companions",                Vector3.zero, 1f);
        SetWorld("Camp/Companions/Seat_Warrior",  new Vector3(-3.6f, -3.0f, 0f), 1.5f);
        SetWorld("Camp/Companions/Seat_Ranger",   new Vector3( 3.6f, -3.0f, 0f), 1.5f);
        SetWorld("Camp/Companions/Seat_Mage",     new Vector3(-6.0f, -1.5f, 0f), 1.5f);
        SetWorld("Camp/Companions/Seat_Paladin",  new Vector3( 6.0f, -1.5f, 0f), 1.5f);
        SetWorld("Camp/Companions/Seat_Hunter",   new Vector3( 0.0f, -4.3f, 0f), 1.5f);

        // 台座往中间收，远离两端的名册碑/升降门，避免按 E 误触
        SetWorld("Pedestals", Vector3.zero, 1f);
        SetWorld("Pedestals/Pedestal_Warrior", new Vector3(-6.0f, 2.3f, 0f), 1.35f);
        SetWorld("Pedestals/Pedestal_Ranger",  new Vector3(-3.0f, 2.3f, 0f), 1.35f);
        SetWorld("Pedestals/Pedestal_Mage",    new Vector3( 0.0f, 2.3f, 0f), 1.35f);
        SetWorld("Pedestals/Pedestal_Paladin", new Vector3( 3.0f, 2.3f, 0f), 1.35f);
        SetWorld("Pedestals/Pedestal_Hunter",  new Vector3( 6.0f, 2.3f, 0f), 1.35f);

        SetWorld("Stations", Vector3.zero, 1f);
        SetWorld("Stations/QuestBoard", new Vector3(-9.5f, -3.4f, 0f), 1.4f);
        SetWorld("Stations/Records",    new Vector3( 9.5f, -3.4f, 0f), 1.4f);
        SetWorld("Stations/Memorial",   new Vector3(-10.6f, 2.7f, 0f), 1.35f);
        SetWorld("Stations/LiftDoor",   new Vector3( 10.4f, 2.5f, 0f), 1.4f);

        SetWorld("HubPlayer", new Vector3(0f, -5.2f, 0f), 1.5f);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HubRebuild] done: floor 12x5, wall 7x4, props rescaled/replaced.");
    }

    static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    static Transform MakeSprite(Transform parent, string name, Sprite sprite,
                                Vector3 pos, float scale, int order, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = col;
        sr.sortingOrder = order;
        return go.transform;
    }

    // 设世界位置；mul>1 时把当前 localScale 整体乘 mul(保持子物体相对布局与各自比例)。
    static void SetWorld(string path, Vector3 worldPos, float mul)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning($"[HubRebuild] not found: {path}"); return; }
        go.transform.position = worldPos;
        if (!Mathf.Approximately(mul, 1f))
        {
            var s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x * mul, s.y * mul, s.z);
        }
    }
}
