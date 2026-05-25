using System.Collections.Generic;
using Game.Narrative;
using Game.Systems;
using UnityEditor;
using UnityEngine;

/// 离线扫描所有 Resources/Story/*.asset 中的 grantStoryItems，
/// 报告每个道具是否在 StoryItemDatabase 中有定义和效果
public static class DebugVerifyStoryItems
{
    public static void Execute()
    {
        var assets = Resources.LoadAll<StoryInteractableData>("Story");
        var allItems = new HashSet<string>();
        foreach (var d in assets)
        {
            if (d == null) continue;
            if (d.grantStoryItems != null)
                foreach (var it in d.grantStoryItems)
                    if (!string.IsNullOrEmpty(it)) allItems.Add(it);
            if (d.choices != null)
                foreach (var c in d.choices)
                    if (c != null && c.grantStoryItems != null)
                        foreach (var it in c.grantStoryItems)
                            if (!string.IsNullOrEmpty(it)) allItems.Add(it);
        }
        Debug.Log($"[Items] 全部出现的剧情道具：{allItems.Count}");
        foreach (var id in allItems)
        {
            if (StoryItemDatabase.TryGet(id, out var def))
            {
                int n = def.effects?.Length ?? 0;
                Debug.Log($"[Items]  · 「{id}」  效果数={n}  备注：{def.flavorTag}");
            }
            else
            {
                Debug.LogWarning($"[Items]  · 「{id}」未在 StoryItemDatabase 注册！");
            }
        }
    }
}
