using UnityEngine;
namespace Game.Data
{
    /// Boss 基础战斗参数 ScriptableObject
    /// 字段顺序映射到 EnemyFactory.MakeBase / ScaleEnemyStats
    /// 创建：Project 右键 → Create → Game → Boss → Boss Stats
    /// 路径：Assets/Resources/Bosses/，运行时由 BossStatsRegistry 扫描
    [CreateAssetMenu(menuName = "Game/Boss/Boss Stats", fileName = "BossStatsData")]
    public class BossStatsData : ScriptableObject
    {
        [Header("身份")]
        [Tooltip("Boss 唯一 ID：hell_giant / frost_lich / chaos_lord / kingdom_guilt 等")]
        public string bossId = "hell_giant";

        [Tooltip("HUD 显示用名（可与 EnemyFactory.MakeBase 的 name 参数不同）")]
        public string displayName = "Hell Giant";

        [Header("基础属性")]
        public float maxHp     = 320f;
        public float attack    = 28f;
        public float defense   = 8f;
        public float moveSpeed = 2.5f;

        [Header("视觉")]
        [Tooltip("整体缩放（白盒尺寸）")]
        public float visualScale = 1.2f;

        [Tooltip("精灵着色")]
        public Color tintColor = new Color(0.7f, 0.12f, 0.08f, 1f);
    }
}
