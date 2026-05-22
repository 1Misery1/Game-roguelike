using UnityEngine;
using UnityEditor;
using Game.Dev;

/// Editor-only: builds a 2D visual preview grid of all 9 maps (3 floors × 3 variants).
/// Each tile is a colour-coded square — see BuildLegend() for the colour key.
/// Positioned below CharacterPreview / WeaponPreview in the same scene.
public class MapPreviewBuilder
{
    // ── Layout constants ─────────────────────────────────────────────
    const float TileSize = 0.45f;   // world units per map tile
    const float MapGapX  = 1.8f;    // horizontal gap between maps
    const float MapGapY  = 2.5f;    // vertical gap between map rows
    const float YStart   = -30f;    // Y of the top-left corner of the F1 row

    static int   MapW       => MapBuilder.TileW;              // 32
    static int   MapH       => MapBuilder.TileH;              // 20
    static float MapPixelW  => MapW * TileSize;               // 14.4 wu
    static float MapPixelH  => (MapH - 1) * TileSize;         // 8.55 wu
    static float StepX      => MapPixelW + MapGapX;           // 16.2 wu per column
    static float StepY      => MapPixelH + MapGapY;           // 11.05 wu per row

    // ── Entry point ──────────────────────────────────────────────────

    public static void Execute()
    {
        var existing = GameObject.Find("MapPreview");
        if (existing != null) Object.DestroyImmediate(existing);

        _whitePixel = null;

        var root = new GameObject("MapPreview");

        float totalW = 3 * MapPixelW + 2 * MapGapX;
        float startX = -totalW * 0.5f;

        for (int fi = 0; fi < 3; fi++)
        {
            float rowY = YStart - fi * StepY;

            FloorRowLabel(root, FloorNames[fi], startX - 1.0f,
                          rowY + MapPixelH * 0.5f, FloorLabelColor(fi));

            for (int vi = 0; vi < 3; vi++)
            {
                float mapX = startX + vi * StepX;
                BuildMap(root, fi + 1, vi, mapX, rowY);
            }
        }

        BuildLegend(root, startX + 3 * StepX + 0.5f, YStart);

        Setup2DView(root, startX, totalW);
        Selection.activeGameObject = root;

        Debug.Log("[MapPreview] 9 张地图预览构建完成");
    }

    // ── Per-map builder ───────────────────────────────────────────────

    static void BuildMap(GameObject root, int floor, int variant, float ox, float oy)
    {
        int fi = floor - 1;
        string label = $"F{floor}-{VariantNames[variant]}";

        var group = new GameObject(label);
        group.transform.SetParent(root.transform, false);
        group.transform.localPosition = new Vector3(ox, oy, 0f);

        // Map label above the grid
        var labelGO = new GameObject("_Label");
        labelGO.transform.SetParent(group.transform, false);
        labelGO.transform.localPosition = new Vector3(MapPixelW * 0.5f, MapPixelH + 0.35f, 0f);
        var tm = labelGO.AddComponent<TextMesh>();
        tm.text          = label;
        tm.fontSize      = 12;
        tm.alignment     = TextAlignment.Center;
        tm.anchor        = TextAnchor.LowerCenter;
        tm.color         = FloorLabelColor(fi);
        tm.characterSize = 0.075f;

        // Tile grid
        string[] rows = MapBuilder.GetMap(floor, variant);
        for (int r = 0; r < MapH; r++)
        {
            string row = r < rows.Length ? rows[r] : new string('#', MapW);
            for (int c = 0; c < MapW; c++)
            {
                char tile = c < row.Length ? row[c] : '#';
                Color col = TileColor(tile, fi);

                var go = new GameObject();
                go.transform.SetParent(group.transform, false);
                go.transform.localPosition = new Vector3(c * TileSize, (MapH - 1 - r) * TileSize, 0f);
                go.transform.localScale    = new Vector3(TileSize * 0.92f, TileSize * 0.92f, 1f);

                var sr   = go.AddComponent<SpriteRenderer>();
                sr.sprite = WhitePixel();
                sr.color  = col;
            }
        }
    }

    // ── Row label on the left ─────────────────────────────────────────

    static void FloorRowLabel(GameObject root, string text, float x, float y, Color c)
    {
        var go = new GameObject("_FloorLabel");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.fontSize      = 14;
        tm.alignment     = TextAlignment.Right;
        tm.anchor        = TextAnchor.MiddleRight;
        tm.color         = c;
        tm.characterSize = 0.08f;
        tm.fontStyle     = FontStyle.Bold;
    }

    // ── Legend ───────────────────────────────────────────────────────

    static void BuildLegend(GameObject root, float x, float y)
    {
        var lg = new GameObject("_Legend");
        lg.transform.SetParent(root.transform, false);

        var items = new (string label, Color col)[]
        {
            ("# 墙体",  new Color(0.30f, 0.18f, 0.10f)),
            (". 地板",  new Color(0.42f, 0.26f, 0.14f)),
            ("p 石柱",  new Color(0.55f, 0.85f, 0.90f)),
            ("t 陷阱",  new Color(1.00f, 0.60f, 0.10f)),
            ("l 岩浆",  new Color(1.00f, 0.25f, 0.05f)),
            ("x 装饰",  new Color(0.75f, 0.35f, 1.00f)),
            ("d 出口",  new Color(0.20f, 0.95f, 0.40f)),
        };

        for (int i = 0; i < items.Length; i++)
        {
            var go = new GameObject($"_L{i}");
            go.transform.SetParent(lg.transform, false);
            go.transform.localPosition = new Vector3(x, y - i * 1.4f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text          = items[i].label;
            tm.fontSize      = 12;
            tm.alignment     = TextAlignment.Left;
            tm.anchor        = TextAnchor.MiddleLeft;
            tm.color         = items[i].col;
            tm.characterSize = 0.075f;
        }
    }

    // ── Scene View 2D setup ───────────────────────────────────────────

    static void Setup2DView(GameObject root, float startX, float totalW)
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        float totalH  = 3 * MapPixelH + 2 * MapGapY;
        float centerX = startX + totalW * 0.5f;
        float centerY = YStart - totalH * 0.5f + MapPixelH * 0.5f;

        sv.in2DMode     = true;
        sv.orthographic = true;
        sv.rotation     = Quaternion.identity;
        sv.pivot        = new Vector3(centerX, centerY, 0f);
        sv.size         = totalH * 0.65f;

        Selection.activeGameObject = root;
        sv.FrameSelected();
        sv.Repaint();
    }

    // ── Colour map ────────────────────────────────────────────────────

    static Color TileColor(char tile, int fi) => tile switch
    {
        '#' => WallColor(fi),
        '.' => FloorColor(fi),
        'p' => new Color(0.55f, 0.85f, 0.90f),
        't' => new Color(1.00f, 0.60f, 0.10f),
        'l' => new Color(1.00f, 0.25f, 0.05f),
        'x' => new Color(0.75f, 0.35f, 1.00f),
        'd' => new Color(0.20f, 0.95f, 0.40f),
        _   => Color.black,
    };

    static Color WallColor(int fi) => fi switch
    {
        0 => new Color(0.25f, 0.12f, 0.05f),
        1 => new Color(0.10f, 0.18f, 0.35f),
        _ => new Color(0.08f, 0.04f, 0.16f),
    };

    static Color FloorColor(int fi) => fi switch
    {
        0 => new Color(0.42f, 0.26f, 0.14f),
        1 => new Color(0.22f, 0.36f, 0.56f),
        _ => new Color(0.18f, 0.10f, 0.28f),
    };

    static Color FloorLabelColor(int fi) => fi switch
    {
        0 => new Color(1.00f, 0.55f, 0.20f),
        1 => new Color(0.55f, 0.85f, 1.00f),
        _ => new Color(0.85f, 0.50f, 1.00f),
    };

    // ── Helpers ───────────────────────────────────────────────────────

    static readonly string[] FloorNames   = { "炼狱 F1", "霜境 F2", "混沌 F3" };
    static readonly string[] VariantNames = { "A", "B", "C" };

    static Sprite _whitePixel;
    static Sprite WhitePixel()
    {
        if (_whitePixel != null) return _whitePixel;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whitePixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _whitePixel;
    }
}
