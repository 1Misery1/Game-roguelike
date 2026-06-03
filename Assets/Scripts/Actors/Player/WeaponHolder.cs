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
        SpriteRenderer _ownerSr;   // 角色本体,用其 flipX 判断朝向
        bool           _animating;

        void Awake()
        {
            _ownerSr = GetComponent<SpriteRenderer>();

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
            float up  = UpAngle();      // 固定「向上挥击」基准角(只随朝向轻微偏移,刀尖恒朝上)
            float aim = Angle(aimDir);  // 仅远程弓矢沿用瞄准方向
            switch (cat)
            {
                case WeaponCategory.Dagger:    yield return Stab(up, 0.13f, 0.32f); break;
                case WeaponCategory.Longsword: yield return Slash(up, 0.20f, 50f);  break;
                case WeaponCategory.Greatsword:yield return Slash(up, 0.30f, 65f);  break;
                case WeaponCategory.Bow:       yield return BowDraw(aim, 0.22f);    break;
                case WeaponCategory.Staff:     yield return StaffThrust(up, 0.18f, 0.30f); break;
            }
            UpdateRestPose(aimDir);
            _animating = false;
        }

        IEnumerator SkillAnim(WeaponCategory cat, Vector2 aimDir)
        {
            _animating = true;
            float up  = UpAngle();      // 固定「向上挥击」基准角
            float aim = Angle(aimDir);  // 仅远程弓矢沿用瞄准方向
            switch (cat)
            {
                case WeaponCategory.Dagger:
                    // Triple quick jab (恒向上)
                    for (int i = 0; i < 3; i++) yield return Stab(up + i * 8f, 0.09f, 0.42f);
                    break;
                case WeaponCategory.Longsword:
                    yield return Slash(up, 0.32f, 90f);
                    break;
                case WeaponCategory.Greatsword:
                    yield return Slash(up, 0.45f, 110f);
                    break;
                case WeaponCategory.Bow:
                    // Rapid double shot
                    yield return BowDraw(aim - 8f, 0.16f);
                    yield return BowDraw(aim + 8f, 0.16f);
                    break;
                case WeaponCategory.Staff:
                    // 向上推击+脉冲(代替会朝下的环绕)
                    yield return StaffThrust(up, 0.40f, 0.40f);
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
            const float r = 0.3f;
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

        // ── Helpers ───────────────────────────────────────────────────────────

        // 固定的「向上」基准角(90°=正上)，只随朝向轻微外倾。
        // 让挥击动作恒为向上、刀尖朝上，绝不出现朝下/绕剑刃甩柄的姿态。
        float UpAngle()
        {
            bool facingRight = _ownerSr != null ? !_ownerSr.flipX : true;
            return 90f + (facingRight ? -15f : 15f);
        }

        void UpdateRestPose(Vector2 aimDir)
        {
            // 武器悬在角色「朝向的另一侧手」：面朝右(flipX=false)→武器在左；面朝左→武器在右。
            bool facingRight = _ownerSr != null ? !_ownerSr.flipX : aimDir.x >= 0f;
            float side = facingRight ? -1f : 1f;
            // 始终竖握「朝上」、落在人物下半身；只随朝向轻微外倾，绝不朝下。
            _go.transform.localPosition = new Vector3(side * 0.3f, -0.45f, 0f);
            _go.transform.localRotation = Quaternion.Euler(0f, 0f, side * 15f);
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
