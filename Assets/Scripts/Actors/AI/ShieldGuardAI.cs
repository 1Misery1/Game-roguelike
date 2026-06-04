using System.Collections;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // Shield Guard — slow, heavy melee with 0.55 s wind-up and periodic shield.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class ShieldGuardAI : MonoBehaviour
    {
        public Transform target;
        public float attackRange       = 1.1f;
        public float attackInterval    = 2.0f;   // slow heavy swing
        public float contactDamage     = 14f;
        public float shieldInterval    = 8f;
        public float shieldDuration    = 3f;
        public float shieldReduction   = 0.8f;

        [Header("Attack Telegraph")]
        public float windupTime = 0.55f;         // clearly telegraphed heavy attack

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private Health         _health;
        private EnemyNavigator _nav;
        private float          _lastAttackTime;
        private float          _lastShieldTime  = -100f;
        private float          _shieldUntil;
        private bool           _halfHpTriggered;
        private bool           _isWindingUp;

        public bool IsShielding => Time.time < _shieldUntil;

        private void Awake()
        {
            _rb     = GetComponent<Rigidbody2D>();
            _stats  = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _nav    = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();

            _health.OnBeforeTakeDamage = InterceptDamage;
        }

        private void Update()
        {
            if (target == null) return;

            if (!_halfHpTriggered && _health != null && _health.Current <= _health.Max * 0.5f)
            {
                _halfHpTriggered = true;
                RaiseShield();
            }

            if (!IsShielding && Time.time >= _lastShieldTime + shieldInterval)
                RaiseShield();

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackRange && !_isWindingUp &&
                Time.time >= _lastAttackTime + attackInterval)
            {
                _lastAttackTime = Time.time;
                StartCoroutine(WindupAttack());
            }
        }

        private void FixedUpdate()
        {
            if (target == null || IsShielding || _isWindingUp) return;

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > attackRange)
            {
                Vector2 dir   = _nav.GetMoveDirection(target.position);
                float   speed = _stats.Get(StatType.MoveSpeed);
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position + dir * speed * Time.fixedDeltaTime));
            }
        }

        private IEnumerator WindupAttack()
        {
            _isWindingUp = true;
            Vector3 origScale = transform.localScale;

            float elapsed = 0f;
            while (elapsed < windupTime)
            {
                if (!gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                // Slow build-up pulse — heavy and threatening
                float t = elapsed / windupTime;
                transform.localScale = origScale * (1f + 0.4f * Mathf.Pow(t, 0.5f) * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            transform.localScale = origScale;

            if (target != null &&
                Vector2.Distance(transform.position, target.position) <= attackRange + 0.5f)
            {
                target.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = contactDamage,
                    Type   = DamageType.Physical,
                    Source = gameObject
                });
            }

            _isWindingUp = false;
        }

        private void RaiseShield()
        {
            _shieldUntil    = Time.time + shieldDuration;
            _lastShieldTime = Time.time;
        }

        private DamageInfo InterceptDamage(DamageInfo info)
        {
            if (!IsShielding || target == null || info.Source == null) return info;

            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Vector2 fromSrc  = ((Vector2)info.Source.transform.position - (Vector2)transform.position).normalized;

            if (Vector2.Dot(toTarget, fromSrc) > 0.5f)
                info.Amount *= (1f - shieldReduction);

            return info;
        }
    }
}
