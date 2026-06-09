using System.Collections;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 第3层「虚空危险格」：潜伏(1×1) → 闪烁预警 → 突然扩大到 3×3(9格)，
    // 对范围内「玩家与敌人」各造成一次低额伤害 + 一次减速（以减速为主），随后收回循环。
    public class VoidRift : MonoBehaviour
    {
        public float idleTime     = 4.0f;   // 潜伏（1×1）
        public float flickerTime  = 1.2f;   // 闪烁预警
        public float expandedTime = 0.7f;   // 扩大持续
        public float baseScale    = 1f;     // 潜伏 1×1
        public float expandScale  = 3f;     // 扩大 3×3 = 9 格
        public float damage       = 10f;    // 一次性低伤害
        public float slowAmount   = 0.40f;  // 减速 40%（主效果）
        public float slowDuration = 3.0f;   // 临时 buff，3 秒后自动解除

        private enum Phase { Idle, Flicker, Expanded }
        private Phase _phase = Phase.Idle;
        private float _timer;
        private SpriteRenderer _sr;
        private bool _stable;

        private static readonly Color IdleColor  = new Color(0.45f, 0.05f, 0.65f, 0.35f);
        private static readonly Color WarnColor  = new Color(0.95f, 0.00f, 1.00f, 0.95f);
        private static readonly Color BurstColor = new Color(0.75f, 0.00f, 1.00f, 0.85f);

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            transform.localScale = new Vector3(baseScale, baseScale, 1f);
            if (_sr != null) _sr.color = IdleColor;
            _timer = Random.Range(0f, idleTime);
        }

        // 战斗胜利后：停止扩张循环，回到 1×1 暗紫稳定态、不再伤害（保留可见，不销毁）。
        public void Stabilize()
        {
            _stable = true;
            transform.localScale = new Vector3(baseScale, baseScale, 1f);
            if (_sr != null) _sr.color = new Color(0.30f, 0.10f, 0.40f, 0.5f);
        }

        private void Update()
        {
            if (_stable) return;
            _timer += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Idle:
                    transform.localScale = new Vector3(baseScale, baseScale, 1f);
                    if (_sr != null) _sr.color = IdleColor;
                    if (_timer >= idleTime) Go(Phase.Flicker);
                    break;
                case Phase.Flicker:
                    if (_sr != null) _sr.color = Color.Lerp(IdleColor, WarnColor, Mathf.PingPong(Time.time * 8f, 1f));
                    if (_timer >= flickerTime) { Expand(); Go(Phase.Expanded); }
                    break;
                case Phase.Expanded:
                    if (_sr != null) _sr.color = BurstColor;
                    if (_timer >= expandedTime) Go(Phase.Idle);
                    break;
            }
        }

        private void Go(Phase p) { _phase = p; _timer = 0f; }

        // 突然扩大到 3×3，对范围内「玩家 + 敌人」各一次：低伤害 + 减速
        private void Expand()
        {
            transform.localScale = new Vector3(expandScale, expandScale, 1f);
            foreach (var col in Physics2D.OverlapBoxAll(transform.position, new Vector2(expandScale, expandScale), 0f))
            {
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
