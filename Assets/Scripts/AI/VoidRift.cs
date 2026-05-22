using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 混沌虚空裂隙（第3层地形杀）：
    //   靠近时持续吸引玩家（减速）并造成真实伤害；周期性脉冲爆发更强效果
    public class VoidRift : MonoBehaviour
    {
        public float slowRadius    = 5.5f;  // 减速区半径
        public float slowAmount    = 0.22f; // 移速减缓幅度（22%）
        public float coreRadius    = 1.4f;  // 核心伤害半径
        public float damagePerSec  = 8f;    // 核心区每秒真实伤害
        public float pulseInterval = 6.5f;  // 脉冲间隔（秒）
        public float pulseDuration = 2.5f;  // 脉冲持续时间
        public float pulseDamage   = 32f;   // 脉冲额外一次性伤害

        private float _cycleTimer;
        private bool  _pulsing;
        private bool  _pulseDamageApplied;

        // 减速追踪
        private bool           _playerInSlowZone;
        private CharacterStats _trackedStats;
        private string         _slowKey;

        private SpriteRenderer _coreSr;
        private SpriteRenderer _auraSr;

        private static readonly Color CoreIdle  = new Color(0.55f, 0.05f, 0.75f, 0.90f);
        private static readonly Color CorePulse = new Color(0.90f, 0.00f, 1.00f, 1.00f);
        private static readonly Color AuraIdle  = new Color(0.38f, 0.00f, 0.58f, 0.18f);
        private static readonly Color AuraPulse = new Color(0.70f, 0.00f, 1.00f, 0.42f);

        private void Awake()
        {
            _slowKey = "VoidRift_" + GetInstanceID();
            _cycleTimer = Random.Range(0f, pulseInterval);

            _coreSr = GetComponent<SpriteRenderer>();
            _coreSr.color = CoreIdle;

            // 发光光晕子物体（代表减速区视觉）
            var auraGO = new GameObject("VoidAura");
            auraGO.transform.SetParent(transform, false);
            auraGO.transform.localScale = new Vector3(slowRadius * 2f, slowRadius * 2f, 1f);
            _auraSr = auraGO.AddComponent<SpriteRenderer>();
            _auraSr.sprite       = _coreSr.sprite;
            _auraSr.color        = AuraIdle;
            _auraSr.sortingOrder = _coreSr.sortingOrder - 1;
        }

        private void Update()
        {
            UpdatePulse();
            UpdateSlowZone();
            UpdateVisuals();

            // 核心区持续真实伤害
            float coreDmg = damagePerSec * (_pulsing ? 2.5f : 1f) * Time.deltaTime;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, coreRadius))
            {
                if (col.GetComponent<EnemyTag>() != null) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount        = coreDmg,
                    Type          = DamageType.True,
                    Source        = null,
                    BypassIFrames = true,
                });
            }
        }

        private void UpdatePulse()
        {
            _cycleTimer += Time.deltaTime;
            if (!_pulsing && _cycleTimer >= pulseInterval)
            {
                _pulsing            = true;
                _pulseDamageApplied = false;
                _cycleTimer         = 0f;
            }
            if (_pulsing)
            {
                // 脉冲开始0.3秒后造成额外一次性伤害
                if (!_pulseDamageApplied && _cycleTimer >= 0.3f)
                {
                    _pulseDamageApplied = true;
                    foreach (var col in Physics2D.OverlapCircleAll(transform.position, slowRadius))
                    {
                        if (col.GetComponent<EnemyTag>() != null) continue;
                        col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                        {
                            Amount = pulseDamage,
                            Type   = DamageType.Magical,
                            Source = null,
                        });
                    }
                }
                if (_cycleTimer >= pulseDuration) _pulsing = false;
            }
        }

        // 轮询玩家是否在减速区内，动态增减 StatModifier
        private void UpdateSlowZone()
        {
            bool nowInZone = false;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, slowRadius))
            {
                if (col.GetComponent<EnemyTag>() != null) continue;
                var stats = col.GetComponent<CharacterStats>();
                if (stats == null) continue;
                _trackedStats = stats;
                nowInZone     = true;
                break;
            }

            if (nowInZone && !_playerInSlowZone)
                _trackedStats?.AddModifier(new StatModifier(StatType.MoveSpeed, ModifierOp.PercentMul, -slowAmount, _slowKey));
            else if (!nowInZone && _playerInSlowZone)
                _trackedStats?.RemoveModifiersFrom(_slowKey);

            _playerInSlowZone = nowInZone;
        }

        private void UpdateVisuals()
        {
            float t = Mathf.PingPong(Time.time * 2.5f, 1f);
            _coreSr.color = _pulsing ? Color.Lerp(CoreIdle,  CorePulse,  t) : CoreIdle;
            _auraSr.color = _pulsing ? Color.Lerp(AuraIdle,  AuraPulse,  t) : AuraIdle;
        }

        private void OnDestroy()
        {
            if (_playerInSlowZone) _trackedStats?.RemoveModifiersFrom(_slowKey);
        }
    }
}
