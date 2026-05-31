using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Game.Bootstrap;
/// MCP 联调用：在 PlayMode 中直接调用 GameBootstrap.TriggerVictory()
/// 跳过房间推进，立即进入过场 → 结算流程。
public static class DebugForceVictory
{
    public static void Execute()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DebugForceVictory] Not in PlayMode; aborting");
            return;
        }
#if UNITY_2023_1_OR_NEWER
        var bs = Object.FindFirstObjectByType<GameBootstrap>();
#else
        var bs = Object.FindObjectOfType<GameBootstrap>();
#endif
        if (bs == null) { Debug.LogWarning("[DebugForceVictory] No GameBootstrap instance"); return; }

        var mi = typeof(GameBootstrap).GetMethod("TriggerVictory",
                    BindingFlags.NonPublic | BindingFlags.Instance);
        if (mi == null) { Debug.LogWarning("[DebugForceVictory] TriggerVictory method not found"); return; }
        mi.Invoke(bs, null);
        Debug.Log("[DebugForceVictory] Victory triggered.");
    }

    /// 强制跳过当前过场（如果已在过场中）
    public static void Skip()
    {
        if (!Application.isPlaying) return;
#if UNITY_2023_1_OR_NEWER
        var bs = Object.FindFirstObjectByType<GameBootstrap>();
#else
        var bs = Object.FindObjectOfType<GameBootstrap>();
#endif
        if (bs != null) bs.SkipEndingCutscene();
    }
}
