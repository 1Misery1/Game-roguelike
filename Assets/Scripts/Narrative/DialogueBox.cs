using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Game.Art;
using Game.UI;
namespace Game.Narrative
{
    /// Dialogue box UI, global singleton. DialogueBox.Get().Play(lines, onComplete).
    /// Time.timeScale = 0 while open (freezes combat); click / space advances. Body text auto-sizes (Best-Fit).
    public class DialogueBox : MonoBehaviour
    {
        public static DialogueBox Instance { get; private set; }

        /// Whether a dialogue is currently showing (PlayerController uses this to block input).
        public static bool IsActive => Instance != null && Instance._open;

        private readonly List<DialogueLine> _lines = new List<DialogueLine>();
        private int           _index;
        private bool          _open;
        private System.Action _onComplete;

        // UI components
        private Canvas _canvas;
        private Image  _portraitImg;
        private Text   _nameText;
        private Text   _bodyText;
        private Text   _hintText;
        private bool   _built;

        /// Get the singleton (creating it on demand).
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

            EnsureUI();
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        private void Advance()
        {
            _index++;
            if (_index >= _lines.Count) Close();
            else Refresh();
        }

        private void Close()
        {
            _open = false;
            Game.Core.GameSignals.DialogueActive = false;
            Time.timeScale = 1f;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
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

        // ── Refresh the current line ──────────────────────────────────────────
        private void Refresh()
        {
            if (_index < 0 || _index >= _lines.Count) return;
            var line = _lines[_index];

            _nameText.text = line.Speaker;
            _bodyText.text = line.Text;
            _hintText.text = _index < _lines.Count - 1
                ? "▶  Click / Space  Continue" : "▶  Click / Space  End";

            var portrait = ResolvePortrait(line.PortraitKey);
            _portraitImg.sprite  = portrait;
            _portraitImg.color   = portrait != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            _portraitImg.enabled = portrait != null;
        }

        // ── UI build (one-time) ───────────────────────────────────────────────
        // Background art is 1672x941; each region uses fractional insets of that frame.
        private void EnsureUI()
        {
            if (_built) return;
            _built = true;

            _canvas = UIFactory.CreateOverlayCanvas("DialogueCanvas", sortingOrder: 500);
            _canvas.transform.SetParent(transform, false);

            // Dialogue panel: bottom-centre of the screen, keeping the 1672:941 art ratio.
            var box = UIFactory.Rect("Box", _canvas.transform);
            box.anchorMin = new Vector2(0.5f, 0f);
            box.anchorMax = new Vector2(0.5f, 0f);
            box.pivot     = new Vector2(0.5f, 0f);
            const float boxW = 640f, boxH = boxW * (941f / 1672f);   // ≈ 360 reference units
            box.sizeDelta        = new Vector2(boxW, boxH);
            box.anchoredPosition = new Vector2(0f, 18f);

            // Background: prefer the texture; fall back to a dark block with top/bottom edges.
            var bgTex = Resources.Load<Texture2D>("UI/dialogue_box");
            if (bgTex != null)
            {
                var bg = UIFactory.Sprite("Bg", box, ToSprite(bgTex));
                UIFactory.Stretch(bg.rectTransform);
            }
            else
            {
                var bg = UIFactory.Image("Bg", box, new Color(0.06f, 0.06f, 0.10f, 0.98f));
                UIFactory.Stretch(bg.rectTransform);
                var top = UIFactory.Image("TopLine", box, new Color(0.5f, 0.4f, 0.7f));
                Frac(top.rectTransform, 0f, 0f, 1f, 0.006f);
                var bot = UIFactory.Image("BotLine", box, new Color(0.5f, 0.4f, 0.7f));
                Frac(bot.rectTransform, 0f, 0.994f, 1f, 0.006f);
            }

            // Portrait on the left (frame x[0.085,0.265], y from top [0.27,0.71]).
            _portraitImg = UIFactory.Sprite("Portrait", box, null);
            _portraitImg.preserveAspect = true;
            Frac(_portraitImg.rectTransform, 0.085f, 0.27f, 0.180f, 0.44f);

            // Name (gold, with shadow) x[0.35,0.91], y from top [0.175,0.305].
            _nameText = UIFactory.Label("Name", box, "", 18, TextAnchor.MiddleLeft,
                FontStyle.Bold, new Color(0.99f, 0.84f, 0.40f));
            Frac(_nameText.rectTransform, 0.35f, 0.175f, 0.56f, 0.13f);
            AddShadow(_nameText);

            // Divider under the name x[0.35,0.882], y from top 0.325.
            var div = UIFactory.Image("Divider", box, new Color(0.62f, 0.50f, 0.30f, 0.45f));
            Frac(div.rectTransform, 0.35f, 0.325f, 0.56f * 0.95f, 0.004f);

            // Body (near-white, auto-scaled to fit without overflow) x[0.35,0.91], y from top [0.375,0.76].
            _bodyText = UIFactory.Label("Body", box, "", 18, TextAnchor.UpperLeft,
                FontStyle.Normal, new Color(0.92f, 0.91f, 0.95f), wrap: true);
            _bodyText.resizeTextForBestFit = true;   // auto-fit the body font
            _bodyText.resizeTextMinSize    = 12;
            _bodyText.resizeTextMaxSize    = 18;
            Frac(_bodyText.rectTransform, 0.35f, 0.375f, 0.56f, 0.385f);
            AddShadow(_bodyText);

            // Advance hint x[0.35,0.91], y from top [0.79,0.90].
            _hintText = UIFactory.Label("Hint", box, "", 11, TextAnchor.MiddleRight,
                FontStyle.Italic, new Color(0.66f, 0.64f, 0.74f));
            Frac(_hintText.rectTransform, 0.35f, 0.79f, 0.56f, 0.11f);
        }

        // Portrait: prefer real art (Resources/Portraits/{key}); fall back to a HeroSprites sprite.
        private Sprite ResolvePortrait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var art = LoadPortraitArt(key);
            if (art != null) return ToSprite(art);
            return HeroSprites.Get(key);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Set a child RectTransform's anchors from a top-down fractional rect (parent = box).
        private static void Frac(RectTransform rt, float x, float yTop, float w, float h)
        {
            rt.anchorMin = new Vector2(x, 1f - (yTop + h));
            rt.anchorMax = new Vector2(x + w, 1f - yTop);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddShadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor    = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // Texture2D → Sprite (cached to avoid rebuilding per line).
        private static readonly Dictionary<Texture2D, Sprite> _spriteCache =
            new Dictionary<Texture2D, Sprite>();
        private static Sprite ToSprite(Texture2D tex)
        {
            if (tex == null) return null;
            if (_spriteCache.TryGetValue(tex, out var s)) return s;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                              new Vector2(0.5f, 0.5f), 100f);
            _spriteCache[tex] = s;
            return s;
        }

        // Hero portrait cache (Resources/Portraits/{heroKey}.png). Caches nulls too, to avoid per-frame Resources.Load.
        private static readonly Dictionary<string, Texture2D> _portraitArt =
            new Dictionary<string, Texture2D>();
        private static Texture2D LoadPortraitArt(string key)
        {
            if (_portraitArt.TryGetValue(key, out var t)) return t;
            t = Resources.Load<Texture2D>($"Portraits/{key}");
            _portraitArt[key] = t;
            return t;
        }
    }
}
