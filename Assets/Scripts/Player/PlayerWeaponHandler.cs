using Game.AI;
using Game.Combat;
using Game.Data;
using Game.Dev;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    // 管理玩家的两把武器槽位、普攻、技能释放、武器切换
    [RequireComponent(typeof(CharacterStats))]
    public class PlayerWeaponHandler : MonoBehaviour
    {
        public WeaponInstance[] Slots = new WeaponInstance[2];  // 武器槽，最多2把
        public int ActiveSlotIndex { get; private set; } = 0;

        public event System.Action OnNormalAttackFired;
        public event System.Action OnSkillFired;
        public float BonusDamageMultiplier { get; set; } = 1f;

        // Set by GameBootstrap after hero selection; drives class-weapon bonus and backstab
        public HeroPassiveType HeroPassive { get; set; } = HeroPassiveType.None;

        // Bow charge state (queried by PlayerAnimator for hold animation)
        public bool  IsChargingBow { get; private set; }
        private const float MaxChargeTime = 1.5f;

        private CharacterStats _stats;
        private Health         _health;
        private WeaponHolder   _holder;
        private PlayerAnimator _anim;
        private float _lastAttackTime;
        private float _skillCooldownRemaining;

        // Per-slot source objects for weapon HP bonus modifiers (removed on unequip)
        private readonly object[] _weaponHPSources = { new object(), new object() };

        public WeaponInstance ActiveWeapon => Slots[ActiveSlotIndex];

        // 技能冷却进度（0=可用，1=冷却中）
        public float SkillCooldownRatio
        {
            get
            {
                float cd = RawSkillCooldown;
                return cd > 0f ? Mathf.Clamp01(_skillCooldownRemaining / cd) : 0f;
            }
        }

        public bool SkillReady => _skillCooldownRemaining <= 0f && ActiveWeapon?.Data?.HasSkill == true;

        public float SkillCooldownRemaining => _skillCooldownRemaining;

        private void Awake()
        {
            _stats  = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _holder = gameObject.AddComponent<WeaponHolder>();
            _anim   = gameObject.AddComponent<PlayerAnimator>();
        }

        private void Update()
        {
            if (_skillCooldownRemaining > 0f)
                _skillCooldownRemaining -= Time.deltaTime;

            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                SwitchWeapon();
        }

        public void SwitchWeapon()
        {
            if (Slots[0] == null && Slots[1] == null) return;
            // 切换到另一槽位（跳过空槽）
            int next = 1 - ActiveSlotIndex;
            if (Slots[next] != null) ActiveSlotIndex = next;
            _holder?.SetWeapon(ActiveWeapon?.Data);
        }

        public void EquipWeapon(WeaponInstance weapon, int slot)
        {
            if (slot < 0 || slot >= Slots.Length) return;
            _stats?.RemoveModifiersFrom(_weaponHPSources[slot]);
            Slots[slot] = weapon;
            ApplyWeaponHPBonus(slot);
            if (slot == ActiveSlotIndex) _holder?.SetWeapon(weapon?.Data);
        }

        private void ApplyWeaponHPBonus(int slot)
        {
            var weapon = Slots[slot];
            if (weapon == null) return;
            float bonus = weapon.HPBonus;
            if (bonus <= 0f) return;
            _stats?.AddModifier(new StatModifier(StatType.MaxHP, ModifierOp.Flat, bonus, _weaponHPSources[slot]));
        }

        // Call after upgrading a weapon to update its HP modifier
        public void RefreshWeaponHPBonus(int slot)
        {
            if (slot < 0 || slot >= Slots.Length) return;
            _stats?.RemoveModifiersFrom(_weaponHPSources[slot]);
            ApplyWeaponHPBonus(slot);
        }

        // 普通攻击
        public bool TryAttack(Vector2 aimDir)
        {
            var weapon = ActiveWeapon;

            // 无武器时进行徒手攻击（回退行为）
            if (weapon == null)
            {
                float punchInterval = 0.5f;
                if (Time.time < _lastAttackTime + punchInterval) return false;
                _lastAttackTime = Time.time;
                PunchAttack();
                return true;
            }

            float atkSpeed = weapon.Data.attackSpeed * Mathf.Max(_stats.Get(StatType.AttackSpeed), 0.1f);
            float interval = 1f / atkSpeed;
            if (Time.time < _lastAttackTime + interval) return false;

            _lastAttackTime = Time.time;
            float bonusMul = BonusDamageMultiplier;
            BonusDamageMultiplier = 1f;
            ExecuteNormalAttack(weapon, aimDir, bonusMul);
            _holder?.PlayAttack(weapon.Data.category, aimDir);
            _anim?.PlayAttack(weapon.Data.category, aimDir);
            OnNormalAttackFired?.Invoke();
            return true;
        }

        // 技能攻击
        public bool TryUseSkill(Vector2 aimDir)
        {
            var weapon = ActiveWeapon;
            if (weapon == null || !weapon.Data.HasSkill) return false;
            if (_skillCooldownRemaining > 0f) return false;

            _skillCooldownRemaining = RawSkillCooldown;
            WeaponSkillExecutor.Execute(weapon, _stats, transform, aimDir);
            _holder?.PlaySkill(weapon.Data.category, aimDir);
            _anim?.PlaySkill(weapon.Data.category, aimDir);
            OnSkillFired?.Invoke();
            return true;
        }

        private void PunchAttack()
        {
            float dmg = _stats.Get(StatType.Attack) + 8f;
            var cols = Physics2D.OverlapCircleAll(transform.position, 1.2f, NonWallMask);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = dmg, Type = DamageType.Physical, IsCrit = false, Source = gameObject
                });
            }
        }

        // ── 弓蓄力系统 ────────────────────────────────────────────────

        public void StartBowCharge()
        {
            IsChargingBow = true;
            _anim?.StartBowCharge();
        }

        public void CancelBowCharge()
        {
            IsChargingBow = false;
            _anim?.StopBowCharge();
        }

        // 释放蓄力箭矢；chargeTime 0~MaxChargeTime → 1x~2.5x 伤害倍率
        public bool TryFireCharged(float chargeTime, Vector2 aimDir)
        {
            IsChargingBow = false;
            _anim?.StopBowCharge();

            var weapon = ActiveWeapon;
            if (weapon?.Data?.category != WeaponCategory.Bow) return false;

            float atkSpeed = weapon.Data.attackSpeed * Mathf.Max(_stats.Get(StatType.AttackSpeed), 0.1f);
            if (Time.time < _lastAttackTime + 1f / atkSpeed) return false;
            _lastAttackTime = Time.time;

            float chargeRatio = Mathf.Clamp01(chargeTime / MaxChargeTime);
            float chargeMul   = Mathf.Lerp(1f, 2.5f, chargeRatio);
            float classBonus  = HeroPassive == HeroPassiveType.EagleEye ? 1.25f : 1f;

            float bonusMul = BonusDamageMultiplier;
            BonusDamageMultiplier = 1f;

            if (weapon.Data.hpCostPerAttack > 0f && _health != null)
            {
                float cost = Mathf.Min(weapon.Data.hpCostPerAttack, _health.Current - 1f);
                if (cost > 0f)
                    _health.TakeDamage(new DamageInfo { Amount = cost, Type = DamageType.True, Source = gameObject });
            }

            float damage = (weapon.EffectiveDamage + _stats.Get(StatType.Attack)) * bonusMul * classBonus * chargeMul;
            bool isCrit = Random.value < _stats.Get(StatType.CritRate);
            if (isCrit) damage *= _stats.Get(StatType.CritDamage);

            RangedAttack(damage, aimDir, weapon.Data.attackRange, weapon.Data.damageType, isCrit);
            if (weapon.Data.lifeStealRate > 0f) _health?.Heal(damage * weapon.Data.lifeStealRate);

            _anim?.PlayAttack(WeaponCategory.Bow, aimDir);
            OnNormalAttackFired?.Invoke();
            return true;
        }

        // ── 普通攻击执行 ──────────────────────────────────────────────

        private void ExecuteNormalAttack(WeaponInstance weapon, Vector2 aimDir, float bonusMul = 1f)
        {
            // HP drain (耗血武器) — 保证不低于1点血量
            if (weapon.Data.hpCostPerAttack > 0f && _health != null)
            {
                float cost = Mathf.Min(weapon.Data.hpCostPerAttack, _health.Current - 1f);
                if (cost > 0f)
                    _health.TakeDamage(new DamageInfo { Amount = cost, Type = DamageType.True, Source = gameObject });
            }

            // 职业-武器专项加成：法师用法杖 +30%，猎人用弓 +25%
            float classBonus = 1f;
            if      (HeroPassive == HeroPassiveType.ManaAmplification && weapon.Data.category == WeaponCategory.Staff)
                classBonus = 1.3f;
            else if (HeroPassive == HeroPassiveType.EagleEye          && weapon.Data.category == WeaponCategory.Bow)
                classBonus = 1.25f;

            float damage = (weapon.EffectiveDamage + _stats.Get(StatType.Attack)) * bonusMul * classBonus;
            bool isCrit = Random.value < _stats.Get(StatType.CritRate);
            if (isCrit) damage *= _stats.Get(StatType.CritDamage);
            var type = weapon.Data.damageType;

            // 背刺：游侠(ComboStrike)固有；匕首类；标有backstabBonus的武器
            bool canBackstab = HeroPassive == HeroPassiveType.ComboStrike
                || weapon.Data.category == WeaponCategory.Dagger
                || weapon.Data.backstabBonus;

            switch (weapon.Data.category)
            {
                case WeaponCategory.Dagger:
                case WeaponCategory.Longsword:
                case WeaponCategory.Greatsword:
                    MeleeAttack(damage, weapon.Data.attackRange, type, isCrit, aimDir, canBackstab);
                    break;
                case WeaponCategory.Bow:
                    RangedAttack(damage, aimDir, weapon.Data.attackRange, type, isCrit);
                    break;
                case WeaponCategory.Staff:
                    float aoe = weapon.Data.aoeRadius > 0f ? weapon.Data.aoeRadius : 1.2f;
                    MagicBlast(damage, aimDir, weapon.Data.attackRange, aoe, type, isCrit);
                    break;
            }

            // Lifesteal (吸血武器)
            if (weapon.Data.lifeStealRate > 0f)
                _health?.Heal(damage * weapon.Data.lifeStealRate);
        }

        private static readonly int NonWallMask = ~(1 << 9);

        private void MeleeAttack(float damage, float range, DamageType type, bool isCrit, Vector2 aimDir,
            bool canBackstab = false)
        {
            var cols = Physics2D.OverlapCircleAll(transform.position, range, NonWallMask);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;
                float finalDmg = damage;
                if (canBackstab)
                {
                    var ef = col.GetComponent<EnemyFacing>();
                    if (ef != null && ef.IsBackExposed((Vector2)transform.position))
                        finalDmg *= 1.2f;
                }
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = finalDmg, Type = type, IsCrit = isCrit, Source = gameObject
                });
            }
            // 近战刀光特效：在攻击方向生成扇形弧光
            if (aimDir == Vector2.zero) aimDir = Vector2.right;
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            var fxPos = transform.position + (Vector3)(aimDir.normalized * range * 0.45f);
            SkillEffect.Spawn(SkillEffectType.MeleeSlash, fxPos, range * 0.8f, 0.28f, null, angle);
        }

        private void RangedAttack(float damage, Vector2 dir, float range, DamageType type, bool isCrit)
        {
            if (dir == Vector2.zero) dir = Vector2.right;
            // 视觉箭矢
            VisualProjectile.Spawn(ProjectileType.Arrow, transform.position, dir,
                14f, range, 0.28f, transform.parent);
            var hits = Physics2D.RaycastAll(transform.position, dir, range);
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider.gameObject.layer == 9) break; // 箭矢被墙阻挡
                var d = hit.collider.GetComponent<IDamageable>();
                if (d != null)
                {
                    d.TakeDamage(new DamageInfo { Amount = damage, Type = type, IsCrit = isCrit, Source = gameObject });
                    break;
                }
            }
        }

        private void MagicBlast(float damage, Vector2 dir, float range, float radius, DamageType type, bool isCrit)
        {
            if (dir == Vector2.zero) dir = Vector2.right;
            float travelDist = Mathf.Min(range, 6f);
            // 视觉魔法球
            VisualProjectile.Spawn(ProjectileType.MagicOrb, transform.position, dir,
                10f, travelDist, 0.32f, transform.parent);
            Vector2 target = (Vector2)transform.position + dir * travelDist;
            var cols = Physics2D.OverlapCircleAll(target, radius, NonWallMask);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = damage, Type = type, IsCrit = isCrit, Source = gameObject
                });
            }
        }

        private float RawSkillCooldown
        {
            get
            {
                if (ActiveWeapon?.Data?.HasSkill != true) return 0f;
                float cdr = Mathf.Clamp01(_stats.Get(StatType.CooldownReduction));
                return ActiveWeapon.Data.skill.cooldown * (1f - cdr);
            }
        }
    }
}
