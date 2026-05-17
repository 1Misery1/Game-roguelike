using Game.Combat;
using Game.Data;
using Game.Dev;
using UnityEngine;

namespace Game.Player
{
    // 武器技能执行器：根据技能类型执行伤害逻辑并生成对应视觉特效
    public static class WeaponSkillExecutor
    {
        public static void Execute(WeaponInstance weapon, CharacterStats stats, Transform origin, Vector2 aimDir)
        {
            if (!weapon.Data.HasSkill) return;

            float baseDmg = weapon.EffectiveDamage + stats.Get(StatType.Attack);
            float skillMul = weapon.EffectiveSkillMultiplier * stats.Get(StatType.SkillPower);
            float totalDmg = baseDmg * skillMul;
            var skill = weapon.Data.skill;

            if (aimDir == Vector2.zero) aimDir = Vector2.right;

            switch (skill.skillType)
            {
                case WeaponSkillType.VenomSpray:   DoVenomSpray(totalDmg, origin, aimDir, skill);        break;
                case WeaponSkillType.PhantomSlash: DoPhantomSlash(totalDmg, origin, skill);               break;
                case WeaponSkillType.HolyStrike:   DoHolyStrike(totalDmg, stats, origin, aimDir, skill);  break;
                case WeaponSkillType.AbyssWave:    DoAbyssWave(totalDmg, origin, aimDir, skill);          break;
                case WeaponSkillType.EarthShatter: DoEarthShatter(totalDmg, origin, skill);               break;
                case WeaponSkillType.DoomFall:     DoDoomFall(totalDmg, origin, skill);                   break;
                case WeaponSkillType.PiercingArrow:DoPiercingArrow(totalDmg, origin, aimDir, skill);      break;
                case WeaponSkillType.RainOfArrows: DoRainOfArrows(totalDmg, origin, aimDir, skill);       break;
                case WeaponSkillType.FrostNova:    DoFrostNova(totalDmg, origin, skill);                  break;
                case WeaponSkillType.ChaosBurst:   DoChaosBurst(totalDmg, origin, aimDir, skill);         break;
                case WeaponSkillType.FrostThrust:  DoFrostThrust(totalDmg, origin, aimDir, skill);        break;
                case WeaponSkillType.ThunderShot:  DoThunderShot(totalDmg, origin, aimDir, skill);        break;
            }
        }

        // 毒牙·毒液喷射：前方AOE毒雾
        private static void DoVenomSpray(float dmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            Vector2 center = (Vector2)origin.position + dir * 1.8f;
            HitCircle(center, skill.skillRadius, dmg, DamageType.Physical, origin.gameObject);
            HitCircle(center, skill.skillRadius, dmg * 0.4f, DamageType.True, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.VenomCloud, center, skill.skillRadius, 0.5f, origin.parent);
        }

        // 幻影之刃·幻影连斩：快速三连击
        private static void DoPhantomSlash(float totalDmg, Transform origin, WeaponSkillData skill)
        {
            int hits = skill.skillHitCount > 0 ? skill.skillHitCount : 1;
            float perHit = totalDmg / hits;
            for (int i = 0; i < hits; i++)
                HitCircle(origin.position, skill.skillRadius, perHit, DamageType.Physical, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.PhantomSlash, origin.position,
                Mathf.Max(skill.skillRadius, 1.5f), 0.4f, origin.parent);
        }

        // 圣光剑·圣光斩：前方一击+治愈
        private static void DoHolyStrike(float dmg, CharacterStats stats, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            Vector2 center = (Vector2)origin.position + dir * 2f;
            HitCircle(center, skill.skillRadius, dmg, DamageType.Physical, origin.gameObject);
            HitCircle(center, skill.skillRadius, dmg * 0.3f, DamageType.True, origin.gameObject);
            var health = origin.GetComponent<Health>();
            if (health != null)
                health.Heal(stats.Get(StatType.MaxHP) * 0.1f);
            SkillEffect.Spawn(SkillEffectType.HolyFlash, center, skill.skillRadius, 0.45f, origin.parent);
        }

        // 龙渊剑·龙渊斩波：前方扇形斩波
        private static void DoAbyssWave(float dmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            Vector2 center = (Vector2)origin.position + dir * (skill.skillRange * 0.5f);
            HitCircle(center, skill.skillRadius, dmg, DamageType.Physical, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.DragonWave, center, skill.skillRadius, 0.5f, origin.parent);
        }

        // 破甲重剑·大地震荡：自身周围AOE
        private static void DoEarthShatter(float dmg, Transform origin, WeaponSkillData skill)
        {
            HitCircle(origin.position, skill.skillRadius, dmg, DamageType.Physical, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.EarthCrack, origin.position, skill.skillRadius, 0.6f, origin.parent);
        }

        // 末日巨剑·毁灭天降：全场毁灭
        private static void DoDoomFall(float dmg, Transform origin, WeaponSkillData skill)
        {
            HitCircle(origin.position, skill.skillRadius, dmg, DamageType.Physical, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.DoomColumn, origin.position, 2.5f, 0.8f, origin.parent);
        }

        // 穿云弓·穿云箭：直线穿透
        private static void DoPiercingArrow(float dmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            var hits = Physics2D.RaycastAll(origin.position, dir, skill.skillRange);
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == origin.gameObject) continue;
                hit.collider.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = dmg, Type = DamageType.Physical, IsCrit = false, Source = origin.gameObject
                });
            }
            VisualProjectile.Spawn(ProjectileType.PiercingArrow, origin.position, dir,
                22f, skill.skillRange, 0.4f, origin.parent);
        }

        // 天风弓·箭雨：随机覆盖目标区域
        private static void DoRainOfArrows(float totalDmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            int hits = skill.skillHitCount > 0 ? skill.skillHitCount : 5;
            float perHit = totalDmg / hits;
            Vector2 center = (Vector2)origin.position + dir * Mathf.Min(skill.skillRange, 5f);
            for (int i = 0; i < hits; i++)
            {
                Vector2 offset = Random.insideUnitCircle * skill.skillRadius * 0.6f;
                Vector2 hitPos = center + offset;
                HitCircle(hitPos, 1.2f, perHit, DamageType.Physical, origin.gameObject);
                SkillEffect.Spawn(SkillEffectType.ArrowImpact, hitPos, 0.6f, 0.3f, origin.parent);
                // 视觉箭矢略微错开出发
                VisualProjectile.Spawn(ProjectileType.RainArrow,
                    (Vector3)hitPos + Vector3.up * (4f + Random.value * 2f),
                    Vector2.down, 18f, 4f + Random.value * 2f, 0.22f, origin.parent);
            }
        }

        // 寒冰法杖·冰霜新星：自身为中心AOE冰爆
        private static void DoFrostNova(float dmg, Transform origin, WeaponSkillData skill)
        {
            HitCircle(origin.position, skill.skillRadius, dmg, DamageType.Magical, origin.gameObject);
            HitCircle(origin.position, skill.skillRadius, dmg * 0.25f, DamageType.True, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.FrostBurst, origin.position, skill.skillRadius, 0.5f, origin.parent);
        }

        // 混沌魔杖·混沌爆发：目标点随机元素AOE
        private static void DoChaosBurst(float dmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            Vector2 center = (Vector2)origin.position + dir * Mathf.Min(skill.skillRange, 5f);
            HitCircle(center, skill.skillRadius, dmg, DamageType.Magical, origin.gameObject);
            var bonusType = (DamageType)Random.Range(0, 3);
            HitCircle(center, skill.skillRadius * 0.7f, dmg * 0.45f, bonusType, origin.gameObject);
            SkillEffect.Spawn(SkillEffectType.ChaosBlast, center, skill.skillRadius, 0.6f, origin.parent);
        }

        // 寒铁长枪·冰枪突刺：直线穿透+冰霜追伤
        private static void DoFrostThrust(float dmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            var hits = Physics2D.RaycastAll(origin.position, dir, skill.skillRange);
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == origin.gameObject) continue;
                var target = hit.collider.GetComponent<IDamageable>();
                if (target == null) continue;
                target.TakeDamage(new DamageInfo { Amount = dmg,           Type = DamageType.Physical, IsCrit = false, Source = origin.gameObject });
                target.TakeDamage(new DamageInfo { Amount = dmg * 0.20f,  Type = DamageType.True,     IsCrit = false, Source = origin.gameObject });
            }
            VisualProjectile.Spawn(ProjectileType.FrostSpear, origin.position, dir,
                18f, skill.skillRange, 0.45f, origin.parent);
        }

        // 雷鸣战弓·落雷箭：命中后闪电链弹跳
        private static void DoThunderShot(float dmg, Transform origin, Vector2 dir, WeaponSkillData skill)
        {
            VisualProjectile.Spawn(ProjectileType.ThunderArrow, origin.position, dir,
                18f, skill.skillRange, 0.35f, origin.parent);

            var hits = Physics2D.RaycastAll(origin.position, dir, skill.skillRange);
            GameObject firstHit = null;
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == origin.gameObject) continue;
                var target = hit.collider.GetComponent<IDamageable>();
                if (target == null) continue;
                target.TakeDamage(new DamageInfo { Amount = dmg, Type = DamageType.Magical, IsCrit = false, Source = origin.gameObject });
                firstHit = hit.collider.gameObject;
                break;
            }
            // 闪电链：对主目标周围3.5f范围内其他敌人造成60%伤害
            if (firstHit != null)
            {
                SkillEffect.Spawn(SkillEffectType.WarCryRing, firstHit.transform.position, 2f, 0.4f, origin.parent);
                var nearby = Physics2D.OverlapCircleAll(firstHit.transform.position, 3.5f);
                int chained = 0;
                foreach (var col in nearby)
                {
                    if (chained >= 3) break;
                    if (col.gameObject == origin.gameObject || col.gameObject == firstHit) continue;
                    var t = col.GetComponent<IDamageable>();
                    if (t == null) continue;
                    t.TakeDamage(new DamageInfo { Amount = dmg * 0.6f, Type = DamageType.Magical, IsCrit = false, Source = origin.gameObject });
                    chained++;
                }
            }
        }

        private static void HitCircle(Vector2 center, float radius, float damage, DamageType type, GameObject source)
        {
            var cols = Physics2D.OverlapCircleAll(center, radius);
            foreach (var col in cols)
            {
                if (col.gameObject == source) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = damage, Type = type, IsCrit = false, Source = source
                });
            }
        }
    }
}
