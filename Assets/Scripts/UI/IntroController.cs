using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// Opening cutscene: 6 story frames + narration; click/space advances, Esc/Skip exits.
    /// Auto-played by HubController on first entry to the camp, or replayed from the campfire.
    /// While playing, HubController freezes input and HUD via IsActive.
    public class IntroController : MonoBehaviour
    {
        private const string SeenKey   = "EmbersIntroSeen_v1";
        private const int    FrameCount = 6;

        private static readonly string[] Narration =
        {
            "Long ago, a proud kingdom rose upon a buried wonder.\nNow only ash and silence remain — and a single ember that will not die.",
            "Deep below, they unearthed the Heartcore, the sleeping heart of the world.\n\"The kingdom's eternal foundation,\" they called it — and they could not leave it be.",
            "They forced it open. Inferno, frost, and void tore loose at once,\nand swallowed the whole kingdom.",
            "Five heroes descended to seal the wound. They never returned —\ntheir souls still linger here, restless and unbroken.",
            "You are no hero. You are only an ember,\na nameless wisp drawn to their dying fire.",
            "But an ember can rekindle the fallen. Take their form.\nTake up their blade. Descend — and finish what they began.",
        };

        public static bool IsActive { get; private set; }
        public static bool HasSeen => PlayerPrefs.GetInt(SeenKey, 0) != 0;
        public static void ClearSeen() { PlayerPrefs.DeleteKey(SeenKey); PlayerPrefs.Save(); }

        public static IntroController Play(Action onComplete = null, bool markSeen = true)
        {
            var go = new GameObject("IntroController");
            var ic = go.AddComponent<IntroController>();
            ic._onComplete = onComplete;
            ic._markSeen   = markSeen;
            return ic;
        }

        private Texture2D[] _frames;
        private int    _index;
        private float  _alpha;    // 当前帧淡入 0→1
        private float  _reveal;   // 旁白已显现字符数
        private bool   _markSeen = true;
        private bool   _done;
        private Action _onComplete;

        // uGUI
        private Canvas _canvas;
        private Image  _cg, _capBorder;
        private Text   _caption, _hint;
        private readonly List<Image> _dots = new List<Image>();
        private readonly Dictionary<Texture2D, Sprite> _sprites = new Dictionary<Texture2D, Sprite>();

        private void Awake()
        {
            IsActive = true;
            _frames = new Texture2D[FrameCount];
            for (int i = 0; i < FrameCount; i++)
                _frames[i] = Resources.Load<Texture2D>($"Intro/Frame{i + 1}");
            BuildUI();
        }

        private void Start()
        {
            bool any = false;
            foreach (var t in _frames) if (t != null) { any = true; break; }
            if (!any) Finish();   // 素材缺失则不卡死,直接结束。
        }

        private void Update()
        {
            if (_done) return;
            _alpha = Mathf.MoveTowards(_alpha, 1f, Time.unscaledDeltaTime / 0.55f);
            if (_alpha >= 0.55f)
                _reveal = Mathf.MoveTowards(_reveal, CurrentText.Length, Time.unscaledDeltaTime * 46f);
            HandleInput();
            Refresh();
        }

        private string CurrentText =>
            _index >= 0 && _index < Narration.Length ? Narration[_index] : "";

        private void HandleInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { Finish(); return; }
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame ||
                kb.numpadEnterKey.wasPressedThisFrame) Advance();
            // 鼠标点击推进由全屏背景按钮处理(避免与 Skip 按钮冲突)
        }

        // 推进:文字未显示完→先补完;否则进入下一帧;末帧→结束。
        private void Advance()
        {
            if (_done) return;
            if (_reveal < CurrentText.Length - 0.5f && _alpha > 0.5f) { _reveal = CurrentText.Length; return; }
            if (_index >= FrameCount - 1) { Finish(); return; }
            _index++;
            _alpha  = 0f;
            _reveal = 0f;
        }

        private void Finish()
        {
            if (_done) return;
            _done = true;
            IsActive = false;
            if (_markSeen) { PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save(); }
            _onComplete?.Invoke();
            Destroy(gameObject);
        }

        // ── 每帧刷新显示 ──────────────────────────────────────────────────────
        private void Refresh()
        {
            float a = Mathf.Clamp01(_alpha);

            var sp = ToSprite(_index >= 0 && _index < _frames.Length ? _frames[_index] : null);
            _cg.enabled = sp != null;
            if (sp != null) { _cg.sprite = sp; _cg.color = new Color(1f, 1f, 1f, a); }

            _capBorder.color = new Color(0.85f, 0.7f, 0.35f, 0.55f * a);
            int shown = Mathf.Clamp(Mathf.FloorToInt(_reveal), 0, CurrentText.Length);
            _caption.text  = CurrentText.Substring(0, shown);
            _caption.color = new Color(0.96f, 0.93f, 0.85f, a);

            for (int i = 0; i < _dots.Count; i++)
                _dots[i].color = i == _index ? new Color(1f, 0.85f, 0.4f, 0.95f)
                               : i <  _index ? new Color(0.7f, 0.7f, 0.78f, 0.7f)
                                             : new Color(0.4f, 0.4f, 0.48f, 0.5f);

            bool showHint = shown >= CurrentText.Length && _alpha > 0.9f;
            _hint.gameObject.SetActive(showHint);
            if (showHint)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                _hint.text  = _index >= FrameCount - 1 ? "Click / Space  ▶  Begin" : "Click / Space  ▶";
                _hint.color = new Color(0.8f, 0.85f, 1f, 0.35f + 0.55f * blink);
            }
        }

        private Sprite ToSprite(Texture2D tex)
        {
            if (tex == null) return null;
            if (_sprites.TryGetValue(tex, out var s)) return s;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _sprites[tex] = s;
            return s;
        }

        // ── 构建 ──────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            _canvas = UIFactory.CreateOverlayCanvas("IntroCanvas", sortingOrder: 900);  // 压在营地 HUD 之上
            _canvas.transform.SetParent(transform, false);
            var root = _canvas.transform;

            // 全屏黑底 + 点击推进(背景按钮)
            var bg = UIFactory.Image("Bg", root, Color.black, raycast: true);
            UIFactory.Stretch(bg.rectTransform);
            var bgBtn = bg.gameObject.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(() => { if (!_done) Advance(); });

            _cg = UIFactory.Image("CG", root, new Color(1f, 1f, 1f, 0f));
            UIFactory.Stretch(_cg.rectTransform);
            _cg.preserveAspect = true;   // 等比适配(letterbox)

            var capBg = UIFactory.Image("CapBg", root, new Color(0f, 0f, 0f, 0.62f));
            var cbg = capBg.rectTransform;
            cbg.anchorMin = new Vector2(0f, 0f); cbg.anchorMax = new Vector2(1f, 0.26f);
            cbg.offsetMin = Vector2.zero; cbg.offsetMax = Vector2.zero;

            _capBorder = UIFactory.Image("CapBorder", root, new Color(0f, 0f, 0f, 0f));
            var cb = _capBorder.rectTransform;
            cb.anchorMin = new Vector2(0f, 0.26f); cb.anchorMax = new Vector2(1f, 0.26f);
            cb.pivot = new Vector2(0.5f, 1f); cb.sizeDelta = new Vector2(0f, 2f); cb.anchoredPosition = Vector2.zero;

            _caption = UIFactory.Label("Caption", capBg.transform, "", 20, TextAnchor.UpperCenter, FontStyle.Normal, Color.white, wrap: true);
            UIFactory.Stretch(_caption.rectTransform, left: 80f, right: 80f, top: 18f, bottom: 28f);

            var dots = UIFactory.Rect("Dots", root);
            dots.anchorMin = dots.anchorMax = new Vector2(0.5f, 0f); dots.pivot = new Vector2(0.5f, 0f);
            dots.anchoredPosition = new Vector2(0f, 8f); dots.sizeDelta = new Vector2(300f, 12f);
            var hlg = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            for (int i = 0; i < FrameCount; i++)
            {
                var d = UIFactory.Image($"Dot{i}", dots, Color.white);
                d.rectTransform.sizeDelta = new Vector2(7f, 7f);
                _dots.Add(d);
            }

            _hint = UIFactory.Label("Hint", root, "", 13, TextAnchor.MiddleRight, FontStyle.Normal, Color.white);
            var hr = _hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f); hr.pivot = new Vector2(1f, 0f);
            hr.sizeDelta = new Vector2(300f, 22f); hr.anchoredPosition = new Vector2(-20f, 12f);
            _hint.gameObject.SetActive(false);

            var skip = UIFactory.Button("Skip", root, Finish, new Color(0f, 0f, 0f, 0.45f));
            var srt = (RectTransform)skip.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(1f, 1f);
            srt.sizeDelta = new Vector2(96f, 28f); srt.anchoredPosition = new Vector2(-14f, -14f);
            var sl = UIFactory.Label("Txt", srt, "Skip  ⏭", 13, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.8f, 0.85f, 1f));
            UIFactory.Stretch(sl.rectTransform);
        }

        private void OnDestroy()
        {
            IsActive = false;   // 兜底:确保不会因异常销毁而把营地永久冻结。
        }
    }
}
