using UnityEngine;

namespace Game.Data
{
    /// 英雄名册资源。在工程里建成 HeroDatabase.asset，拖入 5 个 HeroData.asset；
    /// 由 HubController / MenuController 通过 Inspector 引用（或 Resources 兜底）。
    [CreateAssetMenu(menuName = "Game/Hero Database", fileName = "HeroDatabase")]
    public class HeroDatabase : ScriptableObject
    {
        public HeroData[] heroes;
    }
}
