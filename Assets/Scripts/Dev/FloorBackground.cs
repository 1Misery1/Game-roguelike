using UnityEngine;

namespace Game.Dev
{
    // 按楼层程序化生成背景纹理并嵌入场景（无需外部图片资源）
    // Floor1=灼热炼狱（岩浆裂缝） Floor2=霜境幽域（冰晶格纹） Floor3=混沌深渊（虚空漩涡）
    public static class FloorBackground
    {
        private const int W   = 160;  // 纹理宽度（像素）
        private const int H   = 90;   // 纹理高度（16:9）
        private const int PPU = 20;   // pixels per unit（用于世界尺寸换算）

        // 创建背景 GameObject 并挂载到 parent，填充 worldWidth × worldHeight 世界空间
        public static GameObject Create(int floor, Transform parent, float worldWidth, float worldHeight)
        {
            var tex    = GenerateTexture(floor);
            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PPU);

            var go = new GameObject("FloorBackground");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = -10;  // 最底层

            // 缩放以覆盖整个可见区域（略大于竞技场）
            float spriteW = W / (float)PPU;
            float spriteH = H / (float)PPU;
            float scaleX  = worldWidth  / spriteW;
            float scaleY  = worldHeight / spriteH;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            return go;
        }

        // ── 纹理生成入口 ────────────────────────────────────────────────
        private static Texture2D GenerateTexture(int floor)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };
            var px = new Color[W * H];
            switch (floor)
            {
                case 1:  FillInferno(px); break;
                case 2:  FillFrost(px);   break;
                default: FillChaos(px);   break;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ── 第1层：灼热炼狱 ── 暗红底色 + 橙色岩浆裂缝 + 热斑 ────────
        private static void FillInferno(Color[] px)
        {
            var dark   = new Color(0.10f, 0.03f, 0.01f);
            var rock   = new Color(0.18f, 0.07f, 0.03f);
            var lava   = new Color(1.00f, 0.40f, 0.03f);
            var glow   = new Color(0.75f, 0.18f, 0.01f);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float v = y / (float)(H - 1);

                // 基础岩石渐变（底部偏亮）
                float base_ = Lerp01(dark, rock, v * 0.5f + 0.1f);
                Color c     = Color.Lerp(dark, rock, base_);

                // 岩浆裂缝：多层正弦交叉
                float n1 = Mathf.Abs(Mathf.Sin(u * 14.3f + v * 4.1f));
                float n2 = Mathf.Abs(Mathf.Sin(u * 5.7f  - v * 11.3f + 1.6f));
                float n3 = Mathf.Abs(Mathf.Sin((u + v) * 9.2f + 0.8f));
                float crack = n1 * n2 + n3 * 0.4f;
                crack = Mathf.Clamp01(crack - 0.4f) / 0.6f;

                c = Color.Lerp(c, glow, crack * 0.7f);
                if (crack > 0.55f)
                    c = Color.Lerp(c, lava, (crack - 0.55f) * 2.5f);

                // 热斑：几个固定位置散发橙光
                float h1 = Hotspot(u, v, 0.18f, 0.28f, 0.22f);
                float h2 = Hotspot(u, v, 0.65f, 0.60f, 0.18f);
                float h3 = Hotspot(u, v, 0.82f, 0.20f, 0.15f);
                float h4 = Hotspot(u, v, 0.40f, 0.75f, 0.20f);
                c = Color.Lerp(c, lava, Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(h3, h4)) * 0.50f);

                // 底部更亮（地热）
                c = Color.Lerp(c, glow, (1f - v) * 0.15f);

                px[y * W + x] = c;
            }
        }

        // ── 第2层：霜境幽域 ── 深蓝底色 + 六边形冰晶格纹 + 高光 ────────
        private static void FillFrost(Color[] px)
        {
            var deep    = new Color(0.02f, 0.04f, 0.12f);
            var midBlue = new Color(0.06f, 0.10f, 0.25f);
            var iceBlue = new Color(0.55f, 0.80f, 1.00f);
            var white   = new Color(0.90f, 0.96f, 1.00f);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float v = y / (float)(H - 1);

                // 基础渐变（上方天空感更亮）
                Color c = Color.Lerp(deep, midBlue, 1f - v * 0.7f);

                // 六边形冰晶格：用3个方向的正弦模拟六边形网格
                float a1 = Mathf.Cos(u * 18f + v * 10.4f);
                float a2 = Mathf.Cos(u * 9f  - v * 15.6f);
                float a3 = Mathf.Cos((u - v)  * 12.5f);
                float hex = Mathf.Clamp01((a1 + a2 + a3 + 3f) / 6f);
                hex = Mathf.Pow(hex, 3f);

                c = Color.Lerp(c, iceBlue, hex * 0.65f);

                // 高光 sparkle（冰霜反射）
                float s = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(u * 33f) * Mathf.Cos(v * 28f)), 4f);
                c = Color.Lerp(c, white, s * 0.85f);

                // 冰柱纹路（垂直暗条）
                float pillar = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 22f)), 6f);
                c = Color.Lerp(c, deep, pillar * 0.35f);

                px[y * W + x] = c;
            }
        }

        // ── 第3层：混沌深渊 ── 极暗紫底 + 漩涡纹路 + 裂隙光线 ─────────
        private static void FillChaos(Color[] px)
        {
            var void_   = new Color(0.03f, 0.01f, 0.06f);
            var purple  = new Color(0.20f, 0.00f, 0.35f);
            var magenta = new Color(0.65f, 0.00f, 0.85f);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float v = y / (float)(H - 1);

                Color c = void_;

                // 极坐标漩涡：以画面中心为原点计算角度+半径
                float cx = u - 0.5f, cy = v - 0.5f;
                float angle = Mathf.Atan2(cy, cx);
                float dist  = Mathf.Sqrt(cx * cx + cy * cy);

                // 多层漩涡叠加
                float sw1 = Mathf.Sin(angle * 4f + dist * 18f + 1.0f);
                float sw2 = Mathf.Cos(angle * 6f - dist * 12f + 2.5f);
                float sw3 = Mathf.Sin(angle * 2f + dist * 25f);
                float swirl = Mathf.Clamp01((sw1 * sw2 + sw3 * 0.5f + 1.5f) / 3f);

                c = Color.Lerp(c, purple, swirl * 0.65f);
                if (swirl > 0.65f)
                    c = Color.Lerp(c, magenta, (swirl - 0.65f) * 2.0f);

                // 中央虚空（中心偏暗）
                float voidDepth = 1f - Mathf.Clamp01(dist * 3f);
                c = Color.Lerp(c, void_, voidDepth * 0.45f);

                // 混沌裂缝光线（高频正弦）
                float crack = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 9f + dist * 30f)), 8f);
                c = Color.Lerp(c, magenta, crack * 0.55f);

                px[y * W + x] = c;
            }
        }

        // ── 工具函数 ─────────────────────────────────────────────────────
        private static float Hotspot(float u, float v, float cx, float cy, float falloff)
            => Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy)) / falloff);

        private static float Lerp01(Color a, Color b, float t)
            => Mathf.Clamp01(t);
    }
}
