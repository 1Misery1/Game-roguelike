using UnityEngine;
using System.Collections.Generic;

namespace Game.Art
{
    // Procedurally generates 32×32 weapon pixel sprites (all 26 weapons)
    // y=0 is texture bottom = visual bottom (handle/grip at bottom, tip/orb at top)
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
            "Iron Dagger"        => "Weapons/Daggers/IronDagger",
            "Steel Dagger"       => "Weapons/Daggers/SteelDagger",
            "Venom Fang"         => "Weapons/Daggers/VenomFang",
            "Phantom Blade"      => "Weapons/Daggers/PhantomBlade",
            "Iron Sword"         => "Weapons/Longswords/IronSword",
            "Knight Sword"       => "Weapons/Longswords/KnightSword",
            "Holy Sword"         => "Weapons/Longswords/HolySword",
            "Dragon Sword"       => "Weapons/Longswords/DragonSword",
            "Crescent Blade"     => "Weapons/Longswords/CrescentBlade",
            "Frost Lance"        => "Weapons/Longswords/FrostLance",
            "Iron Greatsword"    => "Weapons/Greatswords/IronGreatsword",
            "Warrior Greatsword" => "Weapons/Greatswords/WarriorGreatsword",
            "Armor Breaker"      => "Weapons/Greatswords/ArmorBreaker",
            "Iron Mallet"        => "Weapons/Greatswords/IronMallet",
            "Doom Blade"         => "Weapons/Greatswords/DoomBlade",
            "Wooden Bow"         => "Weapons/Bows/WoodenBow",
            "Hunter Bow"         => "Weapons/Bows/HunterBow",
            "Cloud Piercer"      => "Weapons/Bows/CloudPiercer",
            "Bone Bow"           => "Weapons/Bows/BoneBow",
            "Elf Bow"            => "Weapons/Bows/ElfBow",
            "Thunder Bow"        => "Weapons/Bows/ThunderBow",
            "Celestial Bow"      => "Weapons/Bows/CelestialBow",
            "Wood Staff"         => "Weapons/Staves/WoodStaff",
            "Magic Staff"        => "Weapons/Staves/MagicStaff",
            "Frost Staff"        => "Weapons/Staves/FrostStaff",
            "Chaos Wand"         => "Weapons/Staves/ChaosWand",
            _                    => null,
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
                // ── Dagger ────────────────────────────────────────────
                case "Iron Dagger":        DrawIronDagger(px, SZ);        break;
                case "Steel Dagger":       DrawSteelDagger(px, SZ);       break;
                case "Venom Fang":         DrawVenomFang(px, SZ);         break;
                case "Phantom Blade":      DrawPhantomBlade(px, SZ);      break;
                // ── Longsword ─────────────────────────────────────────
                case "Iron Sword":         DrawIronSword(px, SZ);         break;
                case "Knight Sword":       DrawKnightSword(px, SZ);       break;
                case "Holy Sword":         DrawHolySword(px, SZ);         break;
                case "Dragon Sword":       DrawDragonSword(px, SZ);       break;
                case "Crescent Blade":     DrawCrescentBlade(px, SZ);     break;
                case "Frost Lance":        DrawFrostLance(px, SZ);        break;
                // ── Greatsword ────────────────────────────────────────
                case "Iron Greatsword":    DrawIronGreatsword(px, SZ);    break;
                case "Warrior Greatsword": DrawWarriorGreatsword(px, SZ); break;
                case "Armor Breaker":      DrawArmorBreaker(px, SZ);      break;
                case "Iron Mallet":        DrawIronMallet(px, SZ);        break;
                case "Doom Blade":         DrawDoomBlade(px, SZ);         break;
                // ── Bow ───────────────────────────────────────────────
                case "Wooden Bow":         DrawWoodenBow(px, SZ);         break;
                case "Hunter Bow":         DrawHunterBow(px, SZ);         break;
                case "Cloud Piercer":      DrawCloudPiercer(px, SZ);      break;
                case "Bone Bow":           DrawBoneBow(px, SZ);           break;
                case "Elf Bow":            DrawElfBow(px, SZ);            break;
                case "Thunder Bow":        DrawThunderBow(px, SZ);        break;
                case "Celestial Bow":      DrawCelestialBow(px, SZ);      break;
                // ── Staff ─────────────────────────────────────────────
                case "Wood Staff":         DrawWoodStaff(px, SZ);         break;
                case "Magic Staff":        DrawMagicStaff(px, SZ);        break;
                case "Frost Staff":        DrawFrostStaff(px, SZ);        break;
                case "Chaos Wand":         DrawChaosWand(px, SZ);         break;
                default: return null;
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
        }

        // ── Drawing primitives ─────────────────────────────────────────

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

        // ── Dagger ──────────────────────────────────────────────────────

        // Iron Dagger: iron-gray narrow blade, cross guard, brown leather grip
        static void DrawIronDagger(Color32[] px, int sz)
        {
            var bl = C(178,180,190); var hi = C(218,220,232); // blade/highlight
            var gd = C(128,130,140); var hd = C(108,74,44);   // guard/grip
            // pommel
            Circle(px,sz,15,2,2,gd); P(px,sz,15,2,Lk(gd));
            // grip
            Rect(px,sz,14,4,16,9,hd);
            P(px,sz,14,6,Dk(hd)); P(px,sz,16,6,Dk(hd));
            P(px,sz,14,8,Dk(hd)); P(px,sz,16,8,Dk(hd));
            // cross guard
            Rect(px,sz,10,10,21,12,gd);
            Line(px,sz,10,11,21,11,Dk(gd));
            // blade body
            Rect(px,sz,14,13,17,27,bl);
            Line(px,sz,14,13,14,27,hi);
            Line(px,sz,17,13,17,27,Dk(bl));
            // tapered tip
            P(px,sz,15,28,bl); P(px,sz,16,28,bl);
            P(px,sz,15,29,hi); P(px,sz,15,30,hi);
        }

        // Steel Dagger: polished steel flash, emerald guard center
        static void DrawSteelDagger(Color32[] px, int sz)
        {
            var bl = C(195,200,215); var hi = C(235,240,255);
            var gd = C(148,152,168); var hd = C(88,62,38);
            var gem = C(60,210,80);
            // metal pommel
            Circle(px,sz,15,2,2,gd); Circle(px,sz,15,2,1,hi);
            // grip
            Rect(px,sz,14,4,16,9,hd);
            for(int y=4;y<=9;y+=2){ P(px,sz,13,y,Dk(hd)); P(px,sz,17,y,Dk(hd)); }
            // guard + emerald
            Rect(px,sz,10,10,21,12,gd);
            Circle(px,sz,15,11,2,gem); P(px,sz,15,11,Lk(gem));
            // blade (bright steel)
            Rect(px,sz,14,13,17,28,bl);
            Line(px,sz,14,13,14,28,hi);
            Line(px,sz,17,13,17,28,Dk(bl));
            // tip
            P(px,sz,15,29,bl); P(px,sz,15,30,hi); P(px,sz,15,31,hi);
        }

        // Venom Fang: dark serrated blade, toxic green seeping venom
        static void DrawVenomFang(Color32[] px, int sz)
        {
            var bl = C(50,58,70);   var hi = C(80,95,110);
            var gd = C(40,80,40);   var hd = C(35,50,35);
            var vn = C(50,220,60);  var dp = C(30,150,40);
            // skull-shaped pommel
            Circle(px,sz,15,2,3,hd);
            P(px,sz,13,2,dp); P(px,sz,17,2,dp); // eye sockets
            // grip (dark bone grain)
            Rect(px,sz,14,5,16,10,hd);
            for(int y=5;y<=10;y++) P(px,sz,15,y,dp);
            // guard (bone)
            Rect(px,sz,10,11,21,13,gd);
            Line(px,sz,10,12,21,12,dp);
            // serrated blade
            Rect(px,sz,14,14,17,28,bl);
            Line(px,sz,14,14,14,28,hi);
            // serrations right side
            for(int y=14;y<=28;y+=3) P(px,sz,18,y,bl);
            for(int y=16;y<=27;y+=3) P(px,sz,19,y,Dk(bl));
            // venom seeping
            P(px,sz,15,16,vn); P(px,sz,16,19,vn); P(px,sz,14,23,vn);
            P(px,sz,15,17,dp); P(px,sz,16,20,dp); P(px,sz,14,24,dp);
            // tip
            P(px,sz,15,29,bl); P(px,sz,15,30,vn); P(px,sz,15,31,dp);
        }

        // Phantom Blade: purple shadow afterimage, semi-transparent main blade
        static void DrawPhantomBlade(Color32[] px, int sz)
        {
            var bl = C(160,120,220,200); var hi = C(210,180,255,220);
            var sh = C(80,40,120,160);   var hd = C(40,20,60);
            var gm = C(200,80,220);
            // dark grip
            Rect(px,sz,14,1,16,8,hd);
            Circle(px,sz,15,1,2,sh);
            for(int y=2;y<=8;y+=2) { P(px,sz,13,y,sh); P(px,sz,17,y,sh); }
            // guard (dark purple)
            Rect(px,sz,11,9,20,11,sh); Circle(px,sz,15,10,2,gm); P(px,sz,15,10,hi);
            // left afterimage
            Rect(px,sz,11,12,12,27,sh);
            // right afterimage
            Rect(px,sz,18,12,19,27,sh);
            // main blade (semi-transparent purple)
            Rect(px,sz,14,12,16,28,bl);
            Line(px,sz,14,12,14,28,hi);
            Line(px,sz,16,12,16,28,sh);
            // tip
            P(px,sz,15,29,bl); P(px,sz,15,30,hi); P(px,sz,15,31,gm);
        }

        // ── Longsword ──────────────────────────────────────────────────

        // Iron Sword: standard iron one-handed sword
        static void DrawIronSword(Color32[] px, int sz)
        {
            var bl = C(180,183,193); var hi = C(220,223,235);
            var gd = C(135,137,148); var hd = C(110,76,46);
            // pommel
            Circle(px,sz,15,1,2,gd); P(px,sz,15,1,hi);
            // grip
            Rect(px,sz,14,3,16,9,hd);
            P(px,sz,14,5,Dk(hd)); P(px,sz,16,5,Dk(hd));
            P(px,sz,14,8,Dk(hd)); P(px,sz,16,8,Dk(hd));
            // cross guard
            Rect(px,sz,10,10,21,12,gd); Line(px,sz,10,11,21,11,Dk(gd));
            P(px,sz,10,10,Dk(gd)); P(px,sz,21,10,Dk(gd));
            // blade (center fuller)
            Rect(px,sz,14,13,17,28,bl);
            Line(px,sz,15,13,15,27,hi); // fuller highlight
            Line(px,sz,17,13,17,28,Dk(bl));
            // tip
            P(px,sz,15,29,bl); P(px,sz,16,29,bl);
            P(px,sz,15,30,hi); P(px,sz,15,31,hi);
        }

        // Knight Sword: polished steel blade, gold guard, imposing presence
        static void DrawKnightSword(Color32[] px, int sz)
        {
            var bl = C(200,205,218); var hi = C(240,244,255);
            var gd = C(200,165,40);  var dk = C(140,110,20);
            var hd = C(95,65,35);
            // round pommel (gold)
            Circle(px,sz,15,2,3,gd); Circle(px,sz,15,2,2,Lk(gd));
            // grip (dark leather)
            Rect(px,sz,14,5,16,9,hd);
            for(int y=5;y<=9;y++) { P(px,sz,13,y,dk); P(px,sz,17,y,dk); } // gold wire wrap
            for(int y=6;y<=9;y+=2) { P(px,sz,13,y,gd); P(px,sz,17,y,gd); }
            // wide gold guard
            Rect(px,sz,9,10,22,13,gd);
            Line(px,sz,9,11,22,11,Lk(gd)); Line(px,sz,9,12,22,12,dk);
            // blade (wide double-edged)
            Rect(px,sz,13,14,18,28,bl);
            Line(px,sz,13,14,13,28,hi); Line(px,sz,18,14,18,28,Dk(bl));
            Line(px,sz,15,14,15,27,hi); // fuller
            // tip
            P(px,sz,14,29,bl); P(px,sz,15,29,bl); P(px,sz,16,29,bl); P(px,sz,17,29,bl);
            P(px,sz,14,30,bl); P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // Holy Sword: holy cross guard, gold-blue radiance, glowing blade
        static void DrawHolySword(Color32[] px, int sz)
        {
            var bl = C(220,225,255); var hi = C(255,255,255);
            var gd = C(220,180,50);  var gl = C(160,200,255,180); // holy glow
            var hd = C(200,160,40);
            // holy glow (around blade)
            for(int y=13;y<=30;y++) { P(px,sz,12,y,gl); P(px,sz,19,y,gl); }
            for(int y=18;y<=25;y++) { P(px,sz,11,y,gl); P(px,sz,20,y,gl); }
            // gold round pommel
            Circle(px,sz,15,2,3,gd); Circle(px,sz,15,2,1,hi);
            // grip
            Rect(px,sz,14,5,16,9,hd);
            // cross guard (holy cross shape)
            Rect(px,sz,9,10,22,12,gd);   // horizontal
            Rect(px,sz,14,10,16,14,gd);  // vertical extension
            Line(px,sz,9,11,22,11,Lk(gd));
            Circle(px,sz,15,11,2,hi);
            // glowing blade
            Rect(px,sz,14,15,17,29,bl);
            Line(px,sz,15,15,15,29,hi); Line(px,sz,16,15,16,29,hi);
            Line(px,sz,14,15,14,29,gl); Line(px,sz,17,15,17,29,gl);
            // tip
            P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,15,31,hi);
        }

        // Dragon Sword: abyss black blade, purple-crimson dragon rune, life-stealing sword
        static void DrawDragonSword(Color32[] px, int sz)
        {
            var bl = C(35,30,50);    var hi = C(130,80,180);
            var gd = C(140,30,30);   var dk = C(80,15,15);
            var hd = C(50,20,30);    var rn = C(180,80,220); // dragon rune
            // dragon rune pommel
            Circle(px,sz,15,2,3,hd); Circle(px,sz,15,2,2,gd); Circle(px,sz,15,2,1,rn);
            // wrapped grip
            Rect(px,sz,14,5,16,9,hd);
            for(int y=5;y<=9;y++) P(px,sz,15,y,dk);
            P(px,sz,13,6,gd); P(px,sz,17,6,gd); P(px,sz,13,8,gd); P(px,sz,17,8,gd);
            // dragon wing guard
            Rect(px,sz,9,10,22,13,gd);
            P(px,sz,8,11,dk);  P(px,sz,23,11,dk);   // wing tips
            Line(px,sz,9,10,22,10,dk); Line(px,sz,9,12,22,12,rn);
            // abyss blade
            Rect(px,sz,14,14,17,28,bl);
            // dragon runes
            P(px,sz,15,17,rn); P(px,sz,16,20,rn); P(px,sz,15,23,rn); P(px,sz,16,26,rn);
            Line(px,sz,14,14,14,28,hi); Line(px,sz,17,14,17,28,dk);
            // tip (purple lightning)
            P(px,sz,15,29,hi); P(px,sz,15,30,rn); P(px,sz,15,31,rn);
        }

        // Crescent Blade: crescent-curved arc blade, high attack speed green steel
        static void DrawCrescentBlade(Color32[] px, int sz)
        {
            var bl = C(175,200,160); var hi = C(220,245,200);
            var gd = C(120,155,80);  var hd = C(95,70,40);
            var mn = C(200,220,150); // crescent color
            // round pommel
            Circle(px,sz,16,2,2,gd); P(px,sz,16,2,hi);
            // grip (slightly right-offset to match arc blade)
            Rect(px,sz,15,4,17,9,hd);
            P(px,sz,14,6,Dk(hd)); P(px,sz,18,6,Dk(hd));
            // guard (crescent shape)
            Rect(px,sz,11,10,22,12,gd);
            Circle(px,sz,16,11,2,mn); P(px,sz,16,11,hi);
            // arc blade (crescent curve)
            // right main edge
            Rect(px,sz,15,13,18,28,bl);
            Line(px,sz,15,13,15,28,hi);
            // rightward curve arc
            for(int y=15;y<=25;y++) {
                int curve = (y-15)*(y-15)/18;
                if(curve>0 && 18+curve<sz) { P(px,sz,18+curve,y,bl); P(px,sz,19+curve,y,Dk(bl)); }
            }
            // tip (bending right)
            P(px,sz,17,29,bl); P(px,sz,19,28,bl); P(px,sz,20,27,mn);
            P(px,sz,21,26,mn); P(px,sz,22,25,hi);
        }

        // Frost Lance: slender ice lance, blue frost spearhead
        static void DrawFrostLance(Color32[] px, int sz)
        {
            var bl = C(160,185,220); var hi = C(200,225,255);
            var ic = C(120,200,255); var dk = C(80,110,160);
            var hd = C(80,90,100);
            // slim lance shaft (very narrow)
            Rect(px,sz,15,1,16,22,hd);
            // metal rings
            for(int y=5;y<=20;y+=5) { P(px,sz,14,y,dk); P(px,sz,17,y,dk); }
            // ice crystal spearhead base
            Rect(px,sz,13,23,18,25,dk);
            // ice crystal head (diamond)
            Rect(px,sz,14,26,17,29,ic);
            Line(px,sz,14,26,14,29,hi); Line(px,sz,17,26,17,29,Dk(ic));
            // crystal tip
            P(px,sz,15,30,ic); P(px,sz,15,31,hi);
            P(px,sz,16,30,ic); P(px,sz,16,29,hi);
            // frost glow
            P(px,sz,13,27,C(160,230,255,120)); P(px,sz,18,27,C(160,230,255,120));
            P(px,sz,12,25,C(160,230,255,80));  P(px,sz,19,25,C(160,230,255,80));
        }

        // ── Greatsword ─────────────────────────────────────────────────

        // Iron Greatsword: wide heavy gray iron greatsword
        static void DrawIronGreatsword(Color32[] px, int sz)
        {
            var bl = C(168,172,182); var hi = C(210,215,228);
            var gd = C(130,133,143); var hd = C(100,70,40);
            // greatsword pommel
            Circle(px,sz,15,2,3,gd); P(px,sz,15,2,hi);
            // two-handed grip (wide)
            Rect(px,sz,13,5,18,10,hd);
            for(int y=5;y<=10;y+=2) { P(px,sz,13,y,Dk(hd)); P(px,sz,18,y,Dk(hd)); }
            // wide guard
            Rect(px,sz,7,11,24,14,gd);
            Line(px,sz,7,12,24,12,hi); Line(px,sz,7,13,24,13,Dk(gd));
            P(px,sz,7,12,Dk(gd)); P(px,sz,24,12,Dk(gd));
            // wide blade (5px)
            Rect(px,sz,12,15,19,28,bl);
            Line(px,sz,12,15,12,28,hi); Line(px,sz,19,15,19,28,Dk(bl));
            Line(px,sz,15,15,15,27,hi); // center fuller
            // tip (wide to narrow)
            Rect(px,sz,13,29,18,29,bl);
            Rect(px,sz,14,30,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // Warrior Greatsword: battle-scarred, green steel forged
        static void DrawWarriorGreatsword(Color32[] px, int sz)
        {
            var bl = C(160,185,150); var hi = C(205,230,195);
            var gd = C(100,130,80);  var dk = C(65,90,50);
            var hd = C(85,60,35);
            // square pommel
            Rect(px,sz,12,0,19,4,gd); Rect(px,sz,13,1,18,3,Lk(gd));
            // two-handed grip
            Rect(px,sz,13,4,18,10,hd);
            for(int y=5;y<=9;y+=2) { Line(px,sz,13,y,18,y,dk); } // leather wrap lines
            // wide guard (with notch)
            Rect(px,sz,7,11,24,14,gd);
            P(px,sz,14,11,Dk(gd)); P(px,sz,15,11,Dk(gd)); P(px,sz,16,11,Dk(gd)); P(px,sz,17,11,Dk(gd));
            Line(px,sz,7,12,24,12,hi);
            // green steel blade (with battle scars)
            Rect(px,sz,12,15,19,28,bl);
            Line(px,sz,12,15,12,28,hi); Line(px,sz,19,15,19,28,dk);
            // battle notches
            P(px,sz,14,19,dk); P(px,sz,14,20,dk); // scar 1
            P(px,sz,17,22,dk); P(px,sz,17,23,dk); // scar 2
            P(px,sz,15,25,dk);                      // scar 3
            // tip
            Rect(px,sz,13,29,18,29,bl);
            P(px,sz,14,30,bl); P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // Armor Breaker: serrated side edges, blue steel heavy strike
        static void DrawArmorBreaker(Color32[] px, int sz)
        {
            var bl = C(130,160,210); var hi = C(180,210,255);
            var gd = C(90,120,170);  var dk = C(60,85,130);
            var hd = C(70,60,80);
            // heavy pommel
            Rect(px,sz,11,0,20,4,dk); Rect(px,sz,12,1,19,3,gd);
            // wide two-handed grip
            Rect(px,sz,13,4,18,10,hd);
            for(int y=5;y<=10;y++) P(px,sz,15,y,Dk(hd)); P(px,sz,16,5,gd);
            // massive guard (blue steel)
            Rect(px,sz,6,11,25,14,gd);
            Line(px,sz,6,11,25,11,dk); Line(px,sz,6,13,25,13,hi);
            // wide blue steel blade
            Rect(px,sz,12,15,19,27,bl);
            Line(px,sz,12,15,12,27,hi); Line(px,sz,19,15,19,27,dk);
            // left serrations
            for(int y=15;y<=26;y+=3) P(px,sz,11,y+1,bl);
            for(int y=16;y<=27;y+=3) P(px,sz,10,y,dk);
            // right serrations
            for(int y=15;y<=26;y+=3) P(px,sz,20,y+1,bl);
            for(int y=16;y<=27;y+=3) P(px,sz,21,y,dk);
            // wide tip
            Rect(px,sz,13,28,18,29,bl);
            P(px,sz,14,30,bl); P(px,sz,15,30,hi); P(px,sz,16,30,hi); P(px,sz,17,30,bl);
            P(px,sz,15,31,hi);
        }

        // Iron Mallet: extremely wide hammerhead, long iron shaft
        static void DrawIronMallet(Color32[] px, int sz)
        {
            var hn = C(90,65,40);   var dk = C(60,40,20);   // wood shaft
            var mt = C(155,158,168); var hi = C(195,198,210); // iron hammer
            // shaft
            Rect(px,sz,15,0,16,17,dk); Rect(px,sz,14,1,17,16,hn);
            // metal rings
            for(int y=4;y<=16;y+=4) Rect(px,sz,14,y,17,y,mt);
            // hammerhead (wide rectangle)
            Rect(px,sz,7,18,24,29,mt);
            // hammer face pattern
            Rect(px,sz,8,19,23,28,Lk(mt));
            Line(px,sz,8,19,23,19,hi); // top highlight
            Line(px,sz,8,28,23,28,Dk(mt)); // bottom shadow
            Line(px,sz,8,19,8,28,hi);   // left highlight
            Line(px,sz,23,19,23,28,Dk(mt)); // right shadow
            // center face marking
            Rect(px,sz,13,22,18,26,Dk(mt));
            Rect(px,sz,14,23,17,25,mt);
            // iron collar
            Rect(px,sz,6,17,25,18,Dk(mt)); Rect(px,sz,6,29,25,30,Dk(mt));
            P(px,sz,6,31,Dk(mt)); P(px,sz,25,31,Dk(mt));
        }

        // Doom Blade: jet-black giant blade, purple demonic glow, bloodthirsty sword
        static void DrawDoomBlade(Color32[] px, int sz)
        {
            var bl = C(20,15,30);    var hi = C(150,80,220);
            var gd = C(120,20,20);   var dk = C(60,10,10);
            var hd = C(30,15,40);    var gl = C(180,50,230,150); // purple glow
            // dark purple glow (blade sides)
            for(int y=14;y<=31;y++) { P(px,sz,11,y,gl); P(px,sz,20,y,gl); }
            for(int y=18;y<=28;y++) { P(px,sz,10,y,C(150,40,200,100)); P(px,sz,21,y,C(150,40,200,100)); }
            // blood-red demon pommel
            Circle(px,sz,15,2,4,dk); Circle(px,sz,15,2,3,gd);
            P(px,sz,13,3,hi); P(px,sz,17,3,hi); // demon eyes
            // two-handed grip
            Rect(px,sz,13,6,17,11,hd);
            for(int y=7;y<=10;y++) P(px,sz,15,y,hi);
            // doomsday cross guard
            Rect(px,sz,7,12,23,15,gd);
            Line(px,sz,7,12,23,12,dk); Line(px,sz,7,14,23,14,hi);
            P(px,sz,7,13,dk); P(px,sz,23,13,dk);
            // jet-black giant blade (extra wide)
            Rect(px,sz,12,16,19,29,bl);
            // purple runes
            P(px,sz,15,18,hi); P(px,sz,16,21,hi); P(px,sz,15,24,hi); P(px,sz,16,27,hi);
            Line(px,sz,12,16,12,29,hi); Line(px,sz,19,16,19,29,dk);
            // tip
            Rect(px,sz,13,30,18,30,bl);
            P(px,sz,14,31,hi); P(px,sz,15,31,hi); P(px,sz,16,31,hi); P(px,sz,17,31,hi);
        }

        // ── Bow ────────────────────────────────────────────────────────
        // Bows are all vertical: string on right (x≈22), limb curves left, arrow centered vertically

        // Wooden Bow: plain wood bow, light brown
        static void DrawWoodenBow(Color32[] px, int sz)
        {
            var wd = C(145,100,55); var dk = C(100,68,30); // wood color
            var st = C(220,210,180); // string
            var ar = C(178,145,85); var tip = C(180,180,185); // fletching/tip
            // bowstring (right vertical line)
            Line(px,sz,21,2,21,30,st);
            // limb (leftward curve)
            for(int y=2;y<=30;y++) {
                float t=(y-16f)/14f; int bx=(int)(21-6*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,dk);
            }
            // nock decoration (string attachment)
            P(px,sz,21,1,dk); P(px,sz,21,0,dk); P(px,sz,21,31,dk);
            // arrow (centered vertical)
            Line(px,sz,18,5,18,27,ar);
            // fletching
            P(px,sz,17,5,ar); P(px,sz,16,5,dk); P(px,sz,19,5,ar); P(px,sz,20,5,dk);
            P(px,sz,17,7,ar); P(px,sz,19,7,ar);
            // arrowhead
            P(px,sz,18,28,tip); P(px,sz,18,29,tip); P(px,sz,18,30,Lk(tip));
        }

        // Hunter Bow: quality bow, green accents, longer
        static void DrawHunterBow(Color32[] px, int sz)
        {
            var wd = C(130,95,50); var dk = C(88,62,25);
            var ac = C(70,160,70);  // green accent
            var st = C(235,225,190); var ar = C(165,130,70); var tip = C(185,185,190);
            // bowstring
            Line(px,sz,22,1,22,30,st);
            // limb (more curved)
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-7*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,dk);
            }
            // green arrow rest
            Rect(px,sz,18,13,21,16,ac);
            // tip nocks
            Circle(px,sz,22,1,1,ac); Circle(px,sz,22,30,1,ac);
            // arrow
            Line(px,sz,19,4,19,28,ar);
            P(px,sz,18,4,ar); P(px,sz,17,4,dk); P(px,sz,20,4,ar); P(px,sz,21,4,dk);
            P(px,sz,18,6,ar); P(px,sz,20,6,ar);
            // arrowhead
            P(px,sz,19,29,tip); P(px,sz,19,30,Lk(tip)); P(px,sz,19,31,Lk(tip));
        }

        // Cloud Piercer: blue steel slim bow, piercing arrow
        static void DrawCloudPiercer(Color32[] px, int sz)
        {
            var wd = C(90,120,180); var hi = C(140,180,240);
            var st = C(180,220,255); var ar = C(160,195,235); var tip = C(200,240,255);
            var gl = C(100,180,255,160); // blue glow
            // limb glow
            for(int y=3;y<=28;y++) {
                float t=(y-15.5f)/12.5f; int bx=(int)(22-7*(1-t*t));
                if(bx-2>=0) P(px,sz,bx-2,y,gl);
            }
            // blue steel bowstring
            Line(px,sz,22,2,22,29,st);
            // blue steel limb
            for(int y=2;y<=29;y++) {
                float t=(y-15.5f)/13.5f; int bx=(int)(22-7*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi);
            }
            // blue light nocks
            Circle(px,sz,22,2,1,hi); Circle(px,sz,22,29,1,hi);
            // blue piercing arrow
            Line(px,sz,19,4,19,27,ar);
            P(px,sz,18,4,hi); P(px,sz,20,4,hi);
            P(px,sz,18,6,ar); P(px,sz,20,6,ar);
            P(px,sz,19,28,tip); P(px,sz,19,29,tip); P(px,sz,19,30,hi);
        }

        // Bone Bow: bone-white primitive bow, rough angular shape
        static void DrawBoneBow(Color32[] px, int sz)
        {
            var bn = C(220,210,185); var dk = C(165,155,132); // bone color
            var st = C(245,238,218); var ar = C(195,182,150); var tip = C(170,170,175);
            // limb (slightly shorter, thicker, angular horn bow)
            for(int y=3;y<=28;y++) {
                float t=(y-15.5f)/12.5f; int bx=(int)(21-5*(1-t*t));
                P(px,sz,bx,y,bn); P(px,sz,bx-1,y,dk); P(px,sz,bx+1,y,C(235,225,200));
            }
            // bone knuckle joints
            Circle(px,sz,16,15,2,Lk(bn)); // center joint
            Circle(px,sz,21,3,2,dk);       // upper tip joint
            Circle(px,sz,21,28,2,dk);      // lower tip joint
            // thick string
            Line(px,sz,22,3,22,28,st); P(px,sz,23,15,st); // slightly thicker at center
            // bone arrow
            Line(px,sz,19,5,19,26,ar);
            P(px,sz,18,5,dk); P(px,sz,20,5,dk); P(px,sz,18,7,bn); P(px,sz,20,7,bn);
            P(px,sz,19,27,tip); P(px,sz,19,28,tip);
        }

        // Elf Bow: compact and elegant, green leaf motifs, very high attack speed
        static void DrawElfBow(Color32[] px, int sz)
        {
            var wd = C(110,155,80); var hi = C(160,215,120);
            var st = C(240,255,220); var ar = C(175,220,130); var tip = C(200,240,150);
            var fl = C(100,190,100); // leaf motif
            // elf limb (short and curved)
            for(int y=5;y<=26;y++) {
                float t=(y-15.5f)/10.5f; int bx=(int)(21-5*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi);
            }
            // leaf decoration
            Circle(px,sz,17,15,2,fl); P(px,sz,17,15,hi);
            P(px,sz,15,14,fl); P(px,sz,15,16,fl); P(px,sz,19,14,fl); P(px,sz,19,16,fl);
            // elf tip (flower bud)
            Circle(px,sz,21,5,2,fl);  P(px,sz,21,5,hi);
            Circle(px,sz,21,26,2,fl); P(px,sz,21,26,hi);
            // thin string
            Line(px,sz,22,5,22,26,st);
            // short arrow
            Line(px,sz,19,8,19,24,ar);
            P(px,sz,18,8,fl); P(px,sz,20,8,fl);
            P(px,sz,19,25,tip); P(px,sz,19,26,tip);
        }

        // Thunder Bow: heavy war bow, blue lightning crackle
        static void DrawThunderBow(Color32[] px, int sz)
        {
            var wd = C(50,65,95); var hi = C(80,110,170);
            var lt = C(120,200,255); var dk = C(30,40,70);
            var st = C(180,230,255); var ar = C(100,170,240); var tip = C(220,245,255);
            // lightning glow
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-8*(1-t*t));
                if(bx-2>=0) P(px,sz,bx-2,y,C(100,180,255,100));
            }
            // heavy limb (2px wide)
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-8*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi); P(px,sz,bx+1,y,dk);
            }
            // thunder runes (on limb)
            P(px,sz,17,10,lt); P(px,sz,16,13,lt); P(px,sz,17,16,lt); P(px,sz,16,19,lt); P(px,sz,17,22,lt);
            // blue lightning tips
            Circle(px,sz,22,1,2,lt); Circle(px,sz,22,30,2,lt);
            // blue lightning string (with zigzag)
            Line(px,sz,23,1,23,30,st);
            P(px,sz,24,8,lt); P(px,sz,25,9,lt); P(px,sz,24,10,lt); // bolt 1
            P(px,sz,24,20,lt); P(px,sz,25,21,lt); P(px,sz,24,22,lt); // bolt 2
            // lightning arrow
            Line(px,sz,20,3,20,27,ar);
            P(px,sz,19,3,lt); P(px,sz,21,3,lt);
            P(px,sz,19,5,ar); P(px,sz,21,5,ar);
            P(px,sz,20,28,tip); P(px,sz,20,29,lt); P(px,sz,20,30,hi);
        }

        // Celestial Bow: heavenly purple divine bow, gold star motifs, rain-of-arrows bow
        static void DrawCelestialBow(Color32[] px, int sz)
        {
            var wd = C(90,50,140); var hi = C(160,100,220);
            var gd = C(220,180,50); var st = C(230,200,255);
            var ar = C(185,140,240); var tip = C(255,220,100);
            var gl = C(150,80,210,150); // purple glow
            // glow
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-7*(1-t*t));
                if(bx-2>=0) P(px,sz,bx-2,y,gl);
            }
            // celestial limb (gold-purple gradient)
            for(int y=1;y<=30;y++) {
                float t=(y-15.5f)/14.5f; int bx=(int)(22-7*(1-t*t));
                P(px,sz,bx,y,wd); P(px,sz,bx-1,y,hi);
            }
            // gold star motifs
            P(px,sz,17,9,gd);  P(px,sz,16,10,gd); P(px,sz,18,10,gd); P(px,sz,17,11,gd); // star 1
            P(px,sz,17,20,gd); P(px,sz,16,21,gd); P(px,sz,18,21,gd); P(px,sz,17,22,gd); // star 2
            // gold tips
            Circle(px,sz,22,1,2,gd);  P(px,sz,22,1,tip);
            Circle(px,sz,22,30,2,gd); P(px,sz,22,30,tip);
            // purple-gold string
            Line(px,sz,23,1,23,30,st);
            for(int y=5;y<=25;y+=5) P(px,sz,23,y,gd); // gold string knots
            // purple-gold arrow
            Line(px,sz,20,3,20,28,ar);
            P(px,sz,19,3,gd); P(px,sz,21,3,gd); P(px,sz,19,5,ar); P(px,sz,21,5,ar);
            P(px,sz,20,29,tip); P(px,sz,20,30,gd); P(px,sz,20,31,hi);
        }

        // ── Staff ──────────────────────────────────────────────────────
        // Staves are all vertical: orb/crystal at top (high y), shaft centered

        // Wood Staff: rough wood staff, wooden orb at top
        static void DrawWoodStaff(Color32[] px, int sz)
        {
            var wd = C(135,95,50);  var dk = C(95,64,28);   // wood color
            var kn = C(160,115,65); // wood knot
            var ob = C(175,115,60); var oh = C(215,165,105); // orb
            // wood shaft
            Rect(px,sz,15,1,16,20,wd);
            Line(px,sz,14,1,14,20,dk);  // shadow edge
            // wood grain knots
            for(int y=4;y<=18;y+=5) { P(px,sz,15,y,kn); P(px,sz,16,y,kn); P(px,sz,14,y,dk); }
            // head binding ring
            Rect(px,sz,13,20,18,22,Dk(wd));
            // wooden orb
            Circle(px,sz,15,26,5,ob);
            Circle(px,sz,15,26,4,Lk(ob));
            Circle(px,sz,15,26,2,oh);
            P(px,sz,14,27,oh); P(px,sz,13,26,kn); // wood grain
            P(px,sz,15,31,oh); P(px,sz,16,30,Lk(ob)); // top highlight
        }

        // Magic Staff: finely carved shaft, emerald orb
        static void DrawMagicStaff(Color32[] px, int sz)
        {
            var wd = C(80,60,40);   var hi = C(130,100,70);   // dark wood shaft
            var gm = C(40,200,80);  var gh = C(120,255,160);  // green orb
            var mt = C(160,140,80); // metal decoration
            // carved shaft
            Rect(px,sz,15,1,16,20,wd);
            Line(px,sz,14,1,14,20,Dk(wd)); Line(px,sz,17,1,17,20,hi);
            // metal rings
            for(int y=4;y<=18;y+=4) { Rect(px,sz,14,y,17,y,mt); }
            // orb mount (metal)
            Rect(px,sz,12,20,19,23,mt);
            Line(px,sz,12,21,19,21,Lk(mt)); Line(px,sz,12,22,19,22,Dk(mt));
            P(px,sz,11,21,Dk(mt)); P(px,sz,20,21,Dk(mt));
            // emerald orb (with glow)
            Circle(px,sz,15,27,4,C(20,100,40));  // outer dark layer
            Circle(px,sz,15,27,3,gm);
            Circle(px,sz,15,27,2,gh);
            P(px,sz,14,28,gh); P(px,sz,14,27,Lk(gh)); // highlight
            // top green light overflow
            P(px,sz,15,31,C(60,220,100,160)); P(px,sz,16,31,C(60,220,100,120));
        }

        // Frost Staff: blue ice crystal top, freezing staff
        static void DrawFrostStaff(Color32[] px, int sz)
        {
            var wd = C(80,100,130); var hi = C(130,160,200); // ice-blue shaft
            var ic = C(160,220,255); var ih = C(220,245,255); // ice crystal
            var ft = C(80,160,220,180); // frost glow
            // ice-blue shaft
            Rect(px,sz,15,1,16,19,wd);
            Line(px,sz,14,1,14,19,Dk(wd)); Line(px,sz,17,1,17,19,hi);
            // frost grain
            for(int y=3;y<=17;y+=4) {
                P(px,sz,13,y,ft); P(px,sz,18,y,ft);
                P(px,sz,12,y+1,C(160,220,255,100)); P(px,sz,19,y+1,C(160,220,255,100));
            }
            // crystal mount
            Rect(px,sz,12,19,19,21,ic);
            // ice crystal top (hexagonal star)
            Circle(px,sz,15,26,4,C(100,180,230)); // outer layer
            Circle(px,sz,15,26,3,ic);
            Circle(px,sz,15,26,2,ih);
            // crystal spikes (8 directions)
            P(px,sz,15,31,ih); P(px,sz,15,30,ic);
            P(px,sz,11,26,ih); P(px,sz,19,26,ih);
            P(px,sz,12,29,ic); P(px,sz,18,29,ic);
            P(px,sz,12,23,ic); P(px,sz,18,23,ic);
            // center crystal core
            P(px,sz,15,26,Lk(ih)); P(px,sz,14,26,ih); P(px,sz,16,26,ih);
        }

        // Chaos Wand: short wand, tri-element chaos orb (fire-red/lightning-blue/venom-green swirl)
        static void DrawChaosWand(Color32[] px, int sz)
        {
            var wd = C(50,30,60);   var hi = C(110,70,140);  // dark purple shaft
            var fr = C(220,80,30);  var lt = C(80,160,255);  // fire/lightning
            var vn = C(60,200,60);  var pk = C(200,80,220);  // venom/purple core
            var gl = C(160,60,200,150); // chaos glow
            // chaos glow (around orb)
            Circle(px,sz,15,24,7,C(100,40,140,80));
            Circle(px,sz,15,24,6,C(130,50,180,100));
            // dark purple short shaft
            Rect(px,sz,15,1,16,16,wd);
            Line(px,sz,14,1,14,16,Dk(wd)); Line(px,sz,17,1,17,16,hi);
            for(int y=3;y<=15;y+=4) { P(px,sz,14,y,pk); P(px,sz,17,y,pk); }
            // head metal ring
            Rect(px,sz,12,16,19,18,Dk(wd)); Rect(px,sz,13,17,18,17,pk);
            // chaos orb (fire/lightning/venom tri-color swirl)
            Circle(px,sz,15,24,4,C(80,20,100)); // deep purple base
            // fire sector (lower right)
            P(px,sz,17,22,fr); P(px,sz,18,23,fr); P(px,sz,17,24,fr); P(px,sz,18,25,C(240,140,30));
            P(px,sz,16,22,C(240,140,30));
            // lightning sector (lower left)
            P(px,sz,13,22,lt); P(px,sz,12,23,lt); P(px,sz,13,24,lt); P(px,sz,12,25,C(150,220,255));
            P(px,sz,14,22,C(150,220,255));
            // venom sector (upper)
            P(px,sz,15,28,vn); P(px,sz,14,27,vn); P(px,sz,16,27,vn); P(px,sz,15,29,C(100,240,100));
            // purple chaos core
            Circle(px,sz,15,24,2,pk); P(px,sz,15,24,Lk(pk));
            // top chaos burst
            P(px,sz,15,31,gl); P(px,sz,14,30,C(200,80,220,180)); P(px,sz,16,30,C(80,180,255,180));
        }
    }
}
