using Game.Art;
using UnityEngine;

namespace Game.AI
{
    // 通用 AOE 可视化：先用「预警色」把圆形范围撑大并闪烁(warnTime)提示玩家躲避，
    // 命中时刻切到「命中色」并快速淡出(fadeTime)。
    // 用途：让 Boss 那些原本「看不见的范围伤害」（OverlapCircle）变得直观可见。
    public sealed class AoeTelegraph : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _warn, _fade, _t;
        private Color _warnColor, _hitColor;
        private Vector3 _full;
        private bool _hit;

        public static AoeTelegraph Spawn(Vector3 pos, float radius, float warnTime, float fadeTime,
                                         Color warn, Color hit, int order = 7)
        {
            var go = new GameObject("AoeTelegraph");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = InteractableSprites.Orb();
            sr.sortingOrder = order;

            var t = go.AddComponent<AoeTelegraph>();
            t._sr = sr; t._warn = warnTime; t._fade = fadeTime;
            t._warnColor = warn; t._hitColor = hit;

            float diameter = radius * 2f;
            var bs = sr.sprite.bounds.size;
            t._full = new Vector3(diameter / Mathf.Max(0.001f, bs.x),
                                  diameter / Mathf.Max(0.001f, bs.y), 1f);
            go.transform.localScale = warnTime > 0f ? t._full * 0.35f : t._full;
            return t;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (!_hit && _t < _warn)                       // 预警：撑大 + 闪烁
            {
                float k = Mathf.Clamp01(_t / Mathf.Max(0.001f, _warn));
                transform.localScale = Vector3.Lerp(_full * 0.35f, _full, k);
                var c = _warnColor;
                c.a = _warnColor.a * (0.5f + 0.5f * Mathf.PingPong(Time.time * 8f, 1f));
                _sr.color = c;
                return;
            }
            if (!_hit) { _hit = true; _t = 0f; transform.localScale = _full; }  // 命中

            float f = Mathf.Clamp01(_t / Mathf.Max(0.001f, _fade));
            var hc = _hitColor; hc.a = _hitColor.a * (1f - f);
            _sr.color = hc;
            if (f >= 1f) Destroy(gameObject);
        }
    }
}
