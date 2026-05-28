using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Dev
{
    // 近战劈砍特效：按武器类别加载对应预制体（Sprite 逐帧动画），
    // 按攻击方向旋转、按攻击范围缩放，播放完自动销毁。
    // 颜色与动画都在各自预制体/AnimationClip 里可视化编辑：
    //   Resources/FX/<类别>/<类别>Slash.prefab  ← 选中改 SpriteRenderer 颜色
    //   Resources/FX/<类别>/<类别>Slash.anim    ← Animation 窗口拖帧
    public class MeleeSlashFX : MonoBehaviour
    {
        const float ClipLength = 0.30f;   // 与各 .anim 时长一致

        static readonly Dictionary<WeaponCategory, GameObject> _cache =
            new Dictionary<WeaponCategory, GameObject>();

        float _life;

        public static void Spawn(WeaponCategory category, Vector3 pos, float angleDeg,
            float range, Transform parent = null)
        {
            if (!_cache.TryGetValue(category, out var prefab))
            {
                prefab = Resources.Load<GameObject>(PrefabPath(category));
                _cache[category] = prefab;
            }
            if (prefab == null) return;   // 无对应预制体时静默跳过，不影响伤害

            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, 0f, angleDeg));
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.localScale = Vector3.one * Mathf.Max(0.1f, range * ScaleFactor(category));

            var fx = go.AddComponent<MeleeSlashFX>();
            fx._life = ClipLength;
        }

        static string PrefabPath(WeaponCategory c)
        {
            switch (c)
            {
                case WeaponCategory.Dagger:     return "FX/Dagger/DaggerSlash";
                case WeaponCategory.Greatsword: return "FX/Greatsword/GreatswordSlash";
                default:                        return "FX/Longsword/LongswordSlash";
            }
        }

        // 相对攻击范围的缩放系数（补偿三套素材本身的大小差异）
        static float ScaleFactor(WeaponCategory c)
        {
            switch (c)
            {
                case WeaponCategory.Dagger:     return 1.2f;
                case WeaponCategory.Greatsword: return 0.85f;
                default:                        return 1.0f;  // Longsword
            }
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
