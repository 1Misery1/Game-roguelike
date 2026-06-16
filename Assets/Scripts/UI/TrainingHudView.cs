using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// Training-arena HUD: a static top-left controls panel (built once) + exit prompt + centre banner.
    public class TrainingHudView : MonoBehaviour
    {
        private Canvas _canvas;
        private Transform _root;
        private GameObject _exitGroup, _bannerGroup;
        private Text _banner;
        private bool _controlsBuilt;
        private WeaponPanelView _weapon;

        private void Awake()
        {
            _canvas = UIFactory.CreateOverlayCanvas("TrainingHudCanvas", sortingOrder: 100);
            _canvas.transform.SetParent(transform, false);
            _root = _canvas.transform;
            BuildExit();
            BuildBanner();
            _weapon = new WeaponPanelView(); _weapon.Build(_root);   // same weapon panel as the combat HUD
        }

        public void SetWeapon(bool visible, WeaponPanelView.WeaponSlot slot0, WeaponPanelView.WeaponSlot slot1,
                              bool skillVisible, bool skillReady, float skillFill, string skillLabel)
            => _weapon?.SetWeapon(visible, slot0, slot1, skillVisible, skillReady, skillFill, skillLabel);

        public void SetControls(string[,] controls)
        {
            if (_controlsBuilt) return;
            _controlsBuilt = true;
            BuildControls(controls);
        }

        public void SetExitPrompt(bool visible) { if (_exitGroup != null) _exitGroup.SetActive(visible); }
        public void SetBanner(bool visible, string text) { _bannerGroup.SetActive(visible); if (visible) _banner.text = text; }

        // ── 构建 ──────────────────────────────────────────────────────────────
        private void BuildControls(string[,] controls)
        {
            var panel = UIFactory.Image("Controls", _root, new Color(0f, 0f, 0f, 0.60f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(0f, 1f);
            prt.sizeDelta = new Vector2(460f, 0f); prt.anchoredPosition = new Vector2(10f, -10f);
            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 8, 10); vlg.spacing = 2f; vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;   // 宽度固定 460
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var accent = UIFactory.Image("Accent", panel.transform, new Color(0.95f, 0.78f, 0.32f, 0.9f));
            var art = accent.rectTransform;
            art.anchorMin = new Vector2(0f, 1f); art.anchorMax = new Vector2(1f, 1f); art.pivot = new Vector2(0.5f, 1f);
            art.sizeDelta = new Vector2(0f, 2f); art.anchoredPosition = Vector2.zero;
            accent.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            AddLine(panel.transform, "Training Arena", 18, FontStyle.Bold,   new Color(0.97f, 0.85f, 0.45f));
            AddLine(panel.transform, "── Controls ──", 13, FontStyle.Normal, new Color(0.55f, 0.80f, 1f));

            int rows = controls.GetLength(0);
            for (int i = 0; i < rows; i++) AddControlRow(panel.transform, controls[i, 0], controls[i, 1]);
        }

        private void AddLine(Transform parent, string text, int size, FontStyle style, Color color)
        {
            var t = UIFactory.Label("Line", parent, text, size, TextAnchor.MiddleLeft, style, color);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 8f;
        }

        private void AddControlRow(Transform parent, string key, string desc)
        {
            var row = UIFactory.Rect("Row", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 21f;
            var k = UIFactory.Label("Key", row, key, 13, TextAnchor.MiddleLeft, FontStyle.Bold, new Color(0.6f, 0.92f, 1f));
            var krt = k.rectTransform;
            krt.anchorMin = new Vector2(0f, 0f); krt.anchorMax = new Vector2(0.42f, 1f); krt.offsetMin = Vector2.zero; krt.offsetMax = Vector2.zero;
            var d = UIFactory.Label("Desc", row, desc, 13, TextAnchor.MiddleLeft, FontStyle.Normal, Color.white);
            var drt = d.rectTransform;
            drt.anchorMin = new Vector2(0.42f, 0f); drt.anchorMax = new Vector2(1f, 1f); drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        }

        private void BuildExit()
        {
            var bg = UIFactory.Image("Exit", _root, new Color(0f, 0f, 0f, 0.6f));
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(420f, 40f); rt.anchoredPosition = new Vector2(0f, 30f);
            _exitGroup = bg.gameObject;
            var t = UIFactory.Label("Txt", bg.transform, "[E]  Return to Camp", 16, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.7f, 0.95f, 1f));
            UIFactory.Stretch(t.rectTransform);
            _exitGroup.SetActive(false);
        }

        private void BuildBanner()
        {
            var bg = UIFactory.Image("Banner", _root, new Color(0f, 0f, 0f, 0.62f));
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760f, 64f); rt.anchoredPosition = new Vector2(0f, -90f);
            _bannerGroup = bg.gameObject;
            _banner = UIFactory.Label("Txt", bg.transform, "", 16, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.96f, 0.9f, 0.78f), wrap: true);
            UIFactory.Stretch(_banner.rectTransform, left: 18f, right: 18f, top: 8f, bottom: 8f);
            _bannerGroup.SetActive(false);
        }
    }
}
