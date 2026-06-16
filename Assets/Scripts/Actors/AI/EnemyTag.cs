using UnityEngine;
using Game.Data;

namespace Game.AI
{
    public class EnemyTag : MonoBehaviour
    {
        public EnemyType type;

        // 飞行怪：在空中飞，免疫岩浆/熔岩伤害；地面怪踩到岩浆会扣血。
        public bool Flying;

        /// 该碰撞体是否属于一个飞行怪（岩浆类危险据此判断是否豁免伤害）。
        public static bool IsFlyingEnemy(Collider2D c)
        {
            if (c == null) return false;
            var t = c.GetComponent<EnemyTag>();
            return t != null && t.Flying;
        }

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
