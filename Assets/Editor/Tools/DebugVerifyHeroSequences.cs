using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Narrative;
using UnityEditor;
using UnityEngine;

/// 验证任意英雄顺序均能到达 Crown 结局，且每周目都产出新对话内容
public static class DebugVerifyHeroSequences
{
    static readonly string[] CrownFlags = {
        "truth_artisan_ledger",
        "truth_church_silence",
        "truth_royal_rejected_stop",
        "truth_kingdom_guilt",
    };

    /// 测试场景：固定从猎人开始，再用任意 4 个不同角色推进
    public static void Execute()
    {
        var assets = Resources.LoadAll<StoryInteractableData>("Story");
        Debug.Log($"[Seq] Loaded {assets.Length} story assets");

        // 几种典型英雄顺序
        var sequences = new[] {
            new[] { "Hunter",  "Mage",     "Paladin", "Warrior", "Ranger" },
            new[] { "Hunter",  "Hunter",   "Hunter",  "Hunter",  "Hunter" },   // 单角色 5 周目
            new[] { "Warrior", "Mage",     "Hunter",  "Paladin", "Ranger" },
            new[] { "Paladin", "Warrior",  "Mage",    "Hunter",  "Ranger" },
            new[] { "Ranger",  "Hunter",   "Paladin", "Warrior", "Mage"   },
        };

        foreach (var seq in sequences)
        {
            var sb = new StringBuilder();
            sb.Append("[Seq] ").Append(string.Join("→", seq)).Append(" | ");

            // 模拟跨周目调查：每个对象每周目被该英雄调查一次
            var heroCounts = new Dictionary<(string objId, string hero), int>();
            var globalCounts = new Dictionary<string, int>();
            var truthFlags = new HashSet<string>();
            var perRunLineCounts = new List<int>();

            for (int run = 0; run < seq.Length; run++)
            {
                string hero = seq[run];
                int runLineCount = 0;
                foreach (var d in assets)
                {
                    if (d == null) continue;
                    // bump
                    var key = (d.objectId, hero);
                    heroCounts[key]                       = heroCounts.TryGetValue(key, out var hc) ? hc + 1 : 1;
                    globalCounts[d.objectId]              = globalCounts.TryGetValue(d.objectId, out var gc) ? gc + 1 : 1;
                    int heroCount   = heroCounts[key];
                    int globalCount = globalCounts[d.objectId];

                    // 计算本次播放产出的对话行数（branch 过滤逻辑同 StoryInteractable.BuildLinesFromData）
                    foreach (var br in d.branches)
                    {
                        if (br == null) continue;
                        if (heroCount < Mathf.Max(1, br.minCount)) continue;
                        if (br.maxCount > 0 && heroCount > br.maxCount) continue;
                        if (!string.IsNullOrEmpty(br.requireHero) && br.requireHero != hero) continue;
                        if (!string.IsNullOrEmpty(br.forbidHero)  && br.forbidHero  == hero) continue;
                        runLineCount += br.lines.Count;
                    }

                    // 真相旗（fallback 用 globalCount）
                    foreach (var ta in d.truthAwards)
                    {
                        if (ta == null || string.IsNullOrEmpty(ta.flag)) continue;
                        bool own      = string.IsNullOrEmpty(ta.requireHero) || ta.requireHero == hero;
                        bool fallback = ta.fallbackCount > 0 && globalCount >= ta.fallbackCount;
                        if (own || fallback) truthFlags.Add(ta.flag);
                    }
                }
                perRunLineCounts.Add(runLineCount);
            }

            sb.Append("旗=").Append(truthFlags.Count).Append("/10")
              .Append(" Crown=").Append(CrownFlags.All(truthFlags.Contains))
              .Append(" 各周目台词行=[").Append(string.Join(",", perRunLineCounts)).Append("]");
            Debug.Log(sb.ToString());
        }
    }
}
