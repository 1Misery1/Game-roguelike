using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// 让编辑器里点 Play 时总是先从 Title（开始界面）启动，而不是当前打开的场景。
/// 默认开启；可通过菜单「Tools/启动场景/点 Play 从 Title 开始」开关。
/// 关闭后恢复 Unity 默认行为：Play 直接跑当前打开的场景（方便单独调试演武场等）。
[InitializeOnLoad]
public static class PlayModeStartScene
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string PrefKey  = "Game.BootFromTitle";
    private const string MenuPath = "Tools/启动场景/点 Play 从 Title 开始";

    static PlayModeStartScene()
    {
        // 编辑器加载 / 脚本重编译后应用一次（延迟到资源数据库就绪）。
        EditorApplication.delayCall += Apply;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true); // 默认开启
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void Apply()
    {
        if (Enabled)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
            if (scene != null)
            {
                EditorSceneManager.playModeStartScene = scene;
            }
            else
            {
                Debug.LogWarning($"[PlayModeStartScene] 找不到 {TitleScenePath}，无法设置 Play 启动场景。");
            }
        }
        else
        {
            // 还原为 Unity 默认：从当前打开的场景启动。
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
