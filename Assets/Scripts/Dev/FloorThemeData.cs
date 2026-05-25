using System.Collections.Generic;
using UnityEngine;

namespace Game.Dev
{
    /// 房间池条目：类型 + 权重
    [System.Serializable]
    public class FloorRoomEntry
    {
        [Tooltip("房间类型：Monster / Coin / Talent / Shop / HellTrial / FrostGrave / ChaosRift / Boss")]
        public string type   = "Monster";
        [Tooltip("被抽中的权重（越大越常见）")]
        public float  weight = 1f;
    }

    /// 楼层程序化背景纹理类型
    public enum FloorProceduralKind
    {
        None,       // 不生成背景纹理（地图自带瓦片足够）
        Inferno,    // 熔岩层（红黑 + 岩浆裂纹 + 热点）
        Frost,      // 冰霜层（深蓝 + 六角晶格 + 雪花高光）
        Chaos,      // 虚空层（极紫 + 极坐标漩涡 + 中央深渊）
        Custom      // 使用 customBackground 精灵覆盖
    }

    /// 楼层主题 ScriptableObject：把楼层显示名、旁白横幅、相机背景色、
    /// 程序化纹理类型 / 自定义背景精灵全部搬入 Inspector。
    /// 创建：Project 面板右键 → Create → Game → Floor → Floor Theme
    /// 路径：放在 Assets/Resources/Floors/，运行时 Resources.LoadAll 扫描
    [CreateAssetMenu(menuName = "Game/Floor/Floor Theme", fileName = "FloorThemeData")]
    public class FloorThemeData : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("楼层编号（1, 2, 3...）。GameBootstrap 按此匹配")]
        public int floorNumber = 1;

        [Tooltip("HUD 显示的楼层名（如「Inferno」「炼狱」）")]
        public string displayName = "Inferno";

        [TextArea(2, 4)]
        [Tooltip("进入该层时显示的剧情横幅")]
        public string narrativeBanner = "[Inferno] Lava surges, demons block the path";

        [Header("视觉")]
        [Tooltip("主相机背景色")]
        public Color cameraBackground = new Color(0.15f, 0.05f, 0.03f, 1f);

        [Tooltip("背景纹理生成方式")]
        public FloorProceduralKind proceduralKind = FloorProceduralKind.Inferno;

        [Tooltip("当 proceduralKind = Custom 时使用的背景精灵；其他模式忽略")]
        public Sprite customBackground;

        // ── 战斗参数（波次表）─────────────────────────────────────────────
        // 小怪类型顺序（共 8 个，对应 SpawnRandomNormalEnemy 的 switch）：
        // 0 Skeleton  1 Soldier  2 Archer  3 Bat
        // 4 ShieldGuard  5 PoisonSpider  6 ShadowAssassin  7 ExplosiveDemon
        [Header("小怪权重（8 项，分前期/中后期）")]
        [Tooltip("前期房间（room index ≤ 2）的小怪权重 8 项")]
        public float[] earlyEnemyWeights = new float[] { 2f, 3f, 2f, 2f, 1f, 1f, 0f, 1f };

        [Tooltip("中后期房间（room index ≥ 3）的小怪权重 8 项")]
        public float[] lateEnemyWeights  = new float[] { 1f, 2f, 1f, 1f, 3f, 1f, 1f, 4f };

        [Header("精英刷新")]
        [Tooltip("精英敌人在中后期房间的出现概率（前 2 间始终为 0）")]
        [Range(0f, 1f)] public float eliteChance = 0.15f;

        [Header("房间池")]
        [Tooltip("该层可能出现的非 Boss 房间类型与权重；为空时使用代码默认池")]
        public List<FloorRoomEntry> roomPool = new List<FloorRoomEntry>();
    }
}
