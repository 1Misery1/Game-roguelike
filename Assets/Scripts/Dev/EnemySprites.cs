using Game.AI;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Dev
{
    // 程序化生成 32×32 像素风格敌人精灵图
    // 每种敌人类型各自独立绘制，缓存后复用
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
            var px = new Color32[SZ * SZ]; // 初始全透明
            switch (type)
            {
                case EnemyType.Skeleton:       DrawSkeleton(px, SZ);       break;
                case EnemyType.Soldier:        DrawSoldier(px, SZ);        break;
                case EnemyType.Archer:         DrawArcher(px, SZ);         break;
                case EnemyType.Bat:            DrawBat(px, SZ);            break;
                case EnemyType.ShieldGuard:    DrawShieldGuard(px, SZ);    break;
                case EnemyType.PoisonSpider:   DrawPoisonSpider(px, SZ);   break;
                case EnemyType.ShadowAssassin: DrawShadowAssassin(px, SZ); break;
                case EnemyType.ExplosiveDemon: DrawExplosiveDemon(px, SZ); break;
                default: return null;
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
        }

        // ── 绘图原语（y=0 在贴图底部 = 世界空间底部）─────────────────

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

        // ── 骷髅怪：白骨 + 空洞眼眶 + 肋骨框架 ──────────────────────
        static void DrawSkeleton(Color32[] px, int sz)
        {
            var O = C( 55,  50,  40);   // 轮廓
            var B = C(200, 195, 175);   // 主骨
            var S = C(135, 130, 115);   // 阴影骨
            var E = C( 15,  10,  10);   // 眼眶
            var T = C(245, 240, 228);   // 牙齿

            // 头骨
            Circle(px, sz, 16, 24, 7, O);
            Circle(px, sz, 16, 24, 6, B);
            // 眼眶
            Circle(px, sz, 13, 25, 2, E);
            Circle(px, sz, 19, 25, 2, E);
            // 鼻孔
            P(px, sz, 16, 23, S);
            // 下颌
            Rect(px, sz, 12, 18, 20, 19, O);
            Rect(px, sz, 13, 18, 19, 19, B);
            // 牙齿
            P(px, sz, 13, 18, T); P(px, sz, 15, 18, T);
            P(px, sz, 17, 18, T); P(px, sz, 19, 18, T);
            // 颈部
            Rect(px, sz, 15, 15, 17, 18, S);
            // 肩膀
            Rect(px, sz, 10, 14, 22, 16, O);
            Rect(px, sz, 11, 14, 21, 15, S);
            // 手臂（纤细骨骼）
            Rect(px, sz, 10,  9, 12, 15, O); Rect(px, sz, 11,  9, 11, 14, B);
            Rect(px, sz, 20,  9, 22, 15, O); Rect(px, sz, 21,  9, 21, 14, B);
            // 肋骨框架
            Rect(px, sz, 13,  7, 19, 14, O);
            Rect(px, sz, 14,  8, 18, 13, S);
            Rect(px, sz, 14,  8, 18,  8, B);  // 肋线
            Rect(px, sz, 14, 10, 18, 10, B);
            Rect(px, sz, 14, 12, 18, 12, B);
            Rect(px, sz, 15,  8, 17, 13, B);  // 脊骨
            // 骨盆
            Rect(px, sz, 12,  4, 20,  7, O);
            Rect(px, sz, 13,  5, 19,  6, S);
            P(px, sz, 16, 5, O); P(px, sz, 16, 6, O);  // 骨盆中缝
            // 腿骨
            Rect(px, sz, 12, 1, 15, 5, O); Rect(px, sz, 13, 1, 14, 4, B);
            Rect(px, sz, 17, 1, 20, 5, O); Rect(px, sz, 18, 1, 19, 4, B);
        }

        // ── 腐败小兵：暗绿锁甲 + 圆盔 + 腰剑 ───────────────────────
        static void DrawSoldier(Color32[] px, int sz)
        {
            var O = C( 25,  45,  15);   // 深绿轮廓
            var A = C( 70, 110,  50);   // 装甲绿
            var L = C(110, 165,  75);   // 高光绿
            var M = C(150, 140, 100);   // 金属扣件
            var W = C(175, 178, 185);   // 武器钢色

            // 头盔（穹顶）
            Circle(px, sz, 16, 25, 6, O);
            Circle(px, sz, 16, 25, 5, A);
            Circle(px, sz, 14, 27, 2, L);  // 头盔高光
            // 面甲缝隙
            Rect(px, sz, 12, 24, 20, 25, O);
            Rect(px, sz, 13, 24, 19, 24, C(8, 8, 10));
            // 肩甲
            Circle(px, sz, 10, 18, 4, O); Circle(px, sz, 10, 18, 3, A);
            Circle(px, sz, 22, 18, 4, O); Circle(px, sz, 22, 18, 3, A);
            // 颈部
            Rect(px, sz, 14, 19, 18, 21, A);
            // 胸板
            Rect(px, sz, 11,  9, 21, 19, O);
            Rect(px, sz, 12, 10, 20, 18, A);
            Line(px, sz, 13, 18, 13, 11, L);  // 胸板高光线
            // 腰带
            Rect(px, sz, 11, 8, 21, 9, O);
            Rect(px, sz, 12, 8, 20, 8, M);
            // 右侧腰剑
            Rect(px, sz, 22, 7, 24, 18, O);
            Rect(px, sz, 23, 7, 23, 18, W);
            Rect(px, sz, 21, 16, 25, 17, O); Rect(px, sz, 22, 16, 24, 16, W);  // 护手
            // 腿甲
            Rect(px, sz, 12, 3, 15, 9, O); Rect(px, sz, 13, 3, 14, 8, A);
            Rect(px, sz, 17, 3, 20, 9, O); Rect(px, sz, 18, 3, 19, 8, A);
            // 战靴
            Rect(px, sz, 11, 1, 16, 4, O); Rect(px, sz, 12, 1, 15, 3, M);
            Rect(px, sz, 16, 1, 21, 4, O); Rect(px, sz, 17, 1, 20, 3, M);
        }

        // ── 腐败弓箭手：苔绿轻甲 + 弓 + 箭袋 ───────────────────────
        static void DrawArcher(Color32[] px, int sz)
        {
            var O  = C( 30,  55,  15);  // 深绿轮廓
            var A  = C( 85, 140,  55);  // 轻甲绿
            var L  = C(120, 185,  85);  // 高光
            var Sk = C(195, 165, 120);  // 皮肤
            var Bw = C(115,  75,  25);  // 弓木
            var Ar = C(178, 178, 182);  // 箭矢钢
            var Ft = C(155, 175, 155);  // 箭羽

            // 头（风帽）
            Circle(px, sz, 16, 26, 5, O);
            Circle(px, sz, 16, 26, 4, A);
            Rect(px, sz, 13, 24, 19, 26, Sk);      // 面部露出
            P(px, sz, 14, 25, O); P(px, sz, 18, 25, O);  // 眼睛
            // 躯干（修长）
            Rect(px, sz, 13, 12, 19, 22, O);
            Rect(px, sz, 14, 13, 18, 21, A);
            Line(px, sz, 15, 20, 15, 13, L);
            // 箭袋（左背）
            Rect(px, sz, 10, 13, 13, 22, O);
            Rect(px, sz, 11, 14, 12, 21, C(100, 70, 30));
            P(px, sz, 11, 22, Ar); P(px, sz, 12, 23, Ar); P(px, sz, 11, 21, Ar);
            // 弓身（右侧弧形）
            Line(px, sz, 23,  8, 25, 16, Bw);
            Line(px, sz, 23, 24, 25, 16, Bw);
            Line(px, sz, 24,  8, 26, 16, Bw);
            Line(px, sz, 24, 24, 26, 16, Bw);
            Line(px, sz, 23,  8, 23, 24, C(210, 195, 160, 200));  // 弓弦
            // 已搭箭
            Line(px, sz, 14, 15, 23, 15, Ar);
            P(px, sz, 13, 15, Ft); P(px, sz, 13, 14, Ft); P(px, sz, 12, 15, Ft);
            // 拉弓手臂
            Rect(px, sz, 19, 14, 23, 16, Sk);
            // 腿
            Rect(px, sz, 13, 5, 15, 13, O); Rect(px, sz, 14, 6, 14, 12, A);
            Rect(px, sz, 17, 5, 19, 13, O); Rect(px, sz, 18, 6, 18, 12, A);
            // 靴
            Rect(px, sz, 12, 2, 16, 6, O); Rect(px, sz, 13, 2, 15, 5, Bw);
            Rect(px, sz, 16, 2, 20, 6, O); Rect(px, sz, 17, 2, 19, 5, Bw);
        }

        // ── 飞天蝙蝠：深紫展翼 + 橙眼 + 獠牙 ───────────────────────
        static void DrawBat(Color32[] px, int sz)
        {
            var O = C( 22,  8,  38);    // 深紫轮廓
            var W = C( 75, 30, 115);    // 翼膜紫
            var B = C(105, 50, 160);    // 身体紫
            var H = C(150, 75, 210);    // 高光
            var E = C(255, 155,  25);   // 橙色眼睛
            var T = C(238, 232, 215);   // 獠牙

            // 左翼（椭圆）
            Oval(px, sz,  7, 17, 9, 7, O);
            Oval(px, sz,  7, 17, 8, 6, W);
            // 右翼
            Oval(px, sz, 25, 17, 9, 7, O);
            Oval(px, sz, 25, 17, 8, 6, W);
            // 翼脉
            Line(px, sz, 16, 18,  3, 22, C(95, 40, 140, 180));
            Line(px, sz, 16, 18,  2, 13, C(95, 40, 140, 180));
            Line(px, sz, 16, 18, 29, 22, C(95, 40, 140, 180));
            Line(px, sz, 16, 18, 30, 13, C(95, 40, 140, 180));
            // 中央身体
            Oval(px, sz, 16, 17, 5, 7, O);
            Oval(px, sz, 16, 17, 4, 6, B);
            Oval(px, sz, 16, 15, 3, 4, H);  // 腹部高光
            // 头（上方）
            Circle(px, sz, 16, 22, 4, O);
            Circle(px, sz, 16, 22, 3, B);
            // 耳朵
            Line(px, sz, 13, 25, 11, 29, O); Line(px, sz, 14, 25, 12, 29, B);
            Line(px, sz, 19, 25, 21, 29, O); Line(px, sz, 18, 25, 20, 29, B);
            // 眼睛
            Circle(px, sz, 14, 22, 1, E);
            Circle(px, sz, 18, 22, 1, E);
            // 獠牙
            P(px, sz, 15, 19, T); P(px, sz, 17, 19, T);
            P(px, sz, 15, 18, T); P(px, sz, 17, 18, T);
            // 翼爪
            P(px, sz,  1, 20, O); P(px, sz,  0, 21, O); P(px, sz,  1, 22, O);
            P(px, sz, 31, 20, O); P(px, sz, 31, 21, O); P(px, sz, 30, 22, O);
        }

        // ── 腐败盾士：钢蓝重甲 + 大盾 + 战锤 ───────────────────────
        static void DrawShieldGuard(Color32[] px, int sz)
        {
            var O  = C( 15,  25,  55);  // 深蓝轮廓
            var A  = C( 50,  90, 175);  // 装甲蓝
            var L  = C( 90, 148, 228);  // 高光蓝
            var Sh = C( 28,  58, 138);  // 盾面深蓝
            var M  = C(162, 165, 178);  // 金属银

            // 大盾（左侧）
            Rect(px, sz, 2,  5, 13, 24, O);
            Rect(px, sz, 3,  6, 12, 23, Sh);
            Circle(px, sz, 7, 15, 3, O);   // 盾心徽章
            Circle(px, sz, 7, 15, 2, M);
            Line(px, sz, 4, 21, 4,  8, L); // 盾面高光
            Line(px, sz, 5, 21, 5,  9, L);
            // 身体（右侧重甲）
            Rect(px, sz, 13,  8, 23, 21, O);
            Rect(px, sz, 14,  9, 22, 20, A);
            Line(px, sz, 15, 19, 15, 10, L);  // 胸板高光
            // 头盔
            Circle(px, sz, 18, 25, 6, O);
            Circle(px, sz, 18, 25, 5, A);
            Rect(px, sz, 14, 23, 22, 25, O);                 // 面甲
            Rect(px, sz, 15, 23, 21, 24, C(5, 5, 14));
            Rect(px, sz, 17, 29, 19, 31, O);                 // 头盔顶饰
            Rect(px, sz, 17, 30, 19, 31, L);
            // 武器臂（右）
            Rect(px, sz, 23, 13, 26, 21, O);
            Rect(px, sz, 24, 13, 25, 20, A);
            // 战锤头
            Rect(px, sz, 22, 21, 27, 26, O);
            Rect(px, sz, 23, 22, 26, 25, M);
            // 腰带
            Rect(px, sz, 13,  7, 23,  8, O);
            Rect(px, sz, 14,  7, 22,  7, M);
            // 腿甲
            Rect(px, sz, 13,  2, 17,  8, O); Rect(px, sz, 14,  2, 16,  7, A);
            Rect(px, sz, 18,  2, 22,  8, O); Rect(px, sz, 19,  2, 21,  7, A);
            // 战靴
            Rect(px, sz, 12,  0, 18,  3, O); Rect(px, sz, 13,  0, 17,  2, M);
            Rect(px, sz, 17,  0, 23,  3, O); Rect(px, sz, 18,  0, 22,  2, M);
        }

        // ── 毒蜘蛛：毒绿双体节 + 八足 + 红眼 ───────────────────────
        static void DrawPoisonSpider(Color32[] px, int sz)
        {
            var O    = C( 10,  40,   5);  // 深绿轮廓
            var G    = C( 40, 140,  20);  // 主绿
            var L    = C( 80, 200,  50);  // 高光绿
            var Dk   = C(  5,  78,   5);  // 腹部深纹
            var E    = C(255,  75,  10);  // 红眼
            var Fang = C(195, 215, 188);  // 螯牙

            // 腹部（大椭圆，后方）
            Oval(px, sz, 15, 11, 9, 8, O);
            Oval(px, sz, 15, 11, 8, 7, G);
            Circle(px, sz, 15, 11, 4, Dk);  // 腹部花纹
            Circle(px, sz, 12, 14, 3, L);   // 腹部高光
            // 头胸部（前圆）
            Circle(px, sz, 16, 21, 5, O);
            Circle(px, sz, 16, 21, 4, G);
            // 眼睛（三对）
            Circle(px, sz, 13, 23, 1, E); Circle(px, sz, 19, 23, 1, E);
            P(px, sz, 14, 21, E); P(px, sz, 18, 21, E);
            P(px, sz, 15, 22, E); P(px, sz, 17, 22, E);
            // 螯肢/獠牙
            Line(px, sz, 14, 18, 11, 15, O); Line(px, sz, 14, 18, 12, 15, Fang);
            Line(px, sz, 18, 18, 21, 15, O); Line(px, sz, 18, 18, 20, 15, Fang);
            // 八条腿（各两段）
            Line(px, sz, 13, 21,  3, 26, O); Line(px, sz, 13, 21,  4, 26, G);
            Line(px, sz, 12, 20,  1, 19, O); Line(px, sz, 13, 20,  2, 19, G);
            Line(px, sz, 12, 18,  2, 13, O); Line(px, sz, 13, 18,  3, 13, G);
            Line(px, sz, 12, 16,  3,  9, O); Line(px, sz, 13, 16,  4,  9, G);
            Line(px, sz, 19, 21, 29, 26, O); Line(px, sz, 19, 21, 28, 26, G);
            Line(px, sz, 20, 20, 31, 19, O); Line(px, sz, 19, 20, 30, 19, G);
            Line(px, sz, 20, 18, 30, 13, O); Line(px, sz, 19, 18, 29, 13, G);
            Line(px, sz, 20, 16, 29,  9, O); Line(px, sz, 19, 16, 28,  9, G);
            // 丝囊（底部）
            Rect(px, sz, 14, 3, 16, 5, O); Rect(px, sz, 14, 3, 16, 4, Dk);
        }

        // ── 暗影刺客：暗紫披风 + 紫眸 + 双匕首 ─────────────────────
        static void DrawShadowAssassin(Color32[] px, int sz)
        {
            var O  = C(  5,   4,   9);  // 近黑轮廓
            var C1 = C( 38,  14,  62);  // 深紫身体
            var C2 = C( 72,  28, 118);  // 中紫披风边
            var Cl = C( 14,   9,  24);  // 披风阴影
            var Sl = C(198, 202, 218);  // 银色匕首
            var Ey = C(178,  28, 198);  // 紫色眼光

            // 披风（由底至顶渐宽）
            for (int y = 1; y <= 20; y++)
            {
                int hw = Mathf.RoundToInt(Mathf.Lerp(9f, 3f, y / 20f));
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Cl);
                P(px, sz, 16 - hw, y, C2); P(px, sz, 16 + hw, y, C2);
            }
            // 披风下内层高光
            for (int y = 1; y <= 8; y++)
                P(px, sz, 16, y, C2);
            // 躯干（披风下）
            Rect(px, sz, 13, 10, 19, 21, C1);
            // 头（兜帽）
            Circle(px, sz, 16, 25, 5, O);
            Circle(px, sz, 16, 25, 4, C1);
            // 面罩/口罩
            Rect(px, sz, 12, 23, 20, 25, O);
            Rect(px, sz, 13, 23, 19, 24, C(42, 16, 68));
            // 发光双眼
            P(px, sz, 14, 26, Ey); P(px, sz, 15, 26, Ey);
            P(px, sz, 17, 26, Ey); P(px, sz, 18, 26, Ey);
            // 兜帽尖角
            Line(px, sz, 16, 29, 13, 31, O);
            Line(px, sz, 16, 29, 19, 31, O);
            Rect(px, sz, 14, 30, 18, 31, C1);
            // 左匕首
            Line(px, sz,  8, 22,  8,  8, O);
            Line(px, sz,  9, 22,  9,  8, Sl);
            Rect(px, sz,  7, 21, 11, 22, O);
            Rect(px, sz,  8, 21, 10, 21, Sl);
            // 右匕首
            Line(px, sz, 23, 22, 23,  8, O);
            Line(px, sz, 24, 22, 24,  8, Sl);
            Rect(px, sz, 21, 21, 25, 22, O);
            Rect(px, sz, 22, 21, 24, 21, Sl);
            // 腿（从披风下露出）
            Rect(px, sz, 13, 2, 15, 10, O); Rect(px, sz, 14, 2, 14,  9, C2);
            Rect(px, sz, 17, 2, 19, 10, O); Rect(px, sz, 18, 2, 18,  9, C2);
            // 靴尖
            Rect(px, sz, 12, 0, 16, 3, O); Rect(px, sz, 13, 0, 15, 2, O);
            Rect(px, sz, 16, 0, 20, 3, O); Rect(px, sz, 17, 0, 19, 2, O);
        }

        // ── 爆炎恶魔：炎橙圆体 + 双角 + 炎纹 + 导火索 ──────────────
        static void DrawExplosiveDemon(Color32[] px, int sz)
        {
            var O   = C( 78,  14,   4);  // 深红轮廓
            var Bd  = C(218, 100,  18);  // 橙色躯体
            var Y   = C(255, 198,  48);  // 炎黄
            var R   = C(188,  38,   4);  // 暗红炎纹
            var Hn  = C(138,  24,   8);  // 角
            var Ey  = C(255, 228,  18);  // 眼睛发光
            var Fus = C(178, 158, 128);  // 导火索
            var Sp  = C(255, 218,  98);  // 导火索火星

            // 双角（往上伸展）
            for (int i = 0; i < 7; i++)
            {
                int w2 = 2 - i / 3;
                Rect(px, sz, 10 - i / 2 - w2, 24 + i, 10 - i / 2 + w2, 24 + i, Hn);
                Rect(px, sz, 22 + i / 2 - w2, 24 + i, 22 + i / 2 + w2, 24 + i, Hn);
            }
            // 主体大圆
            Circle(px, sz, 16, 15, 12, O);
            Circle(px, sz, 16, 15, 11, Bd);
            // 内层炎光
            Circle(px, sz, 16, 15,  7, Y);
            Circle(px, sz, 16, 15,  4, C(255, 238, 178));
            // 放射炎纹（8 条）
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 10);
                int ey = 15 + Mathf.RoundToInt(Mathf.Sin(ang) * 10);
                Line(px, sz, 16, 15, ex, ey, R);
            }
            // 炎心重绘（覆盖炎纹中心）
            Circle(px, sz, 16, 15, 5, Y);
            Circle(px, sz, 16, 15, 3, C(255, 238, 178));
            // 眼睛
            Circle(px, sz, 12, 17, 2, O); Circle(px, sz, 12, 17, 1, Ey);
            Circle(px, sz, 20, 17, 2, O); Circle(px, sz, 20, 17, 1, Ey);
            // 嘴部（咧嘴）
            Rect(px, sz, 11, 11, 21, 12, O);
            Rect(px, sz, 12, 12, 20, 12, C(195, 28, 4));
            for (int tx = 12; tx <= 20; tx += 2)
                P(px, sz, tx, 12, C(238, 232, 215));  // 牙齿
            // 导火索（顶部）
            Line(px, sz, 16, 27, 20, 31, Fus);
            P(px, sz, 20, 31, Sp); P(px, sz, 21, 31, Sp); P(px, sz, 19, 31, Sp);
            // 矮小脚爪
            Circle(px, sz, 12, 4, 3, O); Circle(px, sz, 12, 4, 2, R);
            Circle(px, sz, 20, 4, 3, O); Circle(px, sz, 20, 4, 2, R);
        }
    }
}
