using Game.Data;
using Game.Player;
using UnityEngine;

namespace Game.Dev
{
    // Runtime definitions for all 26 weapons
    // 5 categories × 4 rarities (White/Green/Blue/Purple) = 20 base + 6 extra = 26 total
    // Rarity upgrade caps: White=2, Green=3, Blue=4, Purple=5
    // Enchant caps: White/Green=0, Blue=3, Purple=4
    public static class WeaponLibrary
    {
        // ============================================================
        // Dagger class - fast melee, physical damage, short range
        // ============================================================

        public static WeaponInstance IronDagger() => Make(
            "Iron Dagger", "Standard iron dagger, fast but low damage.",
            WeaponCategory.Dagger, WeaponRarity.White, DamageType.Physical,
            baseDmg: 8f, dmgPerLv: 3f, atkSpd: 1.5f, range: 1.2f,
            hpBonus: 10f, hpBonusPerLevel: 3f);

        public static WeaponInstance SteelDagger() => Make(
            "Steel Dagger", "Finely forged dagger, faster attack speed.",
            WeaponCategory.Dagger, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 13f, dmgPerLv: 4f, atkSpd: 1.6f, range: 1.2f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        public static WeaponInstance VenomFang() => Make(
            "Venom Fang", "Venom-coated dagger, skill unleashes a poison AoE spray.",
            WeaponCategory.Dagger, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 18f, dmgPerLv: 5f, atkSpd: 1.7f, range: 1.2f,
            skill: MakeSkill(
                "Venom Spray",
                "Spray venom forward (R/RMB), dealing 80% damage in a frontal cone plus 40% true poison damage.",
                WeaponSkillType.VenomSpray, cd: 5f, mul: 0.8f, radius: 1.5f, hits: 1, skillRange: 6f),
            hpBonus: 30f, hpBonusPerLevel: 7f);

        // 10% life steal
        public static WeaponInstance PhantomBlade() => Make(
            "Phantom Blade", "Legendary assassin's blade, skill summons a phantom to perform triple slashes.",
            WeaponCategory.Dagger, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 22f, dmgPerLv: 6f, atkSpd: 1.8f, range: 1.3f,
            skill: MakeSkill(
                "Phantom Slash",
                "Phantom double performs 3-hit slash (R/RMB), each hit deals 1/3 of total (180% combined).",
                WeaponSkillType.PhantomSlash, cd: 6f, mul: 1.8f, radius: 1.4f, hits: 3, skillRange: 7f),
            hpBonus: 40f, hpBonusPerLevel: 10f, lifeStealRate: 0.08f);

        // ============================================================
        // Longsword class - balanced melee, physical damage
        // ============================================================

        public static WeaponInstance IronSword() => Make(
            "Iron Sword", "Standard iron longsword, balanced offense and defense.",
            WeaponCategory.Longsword, WeaponRarity.White, DamageType.Physical,
            baseDmg: 15f, dmgPerLv: 5f, atkSpd: 1.0f, range: 1.8f,
            hpBonus: 15f, hpBonusPerLevel: 4f);

        public static WeaponInstance KnightSword() => Make(
            "Knight Sword", "Knight's sword forged from refined steel, sharp and heavy.",
            WeaponCategory.Longsword, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 24f, dmgPerLv: 7f, atkSpd: 1.0f, range: 1.8f,
            hpBonus: 25f, hpBonusPerLevel: 6f);

        // 8% life steal
        public static WeaponInstance HolyBlade() => Make(
            "Holy Sword", "Divine sword imbued with holy light, skill attack also heals the wielder.",
            WeaponCategory.Longsword, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 30f, dmgPerLv: 8f, atkSpd: 1.0f, range: 1.8f,
            skill: MakeSkill(
                "Holy Strike",
                "Pierce enemies ahead with holy light (R/RMB), dealing 150% damage + 30% true damage and restoring 10% max HP.",
                WeaponSkillType.HolyStrike, cd: 6f, mul: 1.5f, radius: 2.0f, hits: 1, skillRange: 8f),
            hpBonus: 40f, hpBonusPerLevel: 8f, lifeStealRate: 0.08f);

        // 15% life steal
        public static WeaponInstance DragonAbyssSword() => Make(
            "Dragon Sword", "Divine sword containing dragon abyss power, skill releases a frontal fan-wave slash.",
            WeaponCategory.Longsword, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 38f, dmgPerLv: 9f, atkSpd: 1.0f, range: 2.0f,
            skill: MakeSkill(
                "Abyss Wave",
                "Unleash a dragon abyss wave (R/RMB), dealing 200% damage in a frontal fan.",
                WeaponSkillType.AbyssWave, cd: 8f, mul: 2.0f, radius: 3.0f, hits: 1, skillRange: 10f),
            hpBonus: 50f, hpBonusPerLevel: 12f, lifeStealRate: 0.15f);

        // ============================================================
        // Greatsword class - slow heavy strikes, high damage and wide range
        // ============================================================

        public static WeaponInstance IronGreatsword() => Make(
            "Iron Greatsword", "Heavy iron greatsword, slow attack but wide range and high damage.",
            WeaponCategory.Greatsword, WeaponRarity.White, DamageType.Physical,
            baseDmg: 25f, dmgPerLv: 8f, atkSpd: 0.6f, range: 2.5f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        public static WeaponInstance WarriorGreatsword() => Make(
            "Warrior Greatsword", "Battle-hardened refined greatsword, every swing is irresistible.",
            WeaponCategory.Greatsword, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 40f, dmgPerLv: 10f, atkSpd: 0.6f, range: 2.5f,
            hpBonus: 30f, hpBonusPerLevel: 7f);

        public static WeaponInstance ArmorBreaker() => Make(
            "Armor Breaker", "Greatsword designed to shatter armor, skill triggers an earth-shaking shockwave.",
            WeaponCategory.Greatsword, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 50f, dmgPerLv: 12f, atkSpd: 0.6f, range: 2.5f,
            skill: MakeSkill(
                "Earth Shatter",
                "Slam down with full force (R/RMB), dealing 180% damage in a massive AoE around self.",
                WeaponSkillType.EarthShatter, cd: 7f, mul: 1.8f, radius: 3.5f, hits: 1, skillRange: 12f),
            hpBonus: 45f, hpBonusPerLevel: 9f);

        // Iron Mallet — White Greatsword, huge HP bonus, very slow attack
        public static WeaponInstance IronMallet() => Make(
            "Iron Mallet", "Heavy cast-iron war hammer, slow attack but rock-solid, greatly increases max HP when equipped.",
            WeaponCategory.Greatsword, WeaponRarity.White, DamageType.Physical,
            baseDmg: 28f, dmgPerLv: 9f, atkSpd: 0.4f, range: 2.2f,
            hpBonus: 35f, hpBonusPerLevel: 9f);

        // 12 HP cost per attack
        public static WeaponInstance DoomBlade() => Make(
            "Doom Blade", "Legendary sword of apocalypse, skill calls down sky-shattering force across the entire map. Attacks drain own HP.",
            WeaponCategory.Greatsword, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 60f, dmgPerLv: 14f, atkSpd: 0.6f, range: 2.5f,
            skill: MakeSkill(
                "Doom Fall",
                "Summon celestial force (R/RMB), dealing 250% devastating damage to all enemies on the map.",
                WeaponSkillType.DoomFall, cd: 10f, mul: 2.5f, radius: 12f, hits: 1, skillRange: 15f),
            hpBonus: 60f, hpBonusPerLevel: 14f, hpCostPerAttack: 8f);

        // Crescent Blade — Green Longsword, high attack speed fast-paced
        public static WeaponInstance CrescentBlade() => Make(
            "Crescent Blade", "Crescent-shaped refined saber, attack speed far exceeds a standard longsword, ideal for rapid output.",
            WeaponCategory.Longsword, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 18f, dmgPerLv: 6f, atkSpd: 1.4f, range: 1.6f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        // Frost Lance — Blue Longsword (spear), skill linear pierce
        public static WeaponInstance FrostLance() => Make(
            "Frost Lance", "Spear forged from frost iron, extremely long attack range. Skill fires a frost thrust that pierces all enemies.",
            WeaponCategory.Longsword, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 26f, dmgPerLv: 7f, atkSpd: 0.9f, range: 2.6f,
            skill: MakeSkill(
                "Frost Thrust",
                "Thrust forward (R/RMB), frost lance pierces all enemies in a line, dealing 130% damage + 20% frost true damage.",
                WeaponSkillType.FrostThrust, cd: 5f, mul: 1.3f, radius: 0f, hits: 1, skillRange: 10f),
            hpBonus: 32f, hpBonusPerLevel: 7f);

        // ============================================================
        // Bow class - physical ranged, linear shots
        // ============================================================

        public static WeaponInstance WoodenBow() => Make(
            "Wooden Bow", "Crude wooden bow, limited range but easy to use.",
            WeaponCategory.Bow, WeaponRarity.White, DamageType.Physical,
            baseDmg: 10f, dmgPerLv: 4f, atkSpd: 1.2f, range: 8f,
            hpBonus: 8f, hpBonusPerLevel: 3f);

        public static WeaponInstance HunterBow() => Make(
            "Hunter Bow", "Refined bow favored by hunters, greater range and high accuracy.",
            WeaponCategory.Bow, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 16f, dmgPerLv: 5f, atkSpd: 1.2f, range: 10f,
            hpBonus: 15f, hpBonusPerLevel: 4f);

        public static WeaponInstance CloudPiercer() => Make(
            "Cloud Piercer", "Legendary cloud-piercing bow, skill fires a penetrating arrow that passes through all enemies.",
            WeaponCategory.Bow, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 20f, dmgPerLv: 6f, atkSpd: 1.2f, range: 12f,
            skill: MakeSkill(
                "Piercing Arrow",
                "Fire a piercing arrow (R/RMB), penetrating all enemies in a line toward the cursor, dealing 150% damage.",
                WeaponSkillType.PiercingArrow, cd: 5f, mul: 1.5f, radius: 0f, hits: 1, skillRange: 12f),
            hpBonus: 25f, hpBonusPerLevel: 6f);

        // Bone Bow — White Bow, lower attack speed but reliable
        public static WeaponInstance BoneBow() => Make(
            "Bone Bow", "Primitive bow made of beast bone, sturdier than the wooden bow, slightly lower attack speed but heavier arrows.",
            WeaponCategory.Bow, WeaponRarity.White, DamageType.Physical,
            baseDmg: 14f, dmgPerLv: 4f, atkSpd: 1.0f, range: 7.5f,
            hpBonus: 10f, hpBonusPerLevel: 3f);

        // Elf Bow — Green Bow, extremely high attack speed
        public static WeaponInstance ElfBow() => Make(
            "Elf Bow", "Lightweight short bow crafted by elven artisans, extremely fast firing rate, ideal for rapid sustained damage.",
            WeaponCategory.Bow, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 13f, dmgPerLv: 5f, atkSpd: 1.6f, range: 9f,
            hpBonus: 14f, hpBonusPerLevel: 4f);

        // Thunder Bow — Blue Bow, skill chain lightning
        public static WeaponInstance ThunderBow() => Make(
            "Thunder Bow", "War bow containing thunder power, skill fires a thunder arrow that chains lightning to nearby enemies on hit.",
            WeaponCategory.Bow, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 19f, dmgPerLv: 6f, atkSpd: 1.2f, range: 11f,
            skill: MakeSkill(
                "Thunder Shot",
                "Fire a thunder-charged arrow (R/RMB), on first hit chains lightning to 3 nearby enemies, each taking 60% of primary damage.",
                WeaponSkillType.ThunderShot, cd: 5f, mul: 1.5f, radius: 0f, hits: 1, skillRange: 11f),
            hpBonus: 24f, hpBonusPerLevel: 6f);

        public static WeaponInstance CelestialBow() => Make(
            "Celestial Bow", "Divine bow harnessing celestial wind, skill rains five arrows down over the target area.",
            WeaponCategory.Bow, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 25f, dmgPerLv: 7f, atkSpd: 1.3f, range: 12f,
            skill: MakeSkill(
                "Rain of Arrows",
                "Rain 5 arrows onto the target area in cursor direction (R/RMB), each dealing 80% damage (400% total).",
                WeaponSkillType.RainOfArrows, cd: 8f, mul: 4.0f, radius: 4.0f, hits: 5, skillRange: 14f),
            hpBonus: 35f, hpBonusPerLevel: 9f);

        // ============================================================
        // Staff class - magic damage, AoE
        // ============================================================

        public static WeaponInstance WoodStaff() => Make(
            "Wood Staff", "Starter wooden staff, normal attack detonates a small magic explosion at the target location.",
            WeaponCategory.Staff, WeaponRarity.White, DamageType.Magical,
            baseDmg: 12f, dmgPerLv: 4f, atkSpd: 0.9f, range: 7f, aoeRadius: 1.2f,
            hpBonus: 12f, hpBonusPerLevel: 3f);

        public static WeaponInstance MagicStaff() => Make(
            "Magic Staff", "Staff infused with magical energy, magic explosions cover a larger area.",
            WeaponCategory.Staff, WeaponRarity.Green, DamageType.Magical,
            baseDmg: 18f, dmgPerLv: 5f, atkSpd: 0.9f, range: 8f, aoeRadius: 1.5f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        public static WeaponInstance FrostStaff() => Make(
            "Frost Staff", "Staff imbued with frost power, skill releases a frost nova centered on self.",
            WeaponCategory.Staff, WeaponRarity.Blue, DamageType.Magical,
            baseDmg: 24f, dmgPerLv: 6f, atkSpd: 0.9f, range: 8f, aoeRadius: 1.8f,
            skill: MakeSkill(
                "Frost Nova",
                "Explode frost centered on self (R/RMB), dealing 130% magic damage to all nearby enemies with added frost follow-up damage.",
                WeaponSkillType.FrostNova, cd: 6f, mul: 1.3f, radius: 3.0f, hits: 1, skillRange: 9f),
            hpBonus: 35f, hpBonusPerLevel: 7f);

        // 6% life steal
        public static WeaponInstance ChaosWand() => Make(
            "Chaos Wand", "Chaos wand fusing fire, lightning, and poison, skill unleashes a random-element chaos burst.",
            WeaponCategory.Staff, WeaponRarity.Purple, DamageType.Magical,
            baseDmg: 30f, dmgPerLv: 7f, atkSpd: 0.9f, range: 9f, aoeRadius: 2.0f,
            skill: MakeSkill(
                "Chaos Burst",
                "Release chaos energy at the cursor target position (R/RMB), dealing 220% magic damage with random element follow-up damage (fire/lightning/poison).",
                WeaponSkillType.ChaosBurst, cd: 8f, mul: 2.2f, radius: 3.5f, hits: 1, skillRange: 12f),
            hpBonus: 45f, hpBonusPerLevel: 10f, lifeStealRate: 0.06f);

        // ============================================================
        // Factory methods
        // ============================================================

        private static WeaponInstance Make(
            string name, string desc,
            WeaponCategory category, WeaponRarity rarity, DamageType dmgType,
            float baseDmg, float dmgPerLv, float atkSpd, float range,
            float aoeRadius = 0f, WeaponSkillData skill = null,
            float hpBonus = 0f, float hpBonusPerLevel = 0f,
            float lifeStealRate = 0f, float hpCostPerAttack = 0f)
        {
            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.weaponName = name;
            data.description = desc;
            data.category = category;
            data.rarity = rarity;
            data.damageType = dmgType;
            data.baseDamage = baseDmg;
            data.damagePerLevel = dmgPerLv;
            data.attackSpeed = atkSpd;
            data.attackRange = range;
            data.aoeRadius = aoeRadius;
            data.skill = skill;
            data.skillMultiplierPerEnchant = 0.1f;
            data.hpBonus = hpBonus;
            data.hpBonusPerLevel = hpBonusPerLevel;
            data.lifeStealRate = lifeStealRate;
            data.hpCostPerAttack = hpCostPerAttack;

            // Rarity-based upgrade / enchant caps
            switch (rarity)
            {
                case WeaponRarity.White:
                    data.maxUpgradeLevel = 2;
                    data.maxEnchantLevel = 0;
                    break;
                case WeaponRarity.Green:
                    data.maxUpgradeLevel = 3;
                    data.maxEnchantLevel = 0;
                    break;
                case WeaponRarity.Blue:
                    data.maxUpgradeLevel = 4;
                    data.maxEnchantLevel = 3;
                    break;
                default: // Purple
                    data.maxUpgradeLevel = 5;
                    data.maxEnchantLevel = 4;
                    break;
            }

            return new WeaponInstance(data);
        }

        private static WeaponSkillData MakeSkill(
            string name, string desc,
            WeaponSkillType type, float cd, float mul,
            float radius, int hits, float skillRange)
        {
            var s = ScriptableObject.CreateInstance<WeaponSkillData>();
            s.skillName = name;
            s.description = desc;
            s.skillType = type;
            s.cooldown = cd;
            s.damageMultiplier = mul;
            s.skillRadius = radius;
            s.skillHitCount = hits;
            s.skillRange = skillRange;
            return s;
        }

        public static (WeaponInstance slot0, WeaponInstance slot1) GetStarterWeapons(string heroName)
        {
            switch (heroName)
            {
                case "Warrior": return (IronSword(),    IronGreatsword());
                case "Ranger":  return (IronDagger(),   WoodenBow());
                case "Mage":    return (WoodStaff(),    IronDagger());
                case "Paladin": return (IronSword(),    WoodStaff());
                case "Hunter":  return (WoodenBow(),    IronDagger());
                default:        return (IronSword(),    WoodenBow());
            }
        }
    }
}
