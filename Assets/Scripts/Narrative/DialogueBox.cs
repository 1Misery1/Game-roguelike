using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Art;
using Game.UI;
namespace Game.Narrative
{
    /// 对话框 UI（IMGUI 绘制），全局单例。
    /// 调用方式：DialogueBox.Get().Play(lines, onComplete)。
    /// 对话期间 Time.timeScale = 0，冻结战斗；点击 / 空格推进。
    public class DialogueBox : MonoBehaviour
    {
        public static DialogueBox Instance { get; private set; }

        /// 对话是否正在显示（PlayerController 据此屏蔽输入）
        public static bool IsActive => Instance != null && Instance._open;

        private readonly List<DialogueLine> _lines = new List<DialogueLine>();
        private int           _index;
        private bool          _open;
        private System.Action _onComplete;

        private Texture2D _bg;
        private bool      _bgLoaded;

        /// 取得（或按需创建）单例
        public static DialogueBox Get()
        {
            if (Instance == null)
            {
                var go = new GameObject("DialogueBox");
                Instance = go.AddComponent<DialogueBox>();
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Play(List<DialogueLine> lines, System.Action onComplete)
        {
            if (_open || lines == null || lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }
            _lines.Clear();
            _lines.AddRange(lines);
            _index      = 0;
            _onComplete = onComplete;
            _open       = true;
            Game.Core.GameSignals.DialogueActive = true;
            Time.timeScale = 0f;
        }

        private void Advance()
        {
            _index++;
            if (_index >= _lines.Count) Close();
        }

        private void Close()
        {
            _open = false;
            Game.Core.GameSignals.DialogueActive = false;
            Time.timeScale = 1f;
            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }

        private void Update()
        {
            if (!_open) return;
            bool advance =
                (Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame ||
                                              Keyboard.current.enterKey.wasPressedThisFrame));
            if (advance) Advance();
        }

        private Texture2D Background
        {
            get
            {
                if (!_bgLoaded)
                {
                    _bg = Resources.Load<Texture2D>("UI/dialogue_box");
                    _bgLoaded = true;
                }
                return _bg;
            }
        }

        // 英雄立绘缓存（Resources/Portraits/{heroKey}.png）。含 null 缓存，避免每帧 Resources.Load。
        private static readonly Dictionary<string, Texture2D> _portraitArt =
            new Dictionary<string, Texture2D>();
        private static Texture2D LoadPortraitArt(string key)
        {
            if (_portraitArt.TryGetValue(key, out var t)) return t;
            t = Resources.Load<Texture2D>($"Portraits/{key}");
            _portraitArt[key] = t;
            return t;
        }

        // ── 布局 ──────────────────────────────────────────────────────────────
        // 背景图 1672×941。各区域按背景图内框比例标定：
        //   · 左侧头像内框 ≈ x[0.07~0.28]  y[0.21~0.79]
        //   · 右侧文字面板 ≈ x[0.32~0.95]  y[0.13~0.87]
        private void OnGUI()
        {
            if (!_open || _index >= _lines.Count) return;
            var line = _lines[_index];

            // 对话框周围保持透明，不再绘制全屏压暗遮罩

            // 对话框尺寸（保持背景图 1672:941 比例），居于屏幕下方
            float boxW = Mathf.Min(900f, Screen.width * 0.80f);
            float boxH = boxW * (941f / 1672f);
            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = Screen.height - boxH - 36f;
            var   box  = new Rect(boxX, boxY, boxW, boxH);

            var bg = Background;
            if (bg != null)
            {
                GUI.DrawTexture(box, bg, ScaleMode.StretchToFill);
            }
            else
            {
                // 背景图未导入时的回退绘制
                FillRect(box, new Color(0.06f, 0.06f, 0.10f, 0.98f));
                FillRect(new Rect(boxX, boxY, boxW, 2f), new Color(0.5f, 0.4f, 0.7f));
                FillRect(new Rect(boxX, boxY + boxH - 2f, boxW, 2f), new Color(0.5f, 0.4f, 0.7f));
            }

            // 左侧头像（嵌入背景图的内框）：优先真立绘（Resources/Portraits/{key}.png），
            // 缺失时回退到 HeroSprites 程序化精灵。
            if (!string.IsNullOrEmpty(line.PortraitKey))
            {
                var portRect = new Rect(boxX + boxW * 0.085f, boxY + boxH * 0.27f,
                                        boxW * 0.180f, boxH * 0.44f);
                var art = LoadPortraitArt(line.PortraitKey);
                if (art != null)
                {
                    GUI.DrawTexture(portRect, art, ScaleMode.ScaleToFit);
                }
                else
                {
                    var portrait = HeroSprites.Get(line.PortraitKey);
                    if (portrait != null)
                        GUI.DrawTexture(portRect, portrait.texture, ScaleMode.ScaleToFit);
                }
            }

            // 右侧文字面板
            float tx = boxX + boxW * 0.35f;
            float tw = boxW * 0.56f;

            // 名字（楷体，金色）
            int nameSize = Mathf.RoundToInt(Mathf.Clamp(boxH * 0.052f, 16f, 30f));
            ShadowLabel(new Rect(tx, boxY + boxH * 0.175f, tw, boxH * 0.13f), line.Speaker,
                Style(NameFont, nameSize, TextAnchor.MiddleLeft, FontStyle.Bold,
                      new Color(0.99f, 0.84f, 0.40f)));

            // 名字下分隔线
            FillRect(new Rect(tx, boxY + boxH * 0.325f, tw * 0.95f, 1f),
                     new Color(0.62f, 0.50f, 0.30f, 0.45f));

            // 对话正文（雅黑，近白）
            int bodySize = Mathf.RoundToInt(Mathf.Clamp(boxH * 0.040f, 13f, 22f));
            var bodyStyle = Style(UIFont, bodySize, TextAnchor.UpperLeft, FontStyle.Normal,
                                  new Color(0.92f, 0.91f, 0.95f));
            bodyStyle.wordWrap = true;
            ShadowLabel(new Rect(tx, boxY + boxH * 0.375f, tw, boxH * 0.37f), line.Text, bodyStyle);

            // 推进提示
            string hint = _index < _lines.Count - 1 ? "▶  Click / Space  Continue" : "▶  Click / Space  End";
            int hintSize = Mathf.RoundToInt(Mathf.Clamp(boxH * 0.030f, 11f, 16f));
            GUI.Label(new Rect(tx, boxY + boxH * 0.79f, tw, boxH * 0.11f), hint,
                Style(UIFont, hintSize, TextAnchor.MiddleRight, FontStyle.Italic,
                      new Color(0.66f, 0.64f, 0.74f)));
        }

        // 字体由全局 UIFonts 统一提供（方舟像素体；缺失时系统中文兜底）
        private static Font UIFont   => UIFonts.UI;
        private static Font NameFont => UIFont;

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

        // 带投影的文字，提升在贴图背景上的可读性
        private static void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            var orig = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, style);
            style.normal.textColor = orig;
            GUI.Label(r, text, style);
        }

        private static GUIStyle Style(Font font, int size, TextAnchor align, FontStyle fs, Color color)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize  = size,
                alignment = align,
                fontStyle = fs,
                richText  = false,
            };
            if (font != null) s.font = font;
            s.normal.textColor = color;
            return s;
        }
    }
}
