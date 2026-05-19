using UnityEngine;

namespace Game.Dev
{
    // AOE 技能视觉特效：支持每种特效独立的动画曲线
    public class SkillEffect : MonoBehaviour
    {
        SpriteRenderer  _sr;
        Vector3         _targetScale;
        float           _duration;
        float           _elapsed;
        SkillEffectType _type;
        float           _baseAngle;     // MeleeSlash 等方向型特效的基础角度

        public static SkillEffect Spawn(
            SkillEffectType type, Vector3 pos, float radius,
            float duration = 0.45f, Transform parent = null, float angleDeg = 0f)
        {
            var go = new GameObject("FX_" + type);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SkillSprites.GetEffect(type);
            sr.color        = Color.white;
            sr.sortingOrder = 8;

            float s = Mathf.Clamp(radius * 2f, 1.2f, 8f);
            go.transform.localScale = Vector3.one * s * 0.35f;

            var fx = go.AddComponent<SkillEffect>();
            fx._sr          = sr;
            fx._duration    = Mathf.Max(duration, 0.08f);
            fx._targetScale = Vector3.one * s;
            fx._type        = type;
            fx._baseAngle   = angleDeg;

            // 初始旋转
            if (IsSweepType(type))
                go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg + SweepStartOffset(type));
            else if (angleDeg != 0f)
                go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            return fx;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            if (IsSweepType(_type))
                UpdateSweep(t);
            else if (_type == SkillEffectType.ArcaneBurst || _type == SkillEffectType.EarthCrack
                  || _type == SkillEffectType.FrostBurst  || _type == SkillEffectType.ChaosBlast)
                UpdatePulse(t);
            else
                UpdateDefault(t);

            if (_elapsed >= _duration) Destroy(gameObject);
        }

        // 扫动型（MeleeSlash / PhantomSlash / DragonWave）：快速旋转弧扫 + 淡出
        void UpdateSweep(float t)
        {
            float sweepRange = SweepRange(_type);
            // 使用平方根缓出，开始快、结束慢，模拟挥砍惯性
            float easedT = Mathf.Sqrt(t);
            float angleDelta = Mathf.Lerp(SweepStartOffset(_type), SweepEndOffset(_type), easedT);
            transform.rotation = Quaternion.Euler(0f, 0f, _baseAngle + angleDelta);

            // 快速扩张到全尺寸（前30%内）
            float s = Mathf.Lerp(0.35f, 1f, Mathf.Min(1f, t * 3.3f));
            transform.localScale = _targetScale * s;

            // 前50%不透明，后50%淡出
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            _sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        // 脉冲型（爆炸/AOE）：快速扩张并带轻微过冲弹性，然后淡出
        void UpdatePulse(float t)
        {
            // 弹性扩张：超过目标尺寸5%再回弹
            float scaleT = Mathf.Min(1f, t * 2f);
            float bounce = 1f + 0.1f * Mathf.Sin(scaleT * Mathf.PI);
            transform.localScale = _targetScale * Mathf.Lerp(0.3f, 1f, scaleT) * bounce;

            float alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
            _sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        // 通用型（圆形光环等）：缩放 + 淡出
        void UpdateDefault(float t)
        {
            float scaleT = Mathf.Min(1f, t * 2.5f);
            transform.localScale = Vector3.Lerp(_targetScale * 0.4f, _targetScale, scaleT);

            float alpha = t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f;
            _sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        static bool IsSweepType(SkillEffectType t) =>
            t == SkillEffectType.MeleeSlash  ||
            t == SkillEffectType.PhantomSlash ||
            t == SkillEffectType.DragonWave;

        // 扫动起始偏移角度（相对 baseAngle）
        static float SweepStartOffset(SkillEffectType t)
        {
            switch (t)
            {
                case SkillEffectType.MeleeSlash:  return  45f;
                case SkillEffectType.PhantomSlash: return  55f;
                case SkillEffectType.DragonWave:  return  30f;
                default:                          return  40f;
            }
        }

        // 扫动结束偏移角度
        static float SweepEndOffset(SkillEffectType t)
        {
            switch (t)
            {
                case SkillEffectType.MeleeSlash:  return -45f;
                case SkillEffectType.PhantomSlash: return -55f;
                case SkillEffectType.DragonWave:  return -30f;
                default:                          return -40f;
            }
        }

        static float SweepRange(SkillEffectType t) =>
            SweepStartOffset(t) - SweepEndOffset(t);
    }
}
