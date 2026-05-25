using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    /// 占位符约定（在 speaker / portraitKey / text 中使用）：
    ///   {hero}    → 当前英雄显示名（HeroData.displayName，如「雷昂·铁誓」）
    ///   {heroKey} → 当前英雄美术键（HeroData.heroName，如 "Warrior"，用于头像）
    [System.Serializable]
    public class StoryLineData
    {
        [Tooltip("说话者显示名。可写「旁白」、「门后的回声」等自由文本，或使用 {hero}")]
        public string speaker = "旁白";

        [Tooltip("头像键。留空 = 无头像（旁白）；写 {hero} 或 {heroKey} 自动取当前英雄头像")]
        public string portraitKey = "";

        [TextArea(2, 6)]
        [Tooltip("对话正文。支持 {hero} / {heroKey} 占位符")]
        public string text = "";
    }

    /// 一段对话分支：满足条件时把 lines 追加到本次播放的对话队列。
    /// 多个分支按列表顺序拼接 → 可用「旁白通用段 + 英雄专属段」的组合。
    [System.Serializable]
    public class StoryBranch
    {
        [Tooltip("调试用名字，不影响逻辑")]
        public string note = "branch";

        [Tooltip("最少调查次数（≥1）。第一次调查时 count=1")]
        public int minCount = 1;

        [Tooltip("最多调查次数（0 = 不限）。例：min=1,max=1 → 只在首次播放")]
        public int maxCount = 0;

        [Tooltip("仅当英雄美术键等于此值才播放（Warrior/Ranger/Mage/Paladin/Hunter；留空=任意英雄）")]
        public string requireHero = "";

        [Tooltip("排除指定英雄美术键（留空=不排除）")]
        public string forbidHero = "";

        [Tooltip("此分支贡献的对话行")]
        public List<StoryLineData> lines = new List<StoryLineData>();
    }

    /// 跨周目真相旗奖励（可绑英雄）
    [System.Serializable]
    public class TruthFlagAward
    {
        [Tooltip("真相旗 ID，写入 PersistentState.TruthFlags")]
        public string flag = "";

        [Tooltip("仅当此英雄美术键触发时才记录；留空 = 任意英雄都记录")]
        public string requireHero = "";
    }

    /// 「剧情交互物」数据模板。
    /// 通过 Inspector 编辑对话、奖励、外观；运行时由 StoryInteractable 组件读取。
    /// 创建方法：Project 面板右键 → Create → Game → Narrative → Story Interactable
    [CreateAssetMenu(menuName = "Game/Narrative/Story Interactable",
                     fileName = "StoryInteractableData")]
    public class StoryInteractableData : ScriptableObject
    {
        [Header("身份")]
        [Tooltip("跨周目调查计数所用唯一 ID（必填，不可与其他交互物重复）")]
        public string objectId = "story_object";

        [Tooltip("调查完成后顶部横幅。留空 = 不显示横幅")]
        public string bannerText = "";

        [Header("外观（白盒占位）")]
        [Tooltip("纯色方块颜色。如果 prefab 自带 SpriteRenderer 已设贴图，此项仍会覆盖颜色")]
        public Color tintColor = new Color(0.40f, 0.39f, 0.46f, 1f);

        [Tooltip("视觉缩放（世界单位）")]
        public Vector2 visualScale = new Vector2(1.5f, 2.7f);

        [Tooltip("触发盒大小（世界单位）")]
        public Vector2 colliderSize = new Vector2(1.5f, 1.25f);

        [Header("对话分支（按顺序拼接所有满足条件的分支）")]
        public List<StoryBranch> branches = new List<StoryBranch>();

        [Header("完成奖励")]
        [Tooltip("本周目剧情旗，写入 RunState.StoryFlags（无英雄过滤）")]
        public List<string> runStoryFlags = new List<string>();

        [Tooltip("跨周目真相旗，写入 PersistentState.TruthFlags（可绑英雄）")]
        public List<TruthFlagAward> truthAwards = new List<TruthFlagAward>();
    }
}
