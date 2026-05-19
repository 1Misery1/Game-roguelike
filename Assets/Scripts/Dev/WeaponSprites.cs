using UnityEngine;
using System.Collections.Generic;

namespace Game.Dev
{
    // 程序化生成 32×32 武器像素精灵图（全部 26 种武器）
    // y=0 在贴图底部 = 视觉底部（刀柄/握把在下，刀尖/法球在上）
    public static class WeaponSprites
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string weaponName)
        {
            if (_cache.TryGetValue(weaponName, out var s)) return s;
            s = Build(weaponName);
            if (s != null) _cache[weaponName] = s;
            return s;
        }

        static string RealSpritePath(string name) => name switch
        {
            "铁匕首"   => "Weapons/Daggers/IronDagger",
            "精铁匕首" => "Weapons/Daggers/SteelDagger",
            "毒牙"     => "Weapons/Daggers/VenomFang",
            "幻影之刃" => "Weapons/Daggers/PhantomBlade",
            "铁剑"     => "Weapons/Longswords/IronSword",
            "骑士剑"   => "Weapons/Longswords/KnightSword",
            "圣光剑"   => "Weapons/Longswords/HolySword",
            "龙渊剑"   => "Weapons/Longswords/DragonSword",
            "月牙弯刀" => "Weapons/Longswords/CrescentBlade",
            "寒铁长枪" => "Weapons/Longswords/FrostLance",
            "铁矛大剑" => "Weapons/Greatswords/IronGreatsword",
            "战士大剑" => "Weapons/Greatswords/WarriorGreatsword",
            "破甲重剑" => "Weapons/Greatswords/ArmorBreaker",
            "铸铁锤"   => "Weapons/Greatswords/IronMallet",
            "末日巨剑" => "Weapons/Greatswords/DoomBlade",
            "木弓"     => "Weapons/Bows/WoodenBow",
            "猎人弓"   => "Weapons/Bows/HunterBow",
            "穿云弓"   => "Weapons/Bows/CloudPiercer",
            "骨弓"     => "Weapons/Bows/BoneBow",
            "精灵短弓" => "Weapons/Bows/ElfBow",
            "雷鸣战弓" => "Weapons/Bows/ThunderBow",
            "天风弓"   => "Weapons/Bows/CelestialBow",
            "木法杖"   => "Weapons/Staves/WoodStaff",
            "魔法法杖" => "Weapons/Staves/MagicStaff",
            "寒冰法杖" => "Weapons/Staves/FrostStaff",
            "混沌魔杖" => "Weapons/Staves/ChaosWand",
            _          => null,
        };

        private static Sprite Build(string name)
        {
            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            switch (name)
            {
                // ── 匕首系 ────────────────────────────────────────────
                case "铁匕首":   DrawIronDagger(px, SZ);        break;
                case "精铁匕首": DrawSteelDagger(px, SZ);       break;
                case "毒牙":     DrawVenomFang(px, SZ);         break;
                case "幻影之刃": DrawPhantomBlade(px, SZ);      break;
                // ── 长剑系 ────────────────────────────────────────────
                case "铁剑":     DrawIronSword(px, SZ);         break;
                case "骑士剑":   DrawKnightSword(px, SZ);       break;
                case "圣光剑":   DrawHolySword(px, SZ);         break;
                case "龙渊剑":   DrawDragonSword(px, SZ);       break;
                case "月牙弯刀": DrawCrescentBlade(px, SZ);     break;
                case "寒铁长枪": DrawFrostLance(px, SZ);        break;
                // ── 大剑系 ────────────────────────────────────────────
                case "铁矛大剑": DrawIronGreatsword(px, SZ);    break;
                case "战士大剑": DrawWarriorGreatsword(px, SZ); break;
                case "破甲重剑": DrawArmorBreaker(px, SZ);      break;
                case "铸铁锤":   DrawIronMallet(px, SZ);        break;
                case "末日巨剑": DrawDoomBlade(px, SZ);         break;
                // ── 弓系 ──────────────────────────────────────────────
                case "木弓":     DrawWoodenBow(px, SZ);         break;
                case "猎人弓":   DrawHunterBow(px, SZ);         break;
                case "穿云弓":   DrawCloudPiercer(px, SZ);      break;
                case "骨弓":     DrawBoneBow(px, SZ);           break;
                case "精灵短弓": DrawElfBow(px, SZ);            break;
                case "雷鸣战弓": DrawThunderBow(px, SZ);        break;
                case "天风弓":   DrawCelestialBow(px, SZ);      break;
                // ── 法杖系 ────────────────────────────────────────────
                case "木法杖":   DrawWoodStaff(px, SZ);         break;
                case "魔法法杖": DrawMagicStaff(px, SZ);        break;
                case "寒冰法杖": DrawFrostStaff(px, SZ);        break;
                case "混沌魔杖": DrawChaosWand(px, SZ);         break;
                default: return null;
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
        }

        // ── 绘图原语 ───────────────────────────────────────────────────

        static void Rect(Color32[] p, int z, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0,y0); y <= Mathf.Min(z-1,y1); y++)
            for (int x = Mathf.Max(0,x0); x <= Mathf.Min(z-1,x1); x++) p[y*z+x] = c;
        }
        static void Circle(Color32[] p, int z, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int y = Mathf.Max(0,cy-r); y <= Mathf.Min(z-1,cy+r); y++)
            for (int x = Mathf.Max(0,cx-r); x <= Mathf.Min(z-1,cx+r); x++)
                if ((x-cx)*(x-cx)+(y-cy)*(y-cy) <= r2) p[y*z+x] = c;
        }
        static void Line(Color32[] p, int z, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx=Mathf.Abs(x1-x0),dy=Mathf.Abs(y1-y0),sx=x0<x1?1:-1,sy=y0<y1?1:-1,e=dx-dy;
            for(;;){ if(x0>=0&&x0<z&&y0>=0&&y0<z) p[y0*z+x0]=c; if(x0==x1&&y0==y1) break;
                int e2=2*e; if(e2>-dy){e-=dy;x0+=sx;} if(e2<dx){e+=dx;y0+=sy;} }
        }
        static void P(Color32[] p, int z, int x, int y, Color32 c)
        { if(x>=0&&x<z&&y>=0&&y<z) p[y*z+x]=c; }
        static Color32 C(int r, int g, int b, int a=255) => new Color32((byte)r,(byte)g,(byte)b,(byte)a);
        static Color32 Lk(Color32 c) => new Color32(
            (byte)Mathf.Min(255,(int)(c.r*1.4f)),(byte)Mathf.Min(255,(int)(c.g*1.4f)),
            (byte)Mathf.Min(255,(int)(c.b*1.4f)),c.a);
        static Color32 Dk(Color32 c) => new Color32(
            (byte)(c.r*0.6f),(byte)(c.g*0.6f),(byte)(c.b*0.6f),c.a);

        // ── 匕首系 ─────────────────────────────────────────────────────

        // 铁匕首：铁灰窄刃，十字护手，棕皮握柄
        static void DrawIronDagger(Color32[] px, int sz)
        {
            var bl = C(178,180,190); var hi = C(218,220,232); // 刀刃/高光
            var gd = C(128,130,140); var hd = C(108,74,44);   // 护手/握柄
            // 剑柄圆头
            Circle(px,sz,15,2,2,gd); P(px,sz,15,2,Lk(gd));
            // 握柄
            Rect(px,sz,14,4,16,9,hd);
            P(px,sz,14,6,Dk(hd)); P(px,sz,16,6,Dk(hd));
            P(px,sz,14,8,Dk(hd)); P(px,sz,16,8,Dk(hd));
            // 十字护手
            Rect(px,sz,10,10,21,12,gd);
            Line(px,sz,10,11,21,11,Dk(gd));
            // 刀刃主体
            Rect(px,sz,14,13,17,27,bl);
            Line(px,sz,14,13,14,27,hi);
            Line(px,sz,17,13,17,27,Dk(bl));
            // 刀尖收窄
            P(px,sz,15,28,bl); P(px,sz,16,28,bl);
            P(px,sz,15,29,hi); P(px,sz,15,30,hi);
        }

        // 精铁匕首：精钢闪光，绿宝石护手中央
        static void DrawSteelDagger(Color32[] px, int sz)
        {
            var bl = C(195,200,215); var hi = C(235,240,255);
            var gd = C(148,152,168); var hd = C(88,62,38);
            var gem = C(60,210,80);
            // 剑柄圆头（金属）
            Circle(px,sz,15,2,2,gd); Circle(px,sz,15,2,1,hi);
            // 握柄
            Rect(px,sz,14,4,16,9,hd);
            for(int y=4;y<=9;y+=2){ P(px,sz,13,y,Dk(hd)); P(px,sz,17,y,Dk(hd)); }
            // 护手 + 绿宝石
            Rect(px,sz,10,10,21,12,gd);
            Circle(px,sz,15,11,2,gem); P(px,sz,15,11,Lk(gem));
            // 刀刃（亮钢）
            Rect(px,sz,14,13,17,28,bl);
            Line(px,sz,14,13,14,28,hi);
            Line(px,sz,17,13,17,28,Dk(bl));
            // 刀尖
            P(px,sz,15,29,bl); P(px,sz,15,30,hi); P(px,sz,15,31,hi);
        }

        // 毒牙：深色锯齿刃，毒绿渗液
        static void DrawVenomFang(Color32[] px, int sz)
        {
            var bl = C(50,58,70);   var hi = C(80,95,110);
            var gd = C(40,80,40);   var hd = C(35,50,35);
            var vn = C(50,220,60);  var dp = C(30,150,40);
            // 骷髅形剑柄圆头
            Circle(px,sz,15,2,3,hd);
            P(px,sz,13,2,dp); P(px,sz,17,2,dp); // 眼窝
            // 握柄（暗骨纹）
            Rect(px,sz,14,5,16,10,hd);
            for(int y=5;y<=10;y++) P(px,sz,15,y,dp);
            // 护手（骨质）
            Rect(px,sz,10,11,21,13,gd);
            Line(px,sz,10,12,21,12,dp);
            // 锯齿刀刃
            Rect(px,sz,14,14,17,28,bl);
            Line(px,sz,14,14,14,28,hi);
            // 锯齿右侧
            for(int y=14;y<=28;y+=3) P(px,sz,18,y,bl);
            for(int y=16;y<=27;y+=3) P(px,sz,19,y,Dk(bl));
            // 毒液渗出
            P(px,sz,15,16,vn); P(px,sz,16,19,vn); P(px,sz,14,23,vn);
            P(px,sz,15,17,dp); P(px,sz,16,20,dp); P(px,sz,14,24,dp);
            // 刀尖
            P(px,sz,15,29,bl); P(px,sz,15,30,vn); P(px,sz,15,31,dp);
        }

        // 幻影之刃：紫影残像，半透明主刃
        static void DrawPhantomBlade(Color32[] px, int sz)
        {
            var bl = C(160,120,220,200); var hi = C(210,180,255,220);
            var sh = C(80,40,120,160);   var hd = C(40,20,60);
            var gm = C(200,80,220);
            // 黑暗握柄
            Rect(px,sz,14,1,16,8,hd);
            Circle(px,sz,15,1,2,sh);
            for(int y=2;y<=8;y+=2) { P(px,sz,13,y,sh); P(px,sz,17,y,sh); }
            // 护手（暗紫）
            Rect(px,sz,11,9,20,11,sh); Circle(px,sz,15,10,2,gm); P(px,sz,15,10,hi);
            // 左侧残像
            Rect(px,sz,11,12,12,27,sh);
            // 右侧残像
            Rect(px,sz,18,12,19,27,sh);
            // 主刀刃（紫色半透明）
            Rect(px,sz,14,12,16,28,bl);
            Line(px,sz,14,12,14,28,hi);
            Line(px,sz,16,12,16,28,sh);
            // 刀尖
            P(px,sz,15,29,bl); P(px,sz,15,30,hi); P(px,sz,15,31,gm);
        }

        // ── 长剑系 ─────────────────────────────────────────────────────

        // 铁剑：标准铁制单手剑
        static void DrawIronSword(Color32[] px, int sz)
        {
            var bl = C(180,183,193); var hi = C(220,223,235);
            var gd = C(135,137,148); var hd = C(110,76,46);
            // 剑柄圆头
            Circle(px,sz,15,1,2,gd); P(px,sz,15,1,hi);
            // 握柄
            Rect(px,sz,14,3,16,9,hd);
            P(px,sz,14,5,Dk(hd)); P(px,sz,16,5,Dk(hd));
            P(px,sz,14,8,Dk(hd)); P(px,sz,16,8,Dk(hd));
            // 十字护手
            Rect(px,sz,10,10,21,12,gd); Line(px,sz,10,11,21,11,Dk(gd));
            P(px,sz,10,10,Dk(gd)); P(px,sz,21,10,Dk(gd));
            // 剑身（中棱线）
            Rect(px,sz,14,13,17,28,bl);
            Line(px,sz,15,13,15,27,hi); // 棱线高光
            Line(px,sz,17,13,17,28,Dk(bl));
            // 剑尖
            P(px,sz,15,29,bl); P(px,sz,16,29,bl);
            P(px,sz,15,30,hi); P(px,sz,15,31,hi);
        }

        // 骑士剑：精钢剑身，金色护手，气势雄浑
        static void DrawKnightSword(Color32[] px, int sz)
        {
            var bl = C(200,205,218); var hi = C(240,244,255);
            var gd = C(200,165,40);  var dk = C(140,110,20);
            var hd = C(95,65,35);
            // 圆形剑柄（金）
            Circle(px,sz,15,2,3,gd); Circle(px,sz,15,2,2,Lk(gd));
            // 握柄（深皮）
            Rect(px,sz,14,5,16,9,hd);
            for(int y=5;y<=9;y++) { P(px,sz,13,y,dk); P(px,sz,17,y,dk); } // 金丝缠绕
            for(int y=6;y<=9;y+=2) { P(px,sz,13,y,gd); P(px,sz,17,y,gd); }
            // 宽金护手
            Rect(px,sz,9,10,22,13,gd);
            Line(px,sz,9,11,22,11,Lk(gd)); Line(px,sz,9,12,22,12,dk);
            // 剑身（宽双刃）
            Rect(px,sz,13,14,18,28,bl);
            Line(px,sz,13,14,13,28,hi); Line(px,sz,18,14,18,28,Dk(bl));
            Line(px,sz,15,14,15,27,hi); // 棱线
            // 剑尖
            P(px,sz,14,29,bl); P(px,sz,15,29,bl); P(px,sz,16,29,bl); P(px,sz,17,29,bl);
            P(px,sz,14,30,bl); P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // 圣光剑：圣十字护手，金蓝光芒，剑身发光
        static void DrawHolySword(Color32[] px, int sz)
        {
            var bl = C(220,225,255); var hi = C(255,255,255);
            var gd = C(220,180,50);  var gl = C(160,200,255,180); // 圣光光晕
            var hd = C(200,160,40);
            // 圣光光晕（剑身周围）
            for(int y=13;y<=30;y++) { P(px,sz,12,y,gl); P(px,sz,19,y,gl); }
            for(int y=18;y<=25;y++) { P(px,sz,11,y,gl); P(px,sz,20,y,gl); }
            // 金色圆形剑柄
            Circle(px,sz,15,2,3,gd); Circle(px,sz,15,2,1,hi);
            // 握柄
            Rect(px,sz,14,5,16,9,hd);
            // 十字形护手（圣十字）
            Rect(px,sz,9,10,22,12,gd);   // 横
            Rect(px,sz,14,10,16,14,gd);  // 竖延伸
            Line(px,sz,9,11,22,11,Lk(gd));
            Circle(px,sz,15,11,2,hi);
            // 发光剑身
            Rect(px,sz,14,15,17,29,bl);
            Line(px,sz,15,15,15,29,hi); Line(px,sz,16,15,16,29,hi);
            Line(px,sz,14,15,14,29,gl); Line(px,sz,17,15,17,29,gl);
            // 剑尖
            P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,15,31,hi);
        }

        // 龙渊剑：深渊黑刃，紫红龙纹，吸血之剑
        static void DrawDragonSword(Color32[] px, int sz)
        {
            var bl = C(35,30,50);    var hi = C(130,80,180);
            var gd = C(140,30,30);   var dk = C(80,15,15);
            var hd = C(50,20,30);    var rn = C(180,80,220); // 龙纹符文
            // 龙纹握柄圆头
            Circle(px,sz,15,2,3,hd); Circle(px,sz,15,2,2,gd); Circle(px,sz,15,2,1,rn);
            // 缠绕握柄
            Rect(px,sz,14,5,16,9,hd);
            for(int y=5;y<=9;y++) P(px,sz,15,y,dk);
            P(px,sz,13,6,gd); P(px,sz,17,6,gd); P(px,sz,13,8,gd); P(px,sz,17,8,gd);
            // 龙翼护手
            Rect(px,sz,9,10,22,13,gd);
            P(px,sz,8,11,dk);  P(px,sz,23,11,dk);   // 翼尖
            Line(px,sz,9,10,22,10,dk); Line(px,sz,9,12,22,12,rn);
            // 深渊剑身
            Rect(px,sz,14,14,17,28,bl);
            // 龙纹（符文）
            P(px,sz,15,17,rn); P(px,sz,16,20,rn); P(px,sz,15,23,rn); P(px,sz,16,26,rn);
            Line(px,sz,14,14,14,28,hi); Line(px,sz,17,14,17,28,dk);
            // 剑尖（紫电）
            P(px,sz,15,29,hi); P(px,sz,15,30,rn); P(px,sz,15,31,rn);
        }

        // 月牙弯刀：弯月弧刃，高攻速绿钢
        static void DrawCrescentBlade(Color32[] px, int sz)
        {
            var bl = C(175,200,160); var hi = C(220,245,200);
            var gd = C(120,155,80);  var hd = C(95,70,40);
            var mn = C(200,220,150); // 月牙色
            // 圆柄
            Circle(px,sz,16,2,2,gd); P(px,sz,16,2,hi);
            // 握柄（稍右偏，配合弧刃）
            Rect(px,sz,15,4,17,9,hd);
            P(px,sz,14,6,Dk(hd)); P(px,sz,18,6,Dk(hd));
            // 护手（月牙形）
            Rect(px,sz,11,10,22,12,gd);
            Circle(px,sz,16,11,2,mn); P(px,sz,16,11,hi);
            // 弧形刀刃（月牙弯曲）
            // 右侧主刃
            Rect(px,sz,15,13,18,28,bl);
            Line(px,sz,15,13,15,28,hi);
            // 向右弯曲的弧
            for(int y=15;y<=25;y++) {
                int curve = (y-15)*(y-15)/18;
                if(curve>0 && 18+curve<sz) { P(px,sz,18+curve,y,bl); P(px,sz,19+curve,y,Dk(bl)); }
            }
            // 刀尖（向右弯）
            P(px,sz,17,29,bl); P(px,sz,19,28,bl); P(px,sz,20,27,mn);
            P(px,sz,21,26,mn); P(px,sz,22,25,hi);
        }

        // 寒铁长枪：细长冰枪，蓝寒枪尖
        static void DrawFrostLance(Color32[] px, int sz)
        {
            var bl = C(160,185,220); var hi = C(200,225,255);
            var ic = C(120,200,255); var dk = C(80,110,160);
            var hd = C(80,90,100);
            // 细长枪杆（极窄）
            Rect(px,sz,15,1,16,22,hd);
            // 金属箍
            for(int y=5;y<=20;y+=5) { P(px,sz,14,y,dk); P(px,sz,17,y,dk); }
            // 冰晶枪尖底座
            Rect(px,sz,13,23,18,25,dk);
            // 冰晶枪尖（菱形）
            Rect(px,sz,14,26,17,29,ic);
            Line(px,sz,14,26,14,29,hi); Line(px,sz,17,26,17,29,Dk(ic));
            // 冰晶尖端
            P(px,sz,15,30,ic); P(px,sz,15,31,hi);
            P(px,sz,16,30,ic); P(px,sz,16,29,hi);
            // 冰霜光晕
            P(px,sz,13,27,C(160,230,255,120)); P(px,sz,18,27,C(160,230,255,120));
            P(px,sz,12,25,C(160,230,255,80));  P(px,sz,19,25,C(160,230,255,80));
        }

        // ── 大剑系 ─────────────────────────────────────────────────────

        // 铁矛大剑：宽厚灰铁巨剑
        static void DrawIronGreatsword(Color32[] px, int sz)
        {
            var bl = C(168,172,182); var hi = C(210,215,228);
            var gd = C(130,133,143); var hd = C(100,70,40);
            // 大剑柄圆头
            Circle(px,sz,15,2,3,gd); P(px,sz,15,2,hi);
            // 双手握柄（宽）
            Rect(px,sz,13,5,18,10,hd);
            for(int y=5;y<=10;y+=2) { P(px,sz,13,y,Dk(hd)); P(px,sz,18,y,Dk(hd)); }
            // 宽护手
            Rect(px,sz,7,11,24,14,gd);
            Line(px,sz,7,12,24,12,hi); Line(px,sz,7,13,24,13,Dk(gd));
            P(px,sz,7,12,Dk(gd)); P(px,sz,24,12,Dk(gd));
            // 宽厚剑身（5px宽）
            Rect(px,sz,12,15,19,28,bl);
            Line(px,sz,12,15,12,28,hi); Line(px,sz,19,15,19,28,Dk(bl));
            Line(px,sz,15,15,15,27,hi); // 中棱
            // 剑尖（宽→窄）
            Rect(px,sz,13,29,18,29,bl);
            Rect(px,sz,14,30,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // 战士大剑：战痕累累，绿钢精锻
        static void DrawWarriorGreatsword(Color32[] px, int sz)
        {
            var bl = C(160,185,150); var hi = C(205,230,195);
            var gd = C(100,130,80);  var dk = C(65,90,50);
            var hd = C(85,60,35);
            // 方形柄头
            Rect(px,sz,12,0,19,4,gd); Rect(px,sz,13,1,18,3,Lk(gd));
            // 双手握柄
            Rect(px,sz,13,4,18,10,hd);
            for(int y=5;y<=9;y+=2) { Line(px,sz,13,y,18,y,dk); } // 皮革横纹
            // 宽护手（带缺口）
            Rect(px,sz,7,11,24,14,gd);
            P(px,sz,14,11,Dk(gd)); P(px,sz,15,11,Dk(gd)); P(px,sz,16,11,Dk(gd)); P(px,sz,17,11,Dk(gd));
            Line(px,sz,7,12,24,12,hi);
            // 绿钢剑身（带战痕）
            Rect(px,sz,12,15,19,28,bl);
            Line(px,sz,12,15,12,28,hi); Line(px,sz,19,15,19,28,dk);
            // 战痕凹槽
            P(px,sz,14,19,dk); P(px,sz,14,20,dk); // 痕1
            P(px,sz,17,22,dk); P(px,sz,17,23,dk); // 痕2
            P(px,sz,15,25,dk);                      // 痕3
            // 剑尖
            Rect(px,sz,13,29,18,29,bl);
            P(px,sz,14,30,bl); P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // 破甲重剑：锯齿侧刃，蓝钢重击
        static void DrawArmorBreaker(Color32[] px, int sz)
        {
            var bl = C(130,160,210); var hi = C(180,210,255);
            var gd = C(90,120,170);  var dk = C(60,85,130);
            var hd = C(70,60,80);
            // 重型柄头
            Rect(px,sz,11,0,20,4,dk); Rect(px,sz,12,1,19,3,gd);
            // 宽双手柄
            Rect(px,sz,13,4,18,10,hd);
            for(int y=5;y<=10;y++) P(px,sz,15,y,Dk(hd)); P(px,sz,16,5,gd);
            // 巨型护手（蓝钢）
            Rect(px,sz,6,11,25,14,gd);
            Line(px,sz,6,11,25,11,dk); Line(px,sz,6,13,25,13,hi);
            // 宽厚蓝钢剑身
            Rect(px,sz,12,15,19,27,bl);
            Line(px,sz,12,15,12,27,hi); Line(px,sz,19,15,19,27,dk);
            // 左锯齿
            for(int y=15;y<=26;y+=3) P(px,sz,11,y+1,bl);
            for(int y=16;y<=27;y+=3) P(px,sz,10,y,dk);
            // 右锯齿
            for(int y=15;y<=26;y+=3) P(px,sz,20,y+1,bl);
            for(int y=16;y<=27;y+=3) P(px,sz,21,y,dk);
            // 宽剑尖
            Rect(px,sz,13,28,18,29,bl);
            P(px,sz,14,30,bl); P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // 铸铁锤：极宽锤头，长铁柄
        static void DrawIronMallet(Color32[] px, int sz)
        {
            var hn = C(90,65,40);   var dk = C(60,40,20);   // 木柄
            var mt = C(155,158,168); var hi = C(195,198,210); // 铁锤
            // 铁柄
            Rect(px,sz,15,0,16,17,dk); Rect(px,sz,14,1,17,16,hn);
            // 金属箍
            for(int y=4;y<=16;y+=4) Rect(px,sz,14,y,17,y,mt);
            // 锤头（宽大矩形）
            Rect(px,sz,7,18,24,29,mt);
            // 锤面花纹
            Rect(px,sz,8,19,23,28,Lk(mt));
            Line(px,sz,8,19,23,19,hi); // 顶面高光
            Line(px,sz,8,28,23,28,Dk(mt)); // 底面阴影
            Line(px,sz,8,19,8,28,hi);   // 左侧高光
            Line(px,sz,23,19,23,28,Dk(mt)); // 右侧阴影
            // 锤面中央纹路
            Rect(px,sz,13,22,18,26,Dk(mt));
            Rect(px,sz,14,23,17,25,mt);
            // 底部铁帽
            Rect(px,sz,6,17,25,18,Dk(mt)); Rect(px,sz,6,29,25,30,Dk(mt));
            P(px,sz,6,31,Dk(mt)); P(px,sz,25,31,Dk(mt));
        }

        // 末日巨剑：漆黑巨刃，紫魔光晕，嗜血之剑
        static void DrawDoomBlade(Color32[] px, int sz)
        {
            var bl = C(20,15,30);    var hi = C(150,80,220);
            var gd = C(120,20,20);   var dk = C(60,10,10);
            var hd = C(30,15,40);    var gl = C(180,50,230,150); // 紫光晕
            // 暗紫光晕（剑身两侧）
            for(int y=14;y<=31;y++) { P(px,sz,11,y,gl); P(px,sz,20,y,gl); }
            for(int y=18;y<=28;y++) { P(px,sz,10,y,C(150,40,200,100)); P(px,sz,21,y,C(150,40,200,100)); }
            // 血色恶魔柄头
            Circle(px,sz,15,2,4,dk); Circle(px,sz,15,2,3,gd);
            P(px,sz,13,3,hi); P(px,sz,17,3,hi); // 恶眼
            // 双手握柄
            Rect(px,sz,13,6,17,11,hd);
            for(int y=7;y<=10;y++) P(px,sz,15,y,hi);
            // 末日十字护手
            Rect(px,sz,7,12,23,15,gd);
            Line(px,sz,7,12,23,12,dk); Line(px,sz,7,14,23,14,hi);
            P(px,sz,7,13,dk); P(px,sz,23,13,dk);
            // 漆黑巨刃（超宽）
            Rect(px,sz,12,16,19,29,bl);
            // 紫色符文
            P(px,sz,15,18,hi); P(px,sz,16,21,hi); P(px,sz,15,24,hi); P(px,sz,16,27,hi);
            Line(px,sz,12,16,12,29,hi); Line(px,sz,19,16,19,29,dk);
            // 剑尖
            Rect(px,sz,13,30,18,30,bl);
            P(px,sz,14,31,hi); P(px,sz,15,31,hi); P(px,sz,16,31,hi); P(px,sz,17,31,hi);
        }

        // ── 弓系 ───────────────────────────────────────────────────────
        // 弓系均竖向布局：弓弦在右（x≈22），弓身向左弯，箭矢垂直居中

        // 木弓：朴素木弓，浅棕色
        static void DrawWoodenBow(Color32[] px, int sz)
        {
            var wd = C(145,100,55); var dk = C(100,68,30); // 木色
            var st = C(220,210,180); // 弦
            var ar = C(178,145,85); var tip = C(180,180,185); // 箭羽/箭尖
            // 弓弦（右侧直线）
            Line(px,sz,21,2,21,30,st);
            // 弓身（向左弯曲弧线）
            for(int y=2;y<=30;y++) {
                float t=(y-16f)/14f; int bx=(int)(21-6*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,dk);
            }
            // 弓端装饰（弦结处）
            P(px,sz,21,1,dk); P(px,sz,21,0,dk); P(px,sz,21,31,dk);
            // 箭矢（中央垂直）
            Line(px,sz,18,5,18,27,ar);
            // 箭羽
            P(px,sz,17,5,ar); P(px,sz,16,5,dk); P(px,sz,19,5,ar); P(px,sz,20,5,dk);
            P(px,sz,17,7,ar); P(px,sz,19,7,ar);
            // 箭尖
            P(px,sz,18,28,tip); P(px,sz,18,29,tip); P(px,sz,18,30,Lk(tip));
        }

        // 猎人弓：精良弓，绿色点缀，更长
        static void DrawHunterBow(Color32[] px, int sz)
        {
            var wd = C(130,95,50); var dk = C(88,62,25);
            var ac = C(70,160,70);  // 绿色装饰
            var st = C(235,225,190); var ar = C(165,130,70); var tip = C(185,185,190);
            // 弓弦
            Line(px,sz,22,1,22,30,st);
            // 弓身（更弯）
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-7*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,dk);
            }
            // 绿色箭台
            Rect(px,sz,18,13,21,16,ac);
            // 弓端结
            Circle(px,sz,22,1,1,ac); Circle(px,sz,22,30,1,ac);
            // 箭矢
            Line(px,sz,19,4,19,28,ar);
            P(px,sz,18,4,ar); P(px,sz,17,4,dk); P(px,sz,20,4,ar); P(px,sz,21,4,dk);
            P(px,sz,18,6,ar); P(px,sz,20,6,ar);
            // 箭尖
            P(px,sz,19,29,tip); P(px,sz,19,30,Lk(tip)); P(px,sz,19,31,Lk(tip));
        }

        // 穿云弓：蓝钢细弓，贯穿之矢
        static void DrawCloudPiercer(Color32[] px, int sz)
        {
            var wd = C(90,120,180); var hi = C(140,180,240);
            var st = C(180,220,255); var ar = C(160,195,235); var tip = C(200,240,255);
            var gl = C(100,180,255,160); // 蓝光
            // 弓身光晕
            for(int y=3;y<=28;y++) {
                float t=(y-15.5f)/12.5f; int bx=(int)(22-7*(1-t*t));
                if(bx-2>=0) P(px,sz,bx-2,y,gl);
            }
            // 蓝钢弓弦
            Line(px,sz,22,2,22,29,st);
            // 蓝钢弓身
            for(int y=2;y<=29;y++) {
                float t=(y-15.5f)/13.5f; int bx=(int)(22-7*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi);
            }
            // 蓝光箭台
            Circle(px,sz,22,2,1,hi); Circle(px,sz,22,29,1,hi);
            // 蓝色穿透箭
            Line(px,sz,19,4,19,27,ar);
            P(px,sz,18,4,hi); P(px,sz,20,4,hi);
            P(px,sz,18,6,ar); P(px,sz,20,6,ar);
            P(px,sz,19,28,tip); P(px,sz,19,29,tip); P(px,sz,19,30,hi);
        }

        // 骨弓：骨白原始弓，粗糙棱角
        static void DrawBoneBow(Color32[] px, int sz)
        {
            var bn = C(220,210,185); var dk = C(165,155,132); // 骨色
            var st = C(245,238,218); var ar = C(195,182,150); var tip = C(170,170,175);
            // 弓身（稍短稍粗，角状弓）
            for(int y=3;y<=28;y++) {
                float t=(y-15.5f)/12.5f; int bx=(int)(21-5*(1-t*t));
                P(px,sz,bx,y,bn); P(px,sz,bx-1,y,dk); P(px,sz,bx+1,y,C(235,225,200));
            }
            // 弓结节（骨节）
            Circle(px,sz,16,15,2,Lk(bn)); // 中央骨节
            Circle(px,sz,21,3,2,dk);       // 上端骨节
            Circle(px,sz,21,28,2,dk);      // 下端骨节
            // 粗弦
            Line(px,sz,22,3,22,28,st); P(px,sz,23,15,st); // 弦中央略粗
            // 箭矢（骨箭）
            Line(px,sz,19,5,19,26,ar);
            P(px,sz,18,5,dk); P(px,sz,20,5,dk); P(px,sz,18,7,bn); P(px,sz,20,7,bn);
            P(px,sz,19,27,tip); P(px,sz,19,28,tip);
        }

        // 精灵短弓：小巧优雅，绿叶纹饰，超高攻速
        static void DrawElfBow(Color32[] px, int sz)
        {
            var wd = C(110,155,80); var hi = C(160,215,120);
            var st = C(240,255,220); var ar = C(175,220,130); var tip = C(200,240,150);
            var fl = C(100,190,100); // 叶纹
            // 精灵弓身（短而弯）
            for(int y=5;y<=26;y++) {
                float t=(y-15.5f)/10.5f; int bx=(int)(21-5*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi);
            }
            // 叶形纹饰
            Circle(px,sz,17,15,2,fl); P(px,sz,17,15,hi);
            P(px,sz,15,14,fl); P(px,sz,15,16,fl); P(px,sz,19,14,fl); P(px,sz,19,16,fl);
            // 精灵弓端（花蕾）
            Circle(px,sz,21,5,2,fl);  P(px,sz,21,5,hi);
            Circle(px,sz,21,26,2,fl); P(px,sz,21,26,hi);
            // 细弦
            Line(px,sz,22,5,22,26,st);
            // 短箭
            Line(px,sz,19,8,19,24,ar);
            P(px,sz,18,8,fl); P(px,sz,20,8,fl);
            P(px,sz,19,25,tip); P(px,sz,19,26,tip);
        }

        // 雷鸣战弓：重型战弓，蓝电闪烁
        static void DrawThunderBow(Color32[] px, int sz)
        {
            var wd = C(50,65,95); var hi = C(80,110,170);
            var lt = C(120,200,255); var dk = C(30,40,70);
            var st = C(180,230,255); var ar = C(100,170,240); var tip = C(220,245,255);
            // 闪电光晕
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-8*(1-t*t));
                if(bx-2>=0) P(px,sz,bx-2,y,C(100,180,255,100));
            }
            // 重型弓身（2px宽）
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-8*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi); P(px,sz,bx+1,y,dk);
            }
            // 雷纹（弓身上）
            P(px,sz,17,10,lt); P(px,sz,16,13,lt); P(px,sz,17,16,lt); P(px,sz,16,19,lt); P(px,sz,17,22,lt);
            // 蓝电弓端
            Circle(px,sz,22,1,2,lt); Circle(px,sz,22,30,2,lt);
            // 蓝电弦（带闪电折线）
            Line(px,sz,23,1,23,30,st);
            P(px,sz,24,8,lt); P(px,sz,25,9,lt); P(px,sz,24,10,lt); // 闪电1
            P(px,sz,24,20,lt); P(px,sz,25,21,lt); P(px,sz,24,22,lt); // 闪电2
            // 雷电箭矢
            Line(px,sz,20,3,20,27,ar);
            P(px,sz,19,3,lt); P(px,sz,21,3,lt);
            P(px,sz,19,5,ar); P(px,sz,21,5,ar);
            P(px,sz,20,28,tip); P(px,sz,20,29,lt); P(px,sz,20,30,hi);
        }

        // 天风弓：天紫神弓，金星纹饰，箭雨之弓
        static void DrawCelestialBow(Color32[] px, int sz)
        {
            var wd = C(90,50,140); var hi = C(160,100,220);
            var gd = C(220,180,50); var st = C(230,200,255);
            var ar = C(185,140,240); var tip = C(255,220,100);
            var gl = C(150,80,210,150); // 紫光晕
            // 光晕
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-7*(1-t*t));
                if(bx-2>=0) P(px,sz,bx-2,y,gl);
            }
            // 天风弓身（金紫渐变）
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-7*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi);
            }
            // 金星纹
            P(px,sz,17,9,gd);  P(px,sz,16,10,gd); P(px,sz,18,10,gd); P(px,sz,17,11,gd); // 星1
            P(px,sz,17,20,gd); P(px,sz,16,21,gd); P(px,sz,18,21,gd); P(px,sz,17,22,gd); // 星2
            // 金色弓端
            Circle(px,sz,22,1,2,gd);  P(px,sz,22,1,tip);
            Circle(px,sz,22,30,2,gd); P(px,sz,22,30,tip);
            // 紫金弦
            Line(px,sz,23,1,23,30,st);
            for(int y=5;y<=25;y+=5) P(px,sz,23,y,gd); // 金色弦结
            // 紫金箭
            Line(px,sz,20,3,20,28,ar);
            P(px,sz,19,3,gd); P(px,sz,21,3,gd); P(px,sz,19,5,ar); P(px,sz,21,5,ar);
            P(px,sz,20,29,tip); P(px,sz,20,30,gd); P(px,sz,20,31,hi);
        }

        // ── 法杖系 ─────────────────────────────────────────────────────
        // 法杖均竖向，法球/水晶在上（高y），杖身居中

        // 木法杖：粗糙木杖，顶端木球
        static void DrawWoodStaff(Color32[] px, int sz)
        {
            var wd = C(135,95,50);  var dk = C(95,64,28);   // 木色
            var kn = C(160,115,65); // 木节
            var ob = C(175,115,60); var oh = C(215,165,105); // 法球
            // 木杖杆
            Rect(px,sz,15,1,16,20,wd);
            Line(px,sz,14,1,14,20,dk);  // 阴影边
            // 木节纹路
            for(int y=4;y<=18;y+=5) { P(px,sz,15,y,kn); P(px,sz,16,y,kn); P(px,sz,14,y,dk); }
            // 杖头固定环
            Rect(px,sz,13,20,18,22,Dk(wd));
            // 木制法球
            Circle(px,sz,15,26,5,ob);
            Circle(px,sz,15,26,4,Lk(ob));
            Circle(px,sz,15,26,2,oh);
            P(px,sz,14,27,oh); P(px,sz,13,26,kn); // 木纹
            P(px,sz,15,31,oh); P(px,sz,16,30,Lk(ob)); // 顶部高光
        }

        // 魔法法杖：精雕杖身，绿宝石法球
        static void DrawMagicStaff(Color32[] px, int sz)
        {
            var wd = C(80,60,40);   var hi = C(130,100,70);   // 深色木杖
            var gm = C(40,200,80);  var gh = C(120,255,160);  // 绿法球
            var mt = C(160,140,80); // 金属装饰
            // 精雕杖身
            Rect(px,sz,15,1,16,20,wd);
            Line(px,sz,14,1,14,20,Dk(wd)); Line(px,sz,17,1,17,20,hi);
            // 金属箍
            for(int y=4;y<=18;y+=4) { Rect(px,sz,14,y,17,y,mt); }
            // 法球底座（金属）
            Rect(px,sz,12,20,19,23,mt);
            Line(px,sz,12,21,19,21,Lk(mt)); Line(px,sz,12,22,19,22,Dk(mt));
            P(px,sz,11,21,Dk(mt)); P(px,sz,20,21,Dk(mt));
            // 绿宝石法球（带发光）
            Circle(px,sz,15,27,4,C(20,100,40));  // 外暗层
            Circle(px,sz,15,27,3,gm);
            Circle(px,sz,15,27,2,gh);
            P(px,sz,14,28,gh); P(px,sz,14,27,Lk(gh)); // 高光
            // 顶端绿光溢出
            P(px,sz,15,31,C(60,220,100,160)); P(px,sz,16,31,C(60,220,100,120));
        }

        // 寒冰法杖：蓝冰水晶顶，冻结之杖
        static void DrawFrostStaff(Color32[] px, int sz)
        {
            var wd = C(80,100,130); var hi = C(130,160,200); // 冰蓝杖身
            var ic = C(160,220,255); var ih = C(220,245,255); // 冰晶
            var ft = C(80,160,220,180); // 冰霜光晕
            // 冰蓝杖身
            Rect(px,sz,15,1,16,19,wd);
            Line(px,sz,14,1,14,19,Dk(wd)); Line(px,sz,17,1,17,19,hi);
            // 冰霜纹路
            for(int y=3;y<=17;y+=4) {
                P(px,sz,13,y,ft); P(px,sz,18,y,ft);
                P(px,sz,12,y+1,C(160,220,255,100)); P(px,sz,19,y+1,C(160,220,255,100));
            }
            // 冰晶底座
            Rect(px,sz,12,19,19,21,ic);
            // 冰晶水晶顶（六边形星形）
            Circle(px,sz,15,26,4,C(100,180,230)); // 外层
            Circle(px,sz,15,26,3,ic);
            Circle(px,sz,15,26,2,ih);
            // 冰晶尖刺（8方向）
            P(px,sz,15,31,ih); P(px,sz,15,30,ic);
            P(px,sz,11,26,ih); P(px,sz,19,26,ih);
            P(px,sz,12,29,ic); P(px,sz,18,29,ic);
            P(px,sz,12,23,ic); P(px,sz,18,23,ic);
            // 中央冰晶核心
            P(px,sz,15,26,Lk(ih)); P(px,sz,14,26,ih); P(px,sz,16,26,ih);
        }

        // 混沌魔杖：短杖，三元素混沌法球（火红/雷蓝/毒绿旋涡）
        static void DrawChaosWand(Color32[] px, int sz)
        {
            var wd = C(50,30,60);   var hi = C(110,70,140);  // 暗紫杖身
            var fr = C(220,80,30);  var lt = C(80,160,255);  // 火/雷
            var vn = C(60,200,60);  var pk = C(200,80,220);  // 毒/紫核
            var gl = C(160,60,200,150); // 混沌光晕
            // 混沌光晕（法球周围）
            Circle(px,sz,15,24,7,C(100,40,140,80));
            Circle(px,sz,15,24,6,C(130,50,180,100));
            // 暗紫短杖
            Rect(px,sz,15,1,16,16,wd);
            Line(px,sz,14,1,14,16,Dk(wd)); Line(px,sz,17,1,17,16,hi);
            for(int y=3;y<=15;y+=4) { P(px,sz,14,y,pk); P(px,sz,17,y,pk); }
            // 杖头金属环
            Rect(px,sz,12,16,19,18,Dk(wd)); Rect(px,sz,13,17,18,17,pk);
            // 混沌法球（火雷毒三色旋涡）
            Circle(px,sz,15,24,4,C(80,20,100)); // 深紫底
            // 火焰扇区（右下）
            P(px,sz,17,22,fr); P(px,sz,18,23,fr); P(px,sz,17,24,fr); P(px,sz,18,25,C(240,140,30));
            P(px,sz,16,22,C(240,140,30));
            // 雷电扇区（左下）
            P(px,sz,13,22,lt); P(px,sz,12,23,lt); P(px,sz,13,24,lt); P(px,sz,12,25,C(150,220,255));
            P(px,sz,14,22,C(150,220,255));
            // 毒雾扇区（上方）
            P(px,sz,15,28,vn); P(px,sz,14,27,vn); P(px,sz,16,27,vn); P(px,sz,15,29,C(100,240,100));
            // 紫色混沌核心
            Circle(px,sz,15,24,2,pk); P(px,sz,15,24,Lk(pk));
            // 顶端混沌迸发
            P(px,sz,15,31,gl); P(px,sz,14,30,C(200,80,220,180)); P(px,sz,16,30,C(80,180,255,180));
        }
    }
}
