using System.IO;
using Game.Dev;
using UnityEditor;
using UnityEngine;

public static class CreateBossStatsDefault
{
    public static void Execute()
    {
        const string dir = "Assets/Resources/Bosses";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // Floor 1 — Hell Giant (原代码：hp 320, atk 28, def 8, spd 2.5, scale 1.2)
        Make(dir + "/HellGiant.asset", "hell_giant", "Hell Giant",
             hp: 320f, atk: 28f, def: 8f, spd: 2.5f,
             scale: 1.2f, color: new Color(0.70f, 0.12f, 0.08f, 1f));

        // Floor 2 — Frost Lich (原代码：hp 480, atk 20, def 5, spd 1.8, scale 1.1)
        Make(dir + "/FrostLich.asset", "frost_lich", "Frost Lich",
             hp: 480f, atk: 20f, def: 5f, spd: 1.8f,
             scale: 1.1f, color: new Color(0.45f, 0.75f, 1.00f, 1f));

        // Floor 3 — Chaos Lord (原代码：hp 700, atk 35, def 12, spd 3.0, scale 1.4)
        Make(dir + "/ChaosLord.asset", "chaos_lord", "Chaos Lord",
             hp: 700f, atk: 35f, def: 12f, spd: 3.0f,
             scale: 1.4f, color: new Color(0.50f, 0.10f, 0.70f, 1f));

        // 隐藏 Boss — 王国之罪 (原 ChaosLord × 2.5 → 直接固化 hp/atk；scale=ChaosLord×1.6=2.24)
        Make(dir + "/KingdomGuilt.asset", "kingdom_guilt", "王国之罪",
             hp: 1750f, atk: 88f, def: 18f, spd: 3.0f,
             scale: 2.24f, color: new Color(0.92f, 0.78f, 0.30f, 1f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BossStats] Created 4 boss stats assets under Resources/Bosses");
    }

    static void Make(string path, string id, string display,
                     float hp, float atk, float def, float spd,
                     float scale, Color color)
    {
        var existing = AssetDatabase.LoadAssetAtPath<BossStatsData>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var d = ScriptableObject.CreateInstance<BossStatsData>();
        d.bossId      = id;
        d.displayName = display;
        d.maxHp       = hp;
        d.attack      = atk;
        d.defense     = def;
        d.moveSpeed   = spd;
        d.visualScale = scale;
        d.tintColor   = color;
        AssetDatabase.CreateAsset(d, path);
    }
}
