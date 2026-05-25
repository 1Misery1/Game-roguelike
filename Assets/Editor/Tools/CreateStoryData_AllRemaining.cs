using System.Collections.Generic;
using System.IO;
using Game.Narrative;
using UnityEditor;
using UnityEngine;

/// 一次性生成「封死的升降门」之外的全部 9 个剧情交互物 .asset。
/// MCP 调用：execute_script("Assets/Editor/Tools/CreateStoryData_AllRemaining.cs", "Execute")
public static class CreateStoryData_AllRemaining
{
    public static void Execute()
    {
        const string dir = "Assets/Resources/Story";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Create_F1_FurnaceConsole();
        Create_F1_ArtisanCorpse();
        Create_F2_ScoutCamp();
        Create_F2_FrozenLake();
        Create_F2_FrostAltar();
        Create_F3_Observatory();
        Create_F3_PreyCorridor();
        Create_F3_BlackMirror();
        Create_F3_BrokenThrone();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Story] Created 9 story interactable assets.");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    static StoryLineData L(string speaker, string portrait, string text) =>
        new StoryLineData { speaker = speaker, portraitKey = portrait, text = text };

    static StoryBranch B(string note, int minCount, int maxCount, string requireHero, string forbidHero,
                          params StoryLineData[] lines) =>
        new StoryBranch {
            note = note, minCount = minCount, maxCount = maxCount,
            requireHero = requireHero ?? "", forbidHero = forbidHero ?? "",
            lines = new List<StoryLineData>(lines)
        };

    static void Save(StoryInteractableData d, string fileName)
    {
        string path = "Assets/Resources/Story/" + fileName + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<StoryInteractableData>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(d, path);
    }

    // ── 第一层 ─────────────────────────────────────────────────────────────

    static void Create_F1_FurnaceConsole()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId    = "f1_furnace_console";
        d.bannerText  = "【调查】熔炉控制台";
        d.tintColor   = new Color(0.55f, 0.28f, 0.18f, 1f);
        d.visualScale = new Vector2(1.4f, 1.0f);
        d.colliderSize = new Vector2(1.4f, 1.0f);
        d.spawnFloor = 1; d.spawnRoomIndex = 1; d.spawnOffset = new Vector3(-3.5f,  1.5f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "锈蚀的机械控制台立在锻造大厅中央。指针仍卡在一组刺眼的数字上——"),
            L("旁白", "", "「界心抽取率：327%。冷却系统警告已忽略。」")));
        d.branches.Add(B("首次-赛琳娜专属", 1, 1, "Mage", "",
            L("{hero}", "{heroKey}", "这台机器没坏——它是被人主动逼到极限的。"),
            L("{hero}", "{heroKey}", "「继续提炼，直到王都主能源塔完成。」"),
            L("{hero}", "{heroKey}", "灾难不是意外。是王国为了能源，强行超载界心。")));
        d.branches.Add(B("首次-非赛琳娜", 1, 1, "", "Mage",
            L("{hero}", "{heroKey}", "我看不懂这些读数，但每个表头都顶进了红区。")));
        d.branches.Add(B("多次-旁白", 2, 0, "", "",
            L("旁白", "", "你触碰断裂的拉杆，控制台短暂闪烁，又再次熄灭。")));

        d.runStoryFlags.Add("f1_furnace_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_furnace_overload", requireHero = "Mage", fallbackCount = 2 });
        Save(d, "Floor1_FurnaceConsole");
    }

    static void Create_F1_ArtisanCorpse()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f1_artisan_corpse";
        d.bannerText = "【调查】灰烬工匠尸体";
        d.tintColor  = new Color(0.18f, 0.13f, 0.10f, 1f);
        d.visualScale = new Vector2(1.2f, 0.7f);
        d.colliderSize = new Vector2(1.2f, 0.9f);
        d.spawnFloor = 1; d.spawnRoomIndex = 2; d.spawnOffset = new Vector3( 3.0f, -2.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "一具半跪的焦黑尸体。双手蜷曲，指尖伸向那扇被封死的铁门。")));
        d.branches.Add(B("首次-奥斯汀祈祷", 1, 1, "Paladin", "",
            L("{hero}", "{heroKey}", "「愿你在更亮的地方安息。」"),
            L("亡魂残影", "", "「我们不是死在火里。」"),
            L("亡魂残影", "", "「我们死在那扇不开的门前。」"),
            L("{hero}", "{heroKey}", "他袖口的名册保住了——上面是第一层全部牺牲者的名字。")));
        d.branches.Add(B("首次-非奥斯汀", 1, 1, "", "Paladin",
            L("{hero}", "{heroKey}", "他没死在火里。他死在了那扇门前。")));
        d.branches.Add(B("多次-旁白", 2, 0, "", "",
            L("旁白", "", "焦黑的手指仍指着那扇门。仿佛要把这件事再说一遍。")));

        d.runStoryFlags.Add("f1_artisan_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_artisan_ledger", requireHero = "Paladin", fallbackCount = 2 });
        d.grantStoryItems.Add("烧焦的工匠名册");
        Save(d, "Floor1_ArtisanCorpse");
    }

    // ── 第二层 ─────────────────────────────────────────────────────────────

    static void Create_F2_ScoutCamp()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f2_scout_camp";
        d.bannerText = "【调查】冰封侦察队营地";
        d.tintColor  = new Color(0.55f, 0.75f, 0.85f, 1f);
        d.visualScale = new Vector2(1.7f, 1.6f);
        d.colliderSize = new Vector2(1.7f, 1.6f);
        d.spawnFloor = 2; d.spawnRoomIndex = 0; d.spawnOffset = new Vector3( 3.8f,  2.6f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "几顶冻硬的帐篷围成半圈，篝火早已熄灭。一截箭袋插在雪里。")));
        d.branches.Add(B("首次-艾薇拉", 1, 1, "Ranger", "",
            L("{hero}", "{heroKey}", "这些箭羽……是我们队的标记。"),
            L("{hero}", "{heroKey}", "他们一直都在这里。从来没人来接他们。")));
        d.branches.Add(B("首次-非艾薇拉", 1, 1, "", "Ranger",
            L("{hero}", "{heroKey}", "一支没能撤回的侦察队。冰把他们留在原地。")));
        d.branches.Add(B("二次-艾薇拉回忆", 2, 2, "Ranger", "",
            L("旁白", "", "冰里浮出模糊的人影，像被冻住的一段录像。"),
            L("冰下的回声", "", "「艾薇拉——往上跑，不要回头！」")));
        d.branches.Add(B("三次-艾薇拉真相", 3, 0, "Ranger", "",
            L("{hero}", "{heroKey}", "……我终于想起来了。"),
            L("{hero}", "{heroKey}", "不是我抛弃了他们。是他们关上冰门，把我推了出去。")));

        d.runStoryFlags.Add("f2_scout_camp_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_scout_sacrifice", requireHero = "Ranger", fallbackCount = 2 });
        Save(d, "Floor2_ScoutCamp");
    }

    static void Create_F2_FrozenLake()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f2_frozen_lake";
        d.bannerText = "【调查】冻结湖面";
        d.tintColor  = new Color(0.05f, 0.10f, 0.18f, 1f);
        d.visualScale = new Vector2(2.4f, 2.0f);
        d.colliderSize = new Vector2(2.4f, 1.6f);
        d.spawnFloor = 2; d.spawnRoomIndex = 1; d.spawnOffset = new Vector3( 0.0f,  0.5f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "脚下的黑色冰面，映出一段你不愿面对的画面。")));
        d.branches.Add(B("首次-雷昂", 1, 1, "Warrior", "",
            L("{hero}", "{heroKey}", "……是祖父。他在熔岩门外，把封锁令递给副官。")));
        d.branches.Add(B("首次-艾薇拉", 1, 1, "Ranger", "",
            L("{hero}", "{heroKey}", "我冲出冰门，转身——身后的人没跟上来。")));
        d.branches.Add(B("首次-赛琳娜", 1, 1, "Mage", "",
            L("{hero}", "{heroKey}", "导师还在翻实验记录。他看到了警告，但没有停手。")));
        d.branches.Add(B("首次-奥斯汀", 1, 1, "Paladin", "",
            L("{hero}", "{heroKey}", "教会的火盆——他们焚烧的是写满名字的册子。")));
        d.branches.Add(B("首次-诺兰", 1, 1, "Hunter", "",
            L("{hero}", "{heroKey}", "一只野兽，眼里有光。它第一次「认识」人类。")));
        d.branches.Add(B("多次-旁白", 2, 0, "", "",
            L("旁白", "", "你再次望向湖面。这次，画面只是缓慢地荡开。")));

        d.runStoryFlags.Add("f2_lake_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_lake_witnessed", requireHero = "" });
        d.addCorruption = 2;   // 直视虚空：少量污染
        Save(d, "Floor2_FrozenLake");
    }

    static void Create_F2_FrostAltar()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f2_frost_altar";
        d.bannerText = "【调查】霜眠祭坛";
        d.tintColor  = new Color(0.78f, 0.82f, 0.90f, 1f);
        d.visualScale = new Vector2(1.6f, 1.0f);
        d.colliderSize = new Vector2(1.6f, 1.0f);
        d.spawnFloor = 2; d.spawnRoomIndex = 2; d.spawnOffset = new Vector3(-3.5f,  2.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "祭坛覆着一层厚冰，冰下是教会留下的祷文。")));
        d.branches.Add(B("首次-奥斯汀祷文", 1, 1, "Paladin", "",
            L("{hero}", "{heroKey}", "「愿冰封邪恶。」"),
            L("{hero}", "{heroKey}", "「愿沉默覆盖罪者。」"),
            L("{hero}", "{heroKey}", "「愿无人再提起地下之名。」"),
            L("{hero}", "{heroKey}", "——这不是封印仪式。是封口仪式。")));
        d.branches.Add(B("首次-非奥斯汀", 1, 1, "", "Paladin",
            L("{hero}", "{heroKey}", "刻字像祷词，但读起来更像在禁止后人提起什么。")));
        d.branches.Add(B("多次-奥斯汀", 2, 0, "Paladin", "",
            L("{hero}", "{heroKey}", "教会不是不知道。他们选择让死者永远沉默。")));

        d.runStoryFlags.Add("f2_altar_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_church_silence", requireHero = "Paladin", fallbackCount = 2 });
        d.addCorruption = -3;   // 教会的封口仪式：祭奠净化少量污染
        Save(d, "Floor2_FrostAltar");
    }

    // ── 第三层 ─────────────────────────────────────────────────────────────

    static void Create_F3_Observatory()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_observatory";
        d.bannerText = "【调查】无光观测台";
        d.tintColor  = new Color(0.22f, 0.16f, 0.32f, 1f);
        d.visualScale = new Vector2(1.8f, 1.4f);
        d.colliderSize = new Vector2(1.8f, 1.4f);
        d.spawnFloor = 3; d.spawnRoomIndex = 0; d.spawnOffset = new Vector3( 3.8f,  2.6f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "破碎的星图与观测仪器。一本日志还摊开在台面上。")));
        d.branches.Add(B("首次-赛琳娜", 1, 1, "Mage", "",
            L("{hero}", "{heroKey}", "导师最后的记录——"),
            L("{hero}", "{heroKey}", "「虚空不是空无。它在看着我们。」"),
            L("{hero}", "{heroKey}", "「它学会了我们的语言、恐惧和欲望。」")));
        d.branches.Add(B("首次-非赛琳娜", 1, 1, "", "Mage",
            L("{hero}", "{heroKey}", "符文与星图。我看不懂，但写满了惊恐。")));
        d.branches.Add(B("二次-赛琳娜", 2, 0, "Mage", "",
            L("{hero}", "{heroKey}", "日志背面还有一行——"),
            L("{hero}", "{heroKey}", "「停止实验的请求已被王室驳回。理由：王国需要永恒能源。」")));

        d.runStoryFlags.Add("f3_observatory_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_royal_rejected_stop", requireHero = "Mage", fallbackCount = 2 });
        Save(d, "Floor3_Observatory");
    }

    static void Create_F3_PreyCorridor()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_prey_corridor";
        d.bannerText = "【调查】猎物走廊";
        d.tintColor  = new Color(0.15f, 0.12f, 0.20f, 1f);
        d.visualScale = new Vector2(2.0f, 0.8f);
        d.colliderSize = new Vector2(2.0f, 1.4f);
        d.spawnFloor = 3; d.spawnRoomIndex = 1; d.spawnOffset = new Vector3(-4.0f,  0.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "墙上、地上、甚至天花板，到处是奇形怪状的足迹。")));
        d.branches.Add(B("首次-诺兰", 1, 1, "Hunter", "",
            L("{hero}", "{heroKey}", "像狼，又像人。深度也不对——它走得太轻了。")));
        d.branches.Add(B("首次-非诺兰", 1, 1, "", "Hunter",
            L("{hero}", "{heroKey}", "数量太多，方向太乱。像是有人在演练某种东西。")));
        d.branches.Add(B("二次-诺兰", 2, 2, "Hunter", "",
            L("旁白", "", "你回过头——身后的尘土里，多出一串足迹。"),
            L("{hero}", "{heroKey}", "刚才那里没有这东西。")));
        d.branches.Add(B("三次-诺兰", 3, 0, "Hunter", "",
            L("{hero}", "{heroKey}", "它一直在我背后。"),
            L("{hero}", "{heroKey}", "虚空不是猎物。它在学怎么当猎人。")));

        d.runStoryFlags.Add("f3_prey_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_void_predator", requireHero = "Hunter", fallbackCount = 2 });
        Save(d, "Floor3_PreyCorridor");
    }

    static void Create_F3_BlackMirror()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_black_mirror";
        d.bannerText = "【调查】黑色镜子";
        d.tintColor  = new Color(0.05f, 0.05f, 0.08f, 1f);
        d.visualScale = new Vector2(1.4f, 2.4f);
        d.colliderSize = new Vector2(1.4f, 2.0f);
        d.spawnFloor = 3; d.spawnRoomIndex = 2; d.spawnOffset = new Vector3( 3.0f, -2.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "镜面没有反射任何光，却映出一个动作与你略错半拍的「你」。")));
        d.branches.Add(B("首次-雷昂分身", 1, 1, "Warrior", "",
            L("镜中的你", "{heroKey}", "「你挥剑保护王国，可王国保护过谁？」")));
        d.branches.Add(B("首次-艾薇拉分身", 1, 1, "Ranger", "",
            L("镜中的你", "{heroKey}", "「你不是幸存者，你只是被留下来记住痛苦的人。」")));
        d.branches.Add(B("首次-赛琳娜分身", 1, 1, "Mage", "",
            L("镜中的你", "{heroKey}", "「你厌恶导师，但你也想知道答案。」")));
        d.branches.Add(B("首次-奥斯汀分身", 1, 1, "Paladin", "",
            L("镜中的你", "{heroKey}", "「你说要净化邪恶，可你敢净化自己的信仰吗？」")));
        d.branches.Add(B("首次-诺兰分身", 1, 1, "Hunter", "",
            L("镜中的你", "{heroKey}", "「猎人和猎物，只差一次转身。」")));
        d.branches.Add(B("多次-旁白", 2, 0, "", "",
            L("旁白", "", "镜中的影子还在那里。它学得越来越像你。")));

        d.runStoryFlags.Add("f3_mirror_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_mirror_confronted", requireHero = "" });
        d.grantStoryItems.Add("虚空记忆碎片");
        d.addCorruption = 4;   // 与虚空分身对峙：明显污染
        Save(d, "Floor3_BlackMirror");
    }

    static void Create_F3_BrokenThrone()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_broken_throne";
        d.bannerText = "【调查】破碎王座";
        d.tintColor  = new Color(0.42f, 0.32f, 0.12f, 1f);
        d.visualScale = new Vector2(2.0f, 2.4f);
        d.colliderSize = new Vector2(2.0f, 2.0f);
        d.spawnFloor = 3; d.spawnRoomIndex = 3; d.spawnOffset = new Vector3( 0.0f,  3.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("旁白", "", "断裂的王座深埋于回廊尽头。靠背上，金字仍未完全褪色。")));
        d.branches.Add(B("首次-王座铭文", 1, 1, "", "",
            L("王座铭文", "", "「若牺牲地下万人，可保王都百年。」"),
            L("王座铭文", "", "「此为王国必要之罪。」")));
        d.branches.Add(B("首次-雷昂", 1, 0, "Warrior", "",
            L("{hero}", "{heroKey}", "……我祖父执行的，就是这道命令。")));
        d.branches.Add(B("首次-艾薇拉", 1, 0, "Ranger", "",
            L("{hero}", "{heroKey}", "我的队友——不是死于意外。")));
        d.branches.Add(B("首次-赛琳娜", 1, 0, "Mage", "",
            L("{hero}", "{heroKey}", "学院的档案……全是粉饰过的副本。")));
        d.branches.Add(B("首次-奥斯汀", 1, 0, "Paladin", "",
            L("{hero}", "{heroKey}", "教会的祷文，是替这块石头唱的安魂曲。")));
        d.branches.Add(B("首次-诺兰", 1, 0, "Hunter", "",
            L("{hero}", "{heroKey}", "污染不是从地里钻出来的。是被人放出来的。")));

        d.runStoryFlags.Add("f3_throne_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_kingdom_guilt", requireHero = "" });
        Save(d, "Floor3_BrokenThrone");
    }
}
