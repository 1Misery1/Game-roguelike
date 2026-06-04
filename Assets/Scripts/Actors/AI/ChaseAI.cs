using System.Collections;
using Game.Combat;
using Game.Data;
using Game.Player;
using UnityEngine;

namespace Game.AI
{
    // Melee chaser (Skeleton / Soldier).
    // Navigates around walls via EnemyNavigator, telegraphs each attack
    // with a visible wind-up pause, and rushes when the player is casting.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class ChaseAI : MonoBehaviour
    {
        public Transform target;
        public float stoppingDistance = 0.9f;
        public float attackInterval   = 1.3f;
        public float contactDamage    = 10f;

        [Header("Attack Telegraph")]
        public float windupTime = 0.3f;

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;
        private float          _lastAttackTime;
        private bool           _isWindingUp;

        // Reduce interval sharply when player is in a cast window
        float EffectiveInterval =>
            PlayerStateReporter.Instance != null && PlayerStateReporter.Instance.IsCasting
                ? attackInterval * 0.5f
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

            float dist = Vector2.Distance(transform.position, target.position);

            if (dist > stoppingDistance)
            {
                Vector2 dir   = _nav.GetMoveDirection(target.position);
                float   speed = _stats.Get(StatType.MoveSpeed);
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position + dir * speed * Time.fixedDeltaTime));
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
                float t = elapsed / windupTime;
                // Grow then snap — visible "about to strike" pulse
                transform.localScale = origScale * (1f + 0.35f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            transform.localScale = origScale;

            // Damage fires at the end of the wind-up; player can dodge in the window
            if (target != null &&
                Vector2.Distance(transform.position, target.position) <= stoppingDistance + 0.6f)
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
    }
}
