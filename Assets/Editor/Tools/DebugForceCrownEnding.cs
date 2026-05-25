using System.Reflection;
using Game.Core;
using Game.Dev;
using UnityEditor;
using UnityEngine;

/// 注入 4 个隐藏 Boss 真相旗 + truth_final_boss_defeated → 验证王冠结局三档分支
public static class DebugForceCrownEnding
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

        var persistField = typeof(GameBootstrap).GetField("_persistent",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var persistent = persistField?.GetValue(bs) as PersistentState;
        if (persistent == null) return;

        foreach (var f in new[] {
            "truth_artisan_ledger",
            "truth_church_silence",
            "truth_royal_rejected_stop",
            "truth_kingdom_guilt",
            "truth_final_boss_defeated"
        })
            persistent.AddTruthFlag(f);

        Debug.Log($"[CrownTest] TruthFlags = {persistent.TruthFlags.Count}, IsHiddenBossUnlocked = {bs.IsHiddenBossUnlocked()}");

        var mi = typeof(GameBootstrap).GetMethod("TriggerVictory",
            BindingFlags.NonPublic | BindingFlags.Instance);
        mi?.Invoke(bs, null);

        // 读取 _endingTier
        var tierField = typeof(GameBootstrap).GetField("_endingTier",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Log($"[CrownTest] _endingTier = {tierField?.GetValue(bs)}");
    }

    /// 清理本脚本注入的旗
    public static void Cleanup()
    {
        var ps = PersistentState.Load();
        foreach (var f in new[] {
            "truth_artisan_ledger",
            "truth_church_silence",
            "truth_royal_rejected_stop",
            "truth_kingdom_guilt",
            "truth_final_boss_defeated"
        })
            ps.TruthFlags.Remove(f);
        ps.Save();
        Debug.Log($"[CrownTest] (offline) cleaned. TruthFlags = {ps.TruthFlags.Count}");
    }
}
