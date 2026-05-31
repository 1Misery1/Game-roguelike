using System.Collections.Generic;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 炼狱火柱（第1层地形杀）：
    //   待机时暗红，预警期橙色闪烁（提示玩家离开），激活期持续魔法伤害 9 DPS
    //   碰撞检测改用 CircleCollider2D trigger，消除每帧 OverlapCircleAll 开销
    public class FlamePillar : MonoBehaviour
    {
        public float idleTime    = 7.5f;
        public float warningTime = 1.5f;
        public float activeTime  = 2.5f;
        public float damage      = 9f;    // 激活期每秒伤害
        // 碰撞体本地半径 0.5 → 世界半径 = 0.5×localScale，正好内切于缩放后的方块视觉，
        // 避免被 transform 放大后伤害超出可见熔岩范围
        public float radius      = 0.5f;

        private enum Phase { Idle, Warning, Active }
        private Phase _phase = Phase.Idle;
        private float _timer;
        private SpriteRenderer _sr;
        private readonly HashSet<IDamageable> _inside = new HashSet<IDamageable>();

        private static readonly Color IdleColor   = new Color(0.45f, 0.08f, 0.04f, 0.80f);
        private static readonly Color WarnColor   = new Color(1.00f, 0.55f, 0.05f, 0.95f);
        private static readonly Color ActiveColor = new Color(1.00f, 0.28f, 0.00f, 1.00f);

        private void Awake()
        {
            _sr       = GetComponent<SpriteRenderer>();
            _sr.color = IdleColor;
            _timer    = Random.Range(0f, idleTime);

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
                    _sr.color = IdleColor;
                    if (_timer >= idleTime) Transition(Phase.Warning);
                    break;

                case Phase.Warning:
                    _sr.color = Color.Lerp(IdleColor, WarnColor, Mathf.PingPong(Time.time * 5f, 1f));
                    if (_timer >= warningTime) Transition(Phase.Active);
                    break;

                case Phase.Active:
                    _sr.color = Color.Lerp(WarnColor, ActiveColor, Mathf.PingPong(Time.time * 8f, 1f));
                    DamageInside();
                    if (_timer >= activeTime) Transition(Phase.Idle);
                    break;
            }
        }

        private void Transition(Phase next) { _phase = next; _timer = 0f; }

        private void DamageInside()
        {
            if (_inside.Count == 0) return;
            // 先快照，避免 TakeDamage → OnDied → Destroy → OnTriggerExit2D 在迭代期间修改集合
            var snapshot = new IDamageable[_inside.Count];
            _inside.CopyTo(snapshot);
            foreach (var d in snapshot)
            {
                if (d == null) { _inside.Remove(d); continue; }
                d.TakeDamage(new DamageInfo
                {
                    Amount        = damage * Time.deltaTime,
                    Type          = DamageType.Magical,
                    Source        = null,
                    BypassIFrames = true,
                });
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<EnemyTag>() != null) return;
            var d = other.GetComponent<IDamageable>();
            if (d != null) _inside.Add(d);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var d = other.GetComponent<IDamageable>();
            if (d != null) _inside.Remove(d);
        }
    }
}
