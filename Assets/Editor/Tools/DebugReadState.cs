using System.Reflection;
using Game.Dev;
using UnityEditor;
using UnityEngine;

public static class DebugReadState
{
    public static void Execute()
    {
        if (!Application.isPlaying) { Debug.Log("[State] Not playing"); return; }
#if UNITY_2023_1_OR_NEWER
        var bs = Object.FindFirstObjectByType<GameBootstrap>();
#else
        var bs = Object.FindObjectOfType<GameBootstrap>();
#endif
        if (bs == null) { Debug.Log("[State] No GameBootstrap"); return; }

        var t = typeof(GameBootstrap);
        var stateField     = t.GetField("_state",             BindingFlags.NonPublic | BindingFlags.Instance);
        var isTrueField    = t.GetField("_isTrueEnding",      BindingFlags.NonPublic | BindingFlags.Instance);
        var truthField     = t.GetField("_endingTruthCount",  BindingFlags.NonPublic | BindingFlags.Instance);
        var durField       = t.GetField("_cutsceneDuration",  BindingFlags.NonPublic | BindingFlags.Instance);
        var startField     = t.GetField("_cutsceneStartTime", BindingFlags.NonPublic | BindingFlags.Instance);

        string state   = stateField  != null ? stateField.GetValue(bs).ToString()   : "?";
        bool   isTrue  = isTrueField != null && (bool)isTrueField.GetValue(bs);
        int    truth   = truthField  != null ? (int)truthField.GetValue(bs)         : -1;
        float  dur     = durField    != null ? (float)durField.GetValue(bs)         : -1f;
        float  start   = startField  != null ? (float)startField.GetValue(bs)       : -1f;
        float  elapsed = Time.unscaledTime - start;

        Debug.Log($"[State] state={state} isTrueEnding={isTrue} truthCount={truth} cutsceneDur={dur:0.00}s elapsed={elapsed:0.00}s");
    }
}
