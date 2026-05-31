using UnityEngine;

namespace Game.UI
{
    /// 启用时让物体在垂直方向做正弦漂浮。HubController 给"残魂"台座的 Figure 启用。
    public class GhostFloat : MonoBehaviour
    {
        public float amplitude = 0.1f;
        public float period    = 2.0f;
        public float phase     = 0f;

        private Vector3 _basePos;
        private bool _captured;

        private void OnEnable()
        {
            if (!_captured) { _basePos = transform.localPosition; _captured = true; }
        }

        private void Update()
        {
            float w = Mathf.PI * 2f / Mathf.Max(0.0001f, period);
            float y = Mathf.Sin(Time.unscaledTime * w + phase) * amplitude;
            transform.localPosition = _basePos + new Vector3(0f, y, 0f);
        }
    }
}
