using UnityEngine;

namespace Game.Dev
{
    // 按楼层生成程序化背景纹理（256×144，高于原160×90）
    // Floor1=炼狱灼热  Floor2=霜境幽域  Floor3=混沌深渊
    // 背景位于 sortingOrder=-10，只在墙体和地图边缘可见
    public static class FloorBackground
    {
        const int W   = 256;
        const int H   = 144;
        const int PPU = 20;

        public static GameObject Create(int floor, Transform parent, float worldWidth, float worldHeight)
        {
            var tex    = GenerateTexture(floor);
            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PPU);

            var go = new GameObject("FloorBackground");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = -10;

            float scaleX = worldWidth  / (W / (float)PPU);
            float scaleY = worldHeight / (H / (float)PPU);
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            return go;
        }

        static Texture2D GenerateTexture(int floor)
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

        // ── 第1层：炼狱 ─────────────────────────────────────────────────────
        // 暗红岩石底色 + 橙色多层岩浆裂缝 + 热斑 + 地热渐变
        static void FillInferno(Color[] px)
        {
            var dark = new Color(0.08f, 0.02f, 0.01f);
            var rock = new Color(0.16f, 0.06f, 0.02f);
            var lava = new Color(1.00f, 0.38f, 0.02f);
            var glow = new Color(0.72f, 0.16f, 0.01f);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float v = y / (float)(H - 1);

                Color c = Color.Lerp(dark, rock, v * 0.5f + 0.08f);

                // 三层交叉岩浆裂缝（更高频）
                float n1 = Mathf.Abs(Mathf.Sin(u * 18.5f + v * 5.2f));
                float n2 = Mathf.Abs(Mathf.Sin(u * 7.3f  - v * 14.1f + 1.6f));
                float n3 = Mathf.Abs(Mathf.Sin((u + v) * 11.8f + 0.8f));
                float n4 = Mathf.Abs(Mathf.Sin(u * 22f  + v * 3.8f + 2.1f));
                float crack = n1 * n2 * 1.3f + n3 * n4 * 0.5f;
                crack = Mathf.Clamp01(crack - 0.38f) / 0.62f;

                c = Color.Lerp(c, glow, crack * 0.72f);
                if (crack > 0.5f)
                    c = Color.Lerp(c, lava, (crack - 0.5f) * 2.8f);

                // 4个热斑（散发橙光的聚集点）
                float h1 = Hotspot(u, v, 0.15f, 0.25f, 0.20f);
                float h2 = Hotspot(u, v, 0.62f, 0.60f, 0.17f);
                float h3 = Hotspot(u, v, 0.82f, 0.20f, 0.14f);
                float h4 = Hotspot(u, v, 0.40f, 0.78f, 0.18f);
                float h5 = Hotspot(u, v, 0.90f, 0.75f, 0.13f);
                float hotMax = Mathf.Max(Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(h3, h4)), h5);
                c = Color.Lerp(c, lava, hotMax * 0.45f);

                // 底部地热（低部更亮）
                c = Color.Lerp(c, glow, (1f - v) * 0.18f);

                // 地板格缝（暗格纹，呼应瓦片大小）
                float gx = Mathf.Abs(Mathf.Sin(u * W * Mathf.PI / 1f));
                float gy = Mathf.Abs(Mathf.Sin(v * H * Mathf.PI / 1f));
                if (gx < 0.05f || gy < 0.05f) c = Color.Lerp(c, dark, 0.25f);

                px[y * W + x] = c;
            }
        }

        // ── 第2层：霜境 ─────────────────────────────────────────────────────
        // 深蓝底色 + 六边形冰晶格纹 + 高光雪花 + 冰柱纹路
        static void FillFrost(Color[] px)
        {
            var deep    = new Color(0.02f, 0.03f, 0.10f);
            var midBlue = new Color(0.05f, 0.09f, 0.22f);
            var iceBlue = new Color(0.50f, 0.75f, 0.98f);
            var white   = new Color(0.88f, 0.94f, 1.00f);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float v = y / (float)(H - 1);

                Color c = Color.Lerp(deep, midBlue, 1f - v * 0.65f);

                // 三方向正弦模拟六边形格
                float a1 = Mathf.Cos(u * 22f + v * 12.7f);
                float a2 = Mathf.Cos(u * 11f - v * 19.1f);
                float a3 = Mathf.Cos((u - v) * 15.4f);
                float hex = Mathf.Clamp01((a1 + a2 + a3 + 3f) / 6f);
                hex = Mathf.Pow(hex, 3.2f);
                c = Color.Lerp(c, iceBlue, hex * 0.68f);

                // 高光 sparkle（冰雪反射）
                float s = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(u * 41f) * Mathf.Cos(v * 35f)), 5f);
                c = Color.Lerp(c, white, s * 0.88f);

                // 竖向冰柱暗条
                float pillar = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 28f)), 7f);
                c = Color.Lerp(c, deep, pillar * 0.32f);

                // 水平结冰层纹
                float layer = Mathf.Pow(Mathf.Abs(Mathf.Sin(v * 18f + u * 4f)), 6f);
                c = Color.Lerp(c, iceBlue, layer * 0.22f);

                // 地板格缝
                float gx = Mathf.Abs(Mathf.Sin(u * W * Mathf.PI / 1f));
                float gy = Mathf.Abs(Mathf.Sin(v * H * Mathf.PI / 1f));
                if (gx < 0.04f || gy < 0.04f) c = Color.Lerp(c, deep, 0.28f);

                px[y * W + x] = c;
            }
        }

        // ── 第3层：混沌深渊 ─────────────────────────────────────────────────
        // 极暗紫底 + 极坐标漩涡 + 高频混沌裂缝光线 + 中央虚空
        static void FillChaos(Color[] px)
        {
            var void_   = new Color(0.02f, 0.01f, 0.05f);
            var purple  = new Color(0.18f, 0.00f, 0.32f);
            var magenta = new Color(0.62f, 0.00f, 0.82f);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float v = y / (float)(H - 1);

                Color c = void_;
                float cx = u - 0.5f, cy = v - 0.5f;
                float angle = Mathf.Atan2(cy, cx);
                float dist  = Mathf.Sqrt(cx * cx + cy * cy);

                // 四层漩涡叠加（更丰富）
                float sw1 = Mathf.Sin(angle * 5f + dist * 22f + 1.0f);
                float sw2 = Mathf.Cos(angle * 7f - dist * 15f + 2.5f);
                float sw3 = Mathf.Sin(angle * 3f + dist * 30f);
                float sw4 = Mathf.Cos(angle * 9f - dist * 10f + 4.2f);
                float swirl = Mathf.Clamp01((sw1 * sw2 + sw3 * sw4 * 0.5f + 2f) / 4f);

                c = Color.Lerp(c, purple, swirl * 0.68f);
                if (swirl > 0.60f)
                    c = Color.Lerp(c, magenta, (swirl - 0.60f) * 2.2f);

                // 中央虚空深渊
                float voidDepth = 1f - Mathf.Clamp01(dist * 3.2f);
                c = Color.Lerp(c, void_, voidDepth * 0.42f);

                // 混沌裂缝放射光线（高频）
                float crack1 = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 10f + dist * 35f)), 9f);
                float crack2 = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 6f  - dist * 20f + 1.5f)), 8f);
                c = Color.Lerp(c, magenta, (crack1 + crack2 * 0.5f) * 0.5f);

                // 地板格缝（暗紫色调）
                float gx = Mathf.Abs(Mathf.Sin(u * W * Mathf.PI / 1f));
                float gy = Mathf.Abs(Mathf.Sin(v * H * Mathf.PI / 1f));
                if (gx < 0.04f || gy < 0.04f) c = Color.Lerp(c, void_, 0.35f);

                px[y * W + x] = c;
            }
        }

        static float Hotspot(float u, float v, float cx, float cy, float falloff)
            => Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy)) / falloff);
    }
}
