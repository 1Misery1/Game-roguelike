using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 腐败士官（精英）：
    //   - 近战使用双手剑大范围AOE攻击
    //   - 周期性释放光环，强化周围小怪（+50% MaxHP, +30% AttackSpeed），持续8s
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class CommanderAI : MonoBehaviour
    {
        public Transform target;

        [Header("Melee (双手剑)")]
        public float attackRange    = 2.2f;   // 宽范围挥击
        public float attackInterval = 2.0f;
        public float attackDamage   = 20f;

        [Header("Aura (光环)")]
        public float auraRadius    = 8f;
        public float auraCooldown  = 10f;
        public float auraDuration  = 8f;
        public float auraHpBonus   = 0.5f;   // +50% MaxHP
        public float auraAtkBonus  = 0.3f;   // +30% AttackSpeed

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;
        private float _lastAttackTime;
        private float _lastAuraTime = -100f;
        private bool  _isWindingUp;

        private readonly List<CharacterStats> _buffedMinions = new List<CharacterStats>();
        private float _auraExpiresAt;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _stats = GetComponent<CharacterStats>();
            _nav   = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();
        }

        private void Update()
        {
            if (target == null) return;

            // 光环到期清除buff
            if (_buffedMinions.Count > 0 && Time.time >= _auraExpiresAt)
                ClearAuraBuff();

            // 周期性光环
            if (Time.time >= _lastAuraTime + auraCooldown)
                CastAura();

            // 近战攻击（大范围挥击）
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackRange && !_isWindingUp && Time.time >= _lastAttackTime + attackInterval)
            {
                _lastAttackTime = Time.time;
                StartCoroutine(WindupSwing());
            }
        }

        private void FixedUpdate()
        {
            if (target == null || _isWindingUp) return;
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > attackRange * 0.8f)
            {
                Vector2 dir   = _nav.GetMoveDirection(target.position);
                float   speed = _stats.Get(StatType.MoveSpeed);
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position + dir * speed * Time.fixedDeltaTime));
            }
        }

        // 0.5 s wind-up before the wide AoE swing lands
        private System.Collections.IEnumerator WindupSwing()
        {
            _isWindingUp = true;
            Vector3 origScale = transform.localScale;
            float elapsed = 0f;
            const float windupTime = 0.5f;
            while (elapsed < windupTime)
            {
                if (!gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / windupTime;
                transform.localScale = origScale * (1f + 0.45f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            transform.localScale = origScale;

            float dmg  = attackDamage;
            var   cols = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = dmg,
                    Type   = DamageType.Physical,
                    Source = gameObject
                });
            }
            _isWindingUp = false;
        }

        private void CastAura()
        {
            _lastAuraTime  = Time.time;
            _auraExpiresAt = Time.time + auraDuration;
            ClearAuraBuff();

            var cols = Physics2D.OverlapCircleAll(transform.position, auraRadius);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;
                var tag = col.GetComponent<EnemyTag>();
                if (tag == null || !tag.IsAuraTarget) continue;

                var stats = col.GetComponent<CharacterStats>();
                if (stats == null) continue;

                stats.AddModifier(new StatModifier(StatType.MaxHP,       ModifierOp.PercentMul, auraHpBonus,  gameObject));
                stats.AddModifier(new StatModifier(StatType.AttackSpeed, ModifierOp.PercentMul, auraAtkBonus, gameObject));
                _buffedMinions.Add(stats);
            }
        }

        private void ClearAuraBuff()
        {
            foreach (var stats in _buffedMinions)
            {
                if (stats != null)
                    stats.RemoveModifiersFrom(gameObject);
            }
            _buffedMinions.Clear();
        }

        private void OnDestroy() => ClearAuraBuff();
    }
}
