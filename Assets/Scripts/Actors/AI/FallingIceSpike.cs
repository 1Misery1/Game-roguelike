using System.Collections;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 第2层「霜暴」单根冰锥：在目标格预警 → 从天落下 → 命中瞬间对该 1×1 格造成伤害+减速。
    // 固定 1×1、不旋转。由 GameBootstrap.FrostStormRoutine 沿玩家位置周期性投放，可走位躲开。
    public class FallingIceSpike : MonoBehaviour
    {
        public float telegraphTime = 0.6f;
        public float fallTime      = 0.22f;
        public float lingerTime    = 0.25f;
        public float fallHeight    = 5f;
        public float damage        = 20f;
        public float slowAmount    = 0.5f;
        public float slowDuration  = 3.0f;   // 临时 buff，3 秒后自动解除

        private SpriteRenderer _spike, _marker;
        private Vector3 _ground;
        private float   _t;
        private bool    _hit;
        private enum Phase { Telegraph, Fall, Linger }
        private Phase _phase = Phase.Telegraph;

        public void Init(Sprite sprite, Vector3 groundPos)
        {
            _ground = groundPos;
            transform.position = groundPos;
            transform.rotation = Quaternion.identity;          // 不旋转

            var mk = new GameObject("Marker");                 // 地面预警标记
            mk.transform.SetParent(transform, false);
            _marker = mk.AddComponent<SpriteRenderer>();
            _marker.sprite       = sprite;
            _marker.color        = new Color(0.55f, 0.8f, 1f, 0.22f);
            _marker.sortingOrder = 2;

            var sp = new GameObject("Spike");                  // 自高空落下的冰锥（1×1）
            sp.transform.SetParent(transform, false);
            sp.transform.localPosition = new Vector3(0f, fallHeight, 0f);
            _spike = sp.AddComponent<SpriteRenderer>();
            _spike.sprite       = sprite;
            _spike.color        = Color.white;
            _spike.sortingOrder = 6;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Telegraph:
                    if (_marker) _marker.color = new Color(0.55f, 0.8f, 1f,
                        0.12f + 0.22f * Mathf.PingPong(Time.time * 6f, 1f));
                    if (_t >= telegraphTime) { _t = 0f; _phase = Phase.Fall; }
                    break;
                case Phase.Fall:
                    float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, fallTime));
                    if (_spike) _spike.transform.localPosition = new Vector3(0f, Mathf.Lerp(fallHeight, 0f, k * k), 0f);
                    if (k >= 1f) { Impact(); _t = 0f; _phase = Phase.Linger; }
                    break;
                case Phase.Linger:
                    if (_t >= lingerTime) Destroy(gameObject);
                    break;
            }
        }

        private void Impact()
        {
            if (_hit) return;
            _hit = true;
            if (_marker) _marker.enabled = false;
            foreach (var col in Physics2D.OverlapBoxAll(_ground, Vector2.one, 0f))
            {
                if (col.GetComponent<EnemyTag>() != null) continue;   // 霜暴针对玩家
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = damage, Type = DamageType.Magical, Source = null,
                });
                var stats = col.GetComponent<CharacterStats>();
                if (stats != null)
                    TimedModifier.Apply(stats, StatType.MoveSpeed, ModifierOp.PercentMul, -slowAmount, slowDuration);
            }
        }
    }
}
