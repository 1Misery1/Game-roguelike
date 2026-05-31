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
        d.bannerText  = "[Investigate] Forge Console";
        d.tintColor   = new Color(0.55f, 0.28f, 0.18f, 1f);
        d.visualScale = new Vector2(1.4f, 1.0f);
        d.colliderSize = new Vector2(1.4f, 1.0f);
        d.spawnFloor = 1; d.spawnRoomIndex = 1; d.spawnOffset = new Vector3(-3.5f,  1.5f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "A rusted mechanical console stands at the center of the forge hall. Its needle is still stuck on a set of glaring numbers —"),
            L("Narrator", "", "Worldcore extraction rate: 327%. Cooling-system warning ignored.")));
        d.branches.Add(B("首次-赛琳娜专属", 1, 1, "Mage", "",
            L("{hero}", "{heroKey}", "This machine isn't broken — someone deliberately drove it past its limit."),
            L("{hero}", "{heroKey}", "Keep refining, until the capital's main energy tower is complete."),
            L("{hero}", "{heroKey}", "The disaster was no accident. The kingdom forcibly overloaded the worldcore for energy.")));
        d.branches.Add(B("首次-非赛琳娜", 1, 1, "", "Mage",
            L("{hero}", "{heroKey}", "I can't read these gauges, but every dial is buried in the red.")));
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("Narrator", "", "You touch the broken lever; the console flickers briefly, then goes dark again.")));
        // 第 3 次：控制台开始显示残缺一句话
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("Narrator", "", "The console lights up for a moment, a single line flashing past —"),
            L("Narrator", "", "…please shut it down.")));
        d.branches.Add(B("三次-赛琳娜抉择", 3, 3, "Mage", "",
            L("{hero}", "{heroKey}", "I've seen it. I will never again yield to 'necessity'.")));
        // 第 4 次起：灰烬
        d.branches.Add(B("四次+-灰烬", 4, 0, "", "",
            L("Narrator", "", "The console falls utterly silent. The needle snaps in two, as if gently pressed down.")));

        d.runStoryFlags.Add("f1_furnace_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_furnace_overload", requireHero = "Mage", fallbackCount = 2 });
        Save(d, "Floor1_FurnaceConsole");
    }

    static void Create_F1_ArtisanCorpse()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f1_artisan_corpse";
        d.bannerText = "[Investigate] Ashen Artisan's Corpse";
        d.tintColor  = new Color(0.18f, 0.13f, 0.10f, 1f);
        d.visualScale = new Vector2(1.2f, 0.7f);
        d.colliderSize = new Vector2(1.2f, 0.9f);
        d.spawnFloor = 1; d.spawnRoomIndex = 2; d.spawnOffset = new Vector3( 3.0f, -2.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "A charred corpse, half-kneeling. Its hands are curled, fingertips reaching toward the sealed iron door.")));
        d.branches.Add(B("首次-奥斯汀祈祷", 1, 1, "Paladin", "",
            L("{hero}", "{heroKey}", "May you rest in a brighter place."),
            L("Wraith Remnant", "", "We did not die in the fire."),
            L("Wraith Remnant", "", "We died before the door that would not open."),
            L("{hero}", "{heroKey}", "The register in his sleeve survived — it bears the names of every victim on the first floor.")));
        d.branches.Add(B("首次-非奥斯汀", 1, 1, "", "Paladin",
            L("{hero}", "{heroKey}", "He did not die in the fire. He died before that door.")));
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("Narrator", "", "The charred fingers still point at the door. As if to say it once more.")));
        // 第 3 次：尸体周围的灰开始流动
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("Narrator", "", "The charred fingers tremble faintly. A flake of ash falls from a knuckle — and does not scatter at once.")));
        d.branches.Add(B("三次-奥斯汀承诺", 3, 3, "Paladin", "",
            L("{hero}", "{heroKey}", "I will read out the names in the register, one by one.")));
        // 第 4 次起：尸体淡出
        d.branches.Add(B("四次+-淡出", 4, 0, "", "",
            L("Narrator", "", "The artisan's outline is fainter than last time. As if he can finally leave.")));

        d.runStoryFlags.Add("f1_artisan_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_artisan_ledger", requireHero = "Paladin", fallbackCount = 2 });
        d.grantStoryItems.Add("Charred Artisan Register");
        Save(d, "Floor1_ArtisanCorpse");
    }

    // ── 第二层 ─────────────────────────────────────────────────────────────

    static void Create_F2_ScoutCamp()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f2_scout_camp";
        d.bannerText = "[Investigate] Frozen Scout Camp";
        d.tintColor  = new Color(0.55f, 0.75f, 0.85f, 1f);
        d.visualScale = new Vector2(1.7f, 1.6f);
        d.colliderSize = new Vector2(1.7f, 1.6f);
        d.spawnFloor = 2; d.spawnRoomIndex = 0; d.spawnOffset = new Vector3( 3.8f,  2.6f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "A few frozen-stiff tents form a half-circle; the campfire long dead. A quiver juts from the snow.")));
        d.branches.Add(B("首次-艾薇拉", 1, 1, "Ranger", "",
            L("{hero}", "{heroKey}", "These fletchings… they're our squad's mark."),
            L("{hero}", "{heroKey}", "They were here all along. No one ever came back for them.")));
        d.branches.Add(B("首次-非艾薇拉", 1, 1, "", "Ranger",
            L("{hero}", "{heroKey}", "A scouting party that never made it back. The ice kept them where they fell.")));
        d.branches.Add(B("二次-艾薇拉回忆", 2, 2, "Ranger", "",
            L("Narrator", "", "Blurred figures surface within the ice, like a frozen strip of film."),
            L("Echo Beneath the Ice", "", "Elvira — run, don't look back!")));
        d.branches.Add(B("三次-艾薇拉真相", 3, 3, "Ranger", "",
            L("{hero}", "{heroKey}", "…I finally remember."),
            L("{hero}", "{heroKey}", "I didn't abandon them. They closed the ice gate and pushed me out.")));
        // 第 4 次起：和解
        d.branches.Add(B("四次+-旁白消融", 4, 0, "", "",
            L("Narrator", "", "The camp's snow begins to melt, little by little. The frozen canvas loosens too.")));
        d.branches.Add(B("四次+-艾薇拉告别", 4, 0, "Ranger", "",
            L("{hero}", "{heroKey}", "Thank you. I carried your names out too.")));

        d.runStoryFlags.Add("f2_scout_camp_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_scout_sacrifice", requireHero = "Ranger", fallbackCount = 2 });
        Save(d, "Floor2_ScoutCamp");
    }

    static void Create_F2_FrozenLake()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f2_frozen_lake";
        d.bannerText = "[Investigate] Frozen Lake";
        d.tintColor  = new Color(0.05f, 0.10f, 0.18f, 1f);
        d.visualScale = new Vector2(2.4f, 2.0f);
        d.colliderSize = new Vector2(2.4f, 1.6f);
        d.spawnFloor = 2; d.spawnRoomIndex = 1; d.spawnOffset = new Vector3( 0.0f,  0.5f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "The black ice underfoot reflects a scene you'd rather not face.")));
        d.branches.Add(B("首次-雷昂", 1, 1, "Warrior", "",
            L("{hero}", "{heroKey}", "…it's grandfather. Outside the lava gate, handing the lockdown order to his adjutant.")));
        d.branches.Add(B("首次-艾薇拉", 1, 1, "Ranger", "",
            L("{hero}", "{heroKey}", "I rushed out the ice gate and turned — no one behind me followed.")));
        d.branches.Add(B("首次-赛琳娜", 1, 1, "Mage", "",
            L("{hero}", "{heroKey}", "My mentor was still leafing through the records. He saw the warning, but did not stop.")));
        d.branches.Add(B("首次-奥斯汀", 1, 1, "Paladin", "",
            L("{hero}", "{heroKey}", "The church braziers — what they burned were ledgers filled with names.")));
        d.branches.Add(B("首次-诺兰", 1, 1, "Hunter", "",
            L("{hero}", "{heroKey}", "A beast, with light in its eyes. For the first time, it 'recognized' humans.")));
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("Narrator", "", "You gaze at the lake again. This time the image only slowly ripples away.")));
        // 第 3 次：湖面映出"现在的自己"——每个英雄看到不同的"现在"
        d.branches.Add(B("三次-雷昂", 3, 3, "Warrior", "",
            L("{hero}", "{heroKey}", "The me in the reflection has set down the family crest. Lighter than I imagined.")));
        d.branches.Add(B("三次-艾薇拉", 3, 3, "Ranger", "",
            L("{hero}", "{heroKey}", "I never looked back again. But with every step, I carry them with me.")));
        d.branches.Add(B("三次-赛琳娜", 3, 3, "Mage", "",
            L("{hero}", "{heroKey}", "Knowledge isn't for surpassing others. It's to keep the disaster from ever happening again.")));
        d.branches.Add(B("三次-奥斯汀", 3, 3, "Paladin", "",
            L("{hero}", "{heroKey}", "Holiness is not on the altar — it's in the forgotten names.")));
        d.branches.Add(B("三次-诺兰", 3, 3, "Hunter", "",
            L("{hero}", "{heroKey}", "I no longer hunt anything. I've learned to listen.")));
        // 第 4 次起：归于平静
        d.branches.Add(B("四次+-旁白", 4, 0, "", "",
            L("Narrator", "", "The lake finally stills. It reflects nothing anymore — only a depthless black.")));

        d.runStoryFlags.Add("f2_lake_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_lake_witnessed", requireHero = "" });
        d.addCorruption = 2;   // 基线：站在湖前已经少量沾染

        // 玩家抉择：直视 vs 打碎
        d.choiceTitle = "How will you face this lake?";
        d.choices.Add(new StoryChoice {
            label       = "Gaze into the Lake",
            description = "Watch every scene you dread to the end — and bear it",
            followLines = new List<StoryLineData> {
                L("Narrator", "", "You force yourself to watch to the end. Something cold gently pierces your chest."),
                L("{hero}", "{heroKey}", "Once you see clearly, there's no need to look back."),
            },
            grantStoryItems = new List<string> { "Frost Mirror Shard" },
            runStoryFlags   = new List<string> { "f2_lake_witnessed_directly" },
            bannerOverride  = "[Choice] You gazed into the lake",
        });
        d.choices.Add(new StoryChoice {
            label       = "Shatter the Lake",
            description = "Refuse to watch further — take something from beneath the shards (corruption rises)",
            followLines = new List<StoryLineData> {
                L("Narrator", "", "A glassy crack rings out. From the lakebed rises a relic cold to the bone — you don't know whom it once belonged to."),
                L("{hero}", "{heroKey}", "I don't want to look anymore. This time, let it be."),
            },
            grantStoryItems = new List<string> { "Lakebed Relic" },
            runStoryFlags   = new List<string> { "f2_lake_shattered" },
            addCorruption   = 5,
            bannerOverride  = "[Choice] You shattered the lake",
        });
        Save(d, "Floor2_FrozenLake");
    }

    static void Create_F2_FrostAltar()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f2_frost_altar";
        d.bannerText = "[Investigate] Frostsleep Altar";
        d.tintColor  = new Color(0.78f, 0.82f, 0.90f, 1f);
        d.visualScale = new Vector2(1.6f, 1.0f);
        d.colliderSize = new Vector2(1.6f, 1.0f);
        d.spawnFloor = 2; d.spawnRoomIndex = 2; d.spawnOffset = new Vector3(-3.5f,  2.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "The altar is sheathed in thick ice; beneath it, prayers left by the church.")));
        d.branches.Add(B("首次-奥斯汀祷文", 1, 1, "Paladin", "",
            L("{hero}", "{heroKey}", "May the ice seal away evil."),
            L("{hero}", "{heroKey}", "May silence cover the guilty."),
            L("{hero}", "{heroKey}", "May none speak the names beneath again."),
            L("{hero}", "{heroKey}", "— this is no sealing rite. It's a silencing rite.")));
        d.branches.Add(B("首次-非奥斯汀", 1, 1, "", "Paladin",
            L("{hero}", "{heroKey}", "The carvings read like prayers, but more like a ban on what later generations may mention.")));
        d.branches.Add(B("多次-奥斯汀", 2, 2, "Paladin", "",
            L("{hero}", "{heroKey}", "The church was not unaware. They chose to keep the dead forever silent.")));
        // 第 3 次：祷文风化 + 奥斯汀新誓
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("Narrator", "", "The strokes of the prayer begin to weather. Beneath the ice, an earlier line emerges —"),
            L("Narrator", "", "Remember them.")));
        d.branches.Add(B("三次-奥斯汀新誓", 3, 3, "Paladin", "",
            L("{hero}", "{heroKey}", "In the name of the Dawnvow Church, I restore their true names.")));
        // 第 4 次起：祭坛上像是被放过一片纸
        d.branches.Add(B("四次+-纸片", 4, 0, "", "",
            L("Narrator", "", "At the altar's center lies a scrap of charred paper — as if someone before you laid a page of the register here.")));

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
        d.bannerText = "[Investigate] Lightless Observatory";
        d.tintColor  = new Color(0.22f, 0.16f, 0.32f, 1f);
        d.visualScale = new Vector2(1.8f, 1.4f);
        d.colliderSize = new Vector2(1.8f, 1.4f);
        d.spawnFloor = 3; d.spawnRoomIndex = 0; d.spawnOffset = new Vector3( 3.8f,  2.6f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "Shattered star charts and observation instruments. A journal still lies open on the desk.")));
        d.branches.Add(B("首次-赛琳娜", 1, 1, "Mage", "",
            L("{hero}", "{heroKey}", "My mentor's final entry —"),
            L("{hero}", "{heroKey}", "The Void is not emptiness. It is watching us."),
            L("{hero}", "{heroKey}", "It has learned our language, our fears, and our desires.")));
        d.branches.Add(B("首次-非赛琳娜", 1, 1, "", "Mage",
            L("{hero}", "{heroKey}", "Runes and star charts. I can't read them, but they're filled with terror.")));
        d.branches.Add(B("二次-赛琳娜", 2, 2, "Mage", "",
            L("{hero}", "{heroKey}", "On the back of the journal is one more line —"),
            L("{hero}", "{heroKey}", "The request to halt the experiment was denied by the crown. Reason: the kingdom needs eternal energy.")));
        // 第 3 次：仪器最后一次闪烁
        d.branches.Add(B("三次-旁白", 3, 3, "", "",
            L("Narrator", "", "The broken instruments flash one last time. A blank cell on the star chart — lights up for a second.")));
        d.branches.Add(B("三次-赛琳娜抉择", 3, 3, "Mage", "",
            L("{hero}", "{heroKey}", "I'll make these records public. Let knowledge no longer crown the throne.")));
        // 第 4 次起：观测仪彻底死去
        d.branches.Add(B("四次+-沉寂", 4, 0, "", "",
            L("Narrator", "", "The observatory never responds again. But you already know what it said.")));

        d.runStoryFlags.Add("f3_observatory_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_royal_rejected_stop", requireHero = "Mage", fallbackCount = 2 });
        Save(d, "Floor3_Observatory");
    }

    static void Create_F3_PreyCorridor()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_prey_corridor";
        d.bannerText = "[Investigate] Prey Corridor";
        d.tintColor  = new Color(0.15f, 0.12f, 0.20f, 1f);
        d.visualScale = new Vector2(2.0f, 0.8f);
        d.colliderSize = new Vector2(2.0f, 1.4f);
        d.spawnFloor = 3; d.spawnRoomIndex = 1; d.spawnOffset = new Vector3(-4.0f,  0.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "On the walls, the floor, even the ceiling — strange footprints everywhere.")));
        d.branches.Add(B("首次-诺兰", 1, 1, "Hunter", "",
            L("{hero}", "{heroKey}", "Like a wolf, yet like a man. The depth is wrong too — it treads too lightly.")));
        d.branches.Add(B("首次-非诺兰", 1, 1, "", "Hunter",
            L("{hero}", "{heroKey}", "Too many, too scattered in direction. As if someone were rehearsing something.")));
        d.branches.Add(B("二次-诺兰", 2, 2, "Hunter", "",
            L("Narrator", "", "You glance back — in the dust behind you, a new trail of prints has appeared."),
            L("{hero}", "{heroKey}", "That wasn't there a moment ago.")));
        d.branches.Add(B("三次-诺兰", 3, 3, "Hunter", "",
            L("{hero}", "{heroKey}", "It's been behind me the whole time."),
            L("{hero}", "{heroKey}", "The Void is not prey. It's learning how to be the hunter.")));
        // 第 4 次起：诺兰放下、足迹淡去
        d.branches.Add(B("四次+-旁白", 4, 0, "", "",
            L("Narrator", "", "The prints are shallower than before. As if it, too, has begun to hesitate whether to keep chasing.")));
        d.branches.Add(B("四次+-诺兰放下", 4, 0, "Hunter", "",
            L("{hero}", "{heroKey}", "You've followed me too long. It's my turn to learn — to stop chasing.")));

        d.runStoryFlags.Add("f3_prey_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_void_predator", requireHero = "Hunter", fallbackCount = 2 });
        Save(d, "Floor3_PreyCorridor");
    }

    static void Create_F3_BlackMirror()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_black_mirror";
        d.bannerText = "[Investigate] Black Mirror";
        d.tintColor  = new Color(0.05f, 0.05f, 0.08f, 1f);
        d.visualScale = new Vector2(1.4f, 2.4f);
        d.colliderSize = new Vector2(1.4f, 2.0f);
        d.spawnFloor = 3; d.spawnRoomIndex = 2; d.spawnOffset = new Vector3( 3.0f, -2.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "The mirror reflects no light, yet shows a 'you' whose every motion lags half a beat behind.")));
        d.branches.Add(B("首次-雷昂分身", 1, 1, "Warrior", "",
            L("The You in the Mirror", "{heroKey}", "You swing your sword to protect the kingdom — but whom did the kingdom ever protect?")));
        d.branches.Add(B("首次-艾薇拉分身", 1, 1, "Ranger", "",
            L("The You in the Mirror", "{heroKey}", "You're not a survivor; you're just the one left behind to remember the pain.")));
        d.branches.Add(B("首次-赛琳娜分身", 1, 1, "Mage", "",
            L("The You in the Mirror", "{heroKey}", "You loathe your mentor, yet you too crave the answer.")));
        d.branches.Add(B("首次-奥斯汀分身", 1, 1, "Paladin", "",
            L("The You in the Mirror", "{heroKey}", "You say you'll purify evil — but dare you purify your own faith?")));
        d.branches.Add(B("首次-诺兰分身", 1, 1, "Hunter", "",
            L("The You in the Mirror", "{heroKey}", "Hunter and prey are but one turn apart.")));
        d.branches.Add(B("多次-旁白", 2, 2, "", "",
            L("Narrator", "", "The shadow in the mirror is still there. It grows ever more like you.")));
        // 第 3 次：与镜中的自己和解（每个英雄独立）
        d.branches.Add(B("三次-雷昂和解", 3, 3, "Warrior", "",
            L("The You in the Mirror", "{heroKey}", "…perhaps you are the true guardian after all.")));
        d.branches.Add(B("三次-艾薇拉和解", 3, 3, "Ranger", "",
            L("The You in the Mirror", "{heroKey}", "You escaped once; this time you stay. That's only fair.")));
        d.branches.Add(B("三次-赛琳娜和解", 3, 3, "Mage", "",
            L("The You in the Mirror", "{heroKey}", "Answer and cost. At last you can tell them apart.")));
        d.branches.Add(B("三次-奥斯汀和解", 3, 3, "Paladin", "",
            L("The You in the Mirror", "{heroKey}", "Holiness isn't something guarded. It's something acknowledged.")));
        d.branches.Add(B("三次-诺兰和解", 3, 3, "Hunter", "",
            L("The You in the Mirror", "{heroKey}", "Turn around. This time, you are the hunter.")));
        // 第 4 次起：镜面化开
        d.branches.Add(B("四次+-镜面化水", 4, 0, "", "",
            L("Narrator", "", "The mirror dissolves like water, along with the shadow that studied you too long.")));

        d.runStoryFlags.Add("f3_mirror_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_mirror_confronted", requireHero = "" });
        d.grantStoryItems.Add("Void Memory Shard");
        d.addCorruption = 4;   // 基线：盯着镜中分身已经被它影响

        // 玩家抉择：对峙 vs 转身
        d.choiceTitle = "The 'you' in the mirror lifts its head —";
        d.choices.Add(new StoryChoice {
            label       = "Confront",
            description = "Face the you in the mirror — touch the Void more deeply, but gain the Memory of Confrontation",
            followLines = new List<StoryLineData> {
                L("Narrator", "", "The two of you stare for a long while. At last the shadow nods gently and returns into the mirror."),
                L("{hero}", "{heroKey}", "I see you. And you see me."),
            },
            grantStoryItems = new List<string> { "Memory of Confrontation" },
            runStoryFlags   = new List<string> { "f3_mirror_confronted_self" },
            addCorruption   = 2,
            bannerOverride  = "[Choice] You confronted the you in the mirror",
        });
        d.choices.Add(new StoryChoice {
            label       = "Turn Away",
            description = "Refuse the encounter — keep your Restraint; minor purification, but you won't gain the Memory of Confrontation",
            followLines = new List<StoryLineData> {
                L("Narrator", "", "You turn and walk away. No footsteps follow — but you know it's been watching all along."),
                L("{hero}", "{heroKey}", "Not every confrontation has to happen."),
            },
            grantStoryItems = new List<string> { "Restraint" },
            runStoryFlags   = new List<string> { "f3_mirror_refused_self" },
            addCorruption   = -4,  // 净化（拒绝与虚空交锋）
            bannerOverride  = "[Choice] You turned away from the mirror",
        });
        Save(d, "Floor3_BlackMirror");
    }

    static void Create_F3_BrokenThrone()
    {
        var d = ScriptableObject.CreateInstance<StoryInteractableData>();
        d.objectId   = "f3_broken_throne";
        d.bannerText = "[Investigate] Broken Throne";
        d.tintColor  = new Color(0.42f, 0.32f, 0.12f, 1f);
        d.visualScale = new Vector2(2.0f, 2.4f);
        d.colliderSize = new Vector2(2.0f, 2.0f);
        d.spawnFloor = 3; d.spawnRoomIndex = 3; d.spawnOffset = new Vector3( 0.0f,  3.0f, 0f);

        d.branches.Add(B("首次-旁白", 1, 1, "", "",
            L("Narrator", "", "The broken throne lies buried at the corridor's end. On its back, gilded letters have not fully faded.")));
        d.branches.Add(B("首次-王座铭文", 1, 1, "", "",
            L("Throne Inscription", "", "If thousands below are sacrificed, the capital is secured for a century."),
            L("Throne Inscription", "", "This is the kingdom's necessary sin.")));
        d.branches.Add(B("首次-雷昂", 1, 0, "Warrior", "",
            L("{hero}", "{heroKey}", "…the order my grandfather carried out was this very one.")));
        d.branches.Add(B("首次-艾薇拉", 1, 0, "Ranger", "",
            L("{hero}", "{heroKey}", "My comrades — they did not die by accident.")));
        d.branches.Add(B("首次-赛琳娜", 1, 0, "Mage", "",
            L("{hero}", "{heroKey}", "The academy's archives… all whitewashed copies.")));
        d.branches.Add(B("首次-奥斯汀", 1, 0, "Paladin", "",
            L("{hero}", "{heroKey}", "The church's prayers were a requiem sung for this stone.")));
        d.branches.Add(B("首次-诺兰", 1, 0, "Hunter", "",
            L("{hero}", "{heroKey}", "The corruption didn't crawl up from the earth. Someone let it loose.")));
        // 第 2 次：王座底座暗刻
        d.branches.Add(B("二次-暗刻", 2, 2, "", "",
            L("Narrator", "", "You steady yourself on the broken throne's edge; your fingertip brushes a small line of text tucked out of sight —"),
            L("Hidden Throne Engraving", "", "In the name of the thousands below, we crown ourselves.")));
        // 第 3 次：王座彻底崩塌
        d.branches.Add(B("三次-崩塌", 3, 0, "", "",
            L("Narrator", "", "You approach the throne one last time. This time it no longer holds — the gilt peels, the stone splits open, as if yielding its place.")));

        // 抉择：坐下 vs 推倒
        d.choiceTitle = "What will you do with this throne?";
        d.choices.Add(new StoryChoice {
            label       = "Sit Upon It",
            description = "Crown yourself for a moment (massive corruption, but gain Throne's Lingering Might)",
            followLines = new List<StoryLineData> {
                L("Narrator", "", "You sit down. The stone is warmer than you expected — as if someone sat here long, and long refused to yield."),
                L("{hero}", "{heroKey}", "So that's it. So this is how it feels."),
            },
            grantStoryItems = new List<string> { "Throne's Lingering Might" },
            runStoryFlags   = new List<string> { "f3_throne_sat" },
            addCorruption   = 6,
            bannerOverride  = "[Choice] You sat upon the throne",
        });
        d.choices.Add(new StoryChoice {
            label       = "Topple It",
            description = "Let the throne collapse entirely (purifies corruption, gain the Courage to Overthrow)",
            followLines = new List<StoryLineData> {
                L("Narrator", "", "You shove forward with all your strength. The base shatters first, the backrest topples after — the gilt drifts into the dark like a cloud of kindled ash."),
                L("{hero}", "{heroKey}", "This kick is for the thousands below."),
            },
            grantStoryItems = new List<string> { "Courage to Overthrow" },
            runStoryFlags   = new List<string> { "f3_throne_toppled" },
            addCorruption   = -3,
            bannerOverride  = "[Choice] You toppled the throne",
        });

        d.runStoryFlags.Add("f3_throne_seen");
        d.truthAwards.Add(new TruthFlagAward { flag = "truth_kingdom_guilt", requireHero = "" });
        Save(d, "Floor3_BrokenThrone");
    }
}
