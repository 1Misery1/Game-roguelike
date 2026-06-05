using System.Collections.Generic;
using UnityEngine;

namespace Game.Art
{
    // 程序化绘制 32×32 像素交互物图标（与 WeaponSprites / MapBuilder 同款手绘风格）。
    // 替换原本「纯色方块占位」：天赋宝石 / 药水 / 锻造铁砧 / 附魔水晶 / 天赋之书 / 出口门 / 神秘祭坛。
    // 透明底（Color32 默认 a=0），缓存复用；天赋宝石画成灰度 → 由 SpriteRenderer.color 按天赋色着色。
    public static class InteractableSprites
    {
        const int SZ = 32;
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Gem()            => Cached("gem",     DrawGem);
        public static Sprite Potion()         => Cached("potion",  DrawPotion);
        public static Sprite Anvil()          => Cached("anvil",   DrawAnvil);
        public static Sprite EnchantCrystal() => Cached("enchant", DrawEnchant);
        public static Sprite Tome()           => Cached("tome",    DrawTome);
        public static Sprite Door()           => Cached("door",    DrawDoor);
        public static Sprite Altar()          => Cached("altar",   DrawAltar);

        static Sprite Cached(string key, System.Action<Color32[], int> draw)
        {
            // s != null 用 Unity 重载的 == 排除「已销毁」的旧缓存（关闭域重载时跨 Play 会出现），命中则重新生成
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[SZ * SZ];
            draw(px, SZ);
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
            _cache[key] = s;
            return s;
        }

        // ── 天赋宝石：灰度切面菱形（由 sr.color 着色为各天赋颜色）──────────────
        static void DrawGem(Color32[] p, int z)
        {
            int cx = 16;
            Color32 D = C(70, 72, 86), L = C(225, 228, 240), M = C(196, 199, 214),
                    S = C(165, 168, 184), H = C(252, 253, 255);
            for (int y = 6; y <= 26; y++)
            {
                int hw = Mathf.RoundToInt(9f * (1f - Mathf.Abs(y - 16) / 10f));
                if (hw < 1) hw = 1;
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    int d = x - cx;
                    Color32 c;
                    if (x == cx - hw || x == cx + hw) c = D;
                    else if (d <= -2) c = (d <= -hw + 1) ? H : L;
                    else if (d <= 0)  c = M;
                    else              c = (d >= hw - 1) ? D : S;
                    P(p, z, x, y, c);
                }
            }
            for (int y = 16; y <= 25; y++) P(p, z, cx, y, M);     // 中心切面线
            P(p, z, cx - 3, 20, H); P(p, z, cx - 4, 19, H);       // 高光闪点
        }

        // ── 生命药水：红液玻璃瓶 + 木塞 ───────────────────────────────────────
        static void DrawPotion(Color32[] p, int z)
        {
            Color32 glass = C(206, 226, 233), gdk = C(150, 180, 190),
                    liq = C(222, 58, 64), ldk = C(165, 30, 45), lhi = C(255, 120, 120),
                    cork = C(126, 84, 48), ckhi = C(158, 110, 66), shine = C(255, 255, 255, 220);
            Circle(p, z, 16, 11, 6, gdk);
            Circle(p, z, 16, 11, 5, glass);
            for (int y = 6; y <= 13; y++)
            for (int x = 10; x <= 22; x++)
            {
                int dx = x - 16, dy = y - 11;
                if (dx * dx + dy * dy <= 25) P(p, z, x, y, y >= 13 ? lhi : (y <= 7 ? ldk : liq));
            }
            Rect(p, z, 14, 15, 17, 20, glass); Line(p, z, 14, 15, 14, 20, gdk); Line(p, z, 17, 15, 17, 20, gdk);
            Rect(p, z, 13, 20, 18, 24, cork);  Rect(p, z, 13, 23, 18, 24, ckhi);
            P(p, z, 13, 13, shine); P(p, z, 12, 11, shine); P(p, z, 12, 12, shine);
        }

        // ── 锻造铁砧：铁色砧身 + 砧角 + 火星 ──────────────────────────────────
        static void DrawAnvil(Color32[] p, int z)
        {
            Color32 ir = C(66, 68, 80), hi = C(112, 115, 130), dk = C(40, 42, 52),
                    face = C(92, 95, 110), em = C(245, 150, 45);
            Rect(p, z, 8, 21, 24, 24, ir);          // 顶面
            Line(p, z, 8, 24, 24, 24, hi);
            Rect(p, z, 9, 22, 23, 23, face);
            P(p, z, 7, 22, ir); P(p, z, 6, 22, ir); P(p, z, 5, 21, ir); P(p, z, 7, 23, face); // 砧角
            Rect(p, z, 22, 19, 24, 21, ir);          // 右台阶
            Rect(p, z, 13, 15, 18, 21, ir);          // 腰
            Line(p, z, 13, 15, 13, 21, dk);
            Rect(p, z, 9, 9, 22, 15, ir);            // 底座
            Rect(p, z, 10, 10, 21, 14, face);
            Line(p, z, 9, 9, 22, 9, dk);
            Rect(p, z, 8, 8, 11, 9, ir); Rect(p, z, 20, 8, 23, 9, ir);  // 脚
            P(p, z, 12, 26, em); P(p, z, 14, 27, C(255, 200, 80)); P(p, z, 17, 26, em); P(p, z, 15, 28, em);
        }

        // ── 附魔水晶：紫色晶柱 + 光晕 + 石座 ─────────────────────────────────
        static void DrawEnchant(Color32[] p, int z)
        {
            Color32 cr = C(150, 80, 225), hi = C(205, 160, 255), dk = C(95, 40, 160),
                    glow = C(170, 100, 240, 90), spk = C(235, 215, 255),
                    bse = C(70, 60, 90), bhi = C(110, 95, 135);
            Circle(p, z, 16, 18, 9, glow);
            for (int y = 8; y <= 28; y++)
            {
                int hw = y < 18 ? (y - 7) / 2 : (28 - y) / 2 + 1;
                hw = Mathf.Clamp(hw, 1, 5);
                for (int x = 16 - hw; x <= 16 + hw; x++)
                    P(p, z, x, y, x < 16 ? hi : (x > 16 ? dk : cr));
            }
            for (int y = 9; y <= 27; y++) P(p, z, 16, y, cr);
            P(p, z, 16, 28, spk); P(p, z, 15, 26, hi);
            P(p, z, 10, 22, spk); P(p, z, 23, 20, spk); P(p, z, 21, 25, spk); P(p, z, 11, 15, spk);
            Rect(p, z, 11, 5, 21, 8, bse); Line(p, z, 11, 8, 21, 8, bhi);
        }

        // ── 天赋之书：紫皮金纹符文书 ─────────────────────────────────────────
        static void DrawTome(Color32[] p, int z)
        {
            Color32 cov = C(78, 56, 140), cdk = C(50, 34, 100), edge = C(212, 180, 84),
                    pg = C(232, 222, 188), pgdk = C(196, 184, 150),
                    rune = C(245, 225, 120), glow = C(255, 240, 160, 150);
            Rect(p, z, 9, 6, 24, 27, pg);                         // 书页
            for (int y = 8; y <= 25; y += 3) P(p, z, 23, y, pgdk);
            Line(p, z, 9, 6, 24, 6, pgdk);
            Rect(p, z, 7, 7, 22, 28, cov);                        // 封面
            Rect(p, z, 7, 7, 9, 28, cdk);                         // 书脊
            Line(p, z, 7, 28, 22, 28, edge); Line(p, z, 7, 7, 22, 7, edge); Line(p, z, 22, 7, 22, 28, edge);
            Circle(p, z, 15, 17, 4, glow);                        // 符文光
            P(p, z, 15, 21, rune); P(p, z, 15, 20, rune); P(p, z, 15, 14, rune); P(p, z, 15, 13, rune);
            P(p, z, 12, 17, rune); P(p, z, 13, 17, rune); P(p, z, 17, 17, rune); P(p, z, 18, 17, rune);
            P(p, z, 15, 17, rune);
            P(p, z, 14, 16, edge); P(p, z, 16, 16, edge); P(p, z, 14, 18, edge); P(p, z, 16, 18, edge);
        }

        // ── 出口门：石拱 + 绿色能量传送门 ───────────────────────────────────
        static void DrawDoor(Color32[] p, int z)
        {
            Color32 st = C(96, 90, 82), sthi = C(132, 126, 116), stdk = C(60, 56, 50),
                    g1 = C(40, 180, 90), g2 = C(120, 255, 165), gdk = C(24, 110, 60), spk = C(210, 255, 225);
            Rect(p, z, 7, 2, 24, 29, st);                         // 石框
            P(p, z, 7, 29, Clear); P(p, z, 8, 29, Clear); P(p, z, 7, 28, Clear);   // 圆顶角
            P(p, z, 24, 29, Clear); P(p, z, 23, 29, Clear); P(p, z, 24, 28, Clear);
            Line(p, z, 7, 2, 7, 28, sthi); Line(p, z, 24, 2, 24, 28, stdk);
            for (int y = 4; y <= 27; y++)
            for (int x = 9; x <= 22; x++)
            {
                if (y > 25 && (x < 11 || x > 20)) continue;       // 拱顶收口
                float t = Mathf.Abs(x - 15) / 7f + (27 - y) / 40f;
                P(p, z, x, y, t < 0.35f ? g2 : (t < 0.7f ? g1 : gdk));
            }
            P(p, z, 15, 16, spk); P(p, z, 13, 12, spk); P(p, z, 18, 20, spk);
            P(p, z, 12, 22, g2); P(p, z, 19, 14, g2);
            Rect(p, z, 8, 2, 23, 3, stdk);                        // 门槛
        }

        // ── 神秘祭坛：石台 + 悬浮紫光球 ─────────────────────────────────────
        static void DrawAltar(Color32[] p, int z)
        {
            Color32 st = C(120, 112, 128), sthi = C(160, 150, 172), stdk = C(78, 72, 90),
                    orb = C(180, 90, 230), ohi = C(225, 170, 255), odk = C(120, 50, 170),
                    glow = C(190, 110, 240, 110), spk = C(240, 225, 255);
            Rect(p, z, 9, 3, 22, 7, st);                          // 台基
            Line(p, z, 9, 7, 22, 7, sthi); Line(p, z, 9, 3, 22, 3, stdk);
            Rect(p, z, 11, 7, 20, 10, st);
            Rect(p, z, 10, 10, 21, 12, stdk); Line(p, z, 10, 12, 21, 12, sthi);  // 台缘
            Circle(p, z, 16, 20, 8, glow);                        // 光晕
            Circle(p, z, 16, 20, 5, odk);                         // 悬浮球
            Circle(p, z, 16, 20, 4, orb);
            Circle(p, z, 16, 21, 2, ohi);
            P(p, z, 14, 22, spk);
            P(p, z, 9, 20, spk); P(p, z, 23, 21, spk); P(p, z, 16, 27, spk);
            P(p, z, 20, 25, orb); P(p, z, 12, 25, orb);
        }

        // ── 绘制工具（与 WeaponSprites 同款）─────────────────────────────────
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        static void Rect(Color32[] p, int z, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(z - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(z - 1, x1); x++) p[y * z + x] = c;
        }
        static void Circle(Color32[] p, int z, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(z - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(z - 1, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) p[y * z + x] = c;
        }
        static void Line(Color32[] p, int z, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, e = dx - dy;
            for (; ; )
            {
                if (x0 >= 0 && x0 < z && y0 >= 0 && y0 < z) p[y0 * z + x0] = c;
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * e; if (e2 > -dy) { e -= dy; x0 += sx; } if (e2 < dx) { e += dx; y0 += sy; }
            }
        }
        static void P(Color32[] p, int z, int x, int y, Color32 c)
        { if (x >= 0 && x < z && y >= 0 && y < z) p[y * z + x] = c; }
        static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);
    }
}
