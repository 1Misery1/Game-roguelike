using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateMainMenuScene
{
    public static void Execute()
    {
        // 1. 创建新场景
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        newScene.name = "MainMenu";

        // 2. 添加 GameManager GameObject
        var gmGO = new GameObject("GameManager");
        SceneManager.MoveGameObjectToScene(gmGO, newScene);
        gmGO.AddComponent<Game.Core.GameManager>();

        // 3. 添加 Menu GameObject（MenuController）
        var menuGO = new GameObject("Menu");
        SceneManager.MoveGameObjectToScene(menuGO, newScene);
        menuGO.AddComponent<Game.UI.MenuController>();

        // 4. 保存场景到磁盘
        string scenePath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        // 5. 加入 Build Settings（MainMenu 为 index 0，Test 为 index 1）
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Test.unity", true),
        };
        EditorBuildSettings.scenes = scenes;

        Debug.Log("[CreateMainMenuScene] MainMenu.unity created and added to Build Settings.");

        // 6. 关闭临时叠加的场景（切回 Test 场景）
        EditorSceneManager.CloseScene(newScene, false);
    }
}
