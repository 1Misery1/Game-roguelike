using System.Collections;
using Game.Combat;
using Game.Data;
using Game.Player;
using UnityEngine;

namespace Game.AI
{
    // Shadow Assassin — stalks at range, blinks to player and bursts.
    // Immediately blinks when it detects the player channelling an ultimate.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class ShadowAssassinAI : MonoBehaviour
    {
        public Transform target;

        [Header("Stalk Movement")]
        public float preferredMinDist = 5f;
        public float preferredMaxDist = 8f;

        [Header("Blink Strike")]
        public float blinkCooldown   = 5f;
        public float burstDamage     = 28f;
        public float retreatDistance = 6f;

        [Header("Interrupt Range")]
        public float interruptRange = 12f;       // trigger immediate blink if player is casting within this range

        [Header("Telegraph")]
        public float telegraphTime = 0.25f;      // visible flash before teleporting

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;
        private float          _nextBlinkTime;
        private bool           _isTelegraphing;

        private void Awake()
        {
            _rb            = GetComponent<Rigidbody2D>();
            _stats         = GetComponent<CharacterStats>();
            _nav           = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();
            _nextBlinkTime = Time.time + blinkCooldown;
        }

        private void Update()
        {
            if (target == null || _isTelegraphing) return;

            bool playerCasting = PlayerStateReporter.Instance != null &&
                                 PlayerStateReporter.Instance.IsCasting;
            float dist = Vector2.Distance(transform.position, target.position);

            // Immediately blink to interrupt the player's cast
            if (playerCasting && dist <= interruptRange)
            {
                _nextBlinkTime = Time.time + blinkCooldown; // reset cooldown
                StartCoroutine(TelegraphBlink());
                return;
            }

            if (Time.time >= _nextBlinkTime)
                StartCoroutine(TelegraphBlink());
        }

        private void FixedUpdate()
        {
            if (target == null || _isTelegraphing) return;

            float   dist  = Vector2.Distance(transform.position, target.position);
            Vector2 dir   = _nav.GetMoveDirection(target.position);
            float   speed = _stats.Get(StatType.MoveSpeed);

            if (dist > preferredMaxDist)
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position + dir * speed * Time.fixedDeltaTime));
            else if (dist < preferredMinDist)
                _rb.MovePosition(EnemyNavigator.Resolve(_rb.position, _rb.position - dir * speed * Time.fixedDeltaTime));
        }

        private IEnumerator TelegraphBlink()
        {
            _isTelegraphing = true;
            _nextBlinkTime  = Time.time + blinkCooldown;

            // Brief visible flash: rapid scale pulse before vanishing
            Vector3 origScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < telegraphTime)
            {
                if (!gameObject.activeInHierarchy) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / telegraphTime;
                transform.localScale = origScale * (1f + 0.5f * Mathf.Sin(t * Mathf.PI * 4f));
                yield return null;
            }
            transform.localScale = origScale;

            DoBlink();
            _isTelegraphing = false;
        }

        private void DoBlink()
        {
            if (target == null) return;

            // Appear beside the player
            Vector2 offset = Random.insideUnitCircle.normalized * 0.6f;
            transform.position = (Vector2)target.position + offset;
            if (_nav != null) _nav.InvalidatePath();

            // Burst damage
            target.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
            {
                Amount = burstDamage,
                Type   = DamageType.Physical,
                Source = gameObject
            });

            // Retreat
            Vector2 retreatDir = ((Vector2)transform.position - (Vector2)target.position).normalized;
            if (retreatDir == Vector2.zero) retreatDir = Random.insideUnitCircle.normalized;
            transform.position = (Vector2)target.position + retreatDir * retreatDistance;
            if (_nav != null) _nav.InvalidatePath();
        }
    }
}
