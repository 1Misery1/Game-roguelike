using System.Collections;
using Game.Data;
using UnityEngine;

namespace Game.Player
{
    // Procedural body animation for the player sprite.
    // Uses squash-and-stretch scale transforms keyed to weapon category.
    // Yields control to HeroActiveSkillHandler during hero-skill cast (IsCasting guard).
    public class PlayerAnimator : MonoBehaviour
    {
        Vector3 _baseScale;
        bool    _animating;

        void Awake() => _baseScale = transform.localScale;

        void Update()
        {
            // Let HeroActiveSkillHandler own the scale during hero-skill cast
            if (PlayerStateReporter.Instance != null && PlayerStateReporter.Instance.IsCasting)
            {
                if (_animating)
                {
                    StopAllCoroutines();
                    transform.localScale = _baseScale;
                    _animating = false;
                }
                return;
            }

            if (_animating) return;

            // Idle breathing: subtle ±1.8% scale oscillation
            float b = 1f + 0.018f * Mathf.Sin(Time.time * 1.6f);
            transform.localScale = _baseScale * b;
        }

        public void PlayAttack(WeaponCategory cat, Vector2 aimDir)
        {
            if (PlayerStateReporter.Instance != null && PlayerStateReporter.Instance.IsCasting) return;
            if (_animating) StopAllCoroutines();
            StartCoroutine(AttackAnim(cat, aimDir));
        }

        // Bow charge hold animation (called by PlayerController)
        public void StartBowCharge()
        {
            if (_animating) StopAllCoroutines();
            _animating = true;
            StartCoroutine(BowChargeHold());
        }

        public void StopBowCharge()
        {
            StopAllCoroutines();
            transform.localScale = _baseScale;
            _animating = false;
        }

        public void PlaySkill(WeaponCategory cat, Vector2 aimDir)
        {
            if (PlayerStateReporter.Instance != null && PlayerStateReporter.Instance.IsCasting) return;
            if (_animating) StopAllCoroutines();
            StartCoroutine(SkillAnim(cat, aimDir));
        }

        // ── Dispatch ─────────────────────────────────────────────────────────

        IEnumerator AttackAnim(WeaponCategory cat, Vector2 aimDir)
        {
            _animating = true;
            switch (cat)
            {
                case WeaponCategory.Dagger:     yield return Jab(0.13f);                  break;
                case WeaponCategory.Longsword:  yield return Slash(aimDir, 0.20f, 0.18f); break;
                case WeaponCategory.Greatsword: yield return Slash(aimDir, 0.30f, 0.25f); break;
                case WeaponCategory.Bow:        yield return BowPull(0.22f);               break;
                case WeaponCategory.Staff:      yield return CastPulse(0.18f, 2.0f);       break;
            }
            transform.localScale = _baseScale;
            _animating = false;
        }

        IEnumerator SkillAnim(WeaponCategory cat, Vector2 aimDir)
        {
            _animating = true;
            switch (cat)
            {
                case WeaponCategory.Dagger:
                    for (int i = 0; i < 3; i++) yield return Jab(0.09f);
                    break;
                case WeaponCategory.Longsword:  yield return Slash(aimDir, 0.32f, 0.28f); break;
                case WeaponCategory.Greatsword: yield return Slash(aimDir, 0.45f, 0.35f); break;
                case WeaponCategory.Bow:
                    yield return BowPull(0.16f);
                    yield return BowPull(0.16f);
                    break;
                case WeaponCategory.Staff:      yield return CastPulse(0.40f, 3.5f);       break;
            }
            transform.localScale = _baseScale;
            _animating = false;
        }

        // ── Primitives ────────────────────────────────────────────────────────

        // Dagger: horizontal squash (body-twist jab feel)
        IEnumerator Jab(float dur)
        {
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p      = t / dur;
                float squash = 1f + 0.25f * Mathf.Sin(p * Mathf.PI);
                transform.localScale = new Vector3(
                    _baseScale.x * squash,
                    _baseScale.y / Mathf.Sqrt(squash),
                    _baseScale.z);
                yield return null;
            }
        }

        // Sword: windup squash → swing stretch → recovery return
        IEnumerator Slash(Vector2 aimDir, float dur, float intensity)
        {
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p    = t / dur;
                float wave = -Mathf.Sin(p * Mathf.PI * 1.5f) * intensity;
                transform.localScale = new Vector3(
                    _baseScale.x * (1f + wave * 0.6f),
                    _baseScale.y * (1f - wave * 0.5f),
                    _baseScale.z);
                yield return null;
            }
        }

        // Bow: crouch draw (Y compress) → spring release
        IEnumerator BowPull(float dur)
        {
            float drawT    = dur * 0.65f;
            float releaseT = dur - drawT;
            for (float t = 0f; t < drawT; t += Time.deltaTime)
            {
                float f = t / drawT;
                transform.localScale = new Vector3(
                    _baseScale.x * Mathf.Lerp(1f,    1.15f, f),
                    _baseScale.y * Mathf.Lerp(1f,    0.86f, f),
                    _baseScale.z);
                yield return null;
            }
            for (float t = 0f; t < releaseT; t += Time.deltaTime)
            {
                float f = t / releaseT;
                transform.localScale = new Vector3(
                    _baseScale.x * Mathf.Lerp(1.15f, 1f,    f),
                    _baseScale.y * Mathf.Lerp(0.86f, 1.08f, f),
                    _baseScale.z);
                yield return null;
            }
        }

        // Bow charge hold: low-frequency tremor simulating draw tension, until StopBowCharge is called
        IEnumerator BowChargeHold()
        {
            while (true)
            {
                float t = Mathf.PingPong(Time.time * 5f, 1f);
                transform.localScale = new Vector3(
                    _baseScale.x * Mathf.Lerp(1.10f, 1.20f, t),
                    _baseScale.y * Mathf.Lerp(0.88f, 0.82f, t),
                    _baseScale.z);
                yield return null;
            }
        }

        // Staff: outward-expanding scale pulse (adjustable frequency)
        IEnumerator CastPulse(float dur, float freq)
        {
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p     = t / dur;
                float pulse = 1f + 0.20f * Mathf.Sin(p * Mathf.PI * freq) * (1f - p * 0.5f);
                transform.localScale = _baseScale * pulse;
                yield return null;
            }
        }
    }
}
