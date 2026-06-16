using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Game.UI;
namespace Game.Narrative
{
    /// Generic multiple-choice popup, singleton. ChoiceBox.Get().Show(title, labels, descs, idx => { ... }).
    /// Time.timeScale = 0 while open; click a button or press 1..N to choose.
    public class ChoiceBox : MonoBehaviour
    {
        public static ChoiceBox Instance { get; private set; }
        public static bool IsActive => Instance != null && Instance._open;

        private bool          _open;
        private List<string>  _labels;
        private Action<int>   _onPick;
        private float         _savedTimeScale;

        private Canvas     _canvas;
        private GameObject _panel;   // rebuilt on each Show

        public static ChoiceBox Get()
        {
            if (Instance == null)
            {
                var go = new GameObject("ChoiceBox");
                Instance = go.AddComponent<ChoiceBox>();
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// Show the choice popup. labels is required; descs may be null (no descriptions), otherwise its length must match labels.
        public void Show(string title, List<string> labels, List<string> descs, Action<int> onPick)
        {
            if (_open || labels == null || labels.Count == 0)
            {
                onPick?.Invoke(-1);
                return;
            }
            _labels         = labels;
            _onPick         = onPick;
            _open           = true;
            _savedTimeScale = Time.timeScale;
            Time.timeScale  = 0f;

            EnsureCanvas();
            BuildPanel(title, labels, descs);
            _canvas.gameObject.SetActive(true);
        }

        private void Pick(int idx)
        {
            if (!_open) return;
            _open          = false;
            Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            var cb = _onPick;
            _labels = null; _onPick = null;
            cb?.Invoke(idx);
        }

        private void Update()
        {
            if (!_open || _labels == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            int n = Mathf.Min(_labels.Count, 9);
            for (int i = 0; i < n; i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (kb[key].wasPressedThisFrame) { Pick(i); return; }
            }
        }

        // ── UI build ──────────────────────────────────────────────────────────
        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            _canvas = UIFactory.CreateOverlayCanvas("ChoiceCanvas", sortingOrder: 600);
            _canvas.transform.SetParent(transform, false);

            // full-screen dim that also blocks click-through
            var dim = UIFactory.Image("Dim", _canvas.transform, new Color(0f, 0f, 0f, 0.72f), raycast: true);
            UIFactory.Stretch(dim.rectTransform);
        }

        private void BuildPanel(string title, List<string> labels, List<string> descs)
        {
            if (_panel != null) Destroy(_panel);

            int   n       = labels.Count;
            const float panelW = 560f, btnH = 52f, gap = 12f, pad = 24f;
            float titleH  = string.IsNullOrEmpty(title) ? 0f : 44f;
            float panelH  = titleH + (btnH + gap) * n + 20f;

            var panelRt = UIFactory.Rect("Panel", _canvas.transform);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot     = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(panelW, panelH);
            _panel = panelRt.gameObject;

            var bg = UIFactory.Image("Bg", panelRt, new Color(0.08f, 0.07f, 0.12f, 0.95f));
            UIFactory.Stretch(bg.rectTransform);
            var topLine = UIFactory.Image("TopLine", panelRt, new Color(0.85f, 0.72f, 0.40f, 0.8f));
            TopLeft(topLine.rectTransform, 0f, 0f, panelW, 2f);
            var botLine = UIFactory.Image("BotLine", panelRt, new Color(0.85f, 0.72f, 0.40f, 0.8f));
            TopLeft(botLine.rectTransform, 0f, panelH - 2f, panelW, 2f);

            float y = 8f;
            if (!string.IsNullOrEmpty(title))
            {
                var t = UIFactory.Label("Title", panelRt, title, 24, TextAnchor.MiddleCenter,
                    FontStyle.Bold, new Color(0.99f, 0.86f, 0.45f));
                TopLeft(t.rectTransform, 0f, y, panelW, titleH);
                y += titleH;
            }

            for (int i = 0; i < n; i++)
            {
                int idx  = i;   // capture for closure
                float bw = panelW - pad * 2f;

                var btn = UIFactory.Button($"Choice{i}", panelRt, () => Pick(idx),
                    new Color(0.14f, 0.13f, 0.20f));
                var brt = (RectTransform)btn.transform;
                TopLeft(brt, pad, y, bw, btnH);
                btn.colors = new ColorBlock
                {
                    normalColor      = new Color(0.14f, 0.13f, 0.20f),
                    highlightedColor = new Color(0.22f, 0.20f, 0.32f),
                    pressedColor     = new Color(0.26f, 0.24f, 0.38f),
                    selectedColor    = new Color(0.22f, 0.20f, 0.32f),
                    disabledColor    = new Color(0.14f, 0.13f, 0.20f),
                    colorMultiplier  = 1f,
                    fadeDuration     = 0.08f,
                };

                var accent = UIFactory.Image("Accent", brt, new Color(0.92f, 0.78f, 0.40f, 0.8f));
                TopLeft(accent.rectTransform, 0f, 0f, 3f, btnH);

                var num = UIFactory.Label("Num", brt, $"{i + 1}.", 20, TextAnchor.MiddleLeft,
                    FontStyle.Bold, new Color(0.85f, 0.72f, 0.45f));
                TopLeft(num.rectTransform, 16f, 2f, 30f, btnH - 4f);

                var lab = UIFactory.Label("Label", brt, labels[i], 20, TextAnchor.MiddleLeft,
                    FontStyle.Bold, new Color(0.95f, 0.94f, 0.92f));
                TopLeft(lab.rectTransform, 50f, 2f, bw - 60f, 26f);

                string desc = (descs != null && i < descs.Count) ? descs[i] : null;
                if (!string.IsNullOrEmpty(desc))
                {
                    var d = UIFactory.Label("Desc", brt, desc, 13, TextAnchor.MiddleLeft,
                        FontStyle.Italic, new Color(0.72f, 0.70f, 0.78f));
                    TopLeft(d.rectTransform, 50f, 26f, bw - 60f, 22f);
                }

                y += btnH + gap;
            }

            var foot = UIFactory.Label("Foot", panelRt, "Click / press 1-9 to choose", 11,
                TextAnchor.MiddleCenter, FontStyle.Italic, new Color(0.55f, 0.55f, 0.62f));
            TopLeft(foot.rectTransform, 0f, panelH - 22f, panelW, 18f);
        }

        // Place relative to the parent's top-left corner, with y growing downward.
        private static void TopLeft(RectTransform rt, float x, float yFromTop, float w, float h)
        {
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.sizeDelta        = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
        }
    }
}
