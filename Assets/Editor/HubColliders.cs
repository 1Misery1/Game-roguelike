using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// 为大厅场景搭建碰撞体积：篝火/台座/石碑/门等障碍各加一个 BoxCollider2D，
// 并把玩家(HubController)的活动边界收紧到地板区域。
public static class HubColliders
{
    // (世界x, 世界y, 宽, 高)
    static readonly (float x, float y, float w, float h, string name)[] Boxes =
    {
        ( 0.0f, -2.0f, 1.6f, 0.9f, "Col_Campfire"),
        (-6.0f,  2.1f, 1.3f, 0.9f, "Col_Pedestal_0"),
        (-3.0f,  2.1f, 1.3f, 0.9f, "Col_Pedestal_1"),
        ( 0.0f,  2.1f, 1.3f, 0.9f, "Col_Pedestal_2"),
        ( 3.0f,  2.1f, 1.3f, 0.9f, "Col_Pedestal_3"),
        ( 6.0f,  2.1f, 1.3f, 0.9f, "Col_Pedestal_4"),
        (-9.5f, -3.4f, 1.3f, 1.8f, "Col_QuestBoard"),
        ( 9.5f, -3.4f, 1.3f, 1.8f, "Col_Records"),
        (-10.6f, 2.4f, 1.4f, 2.2f, "Col_Memorial"),
        ( 10.4f, 2.3f, 1.6f, 2.4f, "Col_LiftDoor"),
    };

    public static void Execute()
    {
        var old = GameObject.Find("Hub_Colliders");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("Hub_Colliders");
        root.transform.position = Vector3.zero;
        foreach (var b in Boxes)
        {
            var go = new GameObject(b.name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(b.x, b.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(b.w, b.h);
        }

        // 收紧玩家活动边界到地板范围（下到底部、上到地平线，刚好能贴近台座按 E）
        var hub = Object.FindObjectOfType<Game.UI.HubController>();
        if (hub != null)
        {
            var so = new SerializedObject(hub);
            var min = so.FindProperty("boundsMin");
            var max = so.FindProperty("boundsMax");
            if (min != null) min.vector2Value = new Vector2(-11f, -6.3f);
            if (max != null) max.vector2Value = new Vector2( 11f,  1.7f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogWarning("[HubColliders] HubController not found");

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[HubColliders] {Boxes.Length} colliders + player bounds set.");
    }
}
