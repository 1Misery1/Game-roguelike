using System.Reflection;
using Game.Core;
using Game.Dev;
using UnityEditor;
using UnityEngine;

/// 撤销 DebugForceTrueEnding 注入的测试真相旗，并保存
public static class DebugCleanupTestFlags
{
    public static void Execute()
    {
        if (!Application.isPlaying)
        {
            // 离 PlayMode 直接读盘上的存档清理
            var ps = PersistentState.Load();
            RemoveAll(ps);
            ps.Save();
            Debug.Log($"[Cleanup] (offline) TruthFlags now = {ps.TruthFlags.Count}");
            return;
        }
#if UNITY_2023_1_OR_NEWER
        var bs = Object.FindFirstObjectByType<GameBootstrap>();
#else
        var bs = Object.FindObjectOfType<GameBootstrap>();
#endif
        var persistField = typeof(GameBootstrap).GetField("_persistent",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var persistent = persistField?.GetValue(bs) as PersistentState;
        if (persistent == null) return;
        RemoveAll(persistent);
        persistent.Save();
        Debug.Log($"[Cleanup] TruthFlags now = {persistent.TruthFlags.Count}");
    }

    static void RemoveAll(PersistentState ps)
    {
        foreach (var f in new[] {
            "truth_kingdom_sealed_door",
            "truth_furnace_overload",
            "truth_artisan_ledger",
            "truth_scout_sacrifice",
            "truth_lake_witnessed"
        })
            ps.TruthFlags.Remove(f);
    }
}
