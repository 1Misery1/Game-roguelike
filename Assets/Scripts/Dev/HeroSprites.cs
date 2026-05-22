using UnityEngine;
using System.Collections.Generic;

namespace Game.Dev
{
    // 程序化生成 32×32 英雄像素精灵图（5 职业）
    // y=0 在贴图底部 = 世界空间底部（脚在下头在上）
    public static class HeroSprites
    {
        private static readonly Dictionary<string, Sprite> _cache     = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _cacheBack = new Dictionary<string, Sprite>();

        public static Sprite Get(string heroName)
        {
            if (_cache.TryGetValue(heroName, out var s)) return s;
            s = Build(heroName);
            if (s != null) _cache[heroName] = s;
            return s;
        }

        public static Sprite GetBack(string heroName)
        {
            if (_cacheBack.TryGetValue(heroName, out var s)) return s;
            s = BuildBack(heroName);
            if (s != null) _cacheBack[heroName] = s;
            return s;
        }

        private static Sprite BuildBack(string name)
        {
            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            switch (name)
            {
                case "Warrior": DrawWarriorBack(px, SZ); break;
                case "Ranger":  DrawRangerBack(px, SZ);  break;
                case "Mage":    DrawMageBack(px, SZ);    break;
                case "Paladin": DrawPaladinBack(px, SZ); break;
                case "Hunter":  DrawHunterBack(px, SZ);  break;
                default: return Get(name); // fallback to front
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ / 2);
        }

        static string RealSpritePath(string name) => name switch
        {
            "Warrior" => "Heroes/Warrior",
            "Ranger"  => "Heroes/Ranger",
            "Mage"    => "Heroes/Mage",
            "Paladin" => "Heroes/Paladin",
            "Hunter"  => "Heroes/Hunter",
            _         => null,
        };

        private static Sprite Build(string name)
        {
            var path = RealSpritePath(name);
            if (path != null)
            {
                var loadedTex = Resources.Load<Texture2D>(path);
                if (loadedTex != null)
                {
                    loadedTex.filterMode = FilterMode.Point;
                    return Sprite.Create(loadedTex, new Rect(0, 0, loadedTex.width, loadedTex.height),
                                         new Vector2(0.5f, 0.5f), loadedTex.width / 2f);
                }
            }

            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            switch (name)
            {
                case "Warrior": DrawWarrior(px, SZ); break;
                case "Ranger":  DrawRanger(px, SZ);  break;
                case "Mage":    DrawMage(px, SZ);    break;
                case "Paladin": DrawPaladin(px, SZ); break;
                case "Hunter":  DrawHunter(px, SZ);  break;
                default: return null;
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ / 2);
        }

        // ── 原语 ─────────────────────────────────────────────────────

        static void Circle(Color32[] px, int sz, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int y = Mathf.Max(0, cy-r); y <= Mathf.Min(sz-1, cy+r); y++)
            for (int x = Mathf.Max(0, cx-r); x <= Mathf.Min(sz-1, cx+r); x++)
                if ((x-cx)*(x-cx) + (y-cy)*(y-cy) <= r2) px[y*sz+x] = c;
        }
        static void Oval(Color32[] px, int sz, int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y = Mathf.Max(0, cy-ry); y <= Mathf.Min(sz-1, cy+ry); y++)
            for (int x = Mathf.Max(0, cx-rx); x <= Mathf.Min(sz-1, cx+rx); x++)
            { float dx=(x-cx)/(float)rx, dy=(y-cy)/(float)ry; if (dx*dx+dy*dy<=1f) px[y*sz+x]=c; }
        }
        static void Rect(Color32[] px, int sz, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y=Mathf.Max(0,y0); y<=Mathf.Min(sz-1,y1); y++)
            for (int x=Mathf.Max(0,x0); x<=Mathf.Min(sz-1,x1); x++) px[y*sz+x]=c;
        }
        static void Line(Color32[] px, int sz, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx=Mathf.Abs(x1-x0), dy=Mathf.Abs(y1-y0), sx=x0<x1?1:-1, sy=y0<y1?1:-1, err=dx-dy;
            for(;;){ if(x0>=0&&x0<sz&&y0>=0&&y0<sz) px[y0*sz+x0]=c; if(x0==x1&&y0==y1) break;
                int e2=2*err; if(e2>-dy){err-=dy;x0+=sx;} if(e2<dx){err+=dx;y0+=sy;} }
        }
        static void P(Color32[] px, int sz, int x, int y, Color32 c)
        { if(x>=0&&x<sz&&y>=0&&y<sz) px[y*sz+x]=c; }
        static Color32 C(int r, int g, int b, int a=255) => new Color32((byte)r,(byte)g,(byte)b,(byte)a);
        static Color32 Lk(Color32 c) => new Color32(
            (byte)Mathf.Min(255,(int)(c.r*1.35f)), (byte)Mathf.Min(255,(int)(c.g*1.35f)),
            (byte)Mathf.Min(255,(int)(c.b*1.35f)), c.a);
        static Color32 Dk(Color32 c) => new Color32(
            (byte)(c.r*0.62f), (byte)(c.g*0.62f), (byte)(c.b*0.62f), c.a);

        // ── 战士：蓝钢板甲 + 全封闭头盔 + 巨剑 ─────────────────────
        static void DrawWarrior(Color32[] px, int sz)
        {
            var O  = C( 18,  38,  78);   // 深蓝轮廓
            var A  = C( 68, 125, 192);   // 甲蓝
            var L  = C(138, 192, 242);   // 高光
            var D  = C( 35,  68, 125);   // 暗纹
            var M  = C(152, 158, 172);   // 金属银
            var Sw = C(178, 182, 195);   // 剑钢

            // 战靴
            Rect(px, sz, 11, 1, 15, 4, O); Rect(px, sz, 12, 1, 14, 3, M);
            Rect(px, sz, 17, 1, 21, 4, O); Rect(px, sz, 18, 1, 20, 3, M);
            // 腿甲
            Rect(px, sz, 12, 4, 15, 9, O); Rect(px, sz, 13, 4, 14, 8, A);
            Rect(px, sz, 17, 4, 20, 9, O); Rect(px, sz, 18, 4, 19, 8, A);
            // 腰裙（fauld）
            Rect(px, sz, 11, 8, 21, 10, O); Rect(px, sz, 12, 8, 20, 9, D);
            // 胸甲
            Rect(px, sz, 10, 10, 22, 20, O);
            Rect(px, sz, 11, 11, 21, 19, A);
            Line(px, sz, 12, 18, 12, 12, L);
            Line(px, sz, 20, 18, 20, 12, D);
            Circle(px, sz, 16, 15, 2, O); Circle(px, sz, 16, 15, 1, L);  // 徽章
            // 肩甲
            Oval(px, sz,  9, 18, 4, 3, O); Oval(px, sz,  9, 18, 3, 2, A);
            Oval(px, sz, 23, 18, 4, 3, O); Oval(px, sz, 23, 18, 3, 2, A);
            // 颈甲
            Rect(px, sz, 14, 20, 18, 23, O); Rect(px, sz, 15, 20, 17, 22, D);
            // 全封头盔（大型盔）
            Rect(px, sz, 11, 23, 21, 31, O);
            Rect(px, sz, 12, 23, 20, 30, A);
            Rect(px, sz, 12, 29, 20, 31, Lk(A));    // 盔顶高光
            // T形面甲缝
            Rect(px, sz, 12, 26, 20, 27, O);         // 横槽
            P(px, sz, 16, 25, O); P(px, sz, 16, 27, O); P(px, sz, 16, 28, O);
            // 头盔脊饰
            Rect(px, sz, 14, 30, 18, 31, M);
            // 右侧腰间剑柄
            Rect(px, sz, 24, 6, 26, 18, O);
            Rect(px, sz, 25, 6, 25, 18, Sw);
            Rect(px, sz, 23, 16, 27, 18, O); Rect(px, sz, 24, 17, 26, 17, Sw);  // 护手
            Circle(px, sz, 25, 5, 2, O); Circle(px, sz, 25, 5, 1, Sw);           // 剑柄圆头
        }

        // ── 游侠：绿皮甲 + 短兜帽 + 腰间双匕 ────────────────────────
        static void DrawRanger(Color32[] px, int sz)
        {
            var O  = C( 20,  50,  14);
            var Lv = C( 78, 138,  50);   // 皮甲绿
            var Dk = C( 38,  85,  25);   // 皮甲暗
            var Ho = C( 32,  72,  20);   // 兜帽
            var Sk = C(190, 155, 115);   // 皮肤
            var Bl = C(180, 182, 192);   // 匕首钢
            var Ac = C(152, 112,  45);   // 腰带金

            // 靴
            Rect(px, sz, 12, 1, 15, 4, O); Rect(px, sz, 13, 1, 14, 3, Ac);
            Rect(px, sz, 17, 1, 20, 4, O); Rect(px, sz, 18, 1, 19, 3, Ac);
            // 腿（修长）
            Rect(px, sz, 12, 4, 15, 10, O); Rect(px, sz, 13, 4, 14, 9, Dk);
            Rect(px, sz, 17, 4, 20, 10, O); Rect(px, sz, 18, 4, 19, 9, Dk);
            // 腰带
            Rect(px, sz, 11, 9, 21, 11, O); Rect(px, sz, 12, 10, 20, 10, Ac);
            // 左腰匕首
            Rect(px, sz, 9, 7, 10, 13, O); Rect(px, sz, 9, 7, 9, 13, Bl);
            // 右腰匕首
            Rect(px, sz, 22, 7, 23, 13, O); Rect(px, sz, 23, 7, 23, 13, Bl);
            // 皮甲躯干（修身）
            Rect(px, sz, 12, 11, 20, 21, O);
            Rect(px, sz, 13, 12, 19, 20, Lv);
            Line(px, sz, 14, 19, 14, 13, C(105,170,68));
            // 手臂
            Rect(px, sz, 10, 13, 12, 21, O); Rect(px, sz, 11, 14, 11, 20, Lv);
            Rect(px, sz, 20, 13, 22, 21, O); Rect(px, sz, 21, 14, 21, 20, Lv);
            // 肩扣
            Circle(px, sz, 12, 21, 2, O); Circle(px, sz, 12, 21, 1, Ac);
            Circle(px, sz, 20, 21, 2, O); Circle(px, sz, 20, 21, 1, Ac);
            // 衣领
            Rect(px, sz, 13, 21, 19, 23, O); Rect(px, sz, 14, 21, 18, 22, Dk);
            // 面部
            Oval(px, sz, 16, 25, 4, 3, O);
            Oval(px, sz, 16, 25, 3, 2, Sk);
            P(px, sz, 14, 26, O); P(px, sz, 18, 26, O);  // 眼
            P(px, sz, 16, 24, C(155, 95, 60));            // 嘴
            // 短兜帽（向后掀开，可见发型）
            Rect(px, sz, 11, 24, 21, 31, O);
            Rect(px, sz, 12, 24, 20, 30, Ho);
            // 头发（从帽下露出）
            Rect(px, sz, 14, 28, 18, 31, C(55, 38, 18));
            // 帽正面开口（显示脸）
            Oval(px, sz, 16, 27, 5, 4, O);
            Oval(px, sz, 16, 27, 4, 3, Ho);
            Oval(px, sz, 16, 25, 3, 2, Sk);
            P(px, sz, 14, 26, O); P(px, sz, 18, 26, O);
        }

        // ── 法师：紫袍 + 尖帽 + 法杖 ────────────────────────────────
        static void DrawMage(Color32[] px, int sz)
        {
            var O  = C( 38,  13,  62);
            var Rb = C(108,  42, 172);   // 袍紫
            var Rd = C( 62,  20, 108);   // 袍暗
            var Ht = C( 45,  14,  72);   // 帽
            var Gl = C(195,  88, 255);   // 法球光
            var Sk = C(208, 180, 152);   // 皮肤
            var St = C(102,  72,  30);   // 法杖木

            // 帽锥（从上到下）
            P(px, sz, 16, 31, Ht);
            Rect(px, sz, 15, 30, 17, 31, Ht);
            Rect(px, sz, 14, 28, 18, 30, Ht);
            Rect(px, sz, 13, 26, 19, 28, Ht);
            // 帽檐（宽）
            Rect(px, sz, 9, 24, 23, 26, O);
            Rect(px, sz, 10, 24, 22, 25, Ht);
            // 发丝（帽檐下）
            Oval(px, sz, 9, 21, 3, 4, C(38, 18, 58));
            Oval(px, sz, 23, 21, 3, 4, C(38, 18, 58));
            // 面部
            Oval(px, sz, 16, 21, 4, 3, O);
            Oval(px, sz, 16, 21, 3, 2, Sk);
            P(px, sz, 14, 22, O); P(px, sz, 18, 22, O);  // 眼
            P(px, sz, 14, 21, C(100,55,130)); P(px, sz, 18, 21, C(100,55,130));  // 眉
            // 袍身（底宽）
            for (int y = 2; y <= 20; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 20f) * 9f);
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Rd);
            }
            for (int y = 3; y <= 19; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 19f) * 8f);
                P(px, sz, 16 - hw, y, Rb); P(px, sz, 16 + hw, y, Rb);
            }
            // 魔力光效（袍边）
            P(px, sz, 16, 10, Gl); P(px, sz, 13, 7, Gl); P(px, sz, 19, 7, Gl);
            P(px, sz, 10, 4, C(145,60,200,180)); P(px, sz, 22, 4, C(145,60,200,180));
            // 法杖（右侧）
            Rect(px, sz, 22, 6, 24, 21, St);
            // 魔法球
            Circle(px, sz, 23, 22, 4, O);
            Circle(px, sz, 23, 22, 3, Gl);
            Circle(px, sz, 23, 23, 2, C(220, 148, 255));
            P(px, sz, 22, 24, C(255, 210, 255));
            // 持杖手
            Rect(px, sz, 20, 19, 22, 21, Sk);
            // 施法手（左）
            Rect(px, sz, 10, 16, 12, 18, Sk);
            P(px, sz, 9, 17, Gl); P(px, sz, 8, 16, Gl);
        }

        // ── 圣骑士：金甲 + 圣光光环 + 圣剑高举 ─────────────────────
        static void DrawPaladin(Color32[] px, int sz)
        {
            var O  = C( 88,  58,   5);  // 深金轮廓
            var A  = C(205, 162,  42);  // 金甲
            var L  = C(252, 218,  98);  // 金高光
            var D  = C(142, 108,  18);  // 金暗
            var Hl = C(255, 245, 208);  // 圣光白
            var Sk = C(195, 158, 118);  // 皮肤
            var Sw = C(220, 225, 238);  // 圣剑白

            // 靴
            Rect(px, sz, 11, 1, 15, 4, O); Rect(px, sz, 12, 1, 14, 3, L);
            Rect(px, sz, 17, 1, 21, 4, O); Rect(px, sz, 18, 1, 20, 3, L);
            // 腿甲（宽）
            Rect(px, sz, 11, 4, 15, 10, O); Rect(px, sz, 12, 4, 14, 9, A);
            Rect(px, sz, 17, 4, 21, 10, O); Rect(px, sz, 18, 4, 20, 9, A);
            // 腰带+裙甲
            Rect(px, sz, 10, 9, 22, 11, O); Rect(px, sz, 11, 9, 21, 10, D);
            // 胸甲（宽厚）
            Rect(px, sz,  9, 11, 23, 21, O);
            Rect(px, sz, 10, 12, 22, 20, A);
            Line(px, sz, 11, 19, 11, 13, L);
            Line(px, sz, 21, 19, 21, 13, D);
            // 十字圣纹
            Rect(px, sz, 15, 13, 17, 19, O); Rect(px, sz, 15, 13, 17, 19, D);
            Rect(px, sz, 13, 15, 19, 17, O); Rect(px, sz, 13, 15, 19, 17, D);
            P(px, sz, 16, 16, L);
            // 肩甲（大型圆弧）
            Circle(px, sz,  8, 19, 5, O); Circle(px, sz,  8, 19, 4, A);
            Circle(px, sz, 24, 19, 5, O); Circle(px, sz, 24, 19, 4, A);
            // 颈甲
            Rect(px, sz, 14, 21, 18, 23, O); Rect(px, sz, 15, 21, 17, 22, D);
            // 面部（可见）
            Oval(px, sz, 16, 25, 4, 3, O);
            Oval(px, sz, 16, 25, 3, 2, Sk);
            P(px, sz, 14, 26, O); P(px, sz, 18, 26, O);
            // 盔甲头盔（半开型，可见脸）
            Rect(px, sz, 11, 24, 21, 31, O);
            Rect(px, sz, 12, 24, 20, 30, A);
            // 开口显脸
            Oval(px, sz, 16, 26, 4, 3, O);
            Oval(px, sz, 16, 25, 3, 2, Sk);
            P(px, sz, 14, 26, O); P(px, sz, 18, 26, O);
            // 圣光光环（头顶）
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 7);
                int ey = 30 + Mathf.RoundToInt(Mathf.Sin(ang) * 3);
                P(px, sz, ex, ey, C(255, 235, 150, 200));
            }
            Circle(px, sz, 16, 30, 6, C(255, 240, 180, 60));
            Circle(px, sz, 16, 30, 4, C(255, 240, 180, 90));
            // 右手高举圣剑
            Rect(px, sz, 24, 8, 26, 24, O);
            Rect(px, sz, 25, 8, 25, 24, Sw);
            Rect(px, sz, 22, 22, 28, 24, O); Rect(px, sz, 23, 22, 27, 23, Sw);
            // 剑刃圣光
            for (int y = 9; y <= 22; y += 2) P(px, sz, 26, y, Hl);
            Circle(px, sz, 25, 7, 2, C(255, 245, 200, 200));  // 剑锋圣光
        }

        // ══════════════════════════════════════════════════════════════
        // 背面精灵（朝向上方时显示）
        // ══════════════════════════════════════════════════════════════

        // ── 战士背面：背甲 + 盔后脑 + 剑柄露肩 ─────────────────────
        static void DrawWarriorBack(Color32[] px, int sz)
        {
            var O  = C( 18,  38,  78);
            var A  = C( 68, 125, 192);
            var L  = C(138, 192, 242);
            var D  = C( 35,  68, 125);
            var M  = C(152, 158, 172);
            var Sw = C(178, 182, 195);

            // 战靴（同正面）
            Rect(px, sz, 11, 1, 15, 4, O); Rect(px, sz, 12, 1, 14, 3, M);
            Rect(px, sz, 17, 1, 21, 4, O); Rect(px, sz, 18, 1, 20, 3, M);
            // 腿甲
            Rect(px, sz, 12, 4, 15, 9, O); Rect(px, sz, 13, 4, 14, 8, A);
            Rect(px, sz, 17, 4, 20, 9, O); Rect(px, sz, 18, 4, 19, 8, A);
            // 腰裙
            Rect(px, sz, 11, 8, 21, 10, O); Rect(px, sz, 12, 8, 20, 9, D);
            // 背甲（简洁，无徽章）
            Rect(px, sz, 10, 10, 22, 20, O);
            Rect(px, sz, 11, 11, 21, 19, A);
            Line(px, sz, 16, 12, 16, 19, D);         // 背脊线
            Line(px, sz, 12, 18, 12, 12, D);
            Line(px, sz, 20, 18, 20, 12, L);
            // 肩甲
            Oval(px, sz,  9, 18, 4, 3, O); Oval(px, sz,  9, 18, 3, 2, A);
            Oval(px, sz, 23, 18, 4, 3, O); Oval(px, sz, 23, 18, 3, 2, A);
            // 颈背
            Rect(px, sz, 14, 20, 18, 23, O); Rect(px, sz, 15, 20, 17, 22, A);
            // 头盔后脑（无面甲开口）
            Rect(px, sz, 11, 23, 21, 31, O);
            Rect(px, sz, 12, 23, 20, 30, A);
            Rect(px, sz, 12, 29, 20, 31, Lk(A));     // 盔顶
            Rect(px, sz, 14, 30, 18, 31, M);          // 脊饰
            // 剑柄从右肩探出
            Rect(px, sz, 24, 10, 26, 20, O);
            Rect(px, sz, 25, 10, 25, 20, Sw);
            Rect(px, sz, 23, 18, 27, 20, O); Rect(px, sz, 24, 19, 26, 19, Sw);
        }

        // ── 游侠背面：兜帽后脑 + 披风 + 双匕首 ──────────────────────
        static void DrawRangerBack(Color32[] px, int sz)
        {
            var O  = C( 20,  50,  14);
            var Lv = C( 78, 138,  50);
            var Dk = C( 38,  85,  25);
            var Ho = C( 32,  72,  20);
            var Ac = C(152, 112,  45);
            var Bl = C(180, 182, 192);
            var Ha = C( 55,  38,  18);  // 发色

            // 靴（同正面）
            Rect(px, sz, 12, 1, 15, 4, O); Rect(px, sz, 13, 1, 14, 3, Ac);
            Rect(px, sz, 17, 1, 20, 4, O); Rect(px, sz, 18, 1, 19, 3, Ac);
            // 腿
            Rect(px, sz, 12, 4, 15, 10, O); Rect(px, sz, 13, 4, 14, 9, Dk);
            Rect(px, sz, 17, 4, 20, 10, O); Rect(px, sz, 18, 4, 19, 9, Dk);
            // 腰带
            Rect(px, sz, 11, 9, 21, 11, O); Rect(px, sz, 12, 10, 20, 10, Ac);
            // 腰间双匕（侧面）
            Rect(px, sz, 9, 7, 10, 13, O); Rect(px, sz, 9, 7, 9, 13, Bl);
            Rect(px, sz, 22, 7, 23, 13, O); Rect(px, sz, 23, 7, 23, 13, Bl);
            // 背甲（同色调）
            Rect(px, sz, 12, 11, 20, 21, O);
            Rect(px, sz, 13, 12, 19, 20, Lv);
            Line(px, sz, 16, 13, 16, 19, Dk);
            // 手臂
            Rect(px, sz, 10, 13, 12, 21, O); Rect(px, sz, 11, 14, 11, 20, Lv);
            Rect(px, sz, 20, 13, 22, 21, O); Rect(px, sz, 21, 14, 21, 20, Lv);
            // 肩扣
            Circle(px, sz, 12, 21, 2, O); Circle(px, sz, 12, 21, 1, Ac);
            Circle(px, sz, 20, 21, 2, O); Circle(px, sz, 20, 21, 1, Ac);
            // 衣领背面
            Rect(px, sz, 13, 21, 19, 23, O); Rect(px, sz, 14, 21, 18, 22, Dk);
            // 兜帽后脑（无面部开口）
            Rect(px, sz, 11, 23, 21, 31, O);
            Rect(px, sz, 12, 23, 20, 30, Ho);
            // 发丝从帽后露出
            Rect(px, sz, 14, 22, 18, 24, Ha);
            Rect(px, sz, 13, 24, 19, 27, Ha);
        }

        // ── 法师背面：锥帽背面 + 宽袍 + 法杖 ────────────────────────
        static void DrawMageBack(Color32[] px, int sz)
        {
            var O  = C( 38,  13,  62);
            var Rb = C(108,  42, 172);
            var Rd = C( 62,  20, 108);
            var Ht = C( 45,  14,  72);
            var Gl = C(195,  88, 255);
            var St = C(102,  72,  30);

            // 锥帽背面（从后看形状相同）
            P(px, sz, 16, 31, Ht);
            Rect(px, sz, 15, 30, 17, 31, Ht);
            Rect(px, sz, 14, 28, 18, 30, Ht);
            Rect(px, sz, 13, 26, 19, 28, Ht);
            // 帽檐背面
            Rect(px, sz, 9, 24, 23, 26, O);
            Rect(px, sz, 10, 24, 22, 25, Ht);
            // 发丝（帽后垂下）
            Oval(px, sz, 9,  22, 3, 4, C(38, 18, 58));
            Oval(px, sz, 23, 22, 3, 4, C(38, 18, 58));
            // 后脑（无面部）
            Oval(px, sz, 16, 22, 4, 3, O);
            Oval(px, sz, 16, 22, 3, 2, C(38, 18, 58));  // 暗发色
            // 宽袍（与正面相同渐变）
            for (int y = 2; y <= 20; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 20f) * 9f);
                Rect(px, sz, 16 - hw, y, 16 + hw, y, Rd);
            }
            for (int y = 3; y <= 19; y++)
            {
                int hw = Mathf.RoundToInt(2f + (1f - y / 19f) * 8f);
                P(px, sz, 16 - hw, y, Rb); P(px, sz, 16 + hw, y, Rb);
            }
            // 袍背魔力纹
            P(px, sz, 16, 10, Gl); P(px, sz, 13, 7, Gl); P(px, sz, 19, 7, Gl);
            // 法杖（同位置）
            Rect(px, sz, 22, 6, 24, 21, St);
            Circle(px, sz, 23, 22, 4, O);
            Circle(px, sz, 23, 22, 3, Gl);
            P(px, sz, 22, 24, C(255, 210, 255));
        }

        // ── 圣骑士背面：背甲 + 盔后 + 圣光光环 ─────────────────────
        static void DrawPaladinBack(Color32[] px, int sz)
        {
            var O  = C( 88,  58,   5);
            var A  = C(205, 162,  42);
            var L  = C(252, 218,  98);
            var D  = C(142, 108,  18);
            var Hl = C(255, 245, 208);
            var Sw = C(220, 225, 238);

            // 靴（同正面）
            Rect(px, sz, 11, 1, 15, 4, O); Rect(px, sz, 12, 1, 14, 3, L);
            Rect(px, sz, 17, 1, 21, 4, O); Rect(px, sz, 18, 1, 20, 3, L);
            // 腿甲
            Rect(px, sz, 11, 4, 15, 10, O); Rect(px, sz, 12, 4, 14, 9, A);
            Rect(px, sz, 17, 4, 21, 10, O); Rect(px, sz, 18, 4, 20, 9, A);
            // 腰带+裙甲
            Rect(px, sz, 10, 9, 22, 11, O); Rect(px, sz, 11, 9, 21, 10, D);
            // 背甲（无十字圣纹）
            Rect(px, sz,  9, 11, 23, 21, O);
            Rect(px, sz, 10, 12, 22, 20, A);
            Line(px, sz, 16, 12, 16, 20, D);   // 背脊
            Line(px, sz, 11, 19, 11, 13, D);
            Line(px, sz, 21, 19, 21, 13, L);
            // 肩甲
            Circle(px, sz,  8, 19, 5, O); Circle(px, sz,  8, 19, 4, A);
            Circle(px, sz, 24, 19, 5, O); Circle(px, sz, 24, 19, 4, A);
            // 颈背
            Rect(px, sz, 14, 21, 18, 23, O); Rect(px, sz, 15, 21, 17, 22, D);
            // 头盔后脑（无面部开口）
            Rect(px, sz, 11, 24, 21, 31, O);
            Rect(px, sz, 12, 24, 20, 30, A);
            // 圣光光环（始终可见）
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f;
                int ex = 16 + Mathf.RoundToInt(Mathf.Cos(ang) * 7);
                int ey = 30 + Mathf.RoundToInt(Mathf.Sin(ang) * 3);
                P(px, sz, ex, ey, C(255, 235, 150, 200));
            }
            Circle(px, sz, 16, 30, 6, C(255, 240, 180, 60));
            Circle(px, sz, 16, 30, 4, C(255, 240, 180, 90));
            // 圣剑（从右肩探出）
            Rect(px, sz, 24, 8, 26, 24, O);
            Rect(px, sz, 25, 8, 25, 24, Sw);
            for (int y = 9; y <= 22; y += 2) P(px, sz, 26, y, Hl);
        }

        // ── 猎人背面：兜帽后 + 背箭袋 + 弓 ──────────────────────────
        static void DrawHunterBack(Color32[] px, int sz)
        {
            var O  = C( 55,  28,   8);
            var Lv = C(148,  88,  38);
            var Dk = C( 88,  48,  18);
            var Ho = C( 72,  38,  15);
            var Bw = C(118,  72,  22);
            var Ar = C(175, 178, 182);
            var Qv = C(100,  58,  18);  // 箭袋

            // 靴
            Rect(px, sz, 11, 1, 16, 4, O); Rect(px, sz, 12, 1, 15, 3, Dk);
            Rect(px, sz, 17, 1, 21, 4, O); Rect(px, sz, 18, 1, 20, 3, Dk);
            // 腿
            Rect(px, sz, 12, 4, 15, 10, O); Rect(px, sz, 13, 4, 14, 9, Lv);
            Rect(px, sz, 17, 4, 21, 10, O); Rect(px, sz, 18, 4, 20, 9, Lv);
            // 腰带
            Rect(px, sz, 11, 9, 21, 11, O); Rect(px, sz, 12, 10, 20, 10, Dk);
            // 背面箭袋（居中显眼）
            Rect(px, sz, 14, 8, 19, 22, O); Rect(px, sz, 15, 9, 18, 21, Qv);
            // 箭羽从袋口露出
            P(px, sz, 15, 21, Ar); P(px, sz, 16, 21, Ar); P(px, sz, 17, 21, Ar);
            P(px, sz, 15, 22, Ar); P(px, sz, 17, 22, Ar);
            // 背甲（箭袋左右各一条皮甲）
            Rect(px, sz, 10, 11, 13, 21, O); Rect(px, sz, 11, 12, 12, 20, Lv);
            Rect(px, sz, 20, 11, 23, 21, O); Rect(px, sz, 21, 12, 22, 20, Lv);
            // 衣领背面
            Rect(px, sz, 13, 21, 19, 23, O); Rect(px, sz, 14, 21, 18, 22, Dk);
            // 兜帽后脑（无面部）
            Rect(px, sz, 10, 23, 22, 31, O);
            Rect(px, sz, 11, 23, 21, 30, Ho);
            // 帽沿（背面略低）
            Rect(px, sz, 10, 25, 22, 26, O);
            Rect(px, sz, 11, 26, 21, 26, Dk);
            // 短弓（背后左侧）
            Line(px, sz, 5, 10, 5, 22, Bw);
            Line(px, sz, 6, 10, 6, 22, Bw);
            Line(px, sz, 5, 10, 8, 11, Bw);
            Line(px, sz, 5, 22, 8, 21, Bw);
        }

        // ── 猎人：深兜帽 + 皮甲 + 背弓 + 箭袋 ──────────────────────
        static void DrawHunter(Color32[] px, int sz)
        {
            var O  = C( 55,  28,   8);
            var Lv = C(148,  88,  38);   // 皮甲棕
            var Dk = C( 88,  48,  18);   // 皮甲暗
            var Ho = C( 72,  38,  15);   // 兜帽深
            var Sk = C(192, 155, 115);   // 皮肤
            var Bw = C(118,  72,  22);   // 弓木
            var Ar = C(175, 178, 182);   // 箭头

            // 靴（厚底）
            Rect(px, sz, 11, 1, 16, 4, O); Rect(px, sz, 12, 1, 15, 3, Dk);
            Rect(px, sz, 17, 1, 21, 4, O); Rect(px, sz, 18, 1, 20, 3, Dk);
            // 腿（略弯伏击感）
            Rect(px, sz, 12, 4, 15, 10, O); Rect(px, sz, 13, 4, 14, 9, Lv);
            Rect(px, sz, 17, 4, 21, 10, O); Rect(px, sz, 18, 4, 20, 9, Lv);
            // 腰带
            Rect(px, sz, 11, 9, 21, 11, O); Rect(px, sz, 12, 10, 20, 10, Dk);
            // 箭袋（右侧，背面显出）
            Rect(px, sz, 21, 8, 23, 22, O); Rect(px, sz, 22, 9, 22, 21, Dk);
            P(px, sz, 22, 21, Ar); P(px, sz, 22, 20, Ar); P(px, sz, 22, 19, Ar);
            // 躯干皮甲
            Rect(px, sz, 11, 11, 21, 21, O);
            Rect(px, sz, 12, 12, 20, 20, Lv);
            Line(px, sz, 13, 19, 13, 13, C(185, 115, 55));
            // 臂（右侧前伸，持弓）
            Rect(px, sz, 20, 14, 22, 21, O); Rect(px, sz, 21, 15, 21, 20, Lv);
            // 左臂
            Rect(px, sz, 9, 13, 11, 21, O); Rect(px, sz, 10, 14, 10, 20, Lv);
            // 衣领
            Rect(px, sz, 13, 21, 19, 23, O); Rect(px, sz, 14, 21, 18, 22, Dk);
            // 面部（深兜帽内侧）
            Oval(px, sz, 16, 25, 4, 3, O);
            Oval(px, sz, 16, 25, 3, 2, Sk);
            P(px, sz, 14, 26, O); P(px, sz, 18, 26, O);  // 眼
            P(px, sz, 15, 24, O); P(px, sz, 17, 24, O);  // 严肃眉
            // 深兜帽（低沿遮脸）
            Rect(px, sz, 10, 23, 22, 31, O);
            Rect(px, sz, 11, 23, 21, 30, Ho);
            // 帽沿（低沿投影）
            Rect(px, sz, 10, 25, 22, 27, O);
            Rect(px, sz, 11, 26, 21, 26, Dk);
            // 帽沿内露出面部
            Oval(px, sz, 16, 25, 4, 3, O);
            Oval(px, sz, 16, 25, 3, 2, Sk);
            P(px, sz, 14, 26, O); P(px, sz, 18, 26, O);
            // 短弓（左背可见）
            Line(px, sz, 5, 10, 5, 22, Bw);
            Line(px, sz, 6, 10, 6, 22, Bw);
            Line(px, sz, 5, 10, 8, 11, Bw);
            Line(px, sz, 5, 22, 8, 21, Bw);
            Line(px, sz, 5, 10, 5, 22, C(210, 195, 165, 200));  // 弓弦
        }
    }
}
