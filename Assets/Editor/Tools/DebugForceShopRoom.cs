using System.Reflection;
using UnityEditor;
using UnityEngine;
using Game.Bootstrap;
public static class DebugForceShopRoom
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
        var mi = typeof(GameBootstrap).GetMethod("BuildShopRoom",
            BindingFlags.NonPublic | BindingFlags.Instance);
        mi?.Invoke(bs, null);
        Debug.Log("[DebugShop] Built shop room");
    }
}
