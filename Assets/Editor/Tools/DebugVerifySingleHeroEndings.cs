using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Data;
using Game.Narrative;
using UnityEditor;
using UnityEngine;

/// 离线验证：模拟单一英雄反复调查 N 次 → 检查能否达到 Crown 隐藏 Boss 解锁条件
/// 不进 PlayMode，纯数据推演
public static class DebugVerifySingleHeroEndings
{
    public static void Execute()
    {
        var assets = Resources.LoadAll<StoryInteractableData>("Story");
        Debug.Log($"[Verify] Loaded {assets.Length} story assets");

        foreach (string heroKey in new[] { "Warrior", "Ranger", "Mage", "Paladin", "Hunter" })
        {
            // 单角色 1 周目（每个交互物调查 1 次）
            var run1 = SimulateRun(assets, heroKey, 1);
            // 单角色 2 周目（每个交互物累计调查 2 次）
            var run2 = SimulateRun(assets, heroKey, 2);
            // 单角色 3 周目
            var run3 = SimulateRun(assets, heroKey, 3);

            Debug.Log($"[Verify] {heroKey}: 1周={run1.Count}旗  2周={run2.Count}旗  3周={run3.Count}旗" +
                      $"  Crown@2周={CrownUnlocked(run2)}  Crown@3周={CrownUnlocked(run3)}");
        }
    }

    static HashSet<string> SimulateRun(StoryInteractableData[] assets, string heroKey, int investigationCount)
    {
        var flags = new HashSet<string>();
        foreach (var d in assets)
        {
            if (d == null) continue;
            foreach (var ta in d.truthAwards)
            {
                if (ta == null || string.IsNullOrEmpty(ta.flag)) continue;
                bool isOwn      = string.IsNullOrEmpty(ta.requireHero) || ta.requireHero == heroKey;
                bool isFallback = ta.fallbackCount > 0 && investigationCount >= ta.fallbackCount;
                if (isOwn || isFallback) flags.Add(ta.flag);
            }
        }
        return flags;
    }

    static readonly string[] CrownFlags = {
        "truth_artisan_ledger",
        "truth_church_silence",
        "truth_royal_rejected_stop",
        "truth_kingdom_guilt",
    };

    static bool CrownUnlocked(HashSet<string> flags) =>
        CrownFlags.All(flags.Contains);
}
