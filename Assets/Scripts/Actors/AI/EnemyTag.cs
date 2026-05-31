using UnityEngine;
using Game.Data;

namespace Game.AI
{
    public class EnemyTag : MonoBehaviour
    {
        public EnemyType type;

        // 可被指挥官光环强化（+MaxHP, +AttackSpeed）
        public bool IsCommanderAuraTarget =>
            type == EnemyType.Soldier     ||
            type == EnemyType.Archer      ||
            type == EnemyType.ShieldGuard ||
            type == EnemyType.ExplosiveDemon;

        // 可被毒蛇祭司光环强化（+ATK, +SPD）
        public bool IsShamanAuraTarget => type == EnemyType.PoisonSpider;

        // 兼容旧命名（CommanderAI 使用）
        public bool IsAuraTarget => IsCommanderAuraTarget;
    }
}
