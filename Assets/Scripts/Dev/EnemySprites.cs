using Game.AI;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Dev
{
    // 程序化生成 32×32 像素风格敌人精灵图（全部 15 种敌人）
    // y=0 在贴图底部 = 世界空间底部，缓存后复用
    public static class EnemySprites
    {
        private static readonly Dictionary<EnemyType, Sprite> _cache = new Dictionary<EnemyType, Sprite>();

        public static Sprite Get(EnemyType type)
        {
            if (_cache.TryGetValue(type, out var s)) return s;
            s = Build(type);
            _cache[type] = s;
            return s;
        }

        private static Sprite Build(EnemyType type)
        {
            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            switch (type)
            {
                // ── 小怪 ──────────────────────────────────────────────
                case EnemyType.Skeleton:       DrawSkeleton(px, SZ);       break;
                case EnemyType.Soldier:        DrawSoldier(px, SZ);        break;
                case EnemyType.Archer:         DrawArcher(px, SZ);         break;
                case EnemyType.Bat:            DrawBat(px, SZ);            break;
                case EnemyType.ShieldGuard:    DrawShieldGuard(px, SZ);    break;
                case EnemyType.PoisonSpider:   DrawPoisonSpider(px, SZ);   break;
                case EnemyType.ShadowAssassin: DrawShadowAssassin(px, SZ); break;
                case EnemyType.ExplosiveDemon: DrawExplosiveDemon(px, SZ); break;
                // ── 精英 ──────────────────────────────────────────────
                case EnemyType.Commander:      DrawCommander(px, SZ);      break;
                case EnemyType.Witch:          DrawWitch(px, SZ);          break;
                case EnemyType.PoisonShaman:   DrawPoisonShaman(px, SZ);   break;
                case EnemyType.Necromancer:    DrawNecromancer(px, SZ);    break;
                // ── Boss ──────────────────────────────────────────────
                case EnemyType.HellGiant:      DrawHellGiant(px, SZ);      break;
                case EnemyType.FrostLich:      DrawFrostLich(px, SZ);      break;
                case EnemyType.ChaosLord:      DrawChaosLord(px, SZ);      break;
                default: return null;
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ / 2);
        }

        // ── 绘图原语 ──────────────────────────────────────────────────

        static void Circle(Color32[] px, int sz, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(sz - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(sz - 1, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2)
                    px[y * sz + x] = c;
        }

        static void Oval(Color32[] px, int sz, int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y = Mathf.Max(0, cy - ry); y <= Mathf.Min(sz - 1, cy + ry); y++)
            for (int x = Mathf.Max(0, cx - rx); x <= Mathf.Min(sz - 1, cx + rx); x++)
            {
                float dx = (x - cx) / (float)rx, dy = (y - cy) / (float)ry;
                if (dx * dx + dy * dy <= 1f) px[y * sz + x] = c;
            }
        }

        static void Rect(Color32[] px, int sz, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(sz - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(sz - 1, x1); x++)
                px[y * sz + x] = c;
        }

        static void Line(Color32[] px, int sz, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
            for (;;)
            {
                if (x0 >= 0 && x0 < sz && y0 >= 0 && y0 < sz) px[y0 * sz + x0] = c;
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }

        static void P(Color32[] px, int sz, int x, int y, Color32 c)
        {
            if (x >= 0 && x < sz && y >= 0 && y < sz) px[y * sz + x] = c;
        }

        static Color32 C(int r, int g, int b, int a = 255) =>
            new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        // ════════════════════════════════════════════════════════════
        //  小怪
        // ════════════════════════════════════════════════════════════

        // ── 骷髅怪：白骨颅骨 + 空洞眼眶 + 肋骨框架 ──────────────────
        static void DrawSkeleton(Color32[] px, int sz)
        {
            var O = C( 55,  50,  40);
            var B = C(200, 195, 175);
            var S = C(135, 130, 115);
            var E = C( 15,  10,  10);
            var T = C(245, 240, 228);

            Circle(px, sz, 16, 24, 7, O);
            Circle(px, sz, 16, 24, 6, B);
            Circle(px, sz, 13, 25, 2, E);
            Circle(px, sz, 19, 25, 2, E);
            P(px, sz, 16, 23, S);
            Rect(px, sz, 12, 18, 20, 19, O);
            Rect(px, sz, 13, 18, 19, 19, B);
            P(px, sz, 13, 18, T); P(px, sz, 15, 18, T);
            P(px, sz, 17, 18, T); P(px, sz, 19, 18, T);
            Rect(px, sz, 15, 15, 17, 18, S);
            Rect(px, sz, 10, 14, 22, 16, O);
            Rect(px, sz, 11, 14, 21, 15, S);
            Rect(px, sz, 10,  9, 12, 15, O); Rect(px, sz, 11,  9, 11, 14, B);
            Rect(px, sz, 20,  9, 22, 15, O); Rect(px, sz, 21,  9, 21, 14, B);
            Rect(px, sz, 13,  7, 19, 14, O);
            Rect(px, sz, 14,  8, 18, 13, S);
            Rect(px, sz, 14,  8, 18,  8, B);
            Rect(px, sz, 14, 10, 18, 10, B);
            Rect(px, sz, 14, 12, 18, 12, B);
            Rect(px, sz, 15,  8, 17, 13, B);
            Rect(px, sz, 12,  4, 20,  7, O);
            Rect(px, sz, 13,  5, 19,  6, S);
            P(px, sz, 16, 5, O); P(px, sz, 16, 6, O);
            Rect(px, sz, 12, 1, 15, 5, O); Rect(px, sz, 13, 1, 14, 4, B);
            Rect(px, sz, 17, 1, 20, 5, O); Rect(px, sz, 18, 1, 19, 4, B);
        }

        // ── 腐败小兵：暗绿锁甲 + 圆盔 + 腰剑 ───────────────────────
        static void DrawSoldier(Color32[] px, int sz)
        {
            var O = C( 25,  45,  15);
            var A = C( 70, 110,  50);
            var L = C(110, 165,  75);
            var M = C(150, 140, 100);
            var W = C(175, 178, 185);

            Circle(px, sz, 16, 25, 6, O);
            Circle(px, sz, 16, 25, 5, A);
            Circle(px, sz, 14, 27, 2, L);
            Rect(px, sz, 12, 24, 20, 25, O);
            Rect(px, sz, 13, 24, 19, 24, C(8, 8, 10));
            Circle(px, sz, 10, 18, 4, O); Circle(px, sz, 10, 18, 3, A);
            Circle(px, sz, 22, 18, 4, O); Circle(px, sz, 22, 18, 3, A);
            Rect(px, sz, 14, 19, 18, 21, A);
            Rect(px, sz, 11,  9, 21, 19, O);
            Rect(px, sz, 12, 10, 20, 18, A);
            Line(px, sz, 13, 18, 13, 11, L);
            Rect(px, sz, 11, 8, 21, 9, O);
            Rect(px, sz, 12, 8, 20, 8, M);
            Rect(px, sz, 22, 7, 24, 18, O); Rect(px, sz, 23, 7, 23, 18, W);
            Rect(px, sz, 21, 16, 25, 17, O); Rect(px, sz, 22, 16, 24, 16, W);
            Rect(px, sz, 12, 3, 15, 9, O); Rect(px, sz, 13, 3, 14, 8, A);
            Rect(px, sz, 17, 3, 20, 9, O); Rect(px, sz, 18, 3, 19, 8, A);
            Rect(px, sz, 11, 1, 16, 4, O); Rect(px, sz, 12, 1, 15, 3, M);
            Rect(px, sz, 16, 1, 21, 4, O); Rect(px, sz, 17, 1, 20, 3, M);
        }

        // ── 腐败弓箭手：苔绿轻甲 + 弓 + 箭袋 ───────────────────────
        static void DrawArcher(Color32[] px, int sz)
        {
            var O  = C( 30,  55,  15);
            var A  = C( 85, 140,  55);
            var L  = C(120, 185,  85);
            var Sk = C(195, 165, 120);
            var Bw = C(115,  75,  25);
            var Ar = C(178, 178, 182);
            var Ft = C(155, 175, 155);

            Circle(px, sz, 16, 26, 5, O);
            Circle(px, sz, 16, 26, 4, A);
            Rect(px, sz, 13, 24, 19, 26, Sk);
            P(px, sz, 14, 25, O); P(px, sz, 18, 25, O);
            Rect(px, sz, 13, 12, 19, 22, O);
            Rect(px, sz, 14, 13, 18, 21, A);
            Line(px, sz, 15, 20, 15, 13, L);
            Rect(px, sz, 10, 13, 13, 22, O);
            Rect(px, sz, 11, 14, 12, 21, C(100, 70, 30));
            P(px, sz, 11, 22, Ar); P(px, sz, 12, 23, Ar); P(px, sz, 11, 21, Ar);
            Line(px, sz, 23,  8, 25, 16, Bw); Line(px, sz, 23, 24, 25, 16, Bw);
            Line(px, sz, 24,  8, 26, 16, Bw); Line(px, sz, 24, 24, 26, 16, Bw);
            Line(px, sz, 23,  8, 23, 24, C(210, 195, 160, 200));
            Line(px, sz, 14, 15, 23, 15, Ar);
            P(px, sz, 13, 15, Ft); P(px, sz, 13, 14, Ft); P(px, sz, 12, 15, Ft);
            Rect(px, sz, 19, 14, 23, 16, Sk);
            Rect(px, sz, 13, 5, 15, 13, O); Rect(px, sz, 14, 6, 14, 12, A);
            Rect(px, sz, 17, 5, 19, 13, O); Rect(px, sz, 18, 6, 18, 12, A);
            Rect(px, sz, 12, 2, 16, 6, O); Rect(px, sz, 13, 2, 15, 5, Bw);
            Rect(px, sz, 16, 2, 20, 6, O); Rect(px, sz, 17, 2, 19, 5, Bw);
        }

        // ── 飞天蝙蝠：深紫展翼 + 橙眼 + 獠牙 ───────────────────────
        static void DrawBat(Color32[] px, int sz)
        {
            var O = C( 22,  8,  38);
            var W = C( 75, 30, 115);
            var B = C(105, 50, 160);
            var H = C(150, 75, 210);
            var E = C(255, 155,  25);
            var T = C(238, 232, 215);

            Oval(px, sz,  7, 17, 9, 7, O); Oval(px, sz,  7, 17, 8, 6, W);
            Oval(px, sz, 25, 17, 9, 7, O); Oval(px, sz, 25, 17, 8, 6, W);
            Line(px, sz, 16, 18,  3, 22, C(95, 40, 140, 180));
            Line(px, sz, 16, 18,  2, 13, C(95, 40, 140, 180));
            Line(px, sz, 16, 18, 29, 22, C(95, 40, 140, 180));
            Line(px, sz, 16, 18, 30, 13, C(95, 40, 140, 180));
            Oval(px, sz, 16, 17, 5, 7, O); Oval(px, sz, 16, 17, 4, 6, B);
            Oval(px, sz, 16, 15, 3, 4, H);
            Circle(px, sz, 16, 22, 4, O); Circle(px, sz, 16, 22, 3, B);
            Line(px, sz, 13, 25, 11, 29, O); Line(px, sz, 14, 25, 12, 29, B);
            Line(px, sz, 19, 25, 21, 29, O); Line(px, sz, 18, 25, 20, 29, B);
            Circle(px, sz, 14, 22, 1, E); Circle(px, sz, 18, 22, 1, E);
            P(px, sz, 15, 19, T); P(px, sz, 17, 19, T);
            P(px, sz, 15, 18, T); P(px, sz, 17, 18, T);
            P(px, sz,  1, 20, O); P(px, sz,  0, 21, O); P(px, sz,  1, 22, O);
            P(px, sz, 31, 20, O); P(px, sz, 31, 21, O); P(px, sz, 30, 22, O);
        }

        // ── 腐败盾士：钢蓝重甲 + 大盾 + 战锤 ───────────────────────
        static void DrawShieldGuard(Color32[] px, int sz)
        {
            var O  = C( 15,  25,  55);
            var A  = C( 50,  90, 175);
            var L  = C( 90, 148, 228);
            var Sh = C( 28,  58, 138);
            var M  = C(162, 165, 178);

            Rect(px, sz, 2,  5, 13, 24, O); Rect(px, sz, 3,  6, 12, 23, Sh);
            Circle(px, sz, 7, 15, 3, O); Circle(px, sz, 7, 15, 2, M);
            Line(px, sz, 4, 21, 4,  8, L); Line(px, sz, 5, 21, 5,  9, L);
            Rect(px, sz, 13,  8, 23, 21, O); Rect(px, sz, 14,  9, 22, 20, A);
            Line(px, sz, 15, 19, 15, 10, L);
            Circle(px, sz, 18, 25, 6, O); Circle(px, sz, 18, 25, 5, A);
            Rect(px, sz, 14, 23, 22, 25, O);
            Rect(px, sz, 15, 23, 21, 24, C(5, 5, 14));
            Rect(px, sz, 17, 29, 19, 31, O); Rect(px, sz, 17, 30, 19, 31, L);
            Rect(px, sz, 23, 13, 26, 21, O); Rect(px, sz, 24, 13, 25, 20, A);
            Rect(px, sz, 22, 21, 27, 26, O); Rect(px, sz, 23, 22, 26, 25, M);
            Rect(px, sz, 13,  7, 23,  8, O); Rect(px, sz, 14,  7, 22,  7, M);
            Rect(px, sz, 13,  2, 17,  8, O); Rect(px, sz, 14,  2, 16,  7, A);
            Rect(px, sz, 18,  2, 22,  8, O); Rect(px, sz, 19,  2, 21,  7, A);
            Rect(px, sz, 12,  0, 18,  3, O); Rect(px, sz, 13,  0, 17,  2, M);
            Rect(px, sz, 17,  0, 23,  3, O); Rect(px, sz, 18,  0, 22,  2, M);
        }

        // ── 毒蜘蛛：毒绿双体节 + 八足 + 红眼 ───────────────────────
        static void DrawPoisonSpider(Color32[] px, int sz)
        {
            var O    = C( 10,  40,   5);
            var G    = C( 40, 140,  20);
            var L    = C( 80, 200,  50);
            var Dk   = C(  5,  78,   5);
            var E    = C(255,  75,  10);
            var Fang = C(195, 215, 188);

            Oval(px, sz, 15, 11, 9, 8, O); Oval(px, sz, 15, 11, 8, 7, G);
            Circle(px, sz, 15, 11, 4, Dk); Circle(px, sz, 12, 14, 3, L);
            Circle(px, sz, 16, 21, 5, O); Circle(px, sz, 16, 21, 4, G);
            Circle(px, sz, 13, 23, 1, E); Circle(px, sz, 19, 23, 1, E);
            P(px, sz, 14, 21, E); P(px, sz, 18, 21, E);
            P(px, sz, 15, 22, E); P(px, sz, 17, 22, E);
            Line(px, sz, 14, 18, 11, 15, O); Line(px, sz, 14, 18, 12, 15, Fang);
            Line(px, sz, 18, 18, 21, 15, O); Line(px, sz, 18, 18, 20, 15, Fang);
            Line(px, sz, 13, 21,  3, 26, O); Line(px, sz, 13, 21,  4, 26, G);
            Line(px, sz, 12, 20,  1, 19, O); Line(px, sz, 13, 20,  2, 19, G);
            Line(px, sz, 12, 18,  2, 13, O); Line(px, sz, 13, 18,  3, 13, G);
            Line(px, sz, 12, 16,  3,  9, O); Line(px, sz, 13, 16,  4,  9, G);
            Line(px, sz, 19, 21, 29, 26, O); Line(px, sz, 19, 21, 28, 26, G);
            Line(px, sz, 20, 20, 31, 19, O); Line(px, sz, 19, 20, 30, 19, G);
            Line(px, sz, 20, 18, 30, 13, O); Line(px, sz, 19, 18, 29, 13, G);
            Line(px, sz, 20, 16, 29,  9, O); Line(px, sz, 19, 16, 28,  9, G);
            Rect(px, sz, 14, 3, 16, 5, O); Rect(px, sz, 14, 3, 16, 4, Dk);
        }

        // ── 暗影刺客：暗紫披风 + 紫眸 + 双匕首 ─────────────────────
        static void DrawShadowAssassin(Color32[] px, int sz)
        {
            var O  = C(  5,   4,   9);
            var C1 = C( 38,  14,  62);
            var C2 = C( 72,  28, 118);
            var Cl = C( 14,   9,  24);
            var Sl = C(198, 202, 218);
            var Ey = C(178,  28, 198);

            for (int y = 1; y <= 20; y++)
            {
                int hw = Mathf.RoundToInt(Mathf.Lerp(9f, 3f, y / 20f));
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Cl);
                P(px, sz, 16 - hw, y, C2); P(px, sz, 16 + hw, y, C2);
            }
            for (int y = 1; y <= 8; y++) P(px, sz, 16, y, C2);
            Rect(px, sz, 13, 10, 19, 21, C1);
            Circle(px, sz, 16, 25, 5, O); Circle(px, sz, 16, 25, 4, C1);
            Rect(px, sz, 12, 23, 20, 25, O);
            Rect(px, sz, 13, 23, 19, 24, C(42, 16, 68));
            P(px, sz, 14, 26, Ey); P(px, sz, 15, 26, Ey);
            P(px, sz, 17, 26, Ey); P(px, sz, 18, 26, Ey);
            Line(px, sz, 16, 29, 13, 31, O);
            Line(px, sz, 16, 29, 19, 31, O);
            Rect(px, sz, 14, 30, 18, 31, C1);
            Line(px, sz,  8, 22,  8,  8, O); Line(px, sz,  9, 22,  9,  8, Sl);
            Rect(px, sz,  7, 21, 11, 22, O); Rect(px, sz,  8, 21, 10, 21, Sl);
            Line(px, sz, 23, 22, 23,  8, O); Line(px, sz, 24, 22, 24,  8, Sl);
            Rect(px, sz, 21, 21, 25, 22, O); Rect(px, sz, 22, 21, 24, 21, Sl);
            Rect(px, sz, 13, 2, 15, 10, O); Rect(px, sz, 14, 2, 14,  9, C2);
            Rect(px, sz, 17, 2, 19, 10, O); Rect(px, sz, 18, 2, 18,  9, C2);
            Rect(px, sz, 12, 0, 16, 3, O); Rect(px, sz, 16, 0, 20, 3, O);
        }

        // ── 爆炎恶魔：炎橙圆体 + 双角 + 炎纹 + 导火索 ──────────────
        static void DrawExplosiveDemon(Color32[] px, int sz)
        {
            var O   = C( 78,  14,   4);
            var Bd  = C(218, 100,  18);
            var Y   = C(255, 198,  48);
            var R   = C(188,  38,   4);
            var Hn  = C(138,  24,   8);
            var Ey  = C(255, 228,  18);
            var Fus = C(178, 158, 128);
            var Sp  = C(255, 218,  98);

            for (int i = 0; i < 7; i++)
            {
                int w2 = 2 - i / 3;
                Rect(px, sz, 10 - i / 2 - w2, 24 + i, 10 - i / 2 + w2, 24 + i, Hn);
                Rect(px, sz, 22 + i / 2 - w2, 24 + i, 22 + i / 2 + w2, 24 + i, Hn);
            }
            Circle(px, sz, 16, 15, 12, O); Circle(px, sz, 16, 15, 11, Bd);
            Circle(px, sz, 16, 15,  7, Y); Circle(px, sz, 16, 15,  4, C(255, 238, 178));
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 10);
                int ey = 15 + Mathf.RoundToInt(Mathf.Sin(ang) * 10);
                Line(px, sz, 16, 15, ex, ey, R);
            }
            Circle(px, sz, 16, 15, 5, Y); Circle(px, sz, 16, 15, 3, C(255, 238, 178));
            Circle(px, sz, 12, 17, 2, O); Circle(px, sz, 12, 17, 1, Ey);
            Circle(px, sz, 20, 17, 2, O); Circle(px, sz, 20, 17, 1, Ey);
            Rect(px, sz, 11, 11, 21, 12, O);
            Rect(px, sz, 12, 12, 20, 12, C(195, 28, 4));
            for (int t = 12; t <= 20; t += 2) P(px, sz, t, 12, C(238, 232, 215));
            Line(px, sz, 16, 27, 20, 31, Fus);
            P(px, sz, 20, 31, Sp); P(px, sz, 21, 31, Sp); P(px, sz, 19, 31, Sp);
            Circle(px, sz, 12, 4, 3, O); Circle(px, sz, 12, 4, 2, R);
            Circle(px, sz, 20, 4, 3, O); Circle(px, sz, 20, 4, 2, R);
        }

        // ════════════════════════════════════════════════════════════
        //  精英
        // ════════════════════════════════════════════════════════════

        // ── 腐败士官：金甲 + 红披风 + 双手巨剑 ──────────────────────
        static void DrawCommander(Color32[] px, int sz)
        {
            var O  = C( 65,  38,   5);  // 深金轮廓
            var A  = C(198, 152,  38);  // 金甲
            var L  = C(242, 202,  82);  // 高光金
            var Ca = C(175,  28,  18);  // 披风红
            var W  = C(188, 192, 205);  // 剑钢
            var M  = C(155, 145,  95);  // 金属扣

            // 披风（左侧半椭圆红）
            Oval(px, sz, 9, 14, 7, 10, Ca);
            Oval(px, sz, 9, 14, 5,  8, C(140, 22, 14));
            // 头盔
            Circle(px, sz, 17, 25, 6, O);
            Circle(px, sz, 17, 25, 5, A);
            Circle(px, sz, 15, 27, 2, L);  // 高光
            // 盔顶羽饰
            Rect(px, sz, 14, 29, 20, 31, Ca);
            Rect(px, sz, 15, 30, 19, 31, C(210, 45, 30));
            // 面甲
            Rect(px, sz, 13, 23, 21, 25, O);
            Rect(px, sz, 14, 23, 20, 24, C(6, 6, 12));
            // 肩甲
            Circle(px, sz, 11, 19, 4, O); Circle(px, sz, 11, 19, 3, A);
            Circle(px, sz, 23, 19, 4, O); Circle(px, sz, 23, 19, 3, A);
            // 胸板
            Rect(px, sz, 12,  9, 22, 20, O);
            Rect(px, sz, 13, 10, 21, 19, A);
            Line(px, sz, 14, 18, 14, 11, L);
            // 胸口徽章
            Circle(px, sz, 17, 15, 2, O); Circle(px, sz, 17, 15, 1, L);
            // 腰带
            Rect(px, sz, 12,  8, 22,  9, O);
            Rect(px, sz, 13,  8, 21,  8, M);
            // 巨剑（右侧竖置）
            Rect(px, sz, 24,  2, 26, 22, O);
            Rect(px, sz, 25,  2, 25, 22, W);
            Line(px, sz, 24, 22, 26, 22, L);  // 剑刃尖
            Rect(px, sz, 22, 20, 28, 22, O);  // 护手
            Rect(px, sz, 23, 21, 27, 21, W);
            Rect(px, sz, 24, 22, 26, 24, C(140, 100, 30));  // 剑柄
            // 腿甲
            Rect(px, sz, 12,  3, 16,  9, O); Rect(px, sz, 13,  3, 15,  8, A);
            Rect(px, sz, 17,  3, 21,  9, O); Rect(px, sz, 18,  3, 20,  8, A);
            // 战靴
            Rect(px, sz, 11,  0, 17,  4, O); Rect(px, sz, 12,  0, 16,  3, L);
            Rect(px, sz, 16,  0, 22,  4, O); Rect(px, sz, 17,  0, 21,  3, L);
        }

        // ── 女巫：尖帽 + 紫袍 + 法杖发光球 ──────────────────────────
        static void DrawWitch(Color32[] px, int sz)
        {
            var O  = C( 32,  10,  52);  // 深紫轮廓
            var Rb = C(118,  48, 178);  // 袍紫
            var Rd = C( 72,  22, 112);  // 袍暗
            var Ht = C( 55,  18,  85);  // 帽暗紫
            var Gl = C(198,  95, 255);  // 法球光
            var Sk = C(208, 182, 155);  // 皮肤
            var St = C(108,  75,  35);  // 法杖木

            // 帽尖（锥形）
            P(px, sz, 16, 31, Ht);
            Rect(px, sz, 15, 30, 17, 31, Ht);
            Rect(px, sz, 14, 28, 18, 30, Ht);
            Rect(px, sz, 13, 26, 19, 28, Ht);
            // 帽檐（宽）
            Rect(px, sz,  9, 24, 23, 26, O);
            Rect(px, sz, 10, 24, 22, 25, Ht);
            // 头发（帽檐两侧垂落）
            Oval(px, sz,  9, 20, 3, 5, C(38, 15, 58));
            Oval(px, sz, 23, 20, 3, 5, C(38, 15, 58));
            // 面部
            Oval(px, sz, 16, 21, 4, 3, Sk);
            P(px, sz, 14, 22, O); P(px, sz, 18, 22, O);  // 眼
            P(px, sz, 15, 21, O); P(px, sz, 17, 21, O);  // 眉
            P(px, sz, 16, 20, C(160, 100, 100));          // 嘴
            // 袍身（下宽上窄）
            for (int y = 2; y <= 20; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 20f) * 8f);
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Rd);
            }
            for (int y = 3; y <= 19; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 19f) * 7f);
                P(px, sz, 16 - hw, y, Rb); P(px, sz, 16 + hw, y, Rb);
            }
            // 法杖（右侧）
            Rect(px, sz, 22, 5, 24, 21, St);
            // 法球
            Circle(px, sz, 23, 22, 4, O);
            Circle(px, sz, 23, 22, 3, Gl);
            Circle(px, sz, 23, 23, 2, C(220, 150, 255));
            P(px, sz, 22, 24, C(255, 210, 255));  // 球高光
            // 持杖手
            Rect(px, sz, 20, 18, 22, 20, Sk);
            // 另一只手伸出施法
            Rect(px, sz, 10, 16, 12, 18, Sk);
            P(px, sz, 9, 17, Gl); P(px, sz, 8, 16, Gl);  // 法力残留
        }

        // ── 毒蛇祭司：部落蛇纹 + 仪式面具 + 蛇杖 ──────────────────
        static void DrawPoisonShaman(Color32[] px, int sz)
        {
            var O  = C( 10,  40,   5);  // 深绿轮廓
            var G  = C( 55, 148,  28);  // 身体绿
            var Yd = C(195, 195,  25);  // 黄色图腾纹
            var Mk = C(208, 175,  28);  // 面具黄
            var Sn = C( 38, 118,  12);  // 蛇体深绿
            var Tk = C(128,  88,  32);  // 图腾木
            var Sk = C(172, 142,  92);  // 肤色
            var Fe = C(155, 178, 158);  // 羽毛

            // 图腾杖（左）
            Rect(px, sz, 8, 3, 10, 22, Tk);
            // 盘绕的蛇（沿杖）
            for (int i = 0; i < 8; i++)
            {
                int ox = (i % 2) * 3 - 1;
                Circle(px, sz, 9 + ox, 10 + i * 2, 1, Sn);
            }
            // 蛇头（杖顶）
            Circle(px, sz, 9, 26, 2, O); Circle(px, sz, 9, 26, 1, Sn);
            P(px, sz, 8, 27, Yd); P(px, sz, 10, 27, Yd);  // 蛇眼
            P(px, sz, 7, 26, O);  P(px, sz, 11, 26, O);   // 蛇信
            // 身体
            Oval(px, sz, 17, 13, 6, 8, O);
            Oval(px, sz, 17, 13, 5, 7, G);
            // 黄色图腾纹路
            Line(px, sz, 14, 19, 20, 9,  Yd);
            Line(px, sz, 20, 19, 14, 9,  Yd);
            P(px, sz, 17, 17, Yd); P(px, sz, 17, 13, Yd); P(px, sz, 17, 9, Yd);
            // 仪式面具（头部）
            Oval(px, sz, 17, 23, 5, 4, O);
            Oval(px, sz, 17, 23, 4, 3, Mk);
            // 面具眼洞
            P(px, sz, 14, 24, O); P(px, sz, 20, 24, O);
            Circle(px, sz, 14, 24, 1, O); Circle(px, sz, 20, 24, 1, O);
            // 面具纹饰
            Rect(px, sz, 15, 22, 19, 22, O);
            P(px, sz, 17, 26, O); P(px, sz, 16, 26, O); P(px, sz, 18, 26, O);
            // 羽冠
            Line(px, sz, 14, 27, 12, 31, Fe); Line(px, sz, 15, 27, 13, 31, Yd);
            Line(px, sz, 17, 27, 17, 31, Fe);
            Line(px, sz, 20, 27, 22, 31, Fe); Line(px, sz, 19, 27, 21, 31, Yd);
            // 腿
            Rect(px, sz, 13, 3, 16, 10, O); Rect(px, sz, 14, 4, 15, 9, G);
            Rect(px, sz, 18, 3, 21, 10, O); Rect(px, sz, 19, 4, 20, 9, G);
            // 腿纹
            P(px, sz, 14, 7, Yd); P(px, sz, 14, 5, Yd);
            P(px, sz, 19, 7, Yd); P(px, sz, 19, 5, Yd);
        }

        // ── 死灵术士：暗袍 + 骷髅权杖 + 灵魂蓝火 ───────────────────
        static void DrawNecromancer(Color32[] px, int sz)
        {
            var O  = C( 18,   7,  32);  // 深紫轮廓
            var Rb = C( 95,  38, 158);  // 袍紫
            var Rd = C( 55,  18,  95);  // 袍暗
            var Sk = C(192, 188, 168);  // 骨白
            var E  = C( 75, 148, 255);  // 灵魂蓝火
            var Cr = C(158, 118,  28);  // 王冠金
            var St = C( 88,  68, 128);  // 权杖紫

            // 王冠
            Rect(px, sz, 12, 28, 20, 29, Cr);
            for (int i = 12; i <= 20; i += 2) P(px, sz, i, 30, Cr);
            P(px, sz, 16, 31, Cr);
            // 骷髅脸
            Circle(px, sz, 16, 24, 5, O);
            Circle(px, sz, 16, 24, 4, Sk);
            Circle(px, sz, 13, 25, 2, O); Circle(px, sz, 13, 25, 1, E);  // 魂火眼
            Circle(px, sz, 19, 25, 2, O); Circle(px, sz, 19, 25, 1, E);
            Rect(px, sz, 13, 20, 19, 21, O);  // 下颌
            Rect(px, sz, 14, 20, 18, 20, Sk);
            P(px, sz, 14, 20, Sk); P(px, sz, 16, 20, Sk); P(px, sz, 18, 20, Sk);
            // 飘动袍身（底宽顶窄）
            for (int y = 1; y <= 19; y++)
            {
                int hw = Mathf.RoundToInt(3f + (1f - y / 19f) * 8f);
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Rd);
            }
            for (int y = 2; y <= 18; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 18f) * 7f);
                P(px, sz, 16 - hw, y, Rb); P(px, sz, 16 + hw, y, Rb);
            }
            // 袍上魂火光效
            P(px, sz, 16, 10, E); P(px, sz, 14, 7, E); P(px, sz, 18, 7, E);
            P(px, sz, 11, 4, E); P(px, sz, 21, 4, E);
            // 骷髅权杖（左）
            Rect(px, sz, 7, 5, 9, 22, St);
            // 权杖顶颅骨
            Circle(px, sz, 8, 25, 3, O); Circle(px, sz, 8, 25, 2, Sk);
            P(px, sz, 7, 26, O); P(px, sz, 9, 26, O);
            Circle(px, sz, 7, 26, 1, E); Circle(px, sz, 9, 26, 1, E);
            // 灵魂能量（权杖周围）
            P(px, sz, 6, 20, E); P(px, sz, 6, 17, E); P(px, sz, 5, 14, E);
            P(px, sz, 10, 19, E); P(px, sz, 10, 16, E);
            // 骸骨手（两侧伸出）
            Line(px, sz, 16, 19, 10, 17, Sk); P(px, sz, 9, 17, E);
            Line(px, sz, 16, 19, 22, 17, Sk); P(px, sz, 23, 17, E);
        }

        // ════════════════════════════════════════════════════════════
        //  Boss
        // ════════════════════════════════════════════════════════════

        // ── 地狱巨人：火焰巨岩躯体 + 熔岩裂缝 + 双拳 ───────────────
        static void DrawHellGiant(Color32[] px, int sz)
        {
            var O  = C( 75,  22,   8);  // 深红岩轮廓
            var Bd = C(148,  52,  18);  // 岩石体
            var R  = C(198,  75,  12);  // 红橙
            var Lv = C(255, 175,  28);  // 熔岩黄
            var Dk = C( 48,   8,   4);  // 深缝
            var Hn = C(118,  32,  12);  // 角

            // 双角（顶部两侧）
            for (int i = 0; i < 9; i++)
            {
                int w = 2 - i / 4;
                Rect(px, sz,  9 - i / 3 - w,  22 + i,  9 - i / 3 + w,  22 + i, Hn);
                Rect(px, sz, 23 + i / 3 - w,  22 + i, 23 + i / 3 + w,  22 + i, Hn);
            }
            // 巨型身体（几乎撑满画布）
            Circle(px, sz, 16, 14, 14, O);
            Circle(px, sz, 16, 14, 13, Bd);
            // 熔岩裂缝（辐射线）
            for (int a = 0; a < 6; a++)
            {
                float ang = a * Mathf.PI / 3f + 0.2f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 12);
                int ey = 14 + Mathf.RoundToInt(Mathf.Sin(ang) * 12);
                Line(px, sz, 16, 14, ex, ey, Lv);
            }
            // 重绘中心覆盖裂缝
            Circle(px, sz, 16, 14, 8, Bd);
            Circle(px, sz, 16, 14, 5, R);
            Circle(px, sz, 16, 14, 3, Lv);
            // 愤怒眼睛（橙色）
            Circle(px, sz, 11, 17, 2, O); Circle(px, sz, 11, 17, 1, Lv);
            Circle(px, sz, 21, 17, 2, O); Circle(px, sz, 21, 17, 1, Lv);
            // 嘴（咆哮）
            Rect(px, sz,  9, 11, 23, 12, O);
            Rect(px, sz, 10, 12, 22, 12, Dk);
            for (int t = 10; t <= 22; t += 3) P(px, sz, t, 12, C(225, 205, 182));
            // 巨拳（两侧底部）
            Circle(px, sz,  7,  5, 5, O); Circle(px, sz,  7,  5, 4, Bd);
            Circle(px, sz, 25,  5, 5, O); Circle(px, sz, 25,  5, 4, Bd);
            // 指节纹路
            P(px, sz,  5,  4, Dk); P(px, sz,  7,  3, Dk); P(px, sz,  9,  4, Dk);
            P(px, sz, 23,  4, Dk); P(px, sz, 25,  3, Dk); P(px, sz, 27,  4, Dk);
            // 熔岩滴落（底部）
            P(px, sz, 12,  1, Lv); P(px, sz, 20,  1, Lv);
            P(px, sz, 10,  0, R);  P(px, sz, 22,  0, R);
        }

        // ── 霜魂巫妖：冰晶冠 + 骸骨袍 + 魂焰权杖 ──────────────────
        static void DrawFrostLich(Color32[] px, int sz)
        {
            var O  = C(  5,  10,  30);  // 深蓝轮廓
            var Ic = C(128, 198, 255);  // 冰蓝
            var IL = C(208, 238, 255);  // 冰高光
            var Sk = C(192, 192, 182);  // 骸骨
            var E  = C( 48, 228, 148);  // 巫妖绿魂焰
            var Cr = C( 78, 168, 238);  // 冰晶冠
            var Rv = C( 20,  50, 120);  // 袍暗蓝
            var Rb = C( 45,  88, 165);  // 袍中蓝

            // 冰晶王冠（顶部）
            Rect(px, sz, 10, 28, 22, 29, Cr);
            Rect(px, sz, 10, 28, 11, 31, Cr);  // 左尖
            Rect(px, sz, 15, 28, 17, 30, Cr);  // 中左尖
            P(px, sz, 16, 31, Cr);             // 最高点
            Rect(px, sz, 19, 28, 17, 30, Cr);  // 中右尖
            Rect(px, sz, 21, 28, 22, 31, Cr);  // 右尖
            P(px, sz, 11, 29, IL); P(px, sz, 16, 30, IL); P(px, sz, 21, 29, IL);
            // 骸骨头颅
            Circle(px, sz, 16, 24, 5, O);
            Circle(px, sz, 16, 24, 4, Sk);
            // 魂焰眼
            Circle(px, sz, 13, 25, 2, O); Circle(px, sz, 13, 25, 1, E);
            Circle(px, sz, 19, 25, 2, O); Circle(px, sz, 19, 25, 1, E);
            // 颌骨
            Rect(px, sz, 13, 20, 19, 21, O);
            Rect(px, sz, 14, 20, 18, 20, Sk);
            // 冰袍（飘浮感，下宽）
            for (int y = 2; y <= 19; y++)
            {
                int hw = Mathf.RoundToInt(3f + (1f - y / 19f) * 9f);
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Rv);
            }
            for (int y = 3; y <= 18; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 18f) * 8f);
                P(px, sz, 16 - hw, y, Rb); P(px, sz, 16 + hw, y, Rb);
            }
            // 冰刺装饰（袍边）
            Rect(px, sz,  7, 15, 9, 10, Ic); Rect(px, sz,  7, 10, 9,  8, IL);
            Rect(px, sz, 23, 15, 25, 10, Ic); Rect(px, sz, 23, 10, 25, 8, IL);
            // 袍上冰晶纹
            P(px, sz, 16, 12, IL); P(px, sz, 13, 9, IL); P(px, sz, 19, 9, IL);
            P(px, sz, 10, 5, Ic);  P(px, sz, 22, 5, Ic);
            // 权杖（左侧）
            Rect(px, sz, 5, 5, 7, 22, C(55, 78, 158));
            // 冰晶法球（权杖顶）
            Circle(px, sz, 6, 24, 4, O);
            Circle(px, sz, 6, 24, 3, Ic);
            P(px, sz, 5, 25, E); P(px, sz, 7, 25, E);
            P(px, sz, 6, 26, IL);
            // 骸骨臂膀
            Line(px, sz, 16, 20, 10, 18, Sk); P(px, sz,  9, 18, E);
            Line(px, sz, 16, 20, 22, 18, Sk); P(px, sz, 23, 18, E);
        }

        // ── 混沌领主：虚空大眼 + 混沌能量 + 多角 ───────────────────
        static void DrawChaosLord(Color32[] px, int sz)
        {
            var O  = C(  7,   3,  13);  // 近黑轮廓
            var Vd = C( 22,   4,  38);  // 虚空紫
            var Pb = C( 68,  18, 108);  // 紫体
            var Mg = C(198,   0, 255);  // 品红混沌
            var Hn = C( 48,   8,  78);  // 角
            var Ey = C(255,  48, 198);  // 混沌眼
            var Yw = C(255, 175,   0);  // 混沌能量黄
            var Rd = C(218,  18,  78);  // 红混沌光

            // 四角（两对）
            for (int i = 0; i < 10; i++)
            {
                // 外角（向外斜）
                P(px, sz,  8 - i / 3, 21 + i, Hn);
                P(px, sz, 24 + i / 3, 21 + i, Hn);
            }
            for (int i = 0; i < 7; i++)
            {
                // 内角（较短）
                Rect(px, sz, 12, 23 + i, 13, 23 + i, Hn);
                Rect(px, sz, 19, 23 + i, 20, 23 + i, Hn);
            }
            // 虚空大身体
            Circle(px, sz, 16, 13, 13, O);
            Circle(px, sz, 16, 13, 12, Vd);
            // 混沌漩涡（辐射）
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f + 0.4f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 11);
                int ey = 13 + Mathf.RoundToInt(Mathf.Sin(ang) * 11);
                Line(px, sz, 16, 13, ex, ey, a % 2 == 0 ? Mg : Rd);
            }
            // 重绘中心
            Circle(px, sz, 16, 13, 7, Vd);
            Circle(px, sz, 16, 13, 5, Pb);
            // 巨眼（核心）
            Circle(px, sz, 16, 14, 4, O);
            Circle(px, sz, 16, 14, 3, Ey);
            Circle(px, sz, 16, 14, 2, C(255, 160, 235));
            Circle(px, sz, 16, 14, 1, C(255, 255, 255));  // 瞳孔高光
            // 虚空光环点（边缘能量）
            for (int a = 0; a < 12; a++)
            {
                float ang = a * Mathf.PI / 6f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 11);
                int ey = 13 + Mathf.RoundToInt(Mathf.Sin(ang) * 11);
                P(px, sz, ex, ey, a % 3 == 0 ? Yw : Mg);
            }
            // 混沌爪（两侧底部）
            Circle(px, sz,  5,  5, 4, O); Circle(px, sz,  5,  5, 3, Pb);
            Circle(px, sz, 27,  5, 4, O); Circle(px, sz, 27,  5, 3, Pb);
            // 爪尖
            P(px, sz,  2,  3, O); P(px, sz,  2,  5, Mg); P(px, sz,  1,  4, Mg);
            P(px, sz,  4,  2, O); P(px, sz,  4,  3, Mg); P(px, sz,  5,  1, Mg);
            P(px, sz, 30,  3, O); P(px, sz, 30,  5, Mg); P(px, sz, 31,  4, Mg);
            P(px, sz, 28,  2, O); P(px, sz, 28,  3, Mg); P(px, sz, 27,  1, Mg);
            // 虚空滴落
            P(px, sz, 11,  0, Mg); P(px, sz, 21,  0, Rd);
            P(px, sz, 13,  1, Vd); P(px, sz, 19,  1, Vd);
        }
    }
}
