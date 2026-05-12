using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 霜境冰刺（第2层地形杀）：
    //   待机时近乎不可见，预警期蓝白闪烁，激活瞬间造成高额伤害 + 移速减缓
    public class IceSpikeTrap : MonoBehaviour
    {
        public float idleTime     = 6.5f;  // 等待时长（秒）
        public float warningTime  = 1.8f;  // 预警闪烁时长
        public float activeTime   = 1.2f;  // 冰刺露出时长
        public float damage       = 28f;   // 激活瞬间一次性伤害
        public float slowAmount   = 0.50f; // 移速减缓幅度（50%）
        public float slowDuration = 2.0f;  // 减缓持续时间（秒）
        public float radius       = 1.0f;  // 伤害半径（世界空间）

        private enum Phase { Idle, Warning, Active }
        private Phase          _phase = Phase.Idle;
        private float          _timer;
        private bool           _hitThisCycle;
        private SpriteRenderer _sr;

        private static readonly Color HiddenColor = new Color(0.25f, 0.45f, 0.80f, 0.10f);
        private static readonly Color WarnColor   = new Color(0.80f, 0.92f, 1.00f, 0.80f);
        private static readonly Color ActiveColor = new Color(0.92f, 0.97f, 1.00f, 1.00f);

        private void Awake()
        {
            _sr       = GetComponent<SpriteRenderer>();
            _sr.color = HiddenColor;
            // 旋转45°呈菱形（冰晶感）
            transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            _timer = Random.Range(0f, idleTime);
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

        // 激活瞬间：OverlapCircle 范围内一次性伤害 + 减速
        private void ActivateSpike()
        {
            if (_hitThisCycle) return;
            _hitThisCycle = true;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, radius))
            {
                if (col.GetComponent<EnemyTag>() != null) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = damage, Type = DamageType.Magical, Source = null
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
    }
}
