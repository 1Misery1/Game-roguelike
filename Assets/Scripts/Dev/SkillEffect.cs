using UnityEngine;

namespace Game.Dev
{
    // AOE 技能视觉特效：在指定位置短暂出现、放大并淡出后自毁
    public class SkillEffect : MonoBehaviour
    {
        SpriteRenderer _sr;
        Vector3        _targetScale;
        float          _duration;
        float          _elapsed;

        // radius：视觉半径（世界单位），决定特效大小；duration：存活时间（秒）
        public static SkillEffect Spawn(
            SkillEffectType type, Vector3 pos, float radius,
            float duration = 0.45f, Transform parent = null, float angleDeg = 0f)
        {
            var go = new GameObject("FX_" + type);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;
            if (angleDeg != 0f)
                go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SkillSprites.GetEffect(type);
            sr.color        = Color.white;
            sr.sortingOrder = 8;

            float s = Mathf.Clamp(radius * 2f, 1.2f, 8f);
            go.transform.localScale = Vector3.one * s * 0.4f;   // 从40%大小开始放大

            var fx = go.AddComponent<SkillEffect>();
            fx._sr          = sr;
            fx._duration    = Mathf.Max(duration, 0.1f);
            fx._targetScale = Vector3.one * s;
            return fx;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // 快速放大（前40%时间内到达目标大小）
            float scaleT = Mathf.Min(1f, t * 2.5f);
            transform.localScale = Vector3.Lerp(_targetScale * 0.4f, _targetScale, scaleT);

            // 后55%开始淡出
            float alpha = t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f;
            _sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

            if (_elapsed >= _duration) Destroy(gameObject);
        }
    }
}
