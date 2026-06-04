using System.Collections;
using Game.Combat;
using Game.Data;
using Game.Player;
using UnityEngine;
using Game.Art;
namespace Game.AI
{
    // Corrupt Archer — keeps preferred distance, draws bow for 0.5 s before firing.
    // Immediately fires when the player is channelling an ultimate.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class ArcherAI : MonoBehaviour
    {
        public Transform target;
        public float preferredDistance = 6f;
        public float attackRange       = 9f;
        public float attackInterval    = 2.5f;
        public float projectileDamage  = 15f;

        [Header("Attack Telegraph")]
        public float windupTime = 0.5f;          // draw-bow animation window

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;
        private float          _lastAttackTime;
        private bool           _isShooting;

        bool WantsToInterrupt =>
            PlayerStateReporter.Instance != null &&
            PlayerStateReporter.Instance.IsCasting;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _stats = GetComponent<CharacterStats>();
            _nav   = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();
        }

        private void FixedUpdate()
        {
            if (target == null || _isShooting) return;

            float   dist = Vector2.Distance(transform.position, target.position);
            float   speed = _stats.Get(StatType.MoveSpeed);

            if (dist > preferredDistance + 1.5f)
            {
                Vector2 dir = _nav.GetMoveDirection(target.position);
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position + dir * speed * Time.fixedDeltaTime));
            }
            else if (dist < preferredDistance - 1.5f)
            {
                Vector2 away = ((Vector2)transform.position - (Vector2)target.position).normalized;
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position + away * speed * Time.fixedDeltaTime));
            }
        }

        private void Update()
        {
            if (target == null || _isShooting) return;

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > attackRange) return;

            // Skip remaining cooldown when interrupting player cast
            float effectiveInterval = WantsToInterrupt ? 0f : attackInterval;
            if (Time.time >= _lastAttackTime + effectiveInterval)
            {
                _lastAttackTime = Time.time;
                StartCoroutine(WindupShoot());
            }
        }

        private IEnumerator WindupShoot()
        {
            _isShooting = true;
            Vector3 origScale = transform.localScale;

            // "Drawing the bow" — enemy grows slightly along X (tension)
            float elapsed = 0f;
            while (elapsed < windupTime)
            {
                if (!gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / windupTime;
                transform.localScale = new Vector3(
                    origScale.x * (1f + 0.25f * t),
                    origScale.y * (1f - 0.15f * t),
                    origScale.z);
                yield return null;
            }
            transform.localScale = origScale;

            // Fire only if target is still in range
            if (target != null &&
                Vector2.Distance(transform.position, target.position) <= attackRange)
            {
                Shoot();
            }

            _isShooting = false;
        }

        private void Shoot()
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float   atk = _stats.Get(StatType.Attack);
            EnemyProjectile.Spawn(
                transform.position, dir, speed: 8f, attackRange,
                new DamageInfo { Amount = projectileDamage + atk, Type = DamageType.Physical, Source = gameObject },
                ProjectileType.Arrow, size: 0.25f, transform.parent);
        }
    }
}
