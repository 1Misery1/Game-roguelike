using UnityEngine;

namespace Game.Dev
{
    // 法杖命中后的 AOE 爆炸视觉。worldRadius 传入实际伤害半径(aoeRadius)，
    // 爆炸预制体 PPU=620 使峰值外缘半径在 scale=1 时为 1 世界单位，
    // 故 localScale=worldRadius 时爆炸外缘正好覆盖伤害判定圆。
    // 颜色/动画可在 Resources/FX/MagicBlast/MagicBlast.prefab / .anim 里可视化编辑。
    public class MagicBlastFX : MonoBehaviour
    {
        const string PrefabPath = "FX/MagicBlast/MagicBlast";
        const float  ClipLength = 0.30f;   // 与 MagicBlast.anim 时长一致

        static GameObject _prefab;
        static bool       _tried;

        float _life;

        public static void Spawn(Vector3 pos, float worldRadius, Transform parent = null)
        {
            if (!_tried) { _prefab = Resources.Load<GameObject>(PrefabPath); _tried = true; }
            if (_prefab == null) return;

            var go = Instantiate(_prefab, pos, Quaternion.identity);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.localScale = Vector3.one * Mathf.Max(0.05f, worldRadius);

            var fx = go.AddComponent<MagicBlastFX>();
            fx._life = ClipLength;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
