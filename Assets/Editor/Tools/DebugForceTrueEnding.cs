using System.Reflection;
using Game.Core;
using Game.Dev;
using UnityEditor;
using UnityEngine;

/// 强制注入 ≥4 个真相旗到 PersistentState，然后触发胜利路径以验证真相结局分支
public static class DebugForceTrueEnding
{
    public static void Execute()
    {
        if (!Application.isPlaying) return;
#if UNITY_2023_1_OR_NEWER
        var bs = Object.FindFirstObjectByType<GameBootstrap>();
#else
        var bs = Object.FindObjectOfType<GameBootstrap>();
#endif
        if (bs == null) return;

        // 注入 5 个真相旗（覆盖 threshold=4）
        var persistField = typeof(GameBootstrap).GetField("_persistent",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var persistent = persistField?.GetValue(bs) as PersistentState;
        if (persistent == null) { Debug.LogWarning("[TrueEnding] no _persistent"); return; }

        foreach (var flag in new[] {
            "truth_kingdom_sealed_door",
            "truth_furnace_overload",
            "truth_artisan_ledger",
            "truth_scout_sacrifice",
            "truth_lake_witnessed"
        })
            persistent.AddTruthFlag(flag);

        Debug.Log($"[TrueEnding] TruthFlags now = {persistent.TruthFlags.Count}");

        var mi = typeof(GameBootstrap).GetMethod("TriggerVictory",
            BindingFlags.NonPublic | BindingFlags.Instance);
        mi?.Invoke(bs, null);
        Debug.Log("[TrueEnding] Victory triggered.");
    }
}
