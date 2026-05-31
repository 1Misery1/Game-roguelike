using System.IO;
using Game.Narrative;
using UnityEditor;
using UnityEngine;

public static class CreateStoryData_SealedDoor
{
    public static void Execute()
    {
        const string dir  = "Assets/Resources/Story";
        const string path = dir + "/Floor1_SealedDoor.asset";

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var data = ScriptableObject.CreateInstance<StoryInteractableData>();
        data.objectId    = "f1_sealed_door";
        data.bannerText  = "[Investigate] The Sealed Lift Door";
        data.tintColor   = new Color(0.40f, 0.39f, 0.46f, 1f);
        data.visualScale = new Vector2(1.5f, 2.7f);
        data.colliderSize = new Vector2(1.5f, 1.25f);
        data.spawnFloor      = 1;
        data.spawnRoomIndex  = 0;
        data.spawnOffset     = new Vector3(3.8f, 2.6f, 0f);

        // 首次调查
        var b1 = new StoryBranch {
            note     = "首次-旁白",
            minCount = 1, maxCount = 1,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "A massive iron door blocks the lift shaft, branded with the kingdom's seal." },
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "The door was locked from the outside — in the gap, a charred handprint is frozen in place." },
            }
        };
        var b1Leon = new StoryBranch {
            note = "首次-雷昂专属", minCount = 1, maxCount = 1, requireHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "This seal… it is the royal guard's door-sealing craft." },
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "Only an officer had the authority to order it. What trapped the people below was no monster — it was our own men, locking the door from outside." },
            }
        };
        var b1Other = new StoryBranch {
            note = "首次-非雷昂", minCount = 1, maxCount = 1, forbidHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "The door was locked from outside. The people within could never escape." },
            }
        };
        // 重复调查
        var b2 = new StoryBranch {
            note = "重复-旁白+回声", minCount = 2,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "You lean close to the cold iron again. From behind it, voices seem to seep through the years —" },
                new StoryLineData { speaker = "Echo Behind the Door", portraitKey = "",
                    text = "Open the door! There are still people alive down here!" },
                new StoryLineData { speaker = "Echo Behind the Door", portraitKey = "",
                    text = "The order is to seal it. Opening is forbidden." },
            }
        };
        var b2Leon = new StoryBranch {
            note = "重复-雷昂感叹", minCount = 2, requireHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "…who, exactly, gave this order." },
            }
        };

        // 第二周目起：门后的回声变得更连贯（多周目剧情递进示例）
        var b3SecondRun = new StoryBranch {
            note = "二周目-门后增段", minCount = 1, minRunCount = 1,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Echo Behind the Door", portraitKey = "",
                    text = "…is anyone still there?" },
                new StoryLineData { speaker = "Echo Behind the Door", portraitKey = "",
                    text = "Please remember our names —" },
            }
        };

        // 第 3 次：更深的层次——回声开始托付
        var b4ThirdVisit = new StoryBranch {
            note = "三次-门后托付", minCount = 3, maxCount = 3,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "This time there is no roar behind the door, only a deeper silence —" },
                new StoryLineData { speaker = "Echo Behind the Door", portraitKey = "",
                    text = "…if you can hear us, speak for us." },
            }
        };
        var b4Leon = new StoryBranch {
            note = "三次-雷昂郑重承诺", minCount = 3, maxCount = 3, requireHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "I will. I will speak for you, until no one can feign deafness again." },
            }
        };

        // 第 4 次起：告别
        var b5FourthPlus = new StoryBranch {
            note = "四次+-沉默告别", minCount = 4,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "You press your hand to the iron door. No voice answers this time — but you know they heard you." },
            }
        };

        data.branches.Add(b1);
        data.branches.Add(b1Leon);
        data.branches.Add(b1Other);
        data.branches.Add(b2);
        data.branches.Add(b2Leon);
        data.branches.Add(b3SecondRun);
        data.branches.Add(b4ThirdVisit);
        data.branches.Add(b4Leon);
        data.branches.Add(b5FourthPlus);

        data.runStoryFlags.Add("f1_sealed_door_seen");
        data.truthAwards.Add(new TruthFlagAward {
            flag          = "truth_kingdom_sealed_door",
            requireHero   = "Warrior",
            fallbackCount = 2,   // 非战士第二次调查即解锁，保留单角色通路
        });

        // 抉择：砸开 vs 静默离开
        data.choiceTitle = "How will you face this door?";
        data.choices.Add(new StoryChoice {
            label       = "Break It Open",
            description = "Vent your rage by force (the seal holds firm, but you leave an echo)",
            followLines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "You strike once, twice, ten times. The door only hurls your rage back at you." },
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "Damn it. Damn it." },
            },
            grantStoryItems = new System.Collections.Generic.List<string> { "Echo of Fury" },
            runStoryFlags   = new System.Collections.Generic.List<string> { "f1_door_struck" },
            addCorruption   = 3,
            bannerOverride  = "[Choice] You struck the iron door",
        });
        data.choices.Add(new StoryChoice {
            label       = "Leave in Silence",
            description = "Make remembrance a vow (minor purification)",
            followLines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "Narrator", portraitKey = "",
                    text = "You release your hand from the cold iron and do not look back. You will remember." },
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "I will speak for you. Starting today." },
            },
            grantStoryItems = new System.Collections.Generic.List<string> { "Memory Pact" },
            runStoryFlags   = new System.Collections.Generic.List<string> { "f1_door_oath" },
            addCorruption   = -2,
            bannerOverride  = "[Choice] You made a silent vow",
        });

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Story] Created " + path);
    }
}
