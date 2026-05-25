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
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("旁白", "", "你触碰断裂的拉杆，控制台短暂闪烁，又再次熄灭。")));
        // 第 3 次：控制台开始显示残缺一句话
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("旁白", "", "控制台短暂亮起，一行字一闪而过——"),
            L("旁白", "", "「……请关停。」")));
        d.branches.Add(B("三次-赛琳娜抉择", 3, 3, "Mage", "",
            L("{hero}", "{heroKey}", "我看见了。我不会再为'必要'让步。")));
        // 第 4 次起：灰烬
        d.branches.Add(B("四次+-灰烬", 4, 0, "", "",
            L("旁白", "", "控制台彻底沉默。指针碎成两段，像被人轻轻按了下去。")));

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
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("旁白", "", "焦黑的手指仍指着那扇门。仿佛要把这件事再说一遍。")));
        // 第 3 次：尸体周围的灰开始流动
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("旁白", "", "焦黑的手指轻轻颤了一下。一片灰从指节落下——竟没有立刻散去。")));
        d.branches.Add(B("三次-奥斯汀承诺", 3, 3, "Paladin", "",
            L("{hero}", "{heroKey}", "我会把名册里的名字，一个一个念出来。")));
        // 第 4 次起：尸体淡出
        d.branches.Add(B("四次+-淡出", 4, 0, "", "",
            L("旁白", "", "工匠的轮廓比上次更浅了一些。仿佛终于可以走了。")));

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
        d.branches.Add(B("三次-艾薇拉真相", 3, 3, "Ranger", "",
            L("{hero}", "{heroKey}", "……我终于想起来了。"),
            L("{hero}", "{heroKey}", "不是我抛弃了他们。是他们关上冰门，把我推了出去。")));
        // 第 4 次起：和解
        d.branches.Add(B("四次+-旁白消融", 4, 0, "", "",
            L("旁白", "", "营地的雪开始一点点融化。冻硬的帆布也松开了。")));
        d.branches.Add(B("四次+-艾薇拉告别", 4, 0, "Ranger", "",
            L("{hero}", "{heroKey}", "谢谢你们。我把你们的名字也带出去了。")));

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
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("旁白", "", "你再次望向湖面。这次，画面只是缓慢地荡开。")));
        // 第 3 次：湖面映出"现在的自己"——每个英雄看到不同的"现在"
        d.branches.Add(B("三次-雷昂", 3, 3, "Warrior", "",
            L("{hero}", "{heroKey}", "镜中的我已经放下了家徽。比想象中轻。")));
        d.branches.Add(B("三次-艾薇拉", 3, 3, "Ranger", "",
            L("{hero}", "{heroKey}", "我没有再回头。但每一步，都带着他们走。")));
        d.branches.Add(B("三次-赛琳娜", 3, 3, "Mage", "",
            L("{hero}", "{heroKey}", "知识不是为了胜过别人。是为了不让灾难再来一次。")));
        d.branches.Add(B("三次-奥斯汀", 3, 3, "Paladin", "",
            L("{hero}", "{heroKey}", "圣洁不在祭坛上——在被忘记的名字里。")));
        d.branches.Add(B("三次-诺兰", 3, 3, "Hunter", "",
            L("{hero}", "{heroKey}", "我不再追击什么。我学会了倾听。")));
        // 第 4 次起：归于平静
        d.branches.Add(B("四次+-旁白", 4, 0, "", "",
            L("旁白", "", "湖面终于平了。再也没有映出任何东西——只是一面深处的黑。")));

        d.runStoryFlags.Add("f2_lake_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_lake_witnessed", requireHero = "" });
        d.addCorruption = 2;   // 基线：站在湖前已经少量沾染

        // 玩家抉择：直视 vs 打碎
        d.choiceTitle = "你怎么面对这面湖？";
        d.choices.Add(new StoryChoice {
            label       = "直视湖面",
            description = "看完所有不愿面对的画面——承担它",
            followLines = new List<StoryLineData> {
                L("旁白", "", "你强迫自己看到最后。胸口被某种冰冷的东西轻轻刺穿。"),
                L("{hero}", "{heroKey}", "看清了，就不必再回头。"),
            },
            grantStoryItems = new List<string> { "寒镜碎片" },
            runStoryFlags   = new List<string> { "f2_lake_witnessed_directly" },
            bannerOverride  = "【抉择】你直视了湖面",
        });
        d.choices.Add(new StoryChoice {
            label       = "打碎湖面",
            description = "拒绝再看下去——从碎片下取走某物（污染加重）",
            followLines = new List<StoryLineData> {
                L("旁白", "", "玻璃般的裂声响起。湖底浮起一件凉到骨子的遗物——你不知道那原本属于谁。"),
                L("{hero}", "{heroKey}", "我不想看了。这一次先这样。"),
            },
            grantStoryItems = new List<string> { "湖底遗物" },
            runStoryFlags   = new List<string> { "f2_lake_shattered" },
            addCorruption   = 5,
            bannerOverride  = "【抉择】你打碎了湖面",
        });
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
        d.branches.Add(B("多次-奥斯汀", 2, 2, "Paladin", "",
            L("{hero}", "{heroKey}", "教会不是不知道。他们选择让死者永远沉默。")));
        // 第 3 次：祷文风化 + 奥斯汀新誓
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("旁白", "", "祷文的笔画开始风化。冰下露出更早的一行字——"),
            L("旁白", "", "「记住他们。」")));
        d.branches.Add(B("三次-奥斯汀新誓", 3, 3, "Paladin", "",
            L("{hero}", "{heroKey}", "我以晨誓教会之名，替他们补回真名。")));
        // 第 4 次起：祭坛上像是被放过一片纸
        d.branches.Add(B("四次+-纸片", 4, 0, "", "",
            L("旁白", "", "祭坛中央留着一片烧焦的纸——像是有人在你之前把名册的一页放了上去。")));

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
        d.branches.Add(B("二次-赛琳娜", 2, 2, "Mage", "",
            L("{hero}", "{heroKey}", "日志背面还有一行——"),
            L("{hero}", "{heroKey}", "「停止实验的请求已被王室驳回。理由：王国需要永恒能源。」")));
        // 第 3 次：仪器最后一次闪烁
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("旁白", "", "破碎的仪器最后闪了一下。星图上空白的一格——竟亮了一秒。")));
        d.branches.Add(B("三次-赛琳娜抉择", 3, 3, "Mage", "",
            L("{hero}", "{heroKey}", "我会把这些记录公开。让知识不再为王座加冕。")));
        // 第 4 次起：观测仪彻底死去
        d.branches.Add(B("四次+-沉寂", 4, 0, "", "",
            L("旁白", "", "观测台再也没有反应。但你已经知道它说过什么。")));

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
        d.branches.Add(B("三次-诺兰", 3, 3, "Hunter", "",
            L("{hero}", "{heroKey}", "它一直在我背后。"),
            L("{hero}", "{heroKey}", "虚空不是猎物。它在学怎么当猎人。")));
        // 第 4 次起：诺兰放下、足迹淡去
        d.branches.Add(B("四次+-旁白", 4, 0, "", "",
            L("旁白", "", "足迹比之前浅了一截。像是它也开始犹豫，是否还要继续追。")));
        d.branches.Add(B("四次+-诺兰放下", 4, 0, "Hunter", "",
            L("{hero}", "{heroKey}", "你跟着我太久了。该轮到我学着——不再追了。")));

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
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("旁白", "", "镜中的影子还在那里。它学得越来越像你。")));
        // 第 3 次：与镜中的自己和解（每个英雄独立）
        d.branches.Add(B("三次-雷昂和解", 3, 3, "Warrior", "",
            L("镜中的你", "{heroKey}", "「……或许你才是真正的守卫。」")));
        d.branches.Add(B("三次-艾薇拉和解", 3, 3, "Ranger", "",
            L("镜中的你", "{heroKey}", "「你逃出去过一次，这一次留下来。也算公平。」")));
        d.branches.Add(B("三次-赛琳娜和解", 3, 3, "Mage", "",
            L("镜中的你", "{heroKey}", "「答案与代价。终于分得清了。」")));
        d.branches.Add(B("三次-奥斯汀和解", 3, 3, "Paladin", "",
            L("镜中的你", "{heroKey}", "「圣洁不是被守住的。是被承认的。」")));
        d.branches.Add(B("三次-诺兰和解", 3, 3, "Hunter", "",
            L("镜中的你", "{heroKey}", "「转身。这一次，你做猎人。」")));
        // 第 4 次起：镜面化开
        d.branches.Add(B("四次+-镜面化水", 4, 0, "", "",
            L("旁白", "", "镜面像水一样化开了，连同那个学了你太久的影子。")));

        d.runStoryFlags.Add("f3_mirror_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_mirror_confronted", requireHero = "" });
        d.grantStoryItems.Add("虚空记忆碎片");
        d.addCorruption = 4;   // 基线：盯着镜中分身已经被它影响

        // 玩家抉择：对峙 vs 转身
        d.choiceTitle = "镜中的「你」抬起头——";
        d.choices.Add(new StoryChoice {
            label       = "对峙",
            description = "正视镜中的你——更深地接触虚空，但能取得对峙记忆",
            followLines = new List<StoryLineData> {
                L("旁白", "", "两个你对视很久。最后那个影子轻轻点了头，回到镜中去。"),
                L("{hero}", "{heroKey}", "我看见你了。你也看见我了。"),
            },
            grantStoryItems = new List<string> { "对峙记忆" },
            runStoryFlags   = new List<string> { "f3_mirror_confronted_self" },
            addCorruption   = 2,
            bannerOverride  = "【抉择】你与镜中的你对峙",
        });
        d.choices.Add(new StoryChoice {
            label       = "转身离开",
            description = "拒绝交手——保留克制；少量净化污染，但不会获得对峙记忆",
            followLines = new List<StoryLineData> {
                L("旁白", "", "你转身走开。背后没有脚步声跟上来——但你知道，它一直在看着。"),
                L("{hero}", "{heroKey}", "不是每场对峙都得发生。"),
            },
            grantStoryItems = new List<string> { "克制" },
            runStoryFlags   = new List<string> { "f3_mirror_refused_self" },
            addCorruption   = -4,  // 净化（拒绝与虚空交锋）
            bannerOverride  = "【抉择】你从镜前转身",
        });
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
        // 第 2 次：王座底座暗刻
        d.branches.Add(B("二次-暗刻", 2, 2, "", "",
            L("旁白", "", "你扶住断裂的王座边缘，指腹蹭到一行不在显眼处的小字——"),
            L("王座暗刻", "", "「我们以下面万人之名，加冕。」")));
        // 第 3 次：王座彻底崩塌
        d.branches.Add(B("三次-崩塌", 3, 0, "", "",
            L("旁白", "", "你最后一次走近王座。这次它没有再坚持——金箔剥落、石身崩开，像在让出位置。")));

        d.runStoryFlags.Add("f3_throne_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_kingdom_guilt", requireHero = "" });
        Save(d, "Floor3_BrokenThrone");
    }
}
