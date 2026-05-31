namespace Game.Data
{
    // 武器稀有度：白色/绿色无技能，蓝色/紫色有技能可附魔
    public enum WeaponRarity
    {
        White,   // 白色 - 初始武器，无技能
        Green,   // 绿色 - 进阶武器，无技能
        Blue,    // 蓝色 - 精良武器，有技能，可附魔
        Purple,  // 紫色 - 史诗武器，有技能，可附魔
    }

    // 武器类别：近战三类 + 远程两类
    public enum WeaponCategory
    {
        Dagger,     // 匕首 - 快速近战
        Longsword,  // 长剑 - 均衡近战
        Greatsword, // 双手剑 - 缓慢强力近战
        Bow,        // 弓 - 物理远程
        Staff,      // 法杖 - 魔法远程
    }

    // 武器技能类型（对应每个蓝/紫武器的独特技能）
    public enum WeaponSkillType
    {
        None,
        VenomSpray,      // 毒液喷射 (毒牙匕首)
        PhantomSlash,    // 幻影连斩 (幻影之刃)
        HolyStrike,      // 圣光斩   (圣光剑)
        AbyssWave,       // 龙渊斩波 (龙渊剑)
        EarthShatter,    // 大地震荡 (破甲重剑)
        DoomFall,        // 毁灭天降 (末日巨剑)
        PiercingArrow,   // 穿云箭   (穿云弓)
        RainOfArrows,    // 箭雨     (天风弓)
        FrostNova,       // 冰霜新星 (寒冰法杖)
        ChaosBurst,      // 混沌爆发 (混沌魔杖)
        FrostThrust,     // 冰枪突刺 (寒铁长枪)
        ThunderShot,     // 落雷箭   (雷鸣战弓)
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    public enum RoomType
    {
        Monster,
        Talent,
        Coin,
        Shop,
        Mystery,
        Elite,   // 精英房间：1精英 + 2小怪
        Boss
    }

    public enum StatType
    {
        MaxHP,
        Attack,
        Defense,
        MoveSpeed,
        AttackSpeed,
        CritRate,
        CritDamage,
        SkillPower,
        CooldownReduction,
        CoinGain
    }

    public enum ModifierOp
    {
        Flat,
        PercentAdd,
        PercentMul
    }

    // 英雄主动技能类型
    public enum HeroSkillType
    {
        None,
        WarCry,         // 战吼      (战士)
        ShadowStep,     // 影步      (游侠)
        ArcaneSurge,    // 奥术迸发  (法师)
        HolyLight,      // 神圣之光  (圣骑士)
        PrecisionShot,  // 精准射击  (猎人)
    }

    // 英雄被动天赋类型
    public enum HeroPassiveType
    {
        None,
        BattlefieldWill,    // 战场意志  (战士)  HP降至30%触发爆发回血
        ComboStrike,        // 连击      (游侠)  连续攻击叠加最多5层攻击加成
        ManaAmplification,  // 魔力增幅  (法师)  使用武器技能后下次普攻加倍
        SacredOath,         // 神圣誓约  (圣骑士) 击杀敌人回复5HP
        EagleEye,           // 鹰眼      (猎人)  永久暴击率+20%、暴击伤害+30%
    }

    // 敌人类型（数据分类，供 AI / 精灵加载 / 工厂 共用）
    public enum EnemyType
    {
        // ── 普通小怪 ──────────────────────────────────────────────
        Skeleton,       // 骷髅怪      — 基础追击
        Soldier,        // 腐败小兵    — 均衡近战
        Archer,         // 腐败弓箭手  — 远程射击
        Bat,            // 飞天蝙蝠    — 环绕冲刺
        ShieldGuard,    // 腐败盾士    — 盾牌减伤
        PoisonSpider,   // 毒蜘蛛      — 接触毒素DoT，死后留毒池
        ShadowAssassin, // 暗影刺客    — 瞬移爆发
        ExplosiveDemon, // 爆炎恶魔    — 近身/死亡爆炸

        // ── 精英 ──────────────────────────────────────────────────
        Commander,      // 腐败士官    — AOE + 战斗光环（联动盾卫/小兵）
        Witch,          // 女巫        — 法术AOE + 召唤蝙蝠
        PoisonShaman,   // 毒蛇祭司    — 毒素光线 + 强化毒蜘蛛
        Necromancer,    // 死灵术士    — 灵魂汲取回血 + 召唤骷髅

        // ── Boss ──────────────────────────────────────────────────
        HellGiant,      // 地狱巨人    — 第一层Boss
        FrostLich,      // 霜魂巫妖    — 第二层Boss
        ChaosLord,      // 混沌领主    — 第三层Boss
    }

    // 投掷物类型（数据分类，供 AI / 精灵加载 共用）
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

    // 技能特效类型（数据分类，供 AI / 精灵加载 / VFX 共用）
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
    }
}
