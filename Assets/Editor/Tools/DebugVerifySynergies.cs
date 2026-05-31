using System.Collections.Generic;
using Game.Core;
using Game.Systems;
using UnityEditor;
using UnityEngine;

/// 离线推演：给 RunState 注入若干道具组合，看哪些协同会触发
public static class DebugVerifySynergies
{
    public static void Execute()
    {
        Debug.Log("[Synergy] 已注册协同：");
        foreach (var s in StoryItemSynergyDatabase.All)
        {
            string need = string.Join("+", s.requiredItems);
            Debug.Log($"[Synergy]   · {s.displayName}  需 {need}  → {s.flavor}");
        }

        Test("纯净化路线", "Frost Mirror Shard", "Memory Pact", "Courage to Overthrow", "Restraint");
        Test("纯污染路线", "Lakebed Relic", "Memory of Confrontation", "Echo of Fury", "Throne's Lingering Might", "Void Memory Shard");
        Test("混合路线",   "Frost Mirror Shard", "Echo of Fury", "Memory of Confrontation", "Restraint", "Courage to Overthrow");
        Test("少量道具",   "Frost Mirror Shard", "Restraint");
    }

    static void Test(string label, params string[] items)
    {
        var run = new RunState();
        foreach (var it in items) run.AddStoryItem(it);
        var activated = StoryItemSynergyDatabase.CheckAndActivate(run, null);
        if (activated.Count == 0)
        {
            Debug.Log($"[Synergy] [{label}] 持有 {items.Length} 件 → 无协同");
            return;
        }
        var names = new List<string>();
        foreach (var s in activated) names.Add(s.displayName);
        string joined = string.Join("、", names);
        Debug.Log($"[Synergy] [{label}] 持有 {items.Length} 件 → 激活 {activated.Count}：{joined}");
    }
}
