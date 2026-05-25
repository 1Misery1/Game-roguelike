using System.IO;
using Game.Dev;
using UnityEditor;
using UnityEngine;

/// 生成 Assets/Resources/Floors/Floor{1,2,3}_*.asset —— 与原 switch 行为 1:1 对齐
public static class CreateFloorThemesDefault
{
    public static void Execute()
    {
        const string dir = "Assets/Resources/Floors";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Make(dir + "/Floor1_Inferno.asset",
             floor: 1,
             name: "Inferno",
             banner: "[Inferno] Lava surges, demons and iron-clad guards block the path — watch for explosions and magma!",
             camBg: new Color(0.15f, 0.05f, 0.03f, 1f),
             kind:  FloorProceduralKind.Inferno,
             early: new float[] { 2f, 3f, 2f, 2f, 1f, 1f, 0f, 1f },
             late:  new float[] { 1f, 2f, 1f, 1f, 3f, 1f, 1f, 4f },
             elite: 0.15f,
             pool:  new[] { ("Monster", 6.0f), ("Coin", 2.5f), ("Talent", 1.0f), ("HellTrial",  1.5f) });

        Make(dir + "/Floor2_FrostRealm.asset",
             floor: 2,
             name: "Frost Realm",
             banner: "[Frost Realm] Bone-chilling cold, undead rise again, elite spawn rate greatly increased!",
             camBg: new Color(0.03f, 0.06f, 0.14f, 1f),
             kind:  FloorProceduralKind.Frost,
             early: new float[] { 4f, 1f, 3f, 3f, 0f, 0f, 0f, 0f },
             late:  new float[] { 4f, 1f, 3f, 3f, 1f, 1f, 1f, 1f },
             elite: 0.30f,
             pool:  new[] { ("Monster", 4.0f), ("Coin", 1.5f), ("Talent", 3.0f), ("FrostGrave", 2.0f) });

        Make(dir + "/Floor3_ChaosAbyss.asset",
             floor: 3,
             name: "Chaos Abyss",
             banner: "[Chaos Abyss] The void shatters, elites run rampant — the final battle awaits, the Chaos Lord is waiting!",
             camBg: new Color(0.07f, 0.03f, 0.11f, 1f),
             kind:  FloorProceduralKind.Chaos,
             early: new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 2f, 1f },
             late:  new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 4f, 2f },
             elite: 0.55f,
             pool:  new[] { ("Monster", 7.0f), ("Coin", 1.0f), ("Talent", 2.0f), ("ChaosRift",  2.5f) });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FloorTheme] Created 3 floor theme assets under Resources/Floors");
    }

    static void Make(string path, int floor, string name, string banner, Color camBg,
                     FloorProceduralKind kind,
                     float[] early, float[] late, float elite,
                     (string type, float weight)[] pool)
    {
        var existing = AssetDatabase.LoadAssetAtPath<FloorThemeData>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var d = ScriptableObject.CreateInstance<FloorThemeData>();
        d.floorNumber        = floor;
        d.displayName        = name;
        d.narrativeBanner    = banner;
        d.cameraBackground   = camBg;
        d.proceduralKind     = kind;
        d.earlyEnemyWeights  = early;
        d.lateEnemyWeights   = late;
        d.eliteChance        = elite;
        d.roomPool           = new System.Collections.Generic.List<FloorRoomEntry>();
        foreach (var e in pool)
            d.roomPool.Add(new FloorRoomEntry { type = e.type, weight = e.weight });
        AssetDatabase.CreateAsset(d, path);
    }
}
