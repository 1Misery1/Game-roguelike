using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// Camp HUD. Screen-space: top bar / interact prompt / hero info / banner.
    /// World-space: station name-tags that appear when the player is near (pooled, via WorldToScreen).
    public class HubHudView : MonoBehaviour
    {
        public struct StationLabel { public Vector3 worldPos; public string name; public string status; public bool near; }

        private Canvas _canvas;
        private Camera _cam;

        private Text _title, _info, _prompt, _heroInfo, _banner;
        private GameObject _promptGroup, _heroInfoGroup, _bannerGroup;

        private Transform _labelRoot;
        private class Lbl { public RectTransform rt; public GameObject go; public Text name; public Text status; }
        private readonly List<Lbl> _labels = new List<Lbl>();

        private void Awake() => Build();
        public void SetCamera(Camera cam) => _cam = cam;
        public void SetVisible(bool on) { if (_canvas != null) _canvas.gameObject.SetActive(on); }

        // ── 屏幕空间推送 ──────────────────────────────────────────────────────
        public void SetTopBar(string title, string info) { _title.text = title; _info.text = info; }
        public void SetPrompt(bool visible, string text)  { _promptGroup.SetActive(visible); if (visible) _prompt.text = text; }
        public void SetHeroInfo(bool visible, string text){ _heroInfoGroup.SetActive(visible); if (visible) _heroInfo.text = text; }
        public void SetBanner(bool visible, string text)  { _bannerGroup.SetActive(visible); if (visible) _banner.text = text; }

        public void SetStationLabels(List<StationLabel> stations)
        {
            for (int i = 0; i < _labels.Count; i++)
            {
                if (_cam == null || stations == null || i >= stations.Count) { _labels[i].go.SetActive(false); continue; }
                var s  = stations[i];
                var sp = _cam.WorldToScreenPoint(s.worldPos);
                if (sp.z < 0f) { _labels[i].go.SetActive(false); continue; }

                var l = _labels[i];
                l.go.SetActive(true);
                l.rt.position = new Vector3(sp.x, sp.y, 0f);   // Overlay 画布:position 即屏幕像素
                l.name.text   = s.name;
                l.name.color  = s.near ? new Color(1f, 0.95f, 0.6f) : new Color(0.88f, 0.88f, 0.92f);
                bool hasStatus = !string.IsNullOrEmpty(s.status);
                l.status.gameObject.SetActive(hasStatus);
                if (hasStatus) l.status.text = s.status;
            }
        }

        // ── 构建 ──────────────────────────────────────────────────────────────
        private void Build()
        {
            _canvas = UIFactory.CreateOverlayCanvas("HubHudCanvas", sortingOrder: 100);
            _canvas.transform.SetParent(transform, false);
            var root = _canvas.transform;

            _title = UIFactory.Label("Title", root, "", 14, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.95f, 0.85f, 0.55f));
            var trt = _title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(0.6f, 1f); trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 30f); trt.anchoredPosition = new Vector2(14f, -4f);

            _info = UIFactory.Label("Info", root, "", 13, TextAnchor.MiddleRight, FontStyle.Normal, Color.white);
            var irt = _info.rectTransform;
            irt.anchorMin = new Vector2(0.4f, 1f); irt.anchorMax = new Vector2(1f, 1f); irt.pivot = new Vector2(1f, 1f);
            irt.sizeDelta = new Vector2(0f, 30f); irt.anchoredPosition = new Vector2(-14f, -4f);

            var pbg = UIFactory.Image("Prompt", root, new Color(0f, 0f, 0f, 0.5f));
            var prt = pbg.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(460f, 34f); prt.anchoredPosition = new Vector2(0f, 40f);
            _promptGroup = pbg.gameObject;
            _prompt = UIFactory.Label("Txt", pbg.transform, "", 13, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(1f, 0.95f, 0.7f));
            UIFactory.Stretch(_prompt.rectTransform);
            _promptGroup.SetActive(false);

            _heroInfo = UIFactory.Label("HeroInfo", root, "", 13, TextAnchor.LowerLeft, FontStyle.Normal, Color.white, wrap: true);
            var hrt = _heroInfo.rectTransform;
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(1f, 0f); hrt.pivot = new Vector2(0f, 0f);
            hrt.sizeDelta = new Vector2(0f, 44f); hrt.offsetMin = new Vector2(12f, 12f); hrt.offsetMax = new Vector2(-12f, 56f);
            _heroInfoGroup = _heroInfo.gameObject;
            _heroInfoGroup.SetActive(false);

            var bbg = UIFactory.Image("Banner", root, new Color(0f, 0f, 0f, 0.55f));
            var brt = bbg.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f); brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(720f, 60f); brt.anchoredPosition = new Vector2(0f, -120f);
            _bannerGroup = bbg.gameObject;
            _banner = UIFactory.Label("Txt", bbg.transform, "", 14, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.95f, 0.9f, 0.8f), wrap: true);
            UIFactory.Stretch(_banner.rectTransform, left: 18f, right: 18f, top: 8f, bottom: 8f);
            _bannerGroup.SetActive(false);

            _labelRoot = UIFactory.Rect("Labels", root);
            UIFactory.Stretch((RectTransform)_labelRoot);
            for (int i = 0; i < 14; i++) _labels.Add(MakeLabel());
        }

        // 世界空间名牌:背景自适应大小(竖排布局+ContentSizeFitter),锚点在底部中心 → 悬在世界点上方。
        private Lbl MakeLabel()
        {
            var bg = UIFactory.Image("Lbl", _labelRoot, new Color(0f, 0f, 0f, 0.6f));
            bg.rectTransform.pivot = new Vector2(0.5f, 0f);
            var vlg = bg.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 3, 3); vlg.spacing = 0f; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var name = UIFactory.Label("Name", bg.transform, "", 13, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;
            var status = UIFactory.Label("Status", bg.transform, "", 11, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.72f, 0.72f, 0.8f));
            status.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;

            var l = new Lbl { rt = bg.rectTransform, go = bg.gameObject, name = name, status = status };
            bg.gameObject.SetActive(false);
            return l;
        }
    }
}
