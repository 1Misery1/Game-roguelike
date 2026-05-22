using UnityEngine;
using UnityEditor;

/// Editor-only: builds a 2D visual preview grid of all 26 weapons,
/// grouped by category (rows) and coloured by rarity.
/// Places WeaponPreview at X+45 so it sits beside CharacterPreview in the same scene.
public class WeaponPreviewBuilder
{
    // ── Layout constants ─────────────────────────────────────────────
    const float ColSpacing = 4.0f;
    const float RowSpacing = 10.0f;
    const float Scale      = 3.5f;     // 32px PPU=16 → 2wu × 3.5 = 7wu per sprite
    const float XOffset    = 45f;      // shift right of CharacterPreview

    // ── Rarity colours ───────────────────────────────────────────────
    static readonly Color White  = new Color(0.88f, 0.88f, 0.90f);
    static readonly Color Green  = new Color(0.30f, 0.90f, 0.35f);
    static readonly Color Blue   = new Color(0.35f, 0.65f, 1.00f);
    static readonly Color Purple = new Color(0.85f, 0.40f, 1.00f);

    // ── Weapon catalogue: (display name, asset file name, rarity colour) ─
    static readonly (string name, string file, Color rarity)[] Daggers =
    {
        ("Iron Dagger",   "IronDagger",   White),
        ("Steel Dagger",  "SteelDagger",  Green),
        ("Venom Fang",    "VenomFang",    Blue),
        ("Phantom Blade", "PhantomBlade", Purple),
    };

    static readonly (string name, string file, Color rarity)[] Longswords =
    {
        ("Iron Sword",     "IronSword",     White),
        ("Knight Sword",   "KnightSword",   Green),
        ("Crescent Blade", "CrescentBlade", Green),
        ("Holy Sword",     "HolySword",     Blue),
        ("Frost Lance",    "FrostLance",    Blue),
        ("Dragon Sword",   "DragonSword",   Purple),
    };

    static readonly (string name, string file, Color rarity)[] Greatswords =
    {
        ("Iron Greatsword",    "IronGreatsword",    White),
        ("Iron Mallet",        "IronMallet",        White),
        ("Warrior Greatsword", "WarriorGreatsword", Green),
        ("Armor Breaker",      "ArmorBreaker",      Blue),
        ("Doom Blade",         "DoomBlade",         Purple),
    };

    static readonly (string name, string file, Color rarity)[] Bows =
    {
        ("Wooden Bow",    "WoodenBow",    White),
        ("Bone Bow",      "BoneBow",      White),
        ("Hunter Bow",    "HunterBow",    Green),
        ("Elf Bow",       "ElfBow",       Green),
        ("Cloud Piercer", "CloudPiercer", Blue),
        ("Thunder Bow",   "ThunderBow",   Blue),
        ("Celestial Bow", "CelestialBow", Purple),
    };

    static readonly (string name, string file, Color rarity)[] Staves =
    {
        ("Wood Staff",  "WoodStaff",  White),
        ("Magic Staff", "MagicStaff", Green),
        ("Frost Staff", "FrostStaff", Blue),
        ("Chaos Wand",  "ChaosWand",  Purple),
    };

    // ── Entry point ──────────────────────────────────────────────────

    public static void Execute()
    {
        var existing = GameObject.Find("WeaponPreview");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("WeaponPreview");

        // Rows: bottom = row 0 (Staves), top = row 4 (Daggers)
        BuildRow(root, "匕首 Dagger",    4, "Daggers",    Daggers);
        BuildRow(root, "长剑 Longsword", 3, "Longswords", Longswords);
        BuildRow(root, "大剑 Greatsword",2, "Greatswords",Greatswords);
        BuildRow(root, "弓  Bow",        1, "Bows",       Bows);
        BuildRow(root, "法杖 Staff",     0, "Staves",     Staves);

        // Rarity legend
        BuildLegend(root);

        Selection.activeGameObject = root;
        Setup2DView(root);

        Debug.Log("[WeaponPreview] 26 把武器预览构建完成");
    }

    // ── Row builder ──────────────────────────────────────────────────

    static void BuildRow(
        GameObject root, string rowLabel, int rowIndex,
        string folder, (string name, string file, Color rarity)[] weapons)
    {
        var group  = new GameObject(rowLabel);
        group.transform.SetParent(root.transform, false);

        float startX = XOffset + CenterX(weapons.Length);
        float y      = rowIndex * RowSpacing;

        // Row label at left edge
        RowLabel(group, rowLabel, startX, y, new Color(0.75f, 0.75f, 0.82f));

        for (int i = 0; i < weapons.Length; i++)
        {
            var (displayName, fileName, rarityColor) = weapons[i];
            string assetPath = $"Assets/Resources/Weapons/{folder}/{fileName}.png";

            var go = new GameObject(displayName);
            go.transform.SetParent(group.transform, false);
            go.transform.localPosition = new Vector3(startX + i * ColSpacing, y, 0f);
            go.transform.localScale    = Vector3.one * Scale;

            var sr  = go.AddComponent<SpriteRenderer>();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                sr.sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    tex.width / 2f);   // PPU = half width → 2 wu square
            }
            else
            {
                Debug.LogWarning($"[WeaponPreview] 未找到: {assetPath}");
            }

            // Rarity-coloured name label below sprite
            NameLabel(go, displayName, rarityColor);

            // Rarity indicator strip above sprite
            RarityStrip(go, rarityColor);
        }
    }

    // ── Legend (bottom-right corner) ─────────────────────────────────

    static void BuildLegend(GameObject root)
    {
        float lx = XOffset + CenterX(Bows.Length) + Bows.Length * ColSpacing + 1.5f;
        float ly = -2f;

        var lg = new GameObject("_Legend");
        lg.transform.SetParent(root.transform, false);

        var labels = new (string text, Color c)[]
        {
            ("◆ 白  White",  White),
            ("◆ 绿  Green",  Green),
            ("◆ 蓝  Blue",   Blue),
            ("◆ 紫  Purple", Purple),
        };

        for (int i = 0; i < labels.Length; i++)
        {
            var go = new GameObject($"_L{i}");
            go.transform.SetParent(lg.transform, false);
            go.transform.localPosition = new Vector3(lx, ly + i * 1.5f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text          = labels[i].text;
            tm.fontSize      = 14;
            tm.alignment     = TextAlignment.Left;
            tm.anchor        = TextAnchor.MiddleLeft;
            tm.color         = labels[i].c;
            tm.characterSize = 0.10f;
        }
    }

    // ── Scene View 2D setup ───────────────────────────────────────────

    static void Setup2DView(GameObject root)
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        float centerX = XOffset;
        float centerY = 2 * RowSpacing;   // middle of 5 rows

        sv.in2DMode     = true;
        sv.orthographic = true;
        sv.rotation     = Quaternion.identity;
        sv.pivot        = new Vector3(centerX, centerY, 0f);
        sv.size         = 28f;

        Selection.activeGameObject = root;
        sv.FrameSelected();
        sv.Repaint();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    static float CenterX(int count) => -(count - 1) * ColSpacing * 0.5f;

    static void RowLabel(GameObject parent, string text, float startX, float y, Color c)
    {
        var go = new GameObject("_Label");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(startX - 3.5f, y, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.fontSize      = 16;
        tm.fontStyle     = FontStyle.Bold;
        tm.alignment     = TextAlignment.Right;
        tm.anchor        = TextAnchor.MiddleRight;
        tm.color         = c;
        tm.characterSize = 0.12f;
    }

    static void NameLabel(GameObject parent, string name, Color rarityColor)
    {
        var go = new GameObject("_Name");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0f, -0.72f, 0f);
        go.transform.localScale    = Vector3.one / Scale;
        var tm = go.AddComponent<TextMesh>();
        tm.text          = name;
        tm.fontSize      = 11;
        tm.alignment     = TextAlignment.Center;
        tm.anchor        = TextAnchor.UpperCenter;
        tm.color         = rarityColor;
        tm.characterSize = 0.09f;
        tm.fontStyle     = FontStyle.Bold;
    }

    static void RarityStrip(GameObject parent, Color rarityColor)
    {
        // Thin colored strip above the sprite to show rarity at a glance
        var go = new GameObject("_RarityStrip");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(0f, 0.62f, 0f);
        go.transform.localScale    = new Vector3(0.9f / Scale, 0.04f / Scale, 1f);
        var sr  = go.AddComponent<SpriteRenderer>();
        sr.color = rarityColor;
        // Reuse a 1×1 white pixel as the strip texture
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
