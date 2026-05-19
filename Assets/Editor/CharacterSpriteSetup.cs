#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // 菜单：Tools > Game > Setup Character Sprites
    // 功能：扫描 Resources/Characters/ 和 Resources/Weapons/ 下所有 sprite_sheet.png，
    //       设置 Point 过滤 + Multiple 模式，自动检测并切割每张精灵表。
    public static class CharacterSpriteSetup
    {
        static readonly string[] RootPaths = {
            "Assets/Resources/Characters",
            "Assets/Resources/Weapons",
        };
        const float PPU = 32f;

        [MenuItem("Tools/Game/Setup Character Sprites")]
        public static void SetupAll()
        {
            int n = 0;
            foreach (var root in RootPaths)
            {
                var guids = AssetDatabase.FindAssets("sprite_sheet t:Texture2D", new[] { root });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith("/sprite_sheet.png",
                            StringComparison.OrdinalIgnoreCase)) continue;
                    SliceSheet(path);
                    n++;
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpriteSetup] 完成，已配置 {n} 张精灵表。");
        }

        // ── 单张精灵表配置 ────────────────────────────────────────────
        static void SliceSheet(string assetPath)
        {
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) return;

            // 基础导入设置
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Multiple;
            ti.filterMode          = FilterMode.Point;
            ti.textureCompression  = TextureImporterCompression.Uncompressed;
            ti.spritePixelsPerUnit = PPU;
            ti.mipmapEnabled       = false;
            ti.isReadable          = true;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) { Debug.LogWarning($"[SpriteSetup] 无法加载: {assetPath}"); return; }

            // 自动检测各精灵矩形
            var rects = AutoDetect(tex);
            if (rects == null || rects.Length == 0)
                rects = GridDetect(tex);
            if (rects == null || rects.Length == 0)
            { Debug.LogWarning($"[SpriteSetup] 未检测到精灵: {assetPath}"); return; }

            // 角色名称作为精灵名前缀（取父文件夹名）
            string charName = new DirectoryInfo(
                Path.GetDirectoryName(assetPath)).Name;

            // 构建 SpriteMetaData 数组（旧 API，兼容所有 Unity 版本）
            #pragma warning disable 618
            var metas = new List<SpriteMetaData>(rects.Length);
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                if (r.width < 8 || r.height < 8) continue;
                metas.Add(new SpriteMetaData
                {
                    name      = $"{charName}_{i:00}",
                    rect      = r,
                    alignment = (int)SpriteAlignment.Center,
                    pivot     = new Vector2(0.5f, 0.5f),
                });
            }
            ti.spritesheet = metas.ToArray();
            #pragma warning restore 618

            ti.SaveAndReimport();
            Debug.Log($"[SpriteSetup] {charName}: {metas.Count} 个精灵");
        }

        // ── Unity 内部自动检测 API（反射调用）─────────────────────────
        static Rect[] AutoDetect(Texture2D tex)
        {
            try
            {
                // Unity 内部工具类，不同版本命名空间略有差异
                var t = typeof(EditorWindow).Assembly.GetType("UnityEditor.InternalSpriteUtility")
                     ?? typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.InternalSpriteUtility");
                if (t == null) return null;

                var m = t.GetMethod("GenerateAutomaticSpriteRectangles",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) return null;

                // 尝试 3 参数版本：(Texture2D, minSize, extrude)
                try { return (Rect[])m.Invoke(null, new object[] { tex, 4, 0 }); }
                catch { }
                // 尝试 4 参数版本（旧版 Unity）
                return (Rect[])m.Invoke(null, new object[] { tex, 4, 0, 0 });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpriteSetup] 自动检测失败，改用网格模式: {e.Message}");
                return null;
            }
        }

        // ── 备用网格切割（64×64，跳过空白格子）─────────────────────────
        static Rect[] GridDetect(Texture2D tex)
        {
            const int cell = 64;
            int cols = tex.width  / cell;
            int rows = tex.height / cell;
            var list = new List<Rect>();

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int px = c * cell, py = r * cell;
                if (!HasContent(tex, px, py, cell, cell)) continue;
                // Unity Rect 原点在左下，纹理坐标原点在左上 → 翻转 Y
                list.Add(new Rect(px, tex.height - py - cell, cell, cell));
            }
            return list.ToArray();
        }

        // 采样格子内是否有非背景像素（亮度 > 0.08 即非纯黑背景）
        static bool HasContent(Texture2D tex, int x, int y, int w, int h)
        {
            int hits = 0;
            for (int py = y; py < Mathf.Min(y + h, tex.height); py += 3)
            for (int px = x; px < Mathf.Min(x + w, tex.width);  px += 3)
            {
                var c = tex.GetPixel(px, py);
                if (c.r + c.g + c.b > 0.24f && ++hits >= 8) return true;
            }
            return false;
        }
    }
}
#endif
