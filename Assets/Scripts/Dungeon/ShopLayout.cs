using UnityEngine;

namespace Game.Dungeon
{
    // 商店可视化布局：把商人 / 货架槽 / 各功能台座的「摆放位置」做成场景里可拖拽的锚点。
    // GameBootstrap.BuildShopRoom 实例化本预制体并读取这些锚点的世界坐标来挂接玩法逻辑，
    // 因此在 Scene / Prefab 视图里拖动这些子物体即可所见即所得地改商店布局，无需改代码。
    public class ShopLayout : MonoBehaviour
    {
        [Tooltip("商人贴图所在的锚点（其本身带 SpriteRenderer 直接显示商人）")]
        public Transform shopkeeper;

        [Tooltip("武器陈列槽：按顺序对应当层 6 件武器；每个槽下挂一段货架贴图，运行时在其上摆武器")]
        public Transform[] weaponSlots;

        [Header("功能台座锚点")]
        public Transform forge;       // 锻造（升级武器）
        public Transform talentDraw;  // 抽天赋
        public Transform enchant;     // 附魔
        public Transform potion;      // 生命药水

        // 取锚点世界坐标；为空则回退到给定默认值，保证缺失时仍可生成。
        public Vector3 Pos(Transform t, Vector3 fallback) => t != null ? t.position : transform.TransformPoint(fallback);

#if UNITY_EDITOR
        // 在 Scene 视图画出各锚点，便于可视化拖拽编辑
        private void OnDrawGizmos()
        {
            DrawAnchor(shopkeeper, new Color(1f, 0.85f, 0.3f), "Shopkeeper", 0.6f);
            if (weaponSlots != null)
                for (int i = 0; i < weaponSlots.Length; i++)
                    DrawAnchor(weaponSlots[i], new Color(0.5f, 0.8f, 1f), "Weapon " + i, 0.35f);
            DrawAnchor(forge,      new Color(1f, 0.5f, 0.2f),  "Forge",   0.4f);
            DrawAnchor(talentDraw, new Color(0.7f, 0.4f, 1f),  "Talent",  0.4f);
            DrawAnchor(enchant,    new Color(0.3f, 1f, 0.8f),  "Enchant", 0.4f);
            DrawAnchor(potion,     new Color(1f, 0.3f, 0.4f),  "Potion",  0.4f);
        }

        private static void DrawAnchor(Transform t, Color c, string label, float r)
        {
            if (t == null) return;
            Gizmos.color = c;
            Gizmos.DrawWireSphere(t.position, r);
            UnityEditor.Handles.color = c;
            UnityEditor.Handles.Label(t.position + Vector3.up * (r + 0.1f), label);
        }
#endif
    }
}
