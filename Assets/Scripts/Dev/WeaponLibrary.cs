using Game.Data;
using Game.Player;
using UnityEngine;

namespace Game.Dev
{
    // 全部26把武器的运行时定义
    // 5类 × 4稀有度（白/绿/蓝/紫）= 20基础 + 6扩充 = 26把
    // 稀有度升级上限: 白=2, 绿=3, 蓝=4, 紫=5
    // 附魔上限: 白/绿=0, 蓝=3, 紫=4
    public static class WeaponLibrary
    {
        // ============================================================
        // 匕首系 (Dagger) - 快速近战，物理伤害，攻击范围小
        // ============================================================

        public static WeaponInstance IronDagger() => Make(
            "铁匕首", "普通铁制匕首，快速但伤害不高。",
            WeaponCategory.Dagger, WeaponRarity.White, DamageType.Physical,
            baseDmg: 8f, dmgPerLv: 3f, atkSpd: 1.5f, range: 1.2f,
            hpBonus: 10f, hpBonusPerLevel: 3f);

        public static WeaponInstance SteelDagger() => Make(
            "精铁匕首", "精良锻造的匕首，攻击更快。",
            WeaponCategory.Dagger, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 13f, dmgPerLv: 4f, atkSpd: 1.6f, range: 1.2f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        public static WeaponInstance VenomFang() => Make(
            "毒牙", "淬有剧毒的匕首，技能可喷射毒液AOE。",
            WeaponCategory.Dagger, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 18f, dmgPerLv: 5f, atkSpd: 1.7f, range: 1.2f,
            skill: MakeSkill(
                "毒液喷射",
                "向前方喷洒毒液（R/右键），对前方范围造成80%伤害并追加40%真实毒素伤害。",
                WeaponSkillType.VenomSpray, cd: 5f, mul: 0.8f, radius: 1.5f, hits: 1, skillRange: 6f),
            hpBonus: 30f, hpBonusPerLevel: 7f);

        // 吸血10%
        public static WeaponInstance PhantomBlade() => Make(
            "幻影之刃", "传说刺客之刃，技能召唤幻影进行三连斩。",
            WeaponCategory.Dagger, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 22f, dmgPerLv: 6f, atkSpd: 1.8f, range: 1.3f,
            skill: MakeSkill(
                "幻影连斩",
                "幻影分身进行3段连斩（R/右键），每段造成总伤害的1/3（总计180%伤害）。",
                WeaponSkillType.PhantomSlash, cd: 6f, mul: 1.8f, radius: 1.4f, hits: 3, skillRange: 7f),
            hpBonus: 40f, hpBonusPerLevel: 10f, lifeStealRate: 0.08f);

        // ============================================================
        // 长剑系 (Longsword) - 均衡近战，物理伤害
        // ============================================================

        public static WeaponInstance IronSword() => Make(
            "铁剑", "标准铁制单手剑，攻守均衡。",
            WeaponCategory.Longsword, WeaponRarity.White, DamageType.Physical,
            baseDmg: 15f, dmgPerLv: 5f, atkSpd: 1.0f, range: 1.8f,
            hpBonus: 15f, hpBonusPerLevel: 4f);

        public static WeaponInstance KnightSword() => Make(
            "骑士剑", "精钢锻造的骑士剑，锋利且厚重。",
            WeaponCategory.Longsword, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 24f, dmgPerLv: 7f, atkSpd: 1.0f, range: 1.8f,
            hpBonus: 25f, hpBonusPerLevel: 6f);

        // 吸血8%
        public static WeaponInstance HolyBlade() => Make(
            "圣光剑", "注入圣光之力的神剑，技能攻击同时自愈。",
            WeaponCategory.Longsword, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 30f, dmgPerLv: 8f, atkSpd: 1.0f, range: 1.8f,
            skill: MakeSkill(
                "圣光斩",
                "以圣光贯穿前方敌人（R/右键），造成150%伤害并附加30%真实伤害，恢复10%最大生命值。",
                WeaponSkillType.HolyStrike, cd: 6f, mul: 1.5f, radius: 2.0f, hits: 1, skillRange: 8f),
            hpBonus: 40f, hpBonusPerLevel: 8f, lifeStealRate: 0.08f);

        // 吸血15%
        public static WeaponInstance DragonAbyssSword() => Make(
            "龙渊剑", "蕴含龙渊之力的神剑，技能释放前方扇形斩波。",
            WeaponCategory.Longsword, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 38f, dmgPerLv: 9f, atkSpd: 1.0f, range: 2.0f,
            skill: MakeSkill(
                "龙渊斩波",
                "挥剑斩出龙渊之波（R/右键），对前方扇形区域造成200%伤害。",
                WeaponSkillType.AbyssWave, cd: 8f, mul: 2.0f, radius: 3.0f, hits: 1, skillRange: 10f),
            hpBonus: 50f, hpBonusPerLevel: 12f, lifeStealRate: 0.15f);

        // ============================================================
        // 双手剑系 (Greatsword) - 缓慢重击，高伤害大范围
        // ============================================================

        public static WeaponInstance IronGreatsword() => Make(
            "铁矛大剑", "沉重的铁制大剑，攻击缓慢但范围广、伤害高。",
            WeaponCategory.Greatsword, WeaponRarity.White, DamageType.Physical,
            baseDmg: 25f, dmgPerLv: 8f, atkSpd: 0.6f, range: 2.5f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        public static WeaponInstance WarriorGreatsword() => Make(
            "战士大剑", "久经沙场的精良大剑，每一击都势如破竹。",
            WeaponCategory.Greatsword, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 40f, dmgPerLv: 10f, atkSpd: 0.6f, range: 2.5f,
            hpBonus: 30f, hpBonusPerLevel: 7f);

        public static WeaponInstance ArmorBreaker() => Make(
            "破甲重剑", "专为破甲设计的重剑，技能引发大地震荡。",
            WeaponCategory.Greatsword, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 50f, dmgPerLv: 12f, atkSpd: 0.6f, range: 2.5f,
            skill: MakeSkill(
                "大地震荡",
                "猛力下劈引发震荡（R/右键），对自身周围超大范围造成180%伤害。",
                WeaponSkillType.EarthShatter, cd: 7f, mul: 1.8f, radius: 3.5f, hits: 1, skillRange: 12f),
            hpBonus: 45f, hpBonusPerLevel: 9f);

        // 铸铁锤 — 白色双手剑，极高HP加成，攻速极慢
        public static WeaponInstance IronMallet() => Make(
            "铸铁锤", "沉重铸铁大锤，攻击迟缓但坚若磐石，装备后大幅提升最大生命值。",
            WeaponCategory.Greatsword, WeaponRarity.White, DamageType.Physical,
            baseDmg: 28f, dmgPerLv: 9f, atkSpd: 0.4f, range: 2.2f,
            hpBonus: 35f, hpBonusPerLevel: 9f);

        // 耗血12/次
        public static WeaponInstance DoomBlade() => Make(
            "末日巨剑", "传说中末日降临之剑，技能招致天空之力降临全场。攻击消耗自身血量。",
            WeaponCategory.Greatsword, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 60f, dmgPerLv: 14f, atkSpd: 0.6f, range: 2.5f,
            skill: MakeSkill(
                "毁灭天降",
                "召唤天空之力（R/右键），对全场所有敌人造成250%毁灭性伤害。",
                WeaponSkillType.DoomFall, cd: 10f, mul: 2.5f, radius: 12f, hits: 1, skillRange: 15f),
            hpBonus: 60f, hpBonusPerLevel: 14f, hpCostPerAttack: 8f);

        // 月牙弯刀 — 绿色长剑，高攻速快节奏
        public static WeaponInstance CrescentBlade() => Make(
            "月牙弯刀", "形如弯月的精良弯刀，攻速远超普通长剑，适合高频输出。",
            WeaponCategory.Longsword, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 18f, dmgPerLv: 6f, atkSpd: 1.4f, range: 1.6f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        // 寒铁长枪 — 蓝色长剑（枪），技能直线穿刺
        public static WeaponInstance FrostLance() => Make(
            "寒铁长枪", "以寒铁铸造的长枪，攻击范围极长。技能可释放穿透所有敌人的冰枪突刺。",
            WeaponCategory.Longsword, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 26f, dmgPerLv: 7f, atkSpd: 0.9f, range: 2.6f,
            skill: MakeSkill(
                "冰枪突刺",
                "向前方突刺（R/右键），冰枪直线贯穿所有敌人，造成130%伤害并附加20%冰霜真实伤害。",
                WeaponSkillType.FrostThrust, cd: 5f, mul: 1.3f, radius: 0f, hits: 1, skillRange: 10f),
            hpBonus: 32f, hpBonusPerLevel: 7f);

        // ============================================================
        // 弓系 (Bow) - 物理远程，直线射击
        // ============================================================

        public static WeaponInstance WoodenBow() => Make(
            "木弓", "粗糙的木制弓箭，射程有限但使用方便。",
            WeaponCategory.Bow, WeaponRarity.White, DamageType.Physical,
            baseDmg: 10f, dmgPerLv: 4f, atkSpd: 1.2f, range: 8f,
            hpBonus: 8f, hpBonusPerLevel: 3f);

        public static WeaponInstance HunterBow() => Make(
            "猎人弓", "猎人常用的精良弓，射程更远，精准度高。",
            WeaponCategory.Bow, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 16f, dmgPerLv: 5f, atkSpd: 1.2f, range: 10f,
            hpBonus: 15f, hpBonusPerLevel: 4f);

        public static WeaponInstance CloudPiercer() => Make(
            "穿云弓", "传说中穿云破雾之弓，技能可射出贯穿所有敌人的穿透箭。",
            WeaponCategory.Bow, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 20f, dmgPerLv: 6f, atkSpd: 1.2f, range: 12f,
            skill: MakeSkill(
                "穿云箭",
                "射出穿透之矢（R/右键），沿鼠标方向贯穿直线上所有敌人，造成150%伤害。",
                WeaponSkillType.PiercingArrow, cd: 5f, mul: 1.5f, radius: 0f, hits: 1, skillRange: 12f),
            hpBonus: 25f, hpBonusPerLevel: 6f);

        // 骨弓 — 白色弓，攻速较低但基础可靠
        public static WeaponInstance BoneBow() => Make(
            "骨弓", "以兽骨制成的原始弓，比木弓更坚韧，攻速略低但箭矢更重。",
            WeaponCategory.Bow, WeaponRarity.White, DamageType.Physical,
            baseDmg: 14f, dmgPerLv: 4f, atkSpd: 1.0f, range: 7.5f,
            hpBonus: 10f, hpBonusPerLevel: 3f);

        // 精灵短弓 — 绿色弓，极高攻速
        public static WeaponInstance ElfBow() => Make(
            "精灵短弓", "精灵工匠打造的轻巧短弓，射速极快，适合连续输出。",
            WeaponCategory.Bow, WeaponRarity.Green, DamageType.Physical,
            baseDmg: 13f, dmgPerLv: 5f, atkSpd: 1.6f, range: 9f,
            hpBonus: 14f, hpBonusPerLevel: 4f);

        // 雷鸣战弓 — 蓝色弓，技能落雷链击
        public static WeaponInstance ThunderBow() => Make(
            "雷鸣战弓", "蕴含雷霆之力的战弓，技能射出落雷箭命中目标后进行闪电链跳。",
            WeaponCategory.Bow, WeaponRarity.Blue, DamageType.Physical,
            baseDmg: 19f, dmgPerLv: 6f, atkSpd: 1.2f, range: 11f,
            skill: MakeSkill(
                "落雷箭",
                "射出雷属性箭矢（R/右键），命中首个目标后闪电链弹至周围3个敌人，各造成主伤害的60%。",
                WeaponSkillType.ThunderShot, cd: 5f, mul: 1.5f, radius: 0f, hits: 1, skillRange: 11f),
            hpBonus: 24f, hpBonusPerLevel: 6f);

        public static WeaponInstance CelestialBow() => Make(
            "天风弓", "借助天风之力的神弓，技能降下五连箭雨覆盖目标区域。",
            WeaponCategory.Bow, WeaponRarity.Purple, DamageType.Physical,
            baseDmg: 25f, dmgPerLv: 7f, atkSpd: 1.3f, range: 12f,
            skill: MakeSkill(
                "箭雨",
                "在鼠标方向目标区域降下5支箭雨（R/右键），每支造成80%伤害（总计400%）。",
                WeaponSkillType.RainOfArrows, cd: 8f, mul: 4.0f, radius: 4.0f, hits: 5, skillRange: 14f),
            hpBonus: 35f, hpBonusPerLevel: 9f);

        // ============================================================
        // 法杖系 (Staff) - 魔法伤害，范围AOE
        // ============================================================

        public static WeaponInstance WoodStaff() => Make(
            "木法杖", "入门木制法杖，普攻在目标位置释放小范围魔法爆炸。",
            WeaponCategory.Staff, WeaponRarity.White, DamageType.Magical,
            baseDmg: 12f, dmgPerLv: 4f, atkSpd: 0.9f, range: 7f, aoeRadius: 1.2f,
            hpBonus: 12f, hpBonusPerLevel: 3f);

        public static WeaponInstance MagicStaff() => Make(
            "魔法法杖", "注入魔法能量的法杖，魔法爆炸范围更大。",
            WeaponCategory.Staff, WeaponRarity.Green, DamageType.Magical,
            baseDmg: 18f, dmgPerLv: 5f, atkSpd: 0.9f, range: 8f, aoeRadius: 1.5f,
            hpBonus: 20f, hpBonusPerLevel: 5f);

        public static WeaponInstance FrostStaff() => Make(
            "寒冰法杖", "蕴含冰霜之力的法杖，技能以自身为中心释放冰霜新星。",
            WeaponCategory.Staff, WeaponRarity.Blue, DamageType.Magical,
            baseDmg: 24f, dmgPerLv: 6f, atkSpd: 0.9f, range: 8f, aoeRadius: 1.8f,
            skill: MakeSkill(
                "冰霜新星",
                "以自身为中心爆发冰霜（R/右键），对周围所有敌人造成130%魔法伤害，并附加冰冻追伤。",
                WeaponSkillType.FrostNova, cd: 6f, mul: 1.3f, radius: 3.0f, hits: 1, skillRange: 9f),
            hpBonus: 35f, hpBonusPerLevel: 7f);

        // 吸血6%
        public static WeaponInstance ChaosWand() => Make(
            "混沌魔杖", "融合火雷毒三元素的混沌魔杖，技能释放随机元素混沌爆发。",
            WeaponCategory.Staff, WeaponRarity.Purple, DamageType.Magical,
            baseDmg: 30f, dmgPerLv: 7f, atkSpd: 0.9f, range: 9f, aoeRadius: 2.0f,
            skill: MakeSkill(
                "混沌爆发",
                "在鼠标方向目标位置释放混沌能量（R/右键），造成220%魔法伤害，并附加随机元素追伤（火/雷/毒）。",
                WeaponSkillType.ChaosBurst, cd: 8f, mul: 2.2f, radius: 3.5f, hits: 1, skillRange: 12f),
            hpBonus: 45f, hpBonusPerLevel: 10f, lifeStealRate: 0.06f);

        // ============================================================
        // 工厂方法
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
