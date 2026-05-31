using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Hero", fileName = "Hero_")]
    public class HeroData : ScriptableObject
    {
        public string heroName;            // 稳定系统键（精灵/初始武器/解锁存档），勿改
        public string displayName;         // 剧情角色显示名（如「雷昂·铁誓」）
        [TextArea] public string description;
        public Sprite portrait;
        public GameObject prefab;
        public Color tintColor = Color.white;

        [Header("Base Stats")]
        public float baseMaxHP = 100f;
        public float baseAttack = 10f;
        public float baseDefense = 0f;
        public float baseMoveSpeed = 5f;
        public float baseAttackSpeed = 1f;

        [Header("Skills")]
        public ActiveSkillData activeSkill;
        public PassiveTalentData passiveTalent;
        public HeroSkillType heroSkillType = HeroSkillType.None;
        public float heroSkillCooldown = 8f;
        public string heroSkillName;
        public HeroPassiveType heroPassiveType = HeroPassiveType.None;
        public string heroPassiveName;

        [Header("Meta Progression")]
        public bool unlockedByDefault = false;
        public int unlockCost = 100;            // 旧货币解锁(已弃用，保留存档兼容)

        [Header("Story Unlock 剧情解锁")]
        [Tooltip("解锁所需的真相旗标 id；留空 = 开局即可用(主角)。在地底揭开该真相后，这缕残魂便会觉醒。")]
        public string requiredTruthFlag;
        [TextArea]
        [Tooltip("尚未觉醒时，靠近台座显示的剧情台词(暗示去哪揭开他/她的真相)。")]
        public string lockedStoryLine;
    }
}
