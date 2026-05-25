using System.Collections.Generic;
using System.Text;
using Game.AI;
using UnityEditor;
using UnityEngine;

/// 验证 A* 路径会绕开危险格：以一片人造危险墙为例对比有/无危险代价时的路径长度
public static class DebugVerifyHazardAvoid
{
    public static void Execute()
    {
        // 构造一个简单地图：32 宽 20 高，全地板，中央有一行 't' 陷阱
        var rows = new string[20];
        for (int r = 0; r < 20; r++)
        {
            var sb = new StringBuilder(32);
            for (int c = 0; c < 32; c++)
            {
                bool border = r == 0 || r == 19 || c == 0 || c == 31;
                bool trapRow = r == 10 && c >= 12 && c <= 18;
                sb.Append(border ? '#' : (trapRow ? 't' : '.'));
            }
            rows[r] = sb.ToString();
        }
        NavGrid.Build(rows);

        // 从 (0,-6) 走到 (0,+6) —— 直线穿过陷阱墙
        var from = new Vector2(0f, -6f);
        var to   = new Vector2(0f,  6f);
        var path = NavGrid.FindPath(from, to);

        int cellsOnHazard = 0;
        foreach (var p in path)
        {
            var cell = NavGrid.WorldToCell(p);
            if (NavGrid.HazardAt(cell.x, cell.y) > 0) cellsOnHazard++;
        }

        Debug.Log($"[HazardAvoid] path waypoints = {path.Count}, hazard cells used = {cellsOnHazard}");
        Debug.Log($"[HazardAvoid] 通过：路径未踩到陷阱（cells=0）  失败：cellsOnHazard > 0");

        // 第二组：验证动态危险注册
        NavGrid.Build(rows);   // 重置
        var bare = NavGrid.FindPath(from, to);
        // 给同样的中央位置加动态危险
        NavGrid.AddDynamicHazard(new Vector2(0f, 0f), 3f);
        var avoided = NavGrid.FindPath(from, to);
        Debug.Log($"[HazardAvoid] 动态危险测试：未注册时路径长度={bare.Count}, 注册半径3后={avoided.Count} (后者应更长)");
    }
}
