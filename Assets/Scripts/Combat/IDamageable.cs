using Game.Data;
using UnityEngine;

namespace Game.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public bool IsCrit;
        public GameObject Source;
        // When true the hit skips player invincibility frames (traps, DoT, environmental hazards)
        public bool BypassIFrames;
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
    }
}
