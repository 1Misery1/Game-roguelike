using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// 把独立的「余烬游魂」鬼魂导入为 Sprite，并赋给 HubPlayer 作为未附身时的默认外观。
public static class HubPlayerGhost
{
    const string Path = "Assets/Art/Hub/Ghost_Wanderer.png";

    public static void Execute()
    {
        AssetDatabase.Refresh();

        // 用户替换的图文件名误带了尾随空格("Ghost_Wanderer .png")，先清理成规范名。
        const string Spaced = "Assets/Art/Hub/Ghost_Wanderer .png";
        if (System.IO.File.Exists(Spaced))
        {
            string err = AssetDatabase.RenameAsset(Spaced, "Ghost_Wanderer");
            if (!string.IsNullOrEmpty(err)) Debug.LogWarning($"[HubPlayerGhost] rename: {err}");
            AssetDatabase.Refresh();
        }

        var imp = AssetImporter.GetAtPath(Path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType        = TextureImporterType.Sprite;
            imp.spriteImportMode   = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.filterMode         = FilterMode.Bilinear;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled      = false;
            imp.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path);
        var player = GameObject.Find("HubPlayer");
        if (sprite == null || player == null)
        { Debug.LogError("[HubPlayerGhost] sprite or HubPlayer missing"); return; }

        var sr = player.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = new Color(0.92f, 0.96f, 1f, 0.95f);  // 极淡冷色+微透明，保留原画本色的幽灵感

        // 归一化到 ~2.0u 高
        float h = sprite.bounds.size.y;
        if (h > 0.0001f)
        {
            float k = 2.0f / h;
            player.transform.localScale = new Vector3(k, k, 1f);
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HubPlayerGhost] assigned Ghost_Wanderer to HubPlayer.");
    }
}
