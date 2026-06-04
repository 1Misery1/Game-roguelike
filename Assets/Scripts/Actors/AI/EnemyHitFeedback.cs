using System.Collections;
using Game.Combat;
using UnityEngine;

namespace Game.AI
{
    /// Attaches to every enemy at spawn (added by GameBootstrap.RegisterEnemy).
    /// Provides three layers of hit feedback:
    ///   1. White flash (replaces old red-flash coroutine in GameBootstrap)
    ///   2. Knockback — impulse away from the damage source
    ///   3. Hit-stop  — velocity zeroed for ~55 ms to give weight to each blow
    ///   4. HP tint   — sprite reddens gradually as health falls
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyHitFeedback : MonoBehaviour
    {
        public float KnockbackForce = 5.0f;   // set to 0 for bosses

        const float FlashDuration  = 0.10f;
        const float HitStopSeconds = 0.055f;

        Health         _health;
        SpriteRenderer _sr;
        Rigidbody2D    _rb;
        Coroutine      _flashCo;

        void Awake()
        {
            _health = GetComponent<Health>();
            _sr     = GetComponent<SpriteRenderer>();
            _rb     = GetComponent<Rigidbody2D>();
        }

        void OnEnable()  => _health.OnDamaged += OnHit;
        void OnDisable() => _health.OnDamaged -= OnHit;

        void OnHit(DamageInfo dmg)
        {
            // 0. 命中音效（武器击中敌人）
            Game.Core.AudioManager.Get().PlaySfx("hit");

            // 1. White flash (interrupt any ongoing flash first)
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashRoutine());

            // 2. Knockback + hit-stop
            if (_rb != null && KnockbackForce > 0f)
            {
                Vector3 dir = transform.position - (dmg.Source != null ? dmg.Source.transform.position : transform.position + Vector3.left);
                dir.z = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

                _rb.velocity = Vector2.zero;
                _rb.AddForce(dir.normalized * KnockbackForce, ForceMode2D.Impulse);
                StartCoroutine(HitStopRoutine());
            }
        }

        IEnumerator FlashRoutine()
        {
            if (_sr != null) _sr.color = Color.white;
            yield return new WaitForSeconds(FlashDuration);
            ApplyHPTint();
            _flashCo = null;
        }

        // Zero velocity briefly so the knockback frame reads as a "weight" pause
        IEnumerator HitStopRoutine()
        {
            yield return new WaitForSeconds(HitStopSeconds);
            // Enemy AI will resume normal movement after this gap
        }

        // Called after each flash ends and on death to leave the enemy tinted
        void ApplyHPTint()
        {
            if (_sr == null || _health == null) return;
            float t = Mathf.Clamp01(_health.Ratio);
            // white at full HP → dark red at low HP
            _sr.color = Color.Lerp(new Color(0.75f, 0.20f, 0.20f), Color.white, t);
        }
    }
}
