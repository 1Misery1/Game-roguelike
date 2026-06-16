using UnityEngine;

namespace Game.Bootstrap
{
    /// 训练场「可视化布局」锚点。挂在 Training 场景里一个空物体上，运行时由 TrainingBootstrap
    /// 读取这些锚点来生成内容（仍是代码生成，锚点只提供坐标）。在 Scene 视图里拖动子锚点即可
    /// 调整：玩家出生点、假人出生点、可走边界、返回营地传送点。缺锚点时 Bootstrap 回退到内置值。
    public class TrainingLayout : MonoBehaviour
    {
        [Header("出生 / 出口锚点（拖动即可）")]
        [Tooltip("玩家进场出生点")]               public Transform playerSpawn;
        [Tooltip("返回营地的传送/出口垫位置")]      public Transform returnPoint;
        [Tooltip("木桩假人出生点；为空则按数量自动横向铺开")]
        public Transform[] dummySpawns;

        [Header("可走边界（以本物体为中心的矩形半尺寸）")]
        [Tooltip("可走区域半宽/半高（隐形边墙范围）")]
        public Vector2 boundaryHalf = new Vector2(9.6f, 7.2f);

        // ── 运行时访问器（Bootstrap 调用）─────────────────────────────
        public Vector3 BoundaryCenter => transform.position;

        public bool HasPlayerSpawn => playerSpawn != null;
        public bool HasReturnPoint => returnPoint != null;

        public int DummyCount
        {
            get
            {
                if (dummySpawns == null) return 0;
                int n = 0; foreach (var t in dummySpawns) if (t != null) n++;
                return n;
            }
        }

#if UNITY_EDITOR
        // 锚点 + 标签 + 轮廓（仅编辑器可视化，不影响运行）
        private void OnDrawGizmos()
        {
            Vector3 c = transform.position;

            // 可走边界轮廓（黄框）
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(c, new Vector3(boundaryHalf.x * 2f, boundaryHalf.y * 2f, 0f));
            UnityEditor.Handles.color = new Color(1f, 0.85f, 0.3f, 1f);
            UnityEditor.Handles.Label(c + new Vector3(-boundaryHalf.x, boundaryHalf.y + 0.3f, 0f), "Play Boundary");

            DrawAnchor(playerSpawn, new Color(0.4f, 0.85f, 1f), "Player Spawn", 0.5f);
            DrawAnchor(returnPoint, new Color(0.95f, 0.7f, 0.35f), "Return Point", 0.9f);

            if (dummySpawns != null)
                for (int i = 0; i < dummySpawns.Length; i++)
                    DrawAnchor(dummySpawns[i], new Color(0.95f, 0.45f, 0.45f), "Dummy " + i, 0.46f);
        }

        private static void DrawAnchor(Transform t, Color col, string label, float radius)
        {
            if (t == null) return;
            Gizmos.color = col;
            Gizmos.DrawWireSphere(t.position, radius);
            Gizmos.DrawLine(t.position + Vector3.left * radius, t.position + Vector3.right * radius);
            Gizmos.DrawLine(t.position + Vector3.up * radius, t.position + Vector3.down * radius);
            UnityEditor.Handles.color = col;
            UnityEditor.Handles.Label(t.position + new Vector3(radius + 0.1f, radius + 0.1f, 0f), label);
        }
#endif
    }
}
