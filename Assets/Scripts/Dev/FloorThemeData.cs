using UnityEngine;

namespace Game.Dev
{
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
    }
}
