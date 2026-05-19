using System.Collections;
using Game.Combat;
using Game.Data;
using Game.Player;
using UnityEngine;

namespace Game.AI
{
    // Poison Spider — fast chaser with a short bite wind-up and DoT on hit.
    // Death effect (poison pool) is handled by GameBootstrap.AttachSpecialDeathEffect.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class PoisonSpiderAI : MonoBehaviour
    {
        public Transform target;
        public float stoppingDistance  = 0.7f;
        public float attackInterval    = 1.0f;   // faster than skeleton
        public float contactDamage     = 10f;

        [Header("Attack Telegraph")]
        public float windupTime = 0.2f;          // short but visible

        [Header("Poison DoT")]
        public float poisonTickDamage   = 5f;
        public int   poisonTicks        = 4;
        public float poisonTickInterval = 0.6f;

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;
        private float          _lastAttackTime;
        private bool           _isWindingUp;

        float EffectiveInterval =>
            PlayerStateReporter.Instance != null && PlayerStateReporter.Instance.IsCasting
                ? attackInterval * 0.2f
                : attackInterval;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _stats = GetComponent<CharacterStats>();
            _nav   = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();
        }

        private void FixedUpdate()
        {
            if (target == null || _isWindingUp) return;

            Vector2 delta = (Vector2)target.position - _rb.position;
            float   dist  = delta.magnitude;
            if (dist < 0.001f) return;

            if (dist > stoppingDistance)
            {
                Vector2 dir   = _nav.GetMoveDirection(target.position);
                float   speed = _stats.Get(StatType.MoveSpeed);
                _rb.MovePosition(_rb.position + dir * speed * Time.fixedDeltaTime);
            }
            else if (Time.time >= _lastAttackTime + EffectiveInterval)
            {
                _lastAttackTime = Time.time;
                StartCoroutine(WindupAttack());
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
                transform.localScale = origScale * (1f + 0.3f * Mathf.Sin(elapsed / windupTime * Mathf.PI));
                yield return null;
            }
            transform.localScale = origScale;

            if (target != null &&
                Vector2.Distance(transform.position, target.position) <= stoppingDistance + 0.5f)
            {
                var d = target.GetComponent<IDamageable>();
                if (d != null)
                {
                    d.TakeDamage(new DamageInfo
                    {
                        Amount = contactDamage,
                        Type   = DamageType.Physical,
                        Source = gameObject
                    });
                    StartCoroutine(ApplyPoisonDoT(d));
                }
            }

            _isWindingUp = false;
        }

        private IEnumerator ApplyPoisonDoT(IDamageable damageable)
        {
            var comp = damageable as Component;
            for (int i = 0; i < poisonTicks; i++)
            {
                yield return new WaitForSeconds(poisonTickInterval);
                if (comp == null) yield break;
                damageable.TakeDamage(new DamageInfo
                {
                    Amount = poisonTickDamage,
                    Type   = DamageType.True,
                    Source = gameObject
                });
            }
        }
    }
}
