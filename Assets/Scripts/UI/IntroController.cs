using System;
using UnityEngine;

namespace Game.UI
{
    /// 开场过场动画：依次展示 6 张剧情背景 + 英文旁白，点击 / 空格推进，可跳过（Esc / Skip）。
    /// 纯 IMGUI 全屏绘制，风格与营地一致。播放期间 HubController 通过 IsActive 冻结输入与 HUD。
    /// 由 HubController 在「首次进入营地」时自动唤起，或由篝火交互手动重温。
    public class IntroController : MonoBehaviour
    {
        private const string SeenKey   = "EmbersIntroSeen_v1";
        private const int    FrameCount = 6;

        /// 英文旁白（每帧 1～2 句，\n 手动换行）。
        private static readonly string[] Narration =
        {
            "Long ago, a proud kingdom rose upon a buried wonder.\nNow only ash and silence remain — and a single ember that will not die.",
            "Deep below, they unearthed the Heartcore, the sleeping heart of the world.\n\"The kingdom's eternal foundation,\" they called it — and they could not leave it be.",
            "They forced it open. Inferno, frost, and void tore loose at once,\nand swallowed the whole kingdom.",
            "Five heroes descended to seal the wound. They never returned —\ntheir souls still linger here, restless and unbroken.",
            "You are no hero. You are only an ember,\na nameless wisp drawn to their dying fire.",
            "But an ember can rekindle the fallen. Take their form.\nTake up their blade. Descend — and finish what they began.",
        };

        /// 播放中标志：营地据此冻结移动 / 交互 / HUD，避免穿透。
        public static bool IsActive { get; private set; }

        /// 是否已看过开场（首次进营地用来决定是否自动播放）。
        public static bool HasSeen => PlayerPrefs.GetInt(SeenKey, 0) != 0;

        /// 清除「已看过」标记（重置存档时调用，使下次进营地重新播放）。
        public static void ClearSeen() { PlayerPrefs.DeleteKey(SeenKey); PlayerPrefs.Save(); }

        /// 生成并播放开场动画。onComplete 在播完 / 跳过后回调。markSeen=true 时播完写入「已看过」。
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
        private float  _alpha;        // 当前帧淡入 0→1
        private float  _reveal;       // 旁白已显现字符数
        private bool   _markSeen = true;
        private bool   _done;
        private Action _onComplete;
        private GUIStyle _textStyle, _hintStyle;

        private void Awake()
        {
            IsActive = true;
            _frames = new Texture2D[FrameCount];
            for (int i = 0; i < FrameCount; i++)
                _frames[i] = Resources.Load<Texture2D>($"Intro/Frame{i + 1}");
        }

        private void Start()
        {
            // 素材缺失则不卡死，直接结束。
            bool any = false;
            foreach (var t in _frames) if (t != null) { any = true; break; }
            if (!any) Finish();
        }

        private void Update()
        {
            if (_done) return;
            _alpha = Mathf.MoveTowards(_alpha, 1f, Time.unscaledDeltaTime / 0.55f);
            if (_alpha >= 0.55f)
                _reveal = Mathf.MoveTowards(_reveal, CurrentText.Length, Time.unscaledDeltaTime * 46f);
        }

        private string CurrentText =>
            _index >= 0 && _index < Narration.Length ? Narration[_index] : "";

        // 推进：文字未显示完→先补完；否则进入下一帧；末帧→结束。
        private void Advance()
        {
            if (_done) return;
            if (_reveal < CurrentText.Length - 0.5f && _alpha > 0.5f)
            {
                _reveal = CurrentText.Length;
                return;
            }
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

        private void OnGUI()
        {
            if (_done) return;
            GUI.depth = -1000;                 // 压在营地 HUD 之上
            UIFonts.ApplyToSkin();
            EnsureStyles();

            float sw = Screen.width, sh = Screen.height;

            // 整屏黑底（同时遮住背后的营地）
            Fill(new Rect(0, 0, sw, sh), Color.black);

            // 当前帧：等比适配 + 黑边，按淡入 alpha 叠加
            var tex = _index >= 0 && _index < _frames.Length ? _frames[_index] : null;
            if (tex != null)
            {
                float scale = Mathf.Min(sw / tex.width, sh / tex.height);
                float w = tex.width * scale, h = tex.height * scale;
                var imgRect = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(_alpha));
                GUI.DrawTexture(imgRect, tex, ScaleMode.ScaleToFit);
                GUI.color = prev;
            }

            // 底部旁白条
            float boxH = Mathf.Clamp(sh * 0.26f, 120f, 220f);
            var boxRect = new Rect(0, sh - boxH, sw, boxH);
            Fill(boxRect, new Color(0f, 0f, 0f, 0.62f));
            Fill(new Rect(0, sh - boxH, sw, 2f), new Color(0.85f, 0.7f, 0.35f, 0.55f * _alpha));

            float pad = Mathf.Max(28f, sw * 0.12f);
            int shown = Mathf.Clamp(Mathf.FloorToInt(_reveal), 0, CurrentText.Length);
            string txt = CurrentText.Substring(0, shown);
            var tprev = GUI.color;
            GUI.color = new Color(0.96f, 0.93f, 0.85f, Mathf.Clamp01(_alpha));
            GUI.Label(new Rect(pad, sh - boxH + 22f, sw - pad * 2f, boxH - 56f), txt, _textStyle);
            GUI.color = tprev;

            // 进度点 + 推进提示（文字显示完后闪烁）
            DrawDots(new Rect(0, sh - 30f, sw, 18f));
            bool revealed = shown >= CurrentText.Length;
            if (revealed && _alpha > 0.9f)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                var hprev = GUI.color;
                GUI.color = new Color(0.8f, 0.85f, 1f, 0.35f + 0.55f * blink);
                string hint = _index >= FrameCount - 1 ? "Click / Space  ▶  Begin" : "Click / Space  ▶";
                GUI.Label(new Rect(sw - 320f, sh - 34f, 300f, 22f), hint, _hintStyle);
                GUI.color = hprev;
            }

            // 跳过按钮（右上）
            var skipRect = new Rect(sw - 96f, 14f, 82f, 26f);
            Fill(skipRect, new Color(0f, 0f, 0f, 0.45f));
            if (GUI.Button(skipRect, GUIContent.none, GUIStyle.none)) { Finish(); return; }
            GUI.Label(skipRect, "Skip  ⏭", _hintStyle);

            // 全局输入：点击 / 空格 / 回车推进；Esc 跳过
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape) { Finish(); e.Use(); return; }
                if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                { Advance(); e.Use(); }
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                Advance(); e.Use();
            }
        }

        private void DrawDots(Rect area)
        {
            const float r = 7f, gap = 10f;
            float total = FrameCount * r + (FrameCount - 1) * gap;
            float x = (area.width - total) * 0.5f;
            float y = area.y + (area.height - r) * 0.5f;
            for (int i = 0; i < FrameCount; i++)
            {
                Color c = i == _index ? new Color(1f, 0.85f, 0.4f, 0.95f)
                        : i <  _index ? new Color(0.7f, 0.7f, 0.78f, 0.7f)
                                      : new Color(0.4f, 0.4f, 0.48f, 0.5f);
                Fill(new Rect(x + i * (r + gap), y, r, r), c);
            }
        }

        private void EnsureStyles()
        {
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize  = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.030f), 15, 26),
                    wordWrap  = true,
                    richText  = false,
                };
            }
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 13,
                };
            }
        }

        private static Texture2D _white;
        private static Texture2D White
        {
            get
            {
                if (_white != null) return _white;
                _white = new Texture2D(1, 1);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
                _white.hideFlags = HideFlags.HideAndDontSave;
                return _white;
            }
        }

        private static void Fill(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = prev;
        }

        private void OnDestroy()
        {
            // 兜底：确保不会因异常销毁而把营地永久冻结。
            IsActive = false;
        }
    }
}
