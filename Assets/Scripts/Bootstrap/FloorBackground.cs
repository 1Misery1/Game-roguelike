using UnityEngine;
using Game.Data;
namespace Game.Bootstrap
{
    // Generates procedural background texture by floor (256×144, larger than original 160×90)
    // Floor1=Inferno  Floor2=Frost Realm  Floor3=Chaos Abyss
    // Background at sortingOrder=-10, visible only behind walls and map borders
    public static class FloorBackground
    {
        const int W   = 256;
        const int H   = 144;
        const int PPU = 20;

        /// 旧接口：根据楼层编号生成程序化背景
        public static GameObject Create(int floor, Transform parent, float worldWidth, float worldHeight)
            => CreateInternal(KindFromFloor(floor), null, parent, worldWidth, worldHeight);

        /// 新接口：按 FloorThemeData 生成（None = 不生成；Custom = 用 theme.customBackground）
        public static GameObject Create(FloorThemeData theme, Transform parent, float worldWidth, float worldHeight)
        {
            if (theme == null) return null;
            if (theme.proceduralKind == FloorProceduralKind.None) return null;
            return CreateInternal(theme.proceduralKind, theme.customBackground, parent, worldWidth, worldHeight);
        }

        static GameObject CreateInternal(FloorProceduralKind kind, Sprite customSprite,
                                         Transform parent, float worldWidth, float worldHeight)
        {
            Sprite sprite;
            if (kind == FloorProceduralKind.Custom && customSprite != null)
                sprite = customSprite;
            else
            {
                var tex = GenerateTexture(kind);
                sprite  = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PPU);
            }

            var go = new GameObject("FloorBackground");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = -10;

            // 自定义精灵按其原始大小填房间；程序化纹理按 W/PPU 已知尺寸
            float refW = sprite.rect.width  / sprite.pixelsPerUnit;
            float refH = sprite.rect.height / sprite.pixelsPerUnit;
            go.transform.localScale = new Vector3(worldWidth / refW, worldHeight / refH, 1f);
            return go;
        }

        static FloorProceduralKind KindFromFloor(int floor)
        {
            switch (floor)
            {
                case 1:  return FloorProceduralKind.Inferno;
                case 2:  return FloorProceduralKind.Frost;
                default: return FloorProceduralKind.Chaos;
            }
        }

        static Texture2D GenerateTexture(FloorProceduralKind kind)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };
            var px = new Color[W * H];
            switch (kind)
            {
                case FloorProceduralKind.Inferno: FillInferno(px); break;
                case FloorProceduralKind.Frost:   FillFrost(px);   break;
                default:                          FillChaos(px);   break;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ── Floor 1: Inferno ─────────────────────────────────────────────────
        // Dark-red rock base + orange multi-layer lava cracks + hotspots + geothermal gradient
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

                // Three-layer crossing lava cracks (higher frequency)
                float n1 = Mathf.Abs(Mathf.Sin(u * 18.5f + v * 5.2f));
                float n2 = Mathf.Abs(Mathf.Sin(u * 7.3f  - v * 14.1f + 1.6f));
                float n3 = Mathf.Abs(Mathf.Sin((u + v) * 11.8f + 0.8f));
                float n4 = Mathf.Abs(Mathf.Sin(u * 22f  + v * 3.8f + 2.1f));
                float crack = n1 * n2 * 1.3f + n3 * n4 * 0.5f;
                crack = Mathf.Clamp01(crack - 0.38f) / 0.62f;

                c = Color.Lerp(c, glow, crack * 0.72f);
                if (crack > 0.5f)
                    c = Color.Lerp(c, lava, (crack - 0.5f) * 2.8f);

                // 4 hotspots (orange-glow clusters)
                float h1 = Hotspot(u, v, 0.15f, 0.25f, 0.20f);
                float h2 = Hotspot(u, v, 0.62f, 0.60f, 0.17f);
                float h3 = Hotspot(u, v, 0.82f, 0.20f, 0.14f);
                float h4 = Hotspot(u, v, 0.40f, 0.78f, 0.18f);
                float h5 = Hotspot(u, v, 0.90f, 0.75f, 0.13f);
                float hotMax = Mathf.Max(Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(h3, h4)), h5);
                c = Color.Lerp(c, lava, hotMax * 0.45f);

                // Bottom geothermal (brighter at base)
                c = Color.Lerp(c, glow, (1f - v) * 0.18f);

                // Floor tile seams (dark grid, matching tile size)
                float gx = Mathf.Abs(Mathf.Sin(u * W * Mathf.PI / 1f));
                float gy = Mathf.Abs(Mathf.Sin(v * H * Mathf.PI / 1f));
                if (gx < 0.05f || gy < 0.05f) c = Color.Lerp(c, dark, 0.25f);

                px[y * W + x] = c;
            }
        }

        // ── Floor 2: Frost Realm ─────────────────────────────────────────────
        // Deep-blue base + hexagonal ice crystal grid + sparkle highlights + ice pillar streaks
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

                // Three-direction sine waves simulate hexagonal grid
                float a1 = Mathf.Cos(u * 22f + v * 12.7f);
                float a2 = Mathf.Cos(u * 11f - v * 19.1f);
                float a3 = Mathf.Cos((u - v) * 15.4f);
                float hex = Mathf.Clamp01((a1 + a2 + a3 + 3f) / 6f);
                hex = Mathf.Pow(hex, 3.2f);
                c = Color.Lerp(c, iceBlue, hex * 0.68f);

                // Sparkle highlights (ice and snow reflection)
                float s = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(u * 41f) * Mathf.Cos(v * 35f)), 5f);
                c = Color.Lerp(c, white, s * 0.88f);

                // Vertical ice pillar dark streaks
                float pillar = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 28f)), 7f);
                c = Color.Lerp(c, deep, pillar * 0.32f);

                // Horizontal ice layer strata
                float layer = Mathf.Pow(Mathf.Abs(Mathf.Sin(v * 18f + u * 4f)), 6f);
                c = Color.Lerp(c, iceBlue, layer * 0.22f);

                // Floor tile seams
                float gx = Mathf.Abs(Mathf.Sin(u * W * Mathf.PI / 1f));
                float gy = Mathf.Abs(Mathf.Sin(v * H * Mathf.PI / 1f));
                if (gx < 0.04f || gy < 0.04f) c = Color.Lerp(c, deep, 0.28f);

                px[y * W + x] = c;
            }
        }

        // ── Floor 3: Chaos Abyss ─────────────────────────────────────────────
        // Ultra-dark purple base + polar swirl + high-freq chaos crack rays + central void
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

                // Four-layer swirl overlay (richer composition)
                float sw1 = Mathf.Sin(angle * 5f + dist * 22f + 1.0f);
                float sw2 = Mathf.Cos(angle * 7f - dist * 15f + 2.5f);
                float sw3 = Mathf.Sin(angle * 3f + dist * 30f);
                float sw4 = Mathf.Cos(angle * 9f - dist * 10f + 4.2f);
                float swirl = Mathf.Clamp01((sw1 * sw2 + sw3 * sw4 * 0.5f + 2f) / 4f);

                c = Color.Lerp(c, purple, swirl * 0.68f);
                if (swirl > 0.60f)
                    c = Color.Lerp(c, magenta, (swirl - 0.60f) * 2.2f);

                // Central void abyss
                float voidDepth = 1f - Mathf.Clamp01(dist * 3.2f);
                c = Color.Lerp(c, void_, voidDepth * 0.42f);

                // Chaos crack radial rays (high frequency)
                float crack1 = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 10f + dist * 35f)), 9f);
                float crack2 = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 6f  - dist * 20f + 1.5f)), 8f);
                c = Color.Lerp(c, magenta, (crack1 + crack2 * 0.5f) * 0.5f);

                // Floor tile seams (dark-purple tint)
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
