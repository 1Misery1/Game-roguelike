using UnityEngine;
using UnityEditor;

/// Forces the active Scene View into 2D orthographic mode and frames the CharacterPreview.
public class SceneView2DSetup
{
    public static void Execute()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            Debug.LogError("[SceneView2DSetup] 没有找到活动的 Scene View");
            return;
        }

        // 强制切换为 2D 正交模式
        sv.in2DMode     = true;
        sv.orthographic = true;

        // 对齐到 XY 平面（朝 -Z 看）
        sv.rotation = Quaternion.identity;
        sv.pivot    = Vector3.zero;

        // 如果场景中有 CharacterPreview，对准它
        var preview = GameObject.Find("CharacterPreview");
        if (preview != null)
        {
            Selection.activeGameObject = preview;
            sv.FrameSelected();
        }

        sv.Repaint();
        Debug.Log("[SceneView2DSetup] Scene View 已切换为 2D 正交模式");
    }
}
