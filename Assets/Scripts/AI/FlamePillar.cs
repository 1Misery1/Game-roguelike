using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 炼狱火柱（第1层地形杀）：
    //   待机时暗红，预警期橙色闪烁（提示玩家离开），激活期持续魔法伤害
    public class FlamePillar : MonoBehaviour
    {
        public float idleTime    = 7.5f;  // 待机时长（秒）
        public float warningTime = 1.5f;  // 预警闪烁时长
        public float activeTime  = 2.5f;  // 燃烧持续时长
        public float damage      = 9f;    // 激活期每秒伤害
        public float radius      = 1.6f;  // 伤害半径（世界空间）

        private enum Phase { Idle, Warning, Active }
        private Phase          _phase = Phase.Idle;
        private float          _timer;
        private SpriteRenderer _sr;

        private static readonly Color IdleColor   = new Color(0.45f, 0.08f, 0.04f, 0.80f);
        private static readonly Color WarnColor   = new Color(1.00f, 0.55f, 0.05f, 0.95f);
        private static readonly Color ActiveColor = new Color(1.00f, 0.28f, 0.00f, 1.00f);

        private void Awake()
        {
            _sr       = GetComponent<SpriteRenderer>();
            _sr.color = IdleColor;
            _timer    = Random.Range(0f, idleTime); // 错开各柱子激活时间
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
                    DamageNearby();
                    if (_timer >= activeTime) Transition(Phase.Idle);
                    break;
            }
        }

        private void Transition(Phase next) { _phase = next; _timer = 0f; }

        private void DamageNearby()
        {
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, radius))
            {
                if (col.GetComponent<EnemyTag>() != null) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = damage * Time.deltaTime,
                    Type   = DamageType.Magical,
                    Source = null
                });
            }
        }
    }
}
