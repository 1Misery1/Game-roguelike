using Game.Data;
using UnityEngine;

namespace Game.Combat
{
    [RequireComponent(typeof(CharacterStats))]
    public class Health : MonoBehaviour, IDamageable
    {
        private CharacterStats _stats;
        private float _current;

        public float Current => _current;
        public float Max => _stats != null ? _stats.Get(StatType.MaxHP) : 0f;
        public float Ratio => Max > 0f ? _current / Max : 0f;

        public GameObject LastDamageSource { get; private set; }

        public System.Action<DamageInfo> OnDamaged;
        public System.Action OnDied;

        // 可选拦截器：在计算防御前修改伤害（用于盾牌减伤等机制）
        public System.Func<DamageInfo, DamageInfo> OnBeforeTakeDamage;

        // Seconds of invincibility after each non-bypass hit (0 = disabled, set to 0.4 for player)
        public float IFrameDuration = 0f;
        private float _iFrameUntil;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _stats.OnStatsChanged += ClampToMax;
            _current = Max;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (_current <= 0f) return;

            if (!info.BypassIFrames && IFrameDuration > 0f)
            {
                if (Time.time < _iFrameUntil) return;
                _iFrameUntil = Time.time + IFrameDuration;
            }

            LastDamageSource = info.Source;
            if (OnBeforeTakeDamage != null) info = OnBeforeTakeDamage(info);

            float defense = _stats.Get(StatType.Defense);
            float dmg = info.Type == DamageType.True
                ? info.Amount
                : Mathf.Max(1f, info.Amount - defense);

            _current = Mathf.Max(0f, _current - dmg);
            // Pass actual dealt damage so subscribers (e.g. floating numbers) show correct values
            OnDamaged?.Invoke(new DamageInfo { Amount = dmg, Type = info.Type, IsCrit = info.IsCrit, Source = info.Source });

            if (_current <= 0f) OnDied?.Invoke();
        }

        public void Heal(float amount)
        {
            if (_current <= 0f) return;
            _current = Mathf.Min(Max, _current + amount);
        }

        // 当 MaxHP 因装备/升级等变化时，按差值同步调整 Current
        // 正差：Current 增加但不超过新 Max
        // 负差：Current 减少但最低保留 1（避免装备变动直接致死）
        public void AdjustCurrentByMaxDelta(float delta)
        {
            if (_current <= 0f) return;            // 已死亡不复活
            _current = Mathf.Max(1f, _current + delta);
            _current = Mathf.Min(_current, Max);
        }

        // 换装 / 升级 武器导致 MaxHP 变化时使用：以「变更前的 Current」为基准 + 上限差值。
        // 必须传入变更前的 curBefore，因为换装过程中 RemoveModifier→ClampToMax 会先把 _current
        // 夹到「去掉加成后的较低上限」，若直接用被夹后的 _current 计算，血量会错误地塌到基础上限附近。
        public void SetCurrentForMaxChange(float curBefore, float maxBefore, float maxAfter)
        {
            if (curBefore <= 0f) return;           // 变更前已死亡不复活
            float delta = maxAfter - maxBefore;
            _current = Mathf.Clamp(curBefore + delta, 1f, Max);
        }

        private void ClampToMax()
        {
            _current = Mathf.Min(_current, Max);
        }
    }
}
