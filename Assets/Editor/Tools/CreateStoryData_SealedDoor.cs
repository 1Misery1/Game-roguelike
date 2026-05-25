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
        data.bannerText  = "【调查】封死的升降门";
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
                new StoryLineData { speaker = "旁白", portraitKey = "",
                    text = "一扇巨大的铁门横在升降井前，门上烙着王国的封印。" },
                new StoryLineData { speaker = "旁白", portraitKey = "",
                    text = "铁门从外侧被锁死——门缝里，凝固着一只烧焦的手印。" },
            }
        };
        var b1Leon = new StoryBranch {
            note = "首次-雷昂专属", minCount = 1, maxCount = 1, requireHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "这封印……是王国守卫军的封门术。" },
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "只有军官才有权下令使用。当年困住下面那些人的不是怪物——是我们的人，从外面锁死了门。" },
            }
        };
        var b1Other = new StoryBranch {
            note = "首次-非雷昂", minCount = 1, maxCount = 1, forbidHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "门是从外面锁上的。里面的人，根本逃不出来。" },
            }
        };
        // 重复调查
        var b2 = new StoryBranch {
            note = "重复-旁白+回声", minCount = 2,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "旁白", portraitKey = "",
                    text = "你再次贴近冰冷的铁门。门后，仿佛有声音穿过岁月渗了出来——" },
                new StoryLineData { speaker = "门后的回声", portraitKey = "",
                    text = "「开门！下面还有活人！」" },
                new StoryLineData { speaker = "门后的回声", portraitKey = "",
                    text = "「命令是封锁。不准开启。」" },
            }
        };
        var b2Leon = new StoryBranch {
            note = "重复-雷昂感叹", minCount = 2, requireHero = "Warrior",
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "{hero}", portraitKey = "{heroKey}",
                    text = "……这道命令，究竟是谁下的。" },
            }
        };

        // 第二周目起：门后的回声变得更连贯（多周目剧情递进示例）
        var b3SecondRun = new StoryBranch {
            note = "二周目-门后增段", minCount = 1, minRunCount = 1,
            lines = new System.Collections.Generic.List<StoryLineData> {
                new StoryLineData { speaker = "门后的回声", portraitKey = "",
                    text = "「……还有人吗？」" },
                new StoryLineData { speaker = "门后的回声", portraitKey = "",
                    text = "「请记住我们的名字——」" },
            }
        };

        data.branches.Add(b1);
        data.branches.Add(b1Leon);
        data.branches.Add(b1Other);
        data.branches.Add(b2);
        data.branches.Add(b2Leon);
        data.branches.Add(b3SecondRun);

        data.runStoryFlags.Add("f1_sealed_door_seen");
        data.truthAwards.Add(new TruthFlagAward {
            flag = "truth_kingdom_sealed_door",
            requireHero = "Warrior"
        });

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Story] Created " + path);
    }
}
