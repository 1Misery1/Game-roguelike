using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// In-game pause menu, shared across Hub / Training / Dungeon. Press ESC to open.
    /// Code-built (UIFactory) so any scene can spawn it via Ensure(); a single instance lives per scene.
    /// Options: Resume, Settings (master volume), Main Menu (back to the Title scene), Quit to Desktop.
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        /// Set by gameplay code (e.g. ending cutscene) to temporarily block the pause toggle.
        public static bool Suppress { get; set; }

        [SerializeField] private GameObject panel;   // legacy in-scene panel (Dungeon); destroyed at runtime

        private const string TitleScene = "Title";
        private const float  BarWidth   = 180f;

        private Canvas        _canvas;
        private GameObject     _main;
        private GameObject     _settings;
        private RectTransform  _volFill;
        private Text           _volPct;
        private bool           _open;

        public bool IsPaused => _open;

        /// Creates the pause menu if the active scene does not already have one.
        public static void Ensure()
        {
            if (Instance != null) return;
            new GameObject("PauseMenu").AddComponent<PauseMenuController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (panel != null) Destroy(panel);   // drop the old scene-built panel; we rebuild in code
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_open) Time.timeScale = 1f;
            if (_canvas != null) Destroy(_canvas.gameObject);   // root canvas isn't our child; clean it up
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            // Don't fight other ESC consumers / modal UI.
            if (Suppress) return;
            if (Game.Core.GameSignals.DialogueActive || Game.Core.GameSignals.ChoiceActive || IntroController.IsActive) return;

            if (!_open)            Open();
            else if (SettingsOpen) ShowMain();   // ESC backs out of settings first
            else                   Resume();
        }

        private bool SettingsOpen => _settings != null && _settings.activeSelf;

        // ── Open / close ──────────────────────────────────────────────────────
        private void Open()
        {
            _open = true;
            Time.timeScale = 0f;
            _canvas.gameObject.SetActive(true);
            ShowMain();
        }

        public void Resume()
        {
            _open = false;
            Time.timeScale = 1f;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _open = false;
            SceneManager.LoadScene(TitleScene);
        }

        /// Quit the application (returns to the OS). Stops play mode inside the editor.
        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowMain()     { if (_main != null) _main.SetActive(true);  if (_settings != null) _settings.SetActive(false); }
        private void ShowSettings()
        {
            if (_settings != null) _settings.SetActive(true);
            if (_main != null)     _main.SetActive(false);
            RefreshVolumeUI();
        }

        // ── Volume ────────────────────────────────────────────────────────────
        private void StepVolume(float delta)
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Round((AudioListener.volume + delta) * 20f) / 20f);
            RefreshVolumeUI();
        }

        private void RefreshVolumeUI()
        {
            float v = Mathf.Clamp01(AudioListener.volume);
            if (_volFill != null) _volFill.sizeDelta = new Vector2(BarWidth * v, 0f);
            if (_volPct  != null) _volPct.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        // ── UI build ──────────────────────────────────────────────────────────
        private void Build()
        {
            // Keep this a ROOT overlay canvas (do NOT parent it under this GameObject).
            // In the Dungeon scene the controller sits on an existing Canvas; nesting a
            // canvas under another makes it a sub-canvas that inherits the parent rect,
            // which pushes the centred panels into a corner. A root canvas is screen-driven.
            _canvas = UIFactory.CreateOverlayCanvas("PauseCanvas", sortingOrder: 650);

            var dim = UIFactory.Image("Dim", _canvas.transform, new Color(0f, 0f, 0f, 0.7f), raycast: true);
            UIFactory.Stretch(dim.rectTransform);

            _main     = BuildMainPanel();
            _settings = BuildSettingsPanel();

            _canvas.gameObject.SetActive(false);
        }

        private GameObject BuildMainPanel()
        {
            var panelRt = CenteredPanel("MainPanel", 360f, 320f);

            var title = UIFactory.Label("Title", panelRt, "PAUSED", 26, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(0.99f, 0.86f, 0.45f));
            Center(title.rectTransform, 0f, 122f, 320f, 40f);

            MakeButton(panelRt, "Resume",          58f,  Resume,           new Color(0.18f, 0.20f, 0.30f));
            MakeButton(panelRt, "Settings",        8f,   ShowSettings,     new Color(0.16f, 0.15f, 0.22f));
            MakeButton(panelRt, "Main Menu",      -42f,  ReturnToMainMenu, new Color(0.16f, 0.15f, 0.22f));
            MakeButton(panelRt, "Quit to Desktop", -100f, QuitGame,        new Color(0.40f, 0.16f, 0.16f));

            return panelRt.gameObject;
        }

        private GameObject BuildSettingsPanel()
        {
            var panelRt = CenteredPanel("SettingsPanel", 360f, 240f);

            var title = UIFactory.Label("Title", panelRt, "SETTINGS", 24, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(0.99f, 0.86f, 0.45f));
            Center(title.rectTransform, 0f, 86f, 320f, 36f);

            var lbl = UIFactory.Label("VolLabel", panelRt, "Master Volume", 16, TextAnchor.MiddleLeft,
                FontStyle.Normal, new Color(0.9f, 0.9f, 0.95f));
            Center(lbl.rectTransform, -34f, 28f, 200f, 24f);

            _volPct = UIFactory.Label("VolPct", panelRt, "100%", 16, TextAnchor.MiddleRight,
                FontStyle.Bold, new Color(0.85f, 0.85f, 0.92f));
            Center(_volPct.rectTransform, 120f, 28f, 70f, 24f);

            // [-]  bar  [+]
            var minus = MakeSmallButton(panelRt, "-", -150f, -14f, () => StepVolume(-0.05f));
            var plus  = MakeSmallButton(panelRt, "+",  150f, -14f, () => StepVolume( 0.05f));

            var barBg = UIFactory.Image("VolBarBg", panelRt, new Color(0.20f, 0.20f, 0.26f, 1f));
            Center(barBg.rectTransform, 0f, -14f, BarWidth, 14f);
            var fill = UIFactory.Image("VolFill", barBg.rectTransform, new Color(0.92f, 0.78f, 0.40f, 1f));
            _volFill = fill.rectTransform;
            _volFill.anchorMin = new Vector2(0f, 0f);
            _volFill.anchorMax = new Vector2(0f, 1f);
            _volFill.pivot     = new Vector2(0f, 0.5f);
            _volFill.anchoredPosition = Vector2.zero;
            _volFill.sizeDelta = new Vector2(BarWidth, 0f);

            MakeButton(panelRt, "Back", -82f, ShowMain, new Color(0.16f, 0.15f, 0.22f));

            return panelRt.gameObject;
        }

        // ── small layout helpers ──────────────────────────────────────────────
        private RectTransform CenteredPanel(string name, float w, float h)
        {
            var rt = UIFactory.Rect(name, _canvas.transform);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            var bg = UIFactory.Image("Bg", rt, new Color(0.08f, 0.07f, 0.12f, 0.97f), raycast: true);
            UIFactory.Stretch(bg.rectTransform);
            var top = UIFactory.Image("TopLine", rt, new Color(0.85f, 0.72f, 0.40f, 0.85f));
            Center(top.rectTransform, 0f, h / 2f - 1f, w, 2f);
            var bot = UIFactory.Image("BotLine", rt, new Color(0.85f, 0.72f, 0.40f, 0.85f));
            Center(bot.rectTransform, 0f, -(h / 2f - 1f), w, 2f);
            return rt;
        }

        private void Center(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private Button MakeButton(Transform parent, string text, float y, System.Action onClick, Color bg)
        {
            var btn = UIFactory.Button("Btn_" + text, parent, onClick, bg);
            Center((RectTransform)btn.transform, 0f, y, 280f, 42f);
            var lbl = UIFactory.Label("Lbl", btn.transform, text, 18, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(0.95f, 0.94f, 0.92f));
            UIFactory.Stretch(lbl.rectTransform);
            return btn;
        }

        private Button MakeSmallButton(Transform parent, string text, float x, float y, System.Action onClick)
        {
            var btn = UIFactory.Button("Btn_vol" + text, parent, onClick, new Color(0.22f, 0.22f, 0.30f));
            Center((RectTransform)btn.transform, x, y, 40f, 36f);
            var lbl = UIFactory.Label("Lbl", btn.transform, text, 22, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(0.95f, 0.94f, 0.92f));
            UIFactory.Stretch(lbl.rectTransform);
            return btn;
        }
    }
}
