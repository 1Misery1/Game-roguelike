using UnityEngine;
using UnityEditor;
using Game.Dev;
using Game.AI;

/// Builds a flat 2D preview grid of all 20 characters.
/// All procedural sprites are 32×32 px, PPU=16 → 2 world-units square at scale 1.
/// Row spacing is set to 10 units so rows never overlap even at max scale.
public class CharacterPreviewBuilder
{
    // All characters rendered at the same base scale → uniform size in preview.
    const float Scale       = 3.5f;   // 3.5 × 2 wu = 7 wu per sprite
    const float ColSpacing  = 4.0f;   // horizontal gap between sprites
    const float RowSpacing  = 10.0f;  // vertical gap (> Scale×2 = 7, so no overlap)

    public static void Execute()
    {
        // ── 1. Clear existing preview ─────────────────────────────────
        var existing = GameObject.Find("CharacterPreview");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("CharacterPreview");

        // ── 2. Build rows (bottom = row 0 Boss, top = row 3 Hero) ─────
        BuildHeroRow(root, 3);

        BuildEnemyRow(root, "小怪 Small", 2,
            new (string, EnemyType)[]
            {
                ("Skeleton",       EnemyType.Skeleton),
                ("Soldier",        EnemyType.Soldier),
                ("Archer",         EnemyType.Archer),
                ("Bat",            EnemyType.Bat),
                ("ShieldGuard",    EnemyType.ShieldGuard),
                ("PoisonSpider",   EnemyType.PoisonSpider),
                ("ShadowAssassin", EnemyType.ShadowAssassin),
                ("ExplosiveDemon", EnemyType.ExplosiveDemon),
            },
            new Color(1f, 0.6f, 0.6f));

        BuildEnemyRow(root, "精英 Elite", 1,
            new (string, EnemyType)[]
            {
                ("Commander",    EnemyType.Commander),
                ("Witch",        EnemyType.Witch),
                ("PoisonShaman", EnemyType.PoisonShaman),
                ("Necromancer",  EnemyType.Necromancer),
            },
            new Color(1f, 0.85f, 0.25f));

        BuildEnemyRow(root, "Boss", 0,
            new (string, EnemyType)[]
            {
                ("HellGiant", EnemyType.HellGiant),
                ("FrostLich",  EnemyType.FrostLich),
                ("ChaosLord",  EnemyType.ChaosLord),
            },
            new Color(1f, 0.4f, 0.4f));

        // ── 3. Force Scene View to true 2D orthographic ───────────────
        Setup2DView(root);

        Debug.Log("[CharacterPreview] 20 个角色预览构建完成（2D 正交视图）");
    }

    // ── Hero row ──────────────────────────────────────────────────────

    static void BuildHeroRow(GameObject root, int row)
    {
        string[] names = { "Warrior", "Ranger", "Mage", "Paladin", "Hunter" };
        var tint = new Color(0.45f, 0.85f, 1f);

        var group = MakeGroup(root, "英雄 Heroes");
        float startX = CenterX(names.Length);
        float y = row * RowSpacing;

        RowLabel(group, "英雄", startX, y, tint);
        for (int i = 0; i < names.Length; i++)
        {
            var go = MakeCharGO(group, names[i],
                new Vector3(startX + i * ColSpacing, y, 0f));
            go.AddComponent<SpriteRenderer>().sprite = HeroSprites.Get(names[i]);
            NameLabel(go, names[i], tint);
        }
    }

    // ── Enemy row ─────────────────────────────────────────────────────

    static void BuildEnemyRow(
        GameObject root, string rowLabel, int row,
        (string name, EnemyType type)[] entries, Color tint)
    {
        var group   = MakeGroup(root, rowLabel);
        float startX = CenterX(entries.Length);
        float y = row * RowSpacing;

        RowLabel(group, rowLabel, startX, y, tint);
        for (int i = 0; i < entries.Length; i++)
        {
            var (name, type) = entries[i];
            var go = MakeCharGO(group, name,
                new Vector3(startX + i * ColSpacing, y, 0f));
            go.AddComponent<SpriteRenderer>().sprite = EnemySprites.Get(type);
            NameLabel(go, name, tint);
        }
    }

    // ── Scene View 2D setup ───────────────────────────────────────────

    static void Setup2DView(GameObject root)
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        // Compute center of all characters
        float maxRow = 3, cols = 8; // widest row = Small (8 chars)
        float centerX = 0f;
        float centerY = (maxRow * RowSpacing) * 0.5f;

        sv.in2DMode     = true;
        sv.orthographic = true;
        sv.rotation     = Quaternion.identity;   // look along -Z, Y=up
        sv.pivot        = new Vector3(centerX, centerY, 0f);
        sv.size         = maxRow * RowSpacing * 0.75f;  // orthographic half-height

        Selection.activeGameObject = root;
        sv.FrameSelected();
        sv.Repaint();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    static float CenterX(int count) => -(count - 1) * ColSpacing * 0.5f;

    static GameObject MakeGroup(GameObject parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        return g;
    }

    static GameObject MakeCharGO(GameObject parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = Vector3.one * Scale;
        return go;
    }

    static void RowLabel(GameObject parent, string text, float startX, float y, Color tint)
    {
        var go = new GameObject("_Label");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(startX - 3.2f, y, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.fontSize      = 20;
        tm.fontStyle     = FontStyle.Bold;
        tm.alignment     = TextAlignment.Right;
        tm.anchor        = TextAnchor.MiddleRight;
        tm.color         = tint;
        tm.characterSize = 0.13f;
    }

    static void NameLabel(GameObject parent, string name, Color tint)
    {
        var go = new GameObject("_Name");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0f, -0.72f, 0f);
        go.transform.localScale    = Vector3.one / Scale;
        var tm = go.AddComponent<TextMesh>();
        tm.text          = name;
        tm.fontSize      = 13;
        tm.alignment     = TextAlignment.Center;
        tm.anchor        = TextAnchor.UpperCenter;
        tm.color         = tint;
        tm.characterSize = 0.1f;
    }
}
