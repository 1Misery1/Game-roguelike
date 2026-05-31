using UnityEngine;

namespace Game.UI
{
    /// 按物体 Y 坐标动态设置 sortingOrder —— Y 越低(越靠近镜头)绘制越靠前,产生 2D 纵深遮挡。
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSort : MonoBehaviour
    {
        public int baseOrder = 0;
        public float unitsToOrder = 10f;

        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        private void LateUpdate()
        {
            if (_sr == null) return;
            _sr.sortingOrder = baseOrder - Mathf.RoundToInt(transform.position.y * unitsToOrder);
        }
    }
}
