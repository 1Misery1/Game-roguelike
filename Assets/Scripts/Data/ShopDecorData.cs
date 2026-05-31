using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// 商店装饰 ScriptableObject：把老板 / 货架 / 武器陈列从代码搬进 Inspector。
    /// 创建方法：Project 面板右键 → Create → Game → Shop → Shop Decor
    /// 路径要求：放在 Assets/Resources/Shop/Default.asset（运行时 Resources.Load 加载）

    /// 一个像素方块（颜色 + 位置 + 大小）
    [System.Serializable]
    public class ShopPart
    {
        [Tooltip("调试用名字（如「兜帽」「眼睛-左」），不影响逻辑")]
        public string note = "part";

        [Tooltip("相对所在 group anchor 的本地位置（世界单位）")]
        public Vector2 localPos = Vector2.zero;

        [Tooltip("世界单位的方块尺寸")]
        public Vector2 size = new Vector2(1f, 1f);

        [Tooltip("方块颜色")]
        public Color color = Color.white;

        [Tooltip("SpriteRenderer.sortingOrder（数字越大越靠前）")]
        public int sortingOrder = 6;
    }

    /// 一组方块的集合（如「商店老板」「货架」），共享一个世界锚点
    [System.Serializable]
    public class ShopDecorGroup
    {
        [Tooltip("分组名（Hierarchy 中的 GameObject 名）")]
        public string name = "group";

        [Tooltip("此组在房间内的世界坐标锚点")]
        public Vector3 anchor = Vector3.zero;

        [Tooltip("是否启用该组（关闭则不生成）")]
        public bool enabled = true;

        public List<ShopPart> parts = new List<ShopPart>();
    }

    [CreateAssetMenu(menuName = "Game/Shop/Shop Decor", fileName = "ShopDecorData")]
    public class ShopDecorData : ScriptableObject
    {
        [Header("商店老板")]
        public ShopDecorGroup shopkeeper = new ShopDecorGroup { name = "Shopkeeper" };

        [Header("货架")]
        public ShopDecorGroup shelf = new ShopDecorGroup { name = "ShopShelf" };

        [Header("武器陈列")]
        [Tooltip("最左侧武器的世界坐标")]
        public Vector3 weaponRowStart = new Vector3(-5.5f, 1.5f, 0f);

        [Tooltip("武器之间的水平间距")]
        public float weaponSpacing = 2.2f;

        [Tooltip("每件武器精灵的缩放")]
        public Vector3 weaponScale = new Vector3(1.1f, 1.1f, 1f);

        [Tooltip("武器精灵的 sortingOrder")]
        public int weaponSortingOrder = 8;

        [Tooltip("武器下方稀有度发光底座的 sortingOrder（应小于武器本身）")]
        public int weaponGlowSortingOrder = 7;

        [Tooltip("发光底座相对武器的偏移与尺寸")]
        public Vector2 weaponGlowLocalPos = new Vector2(0f, -0.08f);
        public Vector2 weaponGlowScale    = new Vector2(0.95f, 0.30f);

        [Tooltip("发光底座的 alpha（颜色取该武器稀有度色）")]
        [Range(0f, 1f)] public float weaponGlowAlpha = 0.55f;

        [Tooltip("武器触发盒半径")]
        public float weaponColliderRadius = 0.85f;

        [Header("额外陈设（可选，例如灯笼/酒桶/挂画）")]
        public List<ShopDecorGroup> extraDecor = new List<ShopDecorGroup>();
    }
}
