using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Narrative
{
    /// 通用二/多选弹窗（IMGUI 单例）。
    /// 用法：ChoiceBox.Get().Show(labels, idx => { ... });
    /// 弹窗期间 Time.timeScale = 0；点击按钮或数字键 1..N 选择。
    public class ChoiceBox : MonoBehaviour
    {
        public static ChoiceBox Instance { get; private set; }
        public static bool IsActive => Instance != null && Instance._open;

        private bool          _open;
        private List<string>  _labels;
        private List<string>  _descs;
        private Action<int>   _onPick;
        private string        _title;
        private float         _savedTimeScale;

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

        /// 显示选项弹窗。labels 必填；descs 可为 null（无描述），否则长度需与 labels 一致
        public void Show(string title, List<string> labels, List<string> descs, Action<int> onPick)
        {
            if (_open || labels == null || labels.Count == 0)
            {
                onPick?.Invoke(-1);
                return;
            }
            _title          = title;
            _labels         = labels;
            _descs          = descs;
            _onPick         = onPick;
            _open           = true;
            _savedTimeScale = Time.timeScale;
            Time.timeScale  = 0f;
        }

        private void Pick(int idx)
        {
            if (!_open) return;
            _open          = false;
            Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
            var cb = _onPick;
            _labels  = null; _descs = null; _onPick = null;
            cb?.Invoke(idx);
        }

        private void Update()
        {
            if (!_open || _labels == null) return;
            // 数字键 1..N
            var kb = Keyboard.current;
            if (kb != null)
            {
                int n = Mathf.Min(_labels.Count, 9);
                for (int i = 0; i < n; i++)
                {
                    Key key = (Key)((int)Key.Digit1 + i);
                    if (kb[key].wasPressedThisFrame) { Pick(i); return; }
                }
            }
        }

        private void OnGUI()
        {
            if (!_open) return;

            // 全屏遮罩
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.72f));

            float panelW = Mathf.Min(720f, Screen.width * 0.70f);
            float btnH   = 56f;
            float gap    = 14f;
            int   n      = _labels.Count;
            float titleH = string.IsNullOrEmpty(_title) ? 0f : 50f;
            float panelH = titleH + (btnH + gap) * n + 24f;
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.5f;

            // 面板背景
            FillRect(new Rect(panelX, panelY, panelW, panelH), new Color(0.08f, 0.07f, 0.12f, 0.95f));
            FillRect(new Rect(panelX, panelY, panelW, 2f),
                     new Color(0.85f, 0.72f, 0.40f, 0.8f));
            FillRect(new Rect(panelX, panelY + panelH - 2f, panelW, 2f),
                     new Color(0.85f, 0.72f, 0.40f, 0.8f));

            float y = panelY + 8f;
            if (!string.IsNullOrEmpty(_title))
            {
                GUI.Label(new Rect(panelX, y, panelW, titleH), _title,
                    Style(24, TextAnchor.MiddleCenter, FontStyle.Bold,
                          new Color(0.99f, 0.86f, 0.45f)));
                y += titleH;
            }

            for (int i = 0; i < n; i++)
            {
                var r = new Rect(panelX + 24f, y, panelW - 48f, btnH);
                bool hover = r.Contains(Event.current.mousePosition);
                FillRect(r, hover ? new Color(0.22f, 0.20f, 0.32f) : new Color(0.14f, 0.13f, 0.20f));
                FillRect(new Rect(r.x, r.y, 3f, r.height),
                         new Color(0.92f, 0.78f, 0.40f, hover ? 1f : 0.6f));

                GUI.Label(new Rect(r.x + 16f, r.y + 4f, 30f, r.height - 8f), $"{i + 1}.",
                    Style(20, TextAnchor.MiddleLeft, FontStyle.Bold,
                          new Color(0.85f, 0.72f, 0.45f)));

                GUI.Label(new Rect(r.x + 50f, r.y + 4f, r.width - 60f, 28f), _labels[i],
                    Style(20, TextAnchor.MiddleLeft, FontStyle.Bold,
                          new Color(0.95f, 0.94f, 0.92f)));

                string desc = (_descs != null && i < _descs.Count) ? _descs[i] : null;
                if (!string.IsNullOrEmpty(desc))
                    GUI.Label(new Rect(r.x + 50f, r.y + 28f, r.width - 60f, 24f), desc,
                        Style(13, TextAnchor.MiddleLeft, FontStyle.Italic,
                              new Color(0.72f, 0.70f, 0.78f)));

                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { Pick(i); return; }
                y += btnH + gap;
            }

            GUI.Label(new Rect(panelX, panelY + panelH - 22f, panelW, 18f),
                "鼠标点击 / 按 1-9 数字键选择",
                Style(11, TextAnchor.MiddleCenter, FontStyle.Italic,
                      new Color(0.55f, 0.55f, 0.62f)));
        }

        // ── IMGUI 工具 ────────────────────────────────────────────────────────
        private static Texture2D _white;
        private static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                    _white.hideFlags = HideFlags.HideAndDontSave;
                }
                return _white;
            }
        }

        private static void FillRect(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = prev;
        }

        private static Font _font;
        private static Font UIFont => _font != null ? _font :
            (_font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体" }, 22));

        private static GUIStyle Style(int size, TextAnchor align, FontStyle fs, Color color)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize  = size,
                alignment = align,
                fontStyle = fs,
                richText  = false,
            };
            if (UIFont != null) s.font = UIFont;
            s.normal.textColor = color;
            return s;
        }
    }
}
