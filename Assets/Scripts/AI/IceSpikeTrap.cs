using System.Collections.Generic;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 霜境冰刺（第2层地形杀）：
    //   待机时近乎不可见，预警期蓝白闪烁，激活瞬间造成高额伤害 + 移速减缓 50%（2s）
    //   碰撞检测改用 CircleCollider2D trigger
    public class IceSpikeTrap : MonoBehaviour
    {
        public float idleTime     = 6.5f;
        public float warningTime  = 1.8f;
        public float activeTime   = 1.2f;
        public float damage       = 28f;
        public float slowAmount   = 0.50f;
        public float slowDuration = 2.0f;
        public float radius       = 0.7f;

        private enum Phase { Idle, Warning, Active }
        private Phase _phase = Phase.Idle;
        private float _timer;
        private bool  _hitThisCycle;
        private SpriteRenderer _sr;

        // Track Collider2D so we can get both IDamageable and CharacterStats
        private readonly HashSet<Collider2D> _inside = new HashSet<Collider2D>();

        private static readonly Color HiddenColor = new Color(0.25f, 0.45f, 0.80f, 0.10f);
        private static readonly Color WarnColor   = new Color(0.80f, 0.92f, 1.00f, 0.80f);
        private static readonly Color ActiveColor = new Color(0.92f, 0.97f, 1.00f, 1.00f);

        private void Awake()
        {
            _sr       = GetComponent<SpriteRenderer>();
            _sr.color = HiddenColor;
            transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            _timer = Random.Range(0f, idleTime);

            var col    = gameObject.AddComponent<CircleCollider2D>();
            col.radius    = radius;
            col.isTrigger = true;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Idle:
                    _sr.color = HiddenColor;
                    if (_timer >= idleTime) { _hitThisCycle = false; Transition(Phase.Warning); }
                    break;

                case Phase.Warning:
                    _sr.color = Color.Lerp(HiddenColor, WarnColor, Mathf.PingPong(Time.time * 6f, 1f));
                    if (_timer >= warningTime) { Transition(Phase.Active); ActivateSpike(); }
                    break;

                case Phase.Active:
                    _sr.color = ActiveColor;
                    if (_timer >= activeTime) Transition(Phase.Idle);
                    break;
            }
        }

        private void Transition(Phase next) { _phase = next; _timer = 0f; }

        private void ActivateSpike()
        {
            if (_hitThisCycle) return;
            _hitThisCycle = true;
            foreach (var col in _inside)
            {
                if (col == null) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Type   = DamageType.Magical,
                    Source = null,
                });
                var stats = col.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    string key = "IceSlow_" + GetInstanceID();
                    stats.AddModifier(new StatModifier(StatType.MoveSpeed, ModifierOp.PercentMul, -slowAmount, key));
                    StartCoroutine(RemoveSlow(stats, key, slowDuration));
                }
            }
        }

        private System.Collections.IEnumerator RemoveSlow(CharacterStats stats, string key, float delay)
        {
            yield return new WaitForSeconds(delay);
            stats?.RemoveModifiersFrom(key);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<EnemyTag>() != null) return;
            _inside.Add(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            _inside.Remove(other);
        }
    }
}
