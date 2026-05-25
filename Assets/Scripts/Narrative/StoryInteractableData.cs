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

        [Tooltip("最少调查次数（≥1）。计数为「本英雄」专属：换英雄重玩时仍从 1 起，保证每个新英雄都能从首次见到自己专属内容")]
        public int minCount = 1;

        [Tooltip("最多调查次数（0 = 不限）。例：min=1,max=1 → 只在该英雄首次播放")]
        public int maxCount = 0;

        [Tooltip("仅当英雄美术键等于此值才播放（Warrior/Ranger/Mage/Paladin/Hunter；留空=任意英雄）")]
        public string requireHero = "";

        [Tooltip("排除指定英雄美术键（留空=不排除）")]
        public string forbidHero = "";

        [Tooltip("最少累计通关次数（PersistentState.TotalVictories）。0=不限。例：2 → 第二次通关后才解锁该分支")]
        public int minRunCount = 0;

        [Tooltip("最多累计通关次数。0=不限")]
        public int maxRunCount = 0;

        [Tooltip("此分支贡献的对话行")]
        public List<StoryLineData> lines = new List<StoryLineData>();
    }

    /// 玩家在主对话播放完后看到的选项（用于"直视 / 打碎"等抉择类交互）
    [System.Serializable]
    public class StoryChoice
    {
        [Tooltip("按钮显示的选项标题（如「直视湖面」「打碎湖面」）")]
        public string label = "选项";

        [TextArea(1, 3)]
        [Tooltip("按钮下方描述行（简短说明此选项后果，可空）")]
        public string description = "";

        [Tooltip("选中后追加播放的对话行（在效果应用之前）")]
        public List<StoryLineData> followLines = new List<StoryLineData>();

        [Tooltip("此选项写入的本周目剧情旗（叠加在主数据 runStoryFlags 之上）")]
        public List<string> runStoryFlags = new List<string>();

        [Tooltip("此选项写入的真相旗（叠加；同主结构，支持 fallback）")]
        public List<TruthFlagAward> truthAwards = new List<TruthFlagAward>();

        [Tooltip("此选项授予的剧情道具")]
        public List<string> grantStoryItems = new List<string>();

        [Tooltip("此选项的虚空污染增量（叠加）")]
        public int addCorruption = 0;

        [Tooltip("此选项的横幅文字。留空 = 不覆盖（仍用主对话的 bannerText）")]
        public string bannerOverride = "";
    }

    /// 跨周目真相旗奖励（可绑英雄）。
    /// 设计哲学：每个真相有一个「专属英雄」能在首次调查就立刻领悟；
    /// 其他英雄通过反复探访（fallbackCount 次）也能逐步拼凑真相，
    /// 从而保证单角色玩家也能解锁全部结局。
    [System.Serializable]
    public class TruthFlagAward
    {
        [Tooltip("真相旗 ID，写入 PersistentState.TruthFlags")]
        public string flag = "";

        [Tooltip("专属英雄美术键。首次调查即解锁；留空 = 任意英雄首次即解锁")]
        public string requireHero = "";

        [Tooltip("后备解锁次数（按「全局」累计调查 N 次后无视 requireHero 解锁——任意英雄顺序都计入）。0 = 不允许非专属英雄解锁。建议 2-3")]
        public int fallbackCount = 0;
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

        [Header("生成位置（运行时自动 spawn）")]
        [Tooltip("在哪一层生成（1/2/3）。0 = 不自动生成，由代码手动 SpawnStoryFromData")]
        public int spawnFloor = 0;

        [Tooltip("在该层第几个房间生成（0 起）")]
        public int spawnRoomIndex = 0;

        [Tooltip("相对玩家出生点的偏移位置（世界单位）")]
        public Vector3 spawnOffset = Vector3.zero;

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

        [Tooltip("本周目剧情道具 ID（如「烧焦的工匠名册」「寒镜碎片」），写入 RunState.StoryItems")]
        public List<string> grantStoryItems = new List<string>();

        [Tooltip("本周目虚空污染增量。0=不变；正=污染；负=净化（如霜眠祭坛放置工匠名册）")]
        public int addCorruption = 0;

        [Header("玩家抉择（可选）")]
        [Tooltip("主对话播放完后弹出的选项菜单。留空 = 不弹窗。每个选项可独立设置追加台词与奖励")]
        public List<StoryChoice> choices = new List<StoryChoice>();

        [Tooltip("弹窗顶部标题（可空，默认为 bannerText）")]
        public string choiceTitle = "";
    }
}
