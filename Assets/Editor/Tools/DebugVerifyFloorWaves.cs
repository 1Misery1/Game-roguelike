using UnityEditor;
using UnityEngine;
using Game.Data;
/// 离线检查：3 个楼层 SO 是否带齐波次数据
public static class DebugVerifyFloorWaves
{
    public static void Execute()
    {
        var all = Resources.LoadAll<FloorThemeData>("Floors");
        Debug.Log($"[Waves] 扫描到 {all.Length} 个楼层 SO");
        foreach (var t in all)
        {
            if (t == null) continue;
            string early = t.earlyEnemyWeights == null ? "?" : string.Join(",", t.earlyEnemyWeights);
            string late_ = t.lateEnemyWeights  == null ? "?" : string.Join(",", t.lateEnemyWeights);
            string pool  = "";
            if (t.roomPool != null)
                foreach (var e in t.roomPool) pool += $"{e.type}:{e.weight}  ";
            Debug.Log($"[Waves] F{t.floorNumber} {t.displayName} | elite={t.eliteChance:0.00}");
            Debug.Log($"[Waves]   early=[{early}]");
            Debug.Log($"[Waves]   late =[{late_}]");
            Debug.Log($"[Waves]   pool ={pool}");
        }
    }
}
