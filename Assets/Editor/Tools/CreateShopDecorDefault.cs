using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Data;
/// 生成 Assets/Resources/Shop/Default.asset —— 与原程序化样式 1:1 对齐
public static class CreateShopDecorDefault
{
    public static void Execute()
    {
        const string dir  = "Assets/Resources/Shop";
        const string path = dir + "/Default.asset";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var existing = AssetDatabase.LoadAssetAtPath<ShopDecorData>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var data = ScriptableObject.CreateInstance<ShopDecorData>();

        // ── 货架（位于 y=1.0，宽 13.5）──────────────────────────────────
        data.shelf = new ShopDecorGroup {
            name    = "ShopShelf",
            anchor  = new Vector3(0f, 1.0f, 0f),
            enabled = true,
            parts   = new List<ShopPart> {
                Part("板-主体",   new Vector2(0f,  0f),    new Vector2(13.5f, 0.45f), Hex(0x5C381F), 4),
                Part("板-高光",   new Vector2(0f,  0.20f), new Vector2(13.5f, 0.06f), Hex(0x9F6B38), 5),
                Part("板-阴影",   new Vector2(0f, -0.21f), new Vector2(13.5f, 0.05f), Hex(0x2E1A0D), 5),
                Part("立柱-左",   new Vector2(-4.86f, -0.55f), new Vector2(0.20f, 1.0f), Hex(0x472B14), 4),
                Part("立柱-中",   new Vector2( 0f,    -0.55f), new Vector2(0.20f, 1.0f), Hex(0x472B14), 4),
                Part("立柱-右",   new Vector2( 4.86f, -0.55f), new Vector2(0.20f, 1.0f), Hex(0x472B14), 4),
            }
        };

        // ── 商店老板（位于 y=3.2，居中）─────────────────────────────────
        data.shopkeeper = new ShopDecorGroup {
            name    = "Shopkeeper",
            anchor  = new Vector3(0f, 3.2f, 0f),
            enabled = true,
            parts   = new List<ShopPart> {
                Part("兜帽",     new Vector2( 0f,    0.55f), new Vector2(1.05f, 0.85f), new Color(0.22f, 0.21f, 0.28f), 6),
                Part("脸",       new Vector2( 0f,    0.42f), new Vector2(0.55f, 0.45f), new Color(0.95f, 0.78f, 0.65f), 7),
                Part("眼-左",    new Vector2(-0.13f, 0.46f), new Vector2(0.08f, 0.10f), new Color(0.10f, 0.07f, 0.05f), 8),
                Part("眼-右",    new Vector2( 0.13f, 0.46f), new Vector2(0.08f, 0.10f), new Color(0.10f, 0.07f, 0.05f), 8),
                Part("大胡子",   new Vector2( 0f,    0.27f), new Vector2(0.55f, 0.20f), new Color(0.78f, 0.75f, 0.72f), 8),
                Part("长袍",     new Vector2( 0f,   -0.30f), new Vector2(1.15f, 1.10f), new Color(0.50f, 0.18f, 0.22f), 6),
                Part("腰带",     new Vector2( 0f,   -0.18f), new Vector2(1.15f, 0.10f), new Color(0.92f, 0.78f, 0.30f), 7),
                Part("招牌-框", new Vector2( 0f,    1.25f), new Vector2(1.50f, 0.32f), new Color(0.10f, 0.08f, 0.12f), 6),
                Part("招牌-灯", new Vector2( 0f,    1.25f), new Vector2(1.42f, 0.22f), new Color(0.95f, 0.78f, 0.30f), 7),
            }
        };

        data.weaponRowStart         = new Vector3(-5.5f, 1.5f, 0f);
        data.weaponSpacing          = 2.2f;
        data.weaponScale            = new Vector3(1.1f, 1.1f, 1f);
        data.weaponSortingOrder     = 8;
        data.weaponGlowSortingOrder = 7;
        data.weaponGlowLocalPos     = new Vector2(0f, -0.08f);
        data.weaponGlowScale        = new Vector2(0.95f, 0.30f);
        data.weaponGlowAlpha        = 0.55f;
        data.weaponColliderRadius   = 0.85f;

        // ── 额外陈设：2 灯笼 + 2 酒桶堆 + 1 挂画 ────────────────────────
        data.extraDecor = new List<ShopDecorGroup> {
            MakeLantern("Lantern-L", new Vector3(-7.0f, 2.8f, 0f)),
            MakeLantern("Lantern-R", new Vector3( 7.0f, 2.8f, 0f)),
            MakeBarrelStack("BarrelStack-L", new Vector3(-7.0f, -0.6f, 0f)),
            MakeBarrelStack("BarrelStack-R", new Vector3( 7.0f, -0.6f, 0f)),
            MakeFramedPainting("Painting", new Vector3(-3.0f, 4.2f, 0f)),
        };

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ShopDecor] Created " + path);
    }

    static ShopPart Part(string note, Vector2 pos, Vector2 size, Color color, int order) =>
        new ShopPart { note = note, localPos = pos, size = size, color = color, sortingOrder = order };

    // ── 复合陈设工厂 ──────────────────────────────────────────────────

    /// 一盏挂吊式灯笼：上方一根麻绳 + 红灯笼 + 黄色亮区 + 底盖
    static ShopDecorGroup MakeLantern(string name, Vector3 anchor) =>
        new ShopDecorGroup {
            name    = name,
            anchor  = anchor,
            enabled = true,
            parts   = new List<ShopPart> {
                Part("吊绳",   new Vector2( 0f,  0.65f), new Vector2(0.08f, 0.55f), Hex(0x3D2818), 5),
                Part("灯顶盖", new Vector2( 0f,  0.36f), new Vector2(0.80f, 0.08f), Hex(0x2A1808), 6),
                Part("灯笼壳", new Vector2( 0f,  0f),    new Vector2(0.70f, 0.70f), Hex(0xA31D1D), 6),
                Part("光晕",   new Vector2( 0f,  0f),    new Vector2(0.50f, 0.50f), Hex(0xF6CC4B), 7),
                Part("十字-竖",new Vector2( 0f,  0f),    new Vector2(0.06f, 0.50f), Hex(0x4B0E0E), 8),
                Part("十字-横",new Vector2( 0f,  0f),    new Vector2(0.50f, 0.06f), Hex(0x4B0E0E), 8),
                Part("灯底盖", new Vector2( 0f, -0.36f), new Vector2(0.80f, 0.08f), Hex(0x2A1808), 6),
                Part("流苏",   new Vector2( 0f, -0.50f), new Vector2(0.08f, 0.15f), Hex(0xC78A2A), 6),
            }
        };

    /// 两层堆叠的酒桶
    static ShopDecorGroup MakeBarrelStack(string name, Vector3 anchor) =>
        new ShopDecorGroup {
            name    = name,
            anchor  = anchor,
            enabled = true,
            parts   = new List<ShopPart> {
                // 下桶
                Part("下桶身",  new Vector2(0f, -0.35f), new Vector2(0.95f, 0.85f), Hex(0x6B3F1E), 4),
                Part("下桶箍上",new Vector2(0f, -0.10f), new Vector2(1.00f, 0.10f), Hex(0x2A1408), 5),
                Part("下桶箍下",new Vector2(0f, -0.60f), new Vector2(1.00f, 0.10f), Hex(0x2A1408), 5),
                Part("下桶顶", new Vector2(0f,  0.03f), new Vector2(0.85f, 0.18f), Hex(0x8B5A30), 5),
                // 上桶（稍小）
                Part("上桶身",  new Vector2(0f,  0.55f), new Vector2(0.80f, 0.70f), Hex(0x6B3F1E), 5),
                Part("上桶箍上",new Vector2(0f,  0.78f), new Vector2(0.85f, 0.08f), Hex(0x2A1408), 6),
                Part("上桶箍下",new Vector2(0f,  0.32f), new Vector2(0.85f, 0.08f), Hex(0x2A1408), 6),
                Part("上桶顶", new Vector2(0f,  0.85f), new Vector2(0.72f, 0.15f), Hex(0x8B5A30), 6),
                Part("上桶塞", new Vector2(0f,  0.85f), new Vector2(0.18f, 0.10f), Hex(0x3D2818), 7),
            }
        };

    /// 一幅小挂画（深色木框 + 油画风内画 + 钉子）
    static ShopDecorGroup MakeFramedPainting(string name, Vector3 anchor) =>
        new ShopDecorGroup {
            name    = name,
            anchor  = anchor,
            enabled = true,
            parts   = new List<ShopPart> {
                Part("钉子",   new Vector2( 0f,  0.42f), new Vector2(0.10f, 0.10f), Hex(0x8C8C92), 6),
                Part("画框",   new Vector2( 0f,  0f),    new Vector2(1.10f, 0.78f), Hex(0x3D2818), 5),
                Part("画-底", new Vector2( 0f,  0f),    new Vector2(0.95f, 0.65f), Hex(0x2C3F5C), 6),
                Part("画-山", new Vector2(-0.18f,-0.08f),new Vector2(0.42f, 0.22f), Hex(0x6B6B6B), 7),
                Part("画-山亮",new Vector2( 0.20f,-0.10f),new Vector2(0.32f, 0.18f), Hex(0x9E9E9E), 7),
                Part("画-月", new Vector2( 0.22f, 0.16f),new Vector2(0.15f, 0.15f), Hex(0xF1E6A8), 8),
            }
        };

    /// 0xRRGGBB 转 Color
    static Color Hex(int rgb)
    {
        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >>  8) & 0xFF) / 255f;
        float b = ( rgb        & 0xFF) / 255f;
        return new Color(r, g, b);
    }
}
