using UnityEngine;
using System.Collections.Generic;

namespace Game.Dev
{
    public enum ProjectileType
    {
        Arrow,          // 普通箭矢（弓系基础攻击、Archer）
        PoisonBolt,     // 毒素飞弹（PoisonShaman）
        MagicOrb,       // 魔法法球（法杖基础攻击、Witch）
        SoulOrb,        // 灵魂汲取（Necromancer）
        IceMissile,     // 冰霜飞弹（FrostLich 基础攻击）
        IceSpike,       // 冰锥（FrostLich 冰锥齐射）
        FrostSpear,     // 冰枪（FrostThrust 技能）
        ThunderArrow,   // 落雷箭（ThunderShot 技能）
        PiercingArrow,  // 穿云箭（PiercingArrow 技能）
        RainArrow,      // 箭雨（RainOfArrows 技能）
    }

    public enum SkillEffectType
    {
        VenomCloud,     // 毒液喷射
        HolyFlash,      // 圣光斩
        DragonWave,     // 龙渊斩波
        EarthCrack,     // 大地震荡
        DoomColumn,     // 毁灭天降
        FrostBurst,     // 冰霜新星 / 冰枪突刺
        ChaosBlast,     // 混沌爆发
        PhantomSlash,   // 幻影连斩
        WarCryRing,     // 战吼 / 落雷冲击
        ArcaneBurst,    // 奥术迸发
        HolyAura,       // 神圣之光
        ShadowBlur,     // 影步残影
        ArrowImpact,    // 箭雨落点闪光
        MeleeSlash,     // 近战挥砍弧光（通用）
    }

    // 程序化生成 32×32 技能/投掷物像素精灵
    // 坐标规则：x 增大 = 视觉右方；y 增大 = 视觉上方；
    // 投掷物朝右绘制（x=31 侧为弹头/箭尖），随飞行方向自动旋转
    public static class SkillSprites
    {
        static readonly Dictionary<ProjectileType,  Sprite> _pCache = new Dictionary<ProjectileType,  Sprite>();
        static readonly Dictionary<SkillEffectType, Sprite> _eCache = new Dictionary<SkillEffectType, Sprite>();

        public static Sprite GetProjectile(ProjectileType t)
        {
            if (!_pCache.TryGetValue(t, out var s)) _pCache[t] = s = BuildProjectile(t);
            return s;
        }

        public static Sprite GetEffect(SkillEffectType t)
        {
            if (!_eCache.TryGetValue(t, out var s)) _eCache[t] = s = BuildEffect(t);
            return s;
        }

        // ── 投掷物精灵 ────────────────────────────────────────────────────────
        static Sprite BuildProjectile(ProjectileType t)
        {
            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            switch (t)
            {
                case ProjectileType.Arrow:         DrawArrow(px, SZ);         break;
                case ProjectileType.PoisonBolt:    DrawPoisonBolt(px, SZ);    break;
                case ProjectileType.MagicOrb:      DrawMagicOrb(px, SZ);      break;
                case ProjectileType.SoulOrb:       DrawSoulOrb(px, SZ);       break;
                case ProjectileType.IceMissile:    DrawIceMissile(px, SZ);    break;
                case ProjectileType.IceSpike:      DrawIceSpike(px, SZ);      break;
                case ProjectileType.FrostSpear:    DrawFrostSpear(px, SZ);    break;
                case ProjectileType.ThunderArrow:  DrawThunderArrow(px, SZ);  break;
                case ProjectileType.PiercingArrow: DrawPiercingArrow(px, SZ); break;
                case ProjectileType.RainArrow:     DrawRainArrow(px, SZ);     break;
            }
            return MakeSprite(px, SZ);
        }

        // ── 技能特效精灵 ───────────────────────────────────────────────────────
        static Sprite BuildEffect(SkillEffectType t)
        {
            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            switch (t)
            {
                case SkillEffectType.VenomCloud:   DrawVenomCloud(px, SZ);   break;
                case SkillEffectType.HolyFlash:    DrawHolyFlash(px, SZ);    break;
                case SkillEffectType.DragonWave:   DrawDragonWave(px, SZ);   break;
                case SkillEffectType.EarthCrack:   DrawEarthCrack(px, SZ);   break;
                case SkillEffectType.DoomColumn:   DrawDoomColumn(px, SZ);   break;
                case SkillEffectType.FrostBurst:   DrawFrostBurst(px, SZ);   break;
                case SkillEffectType.ChaosBlast:   DrawChaosBlast(px, SZ);   break;
                case SkillEffectType.PhantomSlash: DrawPhantomSlash(px, SZ); break;
                case SkillEffectType.WarCryRing:   DrawWarCryRing(px, SZ);   break;
                case SkillEffectType.ArcaneBurst:  DrawArcaneBurst(px, SZ);  break;
                case SkillEffectType.HolyAura:     DrawHolyAura(px, SZ);     break;
                case SkillEffectType.ShadowBlur:   DrawShadowBlur(px, SZ);   break;
                case SkillEffectType.ArrowImpact:  DrawArrowImpact(px, SZ);  break;
                case SkillEffectType.MeleeSlash:   DrawMeleeSlash(px, SZ);   break;
            }
            return MakeSprite(px, SZ);
        }

        // ── 绘图原语 ───────────────────────────────────────────────────────────
        static void Rect(Color32[] p, int z, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(z - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(z - 1, x1); x++)
                p[y * z + x] = c;
        }
        static void Circle(Color32[] p, int z, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(z - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(z - 1, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) p[y * z + x] = c;
        }
        static void Ring(Color32[] p, int z, int cx, int cy, int ro, int ri, Color32 c)
        {
            int ro2 = ro * ro, ri2 = ri * ri;
            for (int y = Mathf.Max(0, cy - ro); y <= Mathf.Min(z - 1, cy + ro); y++)
            for (int x = Mathf.Max(0, cx - ro); x <= Mathf.Min(z - 1, cx + ro); x++)
            {
                int d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d <= ro2 && d > ri2) p[y * z + x] = c;
            }
        }
        static void Line(Color32[] p, int z, int x0, int y0, int x1, int y1, Color32 c, int thick = 1)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, e = dx - dy;
            int h = thick / 2;
            for (;;)
            {
                for (int ty = -h; ty <= h; ty++)
                for (int tx = -h; tx <= h; tx++)
                    P(p, z, x0 + tx, y0 + ty, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * e;
                if (e2 > -dy) { e -= dy; x0 += sx; }
                if (e2 <  dx) { e += dx; y0 += sy; }
            }
        }
        static void P(Color32[] p, int z, int x, int y, Color32 c)
        { if (x >= 0 && x < z && y >= 0 && y < z) p[y * z + x] = c; }
        static Color32 C(int r, int g, int b, int a = 255) =>
            new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        static Color32 Dk(Color32 c) =>
            new Color32((byte)(c.r * 0.55f), (byte)(c.g * 0.55f), (byte)(c.b * 0.55f), c.a);
        static Color32 Lk(Color32 c) =>
            new Color32((byte)Mathf.Min(255, c.r * 1.4f), (byte)Mathf.Min(255, c.g * 1.4f),
                        (byte)Mathf.Min(255, c.b * 1.4f), c.a);
        static Sprite MakeSprite(Color32[] px, int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz / 2);
        }

        // ════════════════════════════════════════════════════════
        //  投掷物
        // ════════════════════════════════════════════════════════

        // 普通箭矢：棕色箭杆，金色三角箭头，红色箭羽，朝右飞行
        static void DrawArrow(Color32[] px, int sz)
        {
            var shaft = C(158, 112, 52);
            var head  = C(212, 178, 72);
            var hilit = C(242, 218, 128);
            var feath = C(205, 52, 52);
            var fdark = C(135, 30, 30);
            // 箭杆
            Rect(px, sz,  5, 14, 22, 16, shaft);
            Line(px, sz,  5, 13, 22, 13, Dk(shaft));        // 下轮廓线
            // 箭头（三角形朝右）
            Rect(px, sz, 22, 12, 25, 18, head);             // 箭头基部
            Rect(px, sz, 26, 13, 28, 17, head);             // 中段
            Rect(px, sz, 29, 14, 30, 16, hilit);            // 窄段高光
            P(px, sz, 31, 15, C(255, 248, 200));            // 尖端
            Line(px, sz, 22, 18, 31, 15, hilit);            // 上边缘高光
            Line(px, sz, 22, 12, 28, 14, Dk(head));         // 下边缘暗线
            // 箭羽（上下各一片）
            Rect(px, sz,  2, 18,  7, 20, feath);
            Rect(px, sz,  2, 11,  7, 13, feath);
            Line(px, sz,  2, 20,  7, 18, fdark);
            Line(px, sz,  2, 11,  7, 13, fdark);
            Rect(px, sz,  4, 14,  6, 16, Dk(shaft));        // 羽根连接
        }

        // 毒素飞弹：绿色球形，底部毒滴
        static void DrawPoisonBolt(Color32[] px, int sz)
        {
            var outer = C(18, 95, 16);
            var mid   = C(44, 172, 36);
            var core  = C(92, 242, 68);
            var hi    = C(188, 255, 152);
            var drip  = C(28, 136, 22);
            Circle(px, sz, 16, 18, 8, outer);
            Circle(px, sz, 16, 18, 6, mid);
            Circle(px, sz, 16, 18, 3, core);
            P(px, sz, 13, 22, hi); P(px, sz, 14, 23, hi);   // 高光点
            // 底部毒滴
            Circle(px, sz, 16,  9, 2, drip);
            P(px, sz, 14,  7, drip); P(px, sz, 18,  6, drip);
            // 外发光环
            Ring(px, sz, 16, 18, 9, 8, C(50, 200, 40, 140));
        }

        // 魔法法球：紫色球体，内层发光
        static void DrawMagicOrb(Color32[] px, int sz)
        {
            var outer = C(58, 0, 138);
            var mid   = C(142, 32, 218);
            var core  = C(202, 138, 255);
            var white = C(255, 255, 255);
            Circle(px, sz, 16, 16, 10, outer);
            Circle(px, sz, 16, 16,  7, mid);
            Circle(px, sz, 16, 16,  4, core);
            Circle(px, sz, 16, 16,  2, white);
            P(px, sz, 12, 21, C(238, 212, 255));             // 高光
            P(px, sz, 11, 22, white);
            Ring(px, sz, 16, 16, 11, 10, C(102, 22, 178, 145)); // 外发光
        }

        // 灵魂汲取：暗紫球体，绿色魂核
        static void DrawSoulOrb(Color32[] px, int sz)
        {
            var outer = C(48, 0, 88);
            var mid   = C(102, 0, 152);
            var soul  = C(48, 228, 98);
            var sbrit = C(152, 255, 182);
            Circle(px, sz, 16, 16, 9, outer);
            Circle(px, sz, 16, 16, 6, mid);
            Circle(px, sz, 16, 16, 3, soul);
            Circle(px, sz, 16, 16, 1, sbrit);
            // 旋转漩涡纹
            for (int a = 0; a < 360; a += 60)
            {
                float rad = a * Mathf.Deg2Rad;
                P(px, sz, 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 5),
                          16 + Mathf.RoundToInt(Mathf.Sin(rad) * 5), mid);
            }
            Ring(px, sz, 16, 16, 10, 9, C(80, 0, 120, 130));
        }

        // 冰霜飞弹：蓝色球+向右尖端
        static void DrawIceMissile(Color32[] px, int sz)
        {
            var ice   = C(68, 162, 238);
            var lite  = C(178, 228, 255);
            var white = C(238, 250, 255);
            // 圆形主体
            Circle(px, sz, 13, 15, 7, ice);
            Circle(px, sz, 13, 15, 5, lite);
            Circle(px, sz, 13, 15, 2, white);
            // 向右的尖端/尾迹
            Rect(px, sz, 19, 14, 27, 16, ice);
            Rect(px, sz, 21, 15, 27, 15, lite);
            P(px, sz, 28, 14, lite); P(px, sz, 28, 16, lite);
            P(px, sz, 29, 15, white); P(px, sz, 30, 15, white);
        }

        // 冰锥：水平菱形冰晶（朝右尖）
        static void DrawIceSpike(Color32[] px, int sz)
        {
            var ice   = C(98, 202, 255);
            var lite  = C(198, 240, 255);
            var white = C(255, 255, 255);
            // 菱形：中心x=16，朝右尖
            for (int i = 0; i <= 8; i++)
            {
                int hw = 8 - i;
                Rect(px, sz, 8 + i, 15 - hw, 8 + i, 15 + hw, ice);
            }
            for (int i = 0; i <= 7; i++)
            {
                int hw = i;
                Rect(px, sz, 16 + i, 15 - hw, 16 + i, 15 + hw, lite);
            }
            // 高光与尖端
            Rect(px, sz, 10, 14, 20, 16, lite);
            Line(px, sz, 10, 15, 22, 15, white);
            P(px, sz, 24, 15, white);
        }

        // 冰枪：长矛形冰矛（朝右）
        static void DrawFrostSpear(Color32[] px, int sz)
        {
            var shaft = C(62, 152, 228);
            var lite  = C(168, 212, 255);
            var white = C(242, 250, 255);
            // 长枪杆
            Rect(px, sz,  2, 14, 29, 16, shaft);
            Line(px, sz,  2, 15, 29, 15, lite);
            // 枪尖（向右三角）
            Rect(px, sz, 28, 13, 31, 17, shaft);
            Rect(px, sz, 29, 14, 31, 16, lite);
            P(px, sz, 30, 15, white); P(px, sz, 31, 15, white);
            // 冰晶装饰节
            Circle(px, sz, 10, 13, 2, lite);
            Circle(px, sz, 20, 17, 2, lite);
            P(px, sz,  8, 15, white); P(px, sz, 22, 15, white);
        }

        // 落雷箭：黄色箭+闪电装饰（朝右）
        static void DrawThunderArrow(Color32[] px, int sz)
        {
            var yel   = C(255, 218, 0);
            var brite = C(255, 255, 148);
            var elec  = C(172, 218, 255);
            var feat  = C(255, 168, 0);
            // 箭杆
            Rect(px, sz,  4, 14, 24, 16, yel);
            Line(px, sz,  4, 13, 24, 13, Dk(yel));
            // 箭头
            Rect(px, sz, 24, 12, 27, 18, yel);
            Rect(px, sz, 28, 13, 30, 17, brite);
            P(px, sz, 31, 15, C(255, 255, 255));
            Line(px, sz, 24, 18, 31, 15, brite);
            // 箭羽
            Rect(px, sz,  2, 18,  7, 20, feat);
            Rect(px, sz,  2, 11,  7, 13, feat);
            // 闪电纹
            P(px, sz, 14, 12, elec); P(px, sz, 17, 11, elec); P(px, sz, 20, 12, elec);
            P(px, sz, 15, 19, elec); P(px, sz, 18, 20, elec); P(px, sz, 21, 19, elec);
            Line(px, sz, 12, 12, 16, 11, elec);
            Line(px, sz, 16, 11, 14,  9, elec);
        }

        // 穿云箭：极细金色长箭（朝右），带光晕
        static void DrawPiercingArrow(Color32[] px, int sz)
        {
            var gold  = C(222, 188, 48);
            var lite  = C(255, 232, 122);
            var white = C(255, 248, 198);
            var glow  = C(255, 242, 178, 155);
            // 极细箭杆（2px）
            Rect(px, sz,  2, 15, 28, 16, gold);
            Line(px, sz,  2, 14, 28, 14, Dk(gold));
            // 锐利箭头
            Rect(px, sz, 27, 14, 30, 16, lite);
            P(px, sz, 29, 15, white); P(px, sz, 30, 15, white); P(px, sz, 31, 15, white);
            P(px, sz, 30, 14, lite); P(px, sz, 30, 16, lite);
            // 纤细尾羽
            Rect(px, sz,  2, 12,  5, 13, gold);
            Rect(px, sz,  2, 18,  5, 19, gold);
            // 金色光晕
            P(px, sz, 10, 14, glow); P(px, sz, 10, 17, glow);
            P(px, sz, 18, 14, glow); P(px, sz, 18, 17, glow);
        }

        // 箭雨：粗重降落箭，带蓝雨滴（朝右）
        static void DrawRainArrow(Color32[] px, int sz)
        {
            var brn  = C(152, 106, 42);
            var dark = C(98, 70, 25);
            var blue = C(125, 185, 255, 198);
            // 粗箭杆 y=13-17
            Rect(px, sz,  3, 13, 22, 17, brn);
            Line(px, sz,  3, 12, 22, 12, dark);
            // 箭头
            Rect(px, sz, 22, 11, 27, 19, brn);
            Rect(px, sz, 27, 12, 30, 18, C(188, 148, 58));
            P(px, sz, 30, 15, C(218, 188, 98)); P(px, sz, 31, 15, C(238, 218, 128));
            // 尾羽
            Rect(px, sz,  2, 19,  7, 21, dark);
            Rect(px, sz,  2, 10,  7, 12, dark);
            // 雨滴
            P(px, sz,  9, 12, blue); P(px, sz,  9, 18, blue);
            P(px, sz, 15, 11, blue); P(px, sz, 15, 19, blue);
            Circle(px, sz, 12, 21, 1, blue);
            Circle(px, sz, 18,  9, 1, blue);
        }

        // ════════════════════════════════════════════════════════
        //  技能特效
        // ════════════════════════════════════════════════════════

        // 毒液喷射：翻滚绿色毒云
        static void DrawVenomCloud(Color32[] px, int sz)
        {
            var outer  = C(18, 98, 18, 228);
            var mid    = C(42, 168, 30, 218);
            var lite   = C(95, 238, 65, 212);
            var hi     = C(172, 255, 132, 198);
            var bubble = C(30, 142, 22, 178);
            Circle(px, sz, 16, 16, 13, outer);
            Circle(px, sz, 10, 17,  8, mid);
            Circle(px, sz, 21, 17,  8, mid);
            Circle(px, sz, 16, 11,  7, mid);
            Circle(px, sz, 12, 13,  6, lite);
            Circle(px, sz, 20, 14,  5, lite);
            Circle(px, sz, 16, 21,  5, lite);
            Circle(px, sz,  7, 22,  3, bubble);
            Circle(px, sz, 24, 22,  3, bubble);
            Circle(px, sz, 16,  5,  2, bubble);
            P(px, sz, 10, 18, hi); P(px, sz, 14, 12, hi); P(px, sz, 20, 19, hi);
        }

        // 圣光斩：金色十字光芒
        static void DrawHolyFlash(Color32[] px, int sz)
        {
            var glow  = C(255, 232, 78, 178);
            var mid   = C(255, 212, 48, 218);
            var core  = C(255, 248, 178, 238);
            var white = C(255, 255, 255, 255);
            // 水平+垂直十字
            Rect(px, sz,  0, 13, 31, 18, glow);
            Rect(px, sz, 13,  0, 18, 31, glow);
            Rect(px, sz,  2, 14, 29, 17, mid);
            Rect(px, sz, 14,  2, 17, 29, mid);
            Rect(px, sz,  4, 15, 27, 16, core);
            Rect(px, sz, 15,  4, 16, 27, core);
            // 中心核心
            Circle(px, sz, 16, 16, 5, core);
            Circle(px, sz, 16, 16, 3, white);
            // 对角线射线
            Line(px, sz,  5,  5, 11, 11, glow, 2);
            Line(px, sz, 21, 21, 27, 27, glow, 2);
            Line(px, sz,  5, 27, 11, 21, glow, 2);
            Line(px, sz, 21, 11, 27,  5, glow, 2);
        }

        // 龙渊斩波：深蓝月牙斩波
        static void DrawDragonWave(Color32[] px, int sz)
        {
            var outer = C(10, 24, 82, 218);
            var mid   = C(20, 78, 172, 208);
            var lite  = C(62, 152, 255, 198);
            var edge  = C(148, 208, 255, 178);
            // 月牙形（左半填充）
            Circle(px, sz, 14, 16, 14, outer);
            // 挖空右侧使其呈月牙状
            Circle(px, sz, 19, 16, 11, C(0, 0, 0, 0));
            // 波纹
            for (int y = 7; y <= 25; y += 4)
                Circle(px, sz, 6 + (y % 8 == 0 ? 2 : 0), y, 4, mid);
            Circle(px, sz, 13, 16, 9, mid);
            Circle(px, sz, 13, 16, 5, lite);
            Ring(px, sz, 14, 16, 14, 11, edge);
            // 斩痕高光
            Line(px, sz,  4,  8, 20, 24, C(198, 232, 255, 198), 2);
            Line(px, sz,  7,  5, 23, 21, C(198, 232, 255, 148));
        }

        // 大地震荡：棕色放射裂缝
        static void DrawEarthCrack(Color32[] px, int sz)
        {
            var earth = C(182, 122, 52, 218);
            var crack = C(52, 28, 8, 255);
            var dust  = C(192, 152, 98, 198);
            var glow  = C(255, 218, 108, 218);
            Circle(px, sz, 16, 16, 14, earth);
            // 8 条放射主裂缝
            int[] angles = { 0, 45, 90, 135, 180, 225, 270, 315 };
            foreach (int a in angles)
            {
                float rad = a * Mathf.Deg2Rad;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 13);
                int ey = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 13);
                Line(px, sz, 16, 16, ex, ey, crack, 2);
                // 二级分叉
                int mx = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 7);
                int my = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 7);
                float r2 = (a + 28) * Mathf.Deg2Rad;
                Line(px, sz, mx, my,
                     mx + Mathf.RoundToInt(Mathf.Cos(r2) * 5),
                     my + Mathf.RoundToInt(Mathf.Sin(r2) * 5), crack);
            }
            Circle(px, sz,  6,  6, 2, dust); Circle(px, sz, 26,  6, 2, dust);
            Circle(px, sz,  6, 26, 2, dust); Circle(px, sz, 26, 26, 2, dust);
            Circle(px, sz, 16, 16, 4, glow);
            Circle(px, sz, 16, 16, 2, C(255, 242, 198, 255));
        }

        // 毁灭天降：暗红能量柱从天而降
        static void DrawDoomColumn(Color32[] px, int sz)
        {
            var outer  = C(78, 0, 0, 218);
            var dark   = C(142, 0, 14, 228);
            var red    = C(212, 28, 0, 232);
            var orange = C(255, 102, 0, 238);
            var bright = C(255, 232, 98, 248);
            // 竖向能量柱
            Rect(px, sz,  8,  0, 23, 31, outer);
            Rect(px, sz, 10,  0, 21, 31, dark);
            Rect(px, sz, 12,  0, 19, 31, red);
            Rect(px, sz, 13,  0, 18, 31, orange);
            Rect(px, sz, 14,  0, 17, 31, bright);
            // 底部冲击波环
            Ring(px, sz, 16, 8, 13, 10, outer);
            Ring(px, sz, 16, 8, 10,  7, dark);
            Circle(px, sz, 16, 8,  5, red);
            Circle(px, sz, 16, 8,  3, orange);
            // 顶部火焰舌
            Circle(px, sz, 11, 28, 3, dark); Circle(px, sz, 21, 28, 3, dark);
            Circle(px, sz,  8, 26, 2, outer); Circle(px, sz, 24, 26, 2, outer);
        }

        // 冰霜新星：六方雪花爆炸
        static void DrawFrostBurst(Color32[] px, int sz)
        {
            var deep  = C(24, 98, 202, 218);
            var mid   = C(102, 192, 255, 208);
            var lite  = C(192, 232, 255, 202);
            var white = C(255, 255, 255, 255);
            Circle(px, sz, 16, 16, 13, deep);
            // 六方向雪花臂
            int[] as6 = { 0, 60, 120, 180, 240, 300 };
            foreach (int a in as6)
            {
                float rad = a * Mathf.Deg2Rad;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 12);
                int ey = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 12);
                Line(px, sz, 16, 16, ex, ey, mid, 2);
                Circle(px, sz, ex, ey, 2, lite);
                // 横向分支
                float br = (a + 90) * Mathf.Deg2Rad;
                int mx = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 7);
                int my = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 7);
                Line(px, sz, mx, my,
                     mx + Mathf.RoundToInt(Mathf.Cos(br) * 3),
                     my + Mathf.RoundToInt(Mathf.Sin(br) * 3), lite, 2);
                Line(px, sz, mx, my,
                     mx - Mathf.RoundToInt(Mathf.Cos(br) * 3),
                     my - Mathf.RoundToInt(Mathf.Sin(br) * 3), lite, 2);
            }
            Circle(px, sz, 16, 16, 4, lite);
            Circle(px, sz, 16, 16, 2, white);
        }

        // 混沌爆发：四象限彩色爆炸
        static void DrawChaosBlast(Color32[] px, int sz)
        {
            var fire   = C(255, 82, 0, 218);
            var elec   = C(152, 218, 255, 212);
            var poison = C(48, 212, 22, 212);
            var dark   = C(128, 0, 202, 212);
            var white  = C(255, 255, 255, 255);
            int r2 = 13 * 13;
            for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
            {
                int dx = x - 16, dy = y - 16;
                if (dx * dx + dy * dy > r2) continue;
                if      (dx >= 0 && dy >= 0) px[y * sz + x] = fire;
                else if (dx <  0 && dy >= 0) px[y * sz + x] = elec;
                else if (dx <  0 && dy <  0) px[y * sz + x] = poison;
                else                         px[y * sz + x] = dark;
            }
            // 放射白线
            int[] as8 = { 0, 45, 90, 135, 180, 225, 270, 315 };
            foreach (int a in as8)
            {
                float rad = a * Mathf.Deg2Rad;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 15);
                int ey = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 15);
                Line(px, sz, 16, 16, ex, ey, white);
            }
            Circle(px, sz, 16, 16, 4, white);
        }

        // 幻影连斩：三道紫色斜刀光
        static void DrawPhantomSlash(Color32[] px, int sz)
        {
            var dk    = C(88, 0, 138, 198);
            var mid   = C(168, 48, 228, 212);
            var lite  = C(218, 152, 255, 222);
            var white = C(255, 242, 255, 238);
            // 三道从左下到右上的斜线刀光
            for (int off = -5; off <= 5; off++) Line(px, sz,  2 + off,  2, 16 + off, 30, dk);
            for (int off = -4; off <= 4; off++) Line(px, sz,  8 + off,  2, 22 + off, 30, mid);
            for (int off = -3; off <= 3; off++) Line(px, sz, 14 + off,  2, 28 + off, 30, lite);
            Line(px, sz,  2,  2, 16, 30, white, 2);
            Line(px, sz, 14,  2, 28, 30, white, 2);
        }

        // 战吼：红色同心冲击波环
        static void DrawWarCryRing(Color32[] px, int sz)
        {
            var dk     = C(138, 14, 14, 218);
            var red    = C(212, 48, 14, 228);
            var orange = C(255, 128, 38, 232);
            var glow   = C(255, 232, 128, 198);
            Ring(px, sz, 16, 16, 14, 10, dk);
            Ring(px, sz, 16, 16, 12,  9, red);
            Ring(px, sz, 16, 16, 11,  8, orange);
            // 8方向环形刺
            int[] as8 = { 0, 45, 90, 135, 180, 225, 270, 315 };
            foreach (int a in as8)
            {
                float rad = a * Mathf.Deg2Rad;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 14);
                int ey = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 14);
                Circle(px, sz, ex, ey, 2, orange);
            }
            Circle(px, sz, 16, 16, 5, glow);
            Circle(px, sz, 16, 16, 2, orange);
        }

        // 奥术迸发：紫色八角星爆炸
        static void DrawArcaneBurst(Color32[] px, int sz)
        {
            var dk    = C(48, 0, 128, 212);
            var mid   = C(128, 28, 212, 222);
            var lite  = C(192, 112, 255, 228);
            var white = C(255, 255, 255, 255);
            Circle(px, sz, 16, 16, 11, dk);
            // 8方向星芒
            int[] as8  = { 0, 45, 90, 135, 180, 225, 270, 315 };
            int[] as8b = { 22, 67, 112, 157, 202, 247, 292, 337 };
            for (int i = 0; i < 8; i++)
            {
                float r1 = as8[i]  * Mathf.Deg2Rad;
                float r2 = as8b[i] * Mathf.Deg2Rad;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(r1) * 15);
                int ey = 16 + Mathf.RoundToInt(Mathf.Sin(r1) * 15);
                int mx = 16 + Mathf.RoundToInt(Mathf.Cos(r2) * 8);
                int my = 16 + Mathf.RoundToInt(Mathf.Sin(r2) * 8);
                Line(px, sz, mx, my, ex, ey, mid, 2);
                Line(px, sz, mx, my, ex, ey, lite);
                Circle(px, sz, ex, ey, 2, lite);
            }
            Circle(px, sz, 16, 16, 7, mid);
            Circle(px, sz, 16, 16, 4, lite);
            Circle(px, sz, 16, 16, 2, white);
        }

        // 神圣之光：金色光芒+光环
        static void DrawHolyAura(Color32[] px, int sz)
        {
            var gold   = C(255, 222, 52, 208);
            var bright = C(255, 248, 152, 228);
            var white  = C(255, 255, 255, 255);
            var fill   = C(255, 242, 138, 98);
            Circle(px, sz, 16, 16, 13, fill);
            // 12方向射线
            for (int a = 0; a < 360; a += 30)
            {
                float rad = a * Mathf.Deg2Rad;
                int ix = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 5);
                int iy = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 5);
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(rad) * 14);
                int ey = 16 + Mathf.RoundToInt(Mathf.Sin(rad) * 14);
                Line(px, sz, ix, iy, ex, ey, gold, 2);
                P(px, sz, ex, ey, bright);
            }
            Ring(px, sz, 16, 16, 13, 10, gold);
            Circle(px, sz, 16, 16, 6, gold);
            Circle(px, sz, 16, 16, 4, bright);
            Circle(px, sz, 16, 16, 2, white);
        }

        // 影步残影：黑暗人形残像
        static void DrawShadowBlur(Color32[] px, int sz)
        {
            var black  = C(14, 9, 24, 212);
            var shadow = C(38, 14, 72, 198);
            var lite   = C(88, 62, 142, 182);
            var purple = C(138, 102, 192, 162);
            // 多层偏移残影（向左）
            Circle(px, sz,  8, 16, 8, C(14, 7, 24, 118));
            Circle(px, sz, 12, 16, 8, C(24, 11, 44, 158));
            Circle(px, sz, 16, 16, 9, black);
            // 粗略人形剪影
            Circle(px, sz, 16, 23, 5, black);
            Circle(px, sz, 16, 28, 3, black);
            Rect(px, sz, 13, 14, 18, 22, black);
            Rect(px, sz, 11, 12, 14, 16, black);
            Rect(px, sz, 18, 12, 21, 16, black);
            // 内部发光
            Circle(px, sz, 16, 16, 4, shadow);
            Circle(px, sz, 16, 16, 2, lite);
            // 残影尾迹
            Circle(px, sz,  5, 16, 4, purple);
            Circle(px, sz,  2, 16, 2, C(78, 52, 122, 98));
        }

        // 箭雨落点：小型金色冲击闪光
        static void DrawArrowImpact(Color32[] px, int sz)
        {
            var dk    = C(182, 152, 18, 218);
            var gold  = C(242, 202, 38, 232);
            var brite = C(255, 238, 148, 242);
            var white = C(255, 255, 255, 255);
            // 十字冲击
            Rect(px, sz,  5, 14, 26, 17, dk);
            Rect(px, sz, 14,  5, 17, 26, dk);
            Rect(px, sz,  8, 15, 23, 16, gold);
            Rect(px, sz, 15,  8, 16, 23, gold);
            // 对角线小射线
            Line(px, sz,  8,  8, 12, 12, dk, 2);
            Line(px, sz, 20, 20, 24, 24, dk, 2);
            Line(px, sz,  8, 24, 12, 20, dk, 2);
            Line(px, sz, 20, 12, 24,  8, dk, 2);
            // 中心核
            Circle(px, sz, 16, 16, 4, brite);
            Circle(px, sz, 16, 16, 2, white);
        }

        // 近战挥砍弧光：朝右扇形弧，使用时由调用方旋转到攻击方向
        static void DrawMeleeSlash(Color32[] px, int sz)
        {
            int cx = sz / 2, cy = sz / 2;
            // 绘制右侧扇形弧（±50° 范围，r=4~14，峰值 r=9）
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                int dx = x - cx, dy = y - cy;
                if (dx <= 0) continue;
                // 限制在 ±50° 锥形内：|dy| <= dx * 1.19（tan50°≈1.19）
                int adx = dx < 0 ? -dx : dx;
                int ady = dy < 0 ? -dy : dy;
                if (ady * 5 > adx * 6) continue;
                int d2 = dx * dx + dy * dy;
                if (d2 < 16 || d2 > 196) continue;  // r ∈ [4, 14]
                float d  = Mathf.Sqrt(d2);
                float t  = 1f - Mathf.Abs(d - 9f) / 5f;
                if (t <= 0f) continue;
                byte a = (byte)(230 * t);
                byte r = 255;
                byte g = (byte)(200 + 55 * t);
                byte b = (byte)(60 * (1f - t));
                px[y * sz + x] = new Color32(r, g, b, a);
            }
            // 中心刀光主线（粗亮线）
            Line(px, sz, cx - 1, cy, cx + 14, cy, C(255, 255, 230, 245), 3);
            // 弧顶尖端光晕
            Circle(px, sz, cx + 14, cy, 2, C(255, 255, 255, 200));
            // 拖尾余光短线（上下各一道，体现扫掠感）
            Line(px, sz, cx + 2, cy + 4, cx + 12, cy + 7, C(255, 220, 80, 160), 2);
            Line(px, sz, cx + 2, cy - 4, cx + 12, cy - 7, C(255, 220, 80, 160), 2);
        }
    }
}
