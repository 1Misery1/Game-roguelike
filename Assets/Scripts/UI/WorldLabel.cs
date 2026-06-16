using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// World-space name-tag on a shared overlay canvas. Objects (e.g. pedestals) call
    /// Set(worldPos, accent, richContent) each frame to show a rich-text label that follows a world point;
    /// rich text drives per-line colour, and the background box auto-sizes via ContentSizeFitter.
    public class WorldLabel : MonoBehaviour
    {
        private static Canvas _shared;
        private static Canvas Shared
        {
            get
            {
                if (_shared == null) _shared = UIFactory.CreateOverlayCanvas("WorldLabelCanvas", sortingOrder: 90);
                return _shared;
            }
        }

        /// 颜色 → #RRGGBB,供组装富文本用。
        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private RectTransform _root;
        private Image  _border;
        private Text   _text;
        private Camera _cam;

        public void Set(Vector3 worldPos, Color accent, string richContent)
        {
            EnsureBuilt();
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) { _root.gameObject.SetActive(false); return; }
            var sp = _cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) { _root.gameObject.SetActive(false); return; }

            _root.gameObject.SetActive(true);
            _root.position = new Vector3(sp.x, sp.y, 0f);   // Overlay 画布:position 即屏幕像素
            _border.color  = accent;
            _text.text     = richContent;
        }

        public void Hide() { if (_root != null) _root.gameObject.SetActive(false); }

        private void EnsureBuilt()
        {
            if (_root != null) return;

            var bg = UIFactory.Image("WorldLabel", Shared.transform, new Color(0f, 0f, 0f, 0.74f));
            _root = bg.rectTransform;
            _root.pivot = new Vector2(0.5f, 0f);   // 底部中心锚在世界点上 → 名牌悬于物体上方
            var vlg = bg.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 5, 5); vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _border = UIFactory.Image("Accent", _root, Color.white);
            var brt = _border.rectTransform;
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f); brt.sizeDelta = new Vector2(0f, 2f); brt.anchoredPosition = Vector2.zero;
            _border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;   // 顶部强调线不参与竖排

            _text = UIFactory.Label("Text", _root, "", 13, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white);
            _text.supportRichText = true;

            _root.gameObject.SetActive(false);
        }

        private void OnDestroy() { if (_root != null) Destroy(_root.gameObject); }
    }
}
