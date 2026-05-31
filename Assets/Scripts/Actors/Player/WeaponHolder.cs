using System.Collections;
using Game.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Art;
namespace Game.Player
{
    // Renders the held weapon as a child sprite that rotates toward the cursor
    // and animates on attack/skill.
    //
    // Sprite convention: handle at y=0, tip at y=31 → sprite naturally points UP.
    // SetWeaponRot(angle) corrects for this with (angle - 90°).
    public class WeaponHolder : MonoBehaviour
    {
        const float NormalScale = 0.55f;
        const float HeavyScale  = 0.68f;

        static readonly Vector3 RestPos = new Vector3(0.35f, -0.2f, 0f);

        GameObject     _go;
        SpriteRenderer _sr;
        bool           _animating;

        void Awake()
        {
            _go = new GameObject("_WeaponDisplay");
            _go.transform.SetParent(transform, false);
            _go.transform.localPosition = RestPos;
            _go.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            _go.transform.localScale    = Vector3.one * NormalScale;

            _sr = _go.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 11;
            _go.SetActive(false);
        }

        void Update()
        {
            if (_animating || !_go.activeSelf) return;
            UpdateRestPose(GetAimDir());
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetWeapon(WeaponData data)
        {
            if (data == null) { _go.SetActive(false); return; }
            _sr.sprite = WeaponSprites.Get(data.weaponName);
            float s = data.category == WeaponCategory.Greatsword ? HeavyScale : NormalScale;
            _go.transform.localScale = Vector3.one * s;
            _go.SetActive(true);
        }

        public void PlayAttack(WeaponCategory cat, Vector2 aimDir)
        {
            if (!_go.activeSelf) return;
            if (_animating) StopAllCoroutines();
            StartCoroutine(AttackAnim(cat, aimDir));
        }

        public void PlaySkill(WeaponCategory cat, Vector2 aimDir)
        {
            if (!_go.activeSelf) return;
            if (_animating) StopAllCoroutines();
            StartCoroutine(SkillAnim(cat, aimDir));
        }

        // ── Animation dispatch ────────────────────────────────────────────────

        IEnumerator AttackAnim(WeaponCategory cat, Vector2 aimDir)
        {
            _animating = true;
            float a = Angle(aimDir);
            switch (cat)
            {
                case WeaponCategory.Dagger:    yield return Stab(a, 0.13f, 0.50f); break;
                case WeaponCategory.Longsword: yield return Slash(a, 0.20f, 75f);  break;
                case WeaponCategory.Greatsword:yield return Slash(a, 0.30f, 110f); break;
                case WeaponCategory.Bow:       yield return BowDraw(a, 0.22f);     break;
                case WeaponCategory.Staff:     yield return StaffThrust(a, 0.18f, 0.45f); break;
            }
            UpdateRestPose(aimDir);
            _animating = false;
        }

        IEnumerator SkillAnim(WeaponCategory cat, Vector2 aimDir)
        {
            _animating = true;
            float a = Angle(aimDir);
            switch (cat)
            {
                case WeaponCategory.Dagger:
                    // Triple quick jab
                    for (int i = 0; i < 3; i++) yield return Stab(a + i * 12f, 0.09f, 0.65f);
                    break;
                case WeaponCategory.Longsword:
                    yield return Slash(a, 0.32f, 140f);
                    break;
                case WeaponCategory.Greatsword:
                    yield return Slash(a, 0.45f, 180f);
                    break;
                case WeaponCategory.Bow:
                    // Rapid double shot
                    yield return BowDraw(a - 8f, 0.16f);
                    yield return BowDraw(a + 8f, 0.16f);
                    break;
                case WeaponCategory.Staff:
                    yield return StaffCircle(a, 0.40f);
                    break;
            }
            UpdateRestPose(aimDir);
            _animating = false;
        }

        // ── Animation primitives ──────────────────────────────────────────────

        // Dagger/lance — lunge forward and pull back
        IEnumerator Stab(float aimAngle, float dur, float reach)
        {
            SetRot(aimAngle);
            Vector3 tip = Dir(aimAngle) * reach;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (!_go) yield break;
                float p    = t / dur;
                float lerp = p < 0.40f ? p / 0.40f : 1f - (p - 0.40f) / 0.60f;
                _go.transform.localPosition = Vector3.LerpUnclamped(RestPos, RestPos + tip, lerp);
                yield return null;
            }
            _go.transform.localPosition = RestPos;
        }

        // Sword — arc sweep from windup side through follow-through
        IEnumerator Slash(float aimAngle, float dur, float arcDeg)
        {
            float start = aimAngle + arcDeg * 0.5f;
            float end   = aimAngle - arcDeg * 0.5f;
            const float r = 0.38f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (!_go) yield break;
                float p = Mathf.SmoothStep(0f, 1f, t / dur);
                float a = Mathf.Lerp(start, end, p) * Mathf.Deg2Rad;
                _go.transform.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                SetRot(Mathf.Lerp(start, end, p));
                yield return null;
            }
        }

        // Bow — draw back toward player, then snap to release position
        IEnumerator BowDraw(float aimAngle, float dur)
        {
            SetRot(aimAngle);
            Vector3 fwd    = Dir(aimAngle) * 0.42f;
            Vector3 drawn  = Dir(aimAngle) * -0.18f;
            float drawT    = dur * 0.65f;
            float releaseT = dur - drawT;
            for (float t = 0f; t < drawT; t += Time.deltaTime)
            {
                if (!_go) yield break;
                _go.transform.localPosition = Vector3.Lerp(RestPos + fwd, RestPos + drawn, t / drawT);
                yield return null;
            }
            for (float t = 0f; t < releaseT; t += Time.deltaTime)
            {
                if (!_go) yield break;
                _go.transform.localPosition = Vector3.Lerp(RestPos + drawn, RestPos + fwd, t / releaseT);
                yield return null;
            }
        }

        // Staff normal — thrust and pulse toward aim
        IEnumerator StaffThrust(float aimAngle, float dur, float reach)
        {
            SetRot(aimAngle);
            Vector3 tip    = Dir(aimAngle) * reach;
            Vector3 baseS  = _go.transform.localScale;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (!_go) yield break;
                float p    = t / dur;
                float lerp = p < 0.40f ? p / 0.40f : 1f - (p - 0.40f) / 0.60f;
                _go.transform.localPosition = Vector3.LerpUnclamped(RestPos, RestPos + tip, lerp);
                _go.transform.localScale    = baseS * (1f + 0.28f * Mathf.Sin(p * Mathf.PI * 2f));
                yield return null;
            }
            _go.transform.localScale    = baseS;
            _go.transform.localPosition = RestPos;
        }

        // Staff skill — sweeping circle flourish
        IEnumerator StaffCircle(float aimAngle, float dur)
        {
            Vector3 baseS = _go.transform.localScale;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (!_go) yield break;
                float p = t / dur;
                float a = (aimAngle + p * 360f) * Mathf.Deg2Rad;
                _go.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.42f, 0f);
                SetRot(aimAngle + p * 360f);
                _go.transform.localScale = baseS * (1f + 0.38f * Mathf.Sin(p * Mathf.PI));
                yield return null;
            }
            _go.transform.localScale = baseS;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void UpdateRestPose(Vector2 aimDir)
        {
            float sign = aimDir.x >= 0f ? 1f : -1f;
            _go.transform.localPosition = new Vector3(sign * 0.35f, -0.2f, 0f);
            // Tip tilts slightly toward aim, not purely aim angle (looks more natural at rest)
            SetRot(Angle(aimDir) * 0.5f + sign * 20f);
        }

        // Sprite points UP; subtract 90° so aimAngle=0 (East) → sprite points East
        void SetRot(float aimAngle) =>
            _go.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle - 90f);

        static float Angle(Vector2 d) =>
            d.sqrMagnitude > 0.001f ? Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg : 45f;

        static Vector3 Dir(float deg) =>
            new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad), 0f);

        Vector2 GetAimDir()
        {
            if (Camera.main == null) return Vector2.right;
            var mouse = Mouse.current;
            if (mouse == null) return Vector2.right;
            Vector3 wp = Camera.main.ScreenToWorldPoint(
                new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, 0f));
            Vector2 d  = (Vector2)(wp - transform.position);
            return d.sqrMagnitude > 0.001f ? d.normalized : Vector2.right;
        }
    }
}
