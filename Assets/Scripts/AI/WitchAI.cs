using System.Collections;
using Game.Combat;
using Game.Data;
using Game.Dev;
using UnityEngine;

namespace Game.AI
{
    // Witch (Elite) — ranged magic attacker with 0.6 s cast wind-up,
    // periodic summoning with a visible ritual pause.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class WitchAI : MonoBehaviour
    {
        public Transform target;

        [Header("Ranged Attack")]
        public float preferredDistance = 6f;
        public float attackRange       = 8f;
        public float attackInterval    = 3.0f;
        public float attackDamage      = 18f;

        [Header("Attack Telegraph")]
        public float castWindupTime = 0.6f;      // visible charge-up before firing

        [Header("Summon")]
        public float summonCooldown = 10f;
        public int   summonCount    = 2;
        public float summonWindupTime = 0.8f;    // ritual pause before bats appear

        public System.Func<Vector3, GameObject> SpawnBatCallback;

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;
        private float          _lastAttackTime;
        private float          _lastSummonTime = -100f;
        private bool           _isCasting;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _stats = GetComponent<CharacterStats>();
            _nav   = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();
        }

        private void FixedUpdate()
        {
            if (target == null || _isCasting) return;

            float   dist  = Vector2.Distance(transform.position, target.position);
            Vector2 dir   = _nav.GetMoveDirection(target.position);
            float   speed = _stats.Get(StatType.MoveSpeed);

            if (dist > preferredDistance + 1.5f)
                _rb.MovePosition(_rb.position + dir * speed * Time.fixedDeltaTime);
            else if (dist < preferredDistance - 1.5f)
                _rb.MovePosition(_rb.position - dir * speed * Time.fixedDeltaTime);
        }

        private void Update()
        {
            if (target == null || _isCasting) return;

            if (Time.time >= _lastSummonTime + summonCooldown)
            {
                _lastSummonTime = Time.time;
                StartCoroutine(SummonRoutine());
                return;
            }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackRange && Time.time >= _lastAttackTime + attackInterval)
            {
                _lastAttackTime = Time.time;
                StartCoroutine(CastBlastRoutine());
            }
        }

        private IEnumerator CastBlastRoutine()
        {
            _isCasting = true;
            Vector3 origScale = transform.localScale;

            // Charge-up: pulse and grow — clearly telegraphed magic blast
            float elapsed = 0f;
            while (elapsed < castWindupTime)
            {
                if (!gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / castWindupTime;
                transform.localScale = origScale * (1f + 0.4f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            transform.localScale = origScale;

            if (target != null &&
                Vector2.Distance(transform.position, target.position) <= attackRange)
            {
                FireBlast();
            }

            _isCasting = false;
        }

        private IEnumerator SummonRoutine()
        {
            _isCasting = true;
            Vector3 origScale = transform.localScale;

            // Ritual: shrink then expand — visual summon circle
            float elapsed = 0f;
            while (elapsed < summonWindupTime)
            {
                if (!gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / summonWindupTime;
                // Contract then burst open
                float s = t < 0.6f
                    ? Mathf.Lerp(1f, 0.7f, t / 0.6f)
                    : Mathf.Lerp(0.7f, 1.3f, (t - 0.6f) / 0.4f);
                transform.localScale = origScale * s;
                yield return null;
            }
            transform.localScale = origScale;

            SummonBats();
            _isCasting = false;
        }

        private void FireBlast()
        {
            float dmg = attackDamage;
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            EnemyProjectile.Spawn(
                transform.position, dir, speed: 7f, attackRange,
                new DamageInfo { Amount = dmg, Type = DamageType.Magical, Source = gameObject },
                ProjectileType.MagicOrb, size: 0.3f, transform.parent);
        }

        private void SummonBats()
        {
            if (SpawnBatCallback == null) return;
            for (int i = 0; i < summonCount; i++)
            {
                Vector2 offset   = Random.insideUnitCircle.normalized * 1.5f;
                Vector3 spawnPos = transform.position + (Vector3)offset;
                SpawnBatCallback(spawnPos);
            }
        }
    }
}
