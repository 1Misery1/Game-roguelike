using System.IO;
using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// 标题 / 开始页面：游戏启动后的第一屏（独立 Title 场景，构建顺序索引 0）。
    /// 纯 IMGUI 全屏绘制，风格与营地 / 开场一致。提供：继续游戏 / 新游戏 / 读取存档 / 设置 / 退出。
    /// 背景默认用代码绘制（暗色渐变 + 上升余烬 + 暗角），若放入 Resources/Title/Background.png 则铺为底图。
    /// 「继续 / 读取」直接进入 Hub；「新游戏」擦档并清除开场标记，进入 Hub 时自然重播开场动画。
    public class TitleController : MonoBehaviour
    {
        private const string HubScene = "Hub";

        private enum Page { Main, Settings, Load, ConfirmNew, Credits }
        private Page _screen = Page.Main;

        // ── 余烬粒子（代码动画）─────────────────────────────────────────────
        private struct Ember { public float x, y, spd, size, phase; }
        private Ember[] _embers;
        private Texture2D _bg;     // 可选底图：Resources/Title/Background
        private bool _saveExists;

        private GUIStyle _titleStyle, _subStyle, _btnStyle, _smallStyle;

        private void Awake()
        {
            _bg = Resources.Load<Texture2D>("Title/Background");   // 没有就走纯代码背景
            _saveExists = PersistentState.SaveExists;

            _embers = new Ember[46];
            for (int i = 0; i < _embers.Length; i++) _embers[i] = NewEmber(UnityEngine.Random.value);
        }

        private static Ember NewEmber(float y) => new Ember
        {
            x     = UnityEngine.Random.value,
            y     = y,
            spd   = UnityEngine.Random.Range(0.018f, 0.055f),
            size  = UnityEngine.Random.Range(1.5f, 4f),
            phase = UnityEngine.Random.Range(0f, 6.28f),
        };

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _embers.Length; i++)
            {
                _embers[i].y += _embers[i].spd * dt;                 // 向上飘
                _embers[i].x += Mathf.Sin((Time.unscaledTime + _embers[i].phase) * 1.3f) * 0.02f * dt; // 轻微横摆
                if (_embers[i].y > 1.05f) _embers[i] = NewEmber(-0.05f);
            }
        }

        // ── 操作 ─────────────────────────────────────────────────────────────

        /// 继续 / 读取：带着现有存档进入营地。
        private void Continue() => SceneManager.LoadScene(HubScene);

        /// 新游戏：清空存档 + 清除开场标记 → 进营地（首入会自动播放开场）。
        private void NewGame()
        {
            var ps = new PersistentState();
            var db = Resources.Load<HeroDatabase>("Heroes/HeroDatabase");   // 解锁初始英雄，保证有可用角色
            if (db != null && db.heroes != null && db.heroes.Length > 0 &&
                !ps.IsHeroUnlocked(db.heroes[0].heroName))
                ps.UnlockedHeroIds.Add(db.heroes[0].heroName);
            ps.Save();
            IntroController.ClearSeen();
            SceneManager.LoadScene(HubScene);
        }

        private static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            Debug.Log("[Title] Quit（编辑器内为空操作）。");
#endif
        }

        // ── OnGUI ────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            UIFonts.ApplyToSkin();
            EnsureStyles();
            float sw = UnityEngine.Screen.width, sh = UnityEngine.Screen.height;

            DrawBackground(sw, sh);
            DrawTitle(sw, sh);

            switch (_screen)
            {
                case Page.Main:       DrawMainMenu(sw, sh);    break;
                case Page.Settings:   DrawSettings(sw, sh);    break;
                case Page.Load:       DrawLoadPanel(sw, sh);   break;
                case Page.ConfirmNew: DrawConfirmNew(sw, sh);  break;
                case Page.Credits:    DrawCredits(sw, sh);     break;
            }

            GUI.Label(new Rect(12, sh - 22, 360, 18),
                $"v{Application.version}", _smallStyle);

            // 返回键：子面板退回主菜单
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _screen != Page.Main)
            { _screen = Page.Main; e.Use(); }
        }

        // ── 背景：底图 / 渐变 + 余烬 + 暗角 ──────────────────────────────────

        private void DrawBackground(float sw, float sh)
        {
            Fill(new Rect(0, 0, sw, sh), new Color(0.05f, 0.045f, 0.06f));   // 不透明底，避免无相机时残影

            if (_bg != null)
            {
                float scale = Mathf.Max(sw / _bg.width, sh / _bg.height);    // cover 铺满
                float w = _bg.width * scale, h = _bg.height * scale;
                GUI.DrawTexture(new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h), _bg, ScaleMode.ScaleAndCrop);
                Fill(new Rect(0, 0, sw, sh), new Color(0.04f, 0.03f, 0.05f, 0.5f));   // 压暗以衬文字
            }
            else
            {
                // 代码渐变：上深下偏暖（余烬微光）
                Fill(new Rect(0, 0, sw, sh * 0.55f), new Color(0.03f, 0.03f, 0.05f, 0.6f));
                Fill(new Rect(0, sh * 0.45f, sw, sh * 0.55f), new Color(0.12f, 0.05f, 0.02f, 0.5f));
            }

            // 余烬粒子
            for (int i = 0; i < _embers.Length; i++)
            {
                var em = _embers[i];
                float life  = 1f - Mathf.Abs(em.y - 0.5f) * 1.4f;            // 中间最亮
                float flick = 0.6f + 0.4f * Mathf.Sin((Time.unscaledTime + em.phase) * 5f);
                float a = Mathf.Clamp01(life) * flick * 0.9f;
                if (a <= 0.01f) continue;
                float px = em.x * sw, py = (1f - em.y) * sh, s = em.size;
                Fill(new Rect(px - s, py - s, s * 2f, s * 2f), new Color(1f, 0.55f, 0.18f, a * 0.35f)); // 外晕
                Fill(new Rect(px - s * 0.5f, py - s * 0.5f, s, s), new Color(1f, 0.82f, 0.4f, a));      // 核心
            }

            // 暗角（四边压暗）
            float vh = sh * 0.22f, vw = sw * 0.16f;
            Fill(new Rect(0, 0, sw, vh), new Color(0, 0, 0, 0.45f));
            Fill(new Rect(0, sh - vh, sw, vh), new Color(0, 0, 0, 0.55f));
            Fill(new Rect(0, 0, vw, sh), new Color(0, 0, 0, 0.35f));
            Fill(new Rect(sw - vw, 0, vw, sh), new Color(0, 0, 0, 0.35f));
        }

        // ── 标题 ─────────────────────────────────────────────────────────────

        private void DrawTitle(float sw, float sh)
        {
            float ty = sh * 0.16f;
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 1.6f);

            // 阴影 + 主标题（金色脉动）
            var shadow = new Color(0f, 0f, 0f, 0.6f);
            GUI.color = shadow;
            GUI.Label(new Rect(0 + 3, ty + 3, sw, 70), "Embers of the Three Realms", _titleStyle);
            GUI.color = new Color(0.99f, 0.86f * pulse, 0.32f * pulse, 1f);
            GUI.Label(new Rect(0, ty, sw, 70), "Embers of the Three Realms", _titleStyle);
            GUI.color = Color.white;

            GUI.Label(new Rect(0, ty + 96, sw, 22),
                "A nameless ember descends to finish what the fallen began.",
                _smallStyle2);
        }

        // ── 主菜单按钮 ────────────────────────────────────────────────────────

        private void DrawMainMenu(float sw, float sh)
        {
            float bw = 280f, bh = 46f, gap = 12f;
            float bx = (sw - bw) * 0.5f;
            float by = sh * 0.46f;

            int row = 0;
            // 继续游戏（无存档则禁用）
            if (Button(new Rect(bx, by + row++ * (bh + gap), bw, bh),
                    "Continue", _saveExists, accent: true) && _saveExists)
                Continue();

            if (Button(new Rect(bx, by + row++ * (bh + gap), bw, bh),
                    "New Game", true))
            { if (_saveExists) _screen = Page.ConfirmNew; else NewGame(); }

            if (Button(new Rect(bx, by + row++ * (bh + gap), bw, bh),
                    "Load Save", true))
                _screen = Page.Load;

            if (Button(new Rect(bx, by + row++ * (bh + gap), bw, bh),
                    "Settings", true))
                _screen = Page.Settings;

            if (Button(new Rect(bx, by + row++ * (bh + gap), bw, bh),
                    "Credits", true))
                _screen = Page.Credits;

            if (Button(new Rect(bx, by + row++ * (bh + gap), bw, bh),
                    "Quit", true))
                Quit();

            // 回车 = 继续（有存档时）
            var e = Event.current;
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && _saveExists)
            { Continue(); e.Use(); }
        }

        // ── 读取存档面板 ──────────────────────────────────────────────────────

        private void DrawLoadPanel(float sw, float sh)
        {
            float pw = Mathf.Min(440f, sw - 60f), ph = 300f;
            float px = (sw - pw) * 0.5f, py = sh * 0.42f;
            Panel(px, py, pw, ph, "Load Save");

            if (!_saveExists)
            {
                GUI.Label(new Rect(px, py + ph * 0.5f - 14, pw, 28), "No save data",
                    Center(14, new Color(0.7f, 0.6f, 0.55f)));
            }
            else
            {
                var ps = PersistentState.Load();
                string when = "—";
                try { when = File.GetLastWriteTime(PersistentState.SaveFilePath).ToString("yyyy-MM-dd HH:mm"); }
                catch { }

                var rows = new (string, string)[]
                {
                    ("Deepest Floor", ps.BestFloor > 0 ? $"Floor {ps.BestFloor}" : "—"),
                    ("Victories",     ps.TotalVictories.ToString()),
                    ("Total Runs",    ps.TotalRuns.ToString()),
                    ("Currency",      $"{ps.UnlockCurrency} ◈"),
                    ("Heroes",        ps.UnlockedHeroIds.Count.ToString()),
                    ("Last Saved",    when),
                };
                float ry = py + 48f;
                for (int i = 0; i < rows.Length; i++)
                {
                    Fill(new Rect(px + 18, ry, pw - 36, 30),
                        i % 2 == 0 ? new Color(0.13f, 0.14f, 0.2f, 0.7f) : new Color(0.1f, 0.1f, 0.15f, 0.7f));
                    GUI.Label(new Rect(px + 30, ry, pw - 60, 30), rows[i].Item1,
                        Left(12, new Color(0.72f, 0.72f, 0.8f)));
                    GUI.Label(new Rect(px + 30, ry, pw - 60, 30), rows[i].Item2,
                        Right(13, new Color(0.95f, 0.95f, 1f)));
                    ry += 32f;
                }
            }

            float byy = py + ph - 56f, half = (pw - 48f) * 0.5f;
            if (Button(new Rect(px + 18, byy, half, 38), "Load", _saveExists, accent: true) && _saveExists)
                Continue();
            if (Button(new Rect(px + 30 + half, byy, half, 38), "Back", true))
                _screen = Page.Main;
        }

        // ── 新游戏确认 ────────────────────────────────────────────────────────

        private void DrawConfirmNew(float sw, float sh)
        {
            float pw = Mathf.Min(420f, sw - 60f), ph = 180f;
            float px = (sw - pw) * 0.5f, py = sh * 0.46f;
            Panel(px, py, pw, ph, "Start a New Game?");

            var warn = Center(13, new Color(0.95f, 0.7f, 0.55f));
            warn.wordWrap = true;
            GUI.Label(new Rect(px + 24, py + 50, pw - 48, 56),
                "This erases your current save and starts over.", warn);

            float byy = py + ph - 56f, half = (pw - 48f) * 0.5f;
            if (Button(new Rect(px + 18, byy, half, 38), "Confirm", true, danger: true))
                NewGame();
            if (Button(new Rect(px + 30 + half, byy, half, 38), "Back", true))
                _screen = Page.Main;
        }

        // ── 设置面板 ──────────────────────────────────────────────────────────

        private void DrawSettings(float sw, float sh)
        {
            float pw = Mathf.Min(440f, sw - 60f), ph = 220f;
            float px = (sw - pw) * 0.5f, py = sh * 0.44f;
            Panel(px, py, pw, ph, "Settings");

            float fy = py + 52f;
            GUI.Label(new Rect(px + 24, fy, 180, 26), "Master Volume", Left(13, new Color(0.82f, 0.82f, 0.88f)));
            float vol = AudioListener.volume;
            float nv  = GUI.HorizontalSlider(new Rect(px + 200, fy + 8, pw - 270, 12), vol, 0f, 1f);
            if (!Mathf.Approximately(nv, vol)) AudioListener.volume = nv;
            GUI.Label(new Rect(px + pw - 60, fy, 44, 26), $"{nv * 100:0}%", Right(12, new Color(0.65f, 0.65f, 0.72f)));

            fy += 40f;
            GUI.Label(new Rect(px + 24, fy, 180, 26), "Fullscreen", Left(13, new Color(0.82f, 0.82f, 0.88f)));
            bool fs = UnityEngine.Screen.fullScreen;
            bool nf = GUI.Toggle(new Rect(px + 200, fy + 4, 18, 18), fs, "");
            if (nf != fs) UnityEngine.Screen.fullScreen = nf;
            GUI.Label(new Rect(px + 224, fy, 80, 26), fs ? "On" : "Off", Left(12, new Color(0.6f, 0.6f, 0.66f)));

            fy += 40f;
            GUI.Label(new Rect(px + 24, fy, pw - 48, 26),
                $"Resolution  {UnityEngine.Screen.width} × {UnityEngine.Screen.height}",
                Left(12, new Color(0.6f, 0.6f, 0.66f)));

            if (Button(new Rect(px + (pw - 160) * 0.5f, py + ph - 50f, 160, 38), "Back", true))
                _screen = Page.Main;
        }

        // ── 制作名单 / Credits ─────────────────────────────────────────────────
        // 玩家可见的素材署名页，满足 LPC (CC-BY-SA) 等在使用处给出署名的义务。
        // 完整许可证与逐源说明见仓库根目录 ATTRIBUTIONS.md。

        private void DrawCredits(float sw, float sh)
        {
            float pw = Mathf.Min(560f, sw - 60f);
            float ph = Mathf.Min(sh - 110f, 438f);
            float px = (sw - pw) * 0.5f;
            float py = Mathf.Max(70f, sh * 0.30f - 10f);
            Panel(px, py, pw, ph, "Credits");

            var head = Left(13, new Color(0.98f, 0.88f, 0.4f));
            var line = Left(11, new Color(0.78f, 0.78f, 0.84f));
            var dim  = Left(10, new Color(0.55f, 0.55f, 0.62f));
            float x = px + 26f, w = pw - 52f, y = py + 50f;

            void H(string s) { GUI.Label(new Rect(x, y, w, 18), s, head); y += 22f; }
            void L(string s) { GUI.Label(new Rect(x + 10, y, w - 10, 16), s, line); y += 17f; }
            void D(string s) { GUI.Label(new Rect(x + 10, y, w - 10, 16), s, dim);  y += 16f; }

            H("Game Design & Programming");
            L("1Misery1    ·    Engine: Unity 2022.3 LTS");
            y += 6f;

            H("Art assets (third-party)");
            L("0x72 — DungeonTileset II  (CC0)");
            L("Kenney — Roguelike / Characters / Micro  (CC0)");
            L("LPC — Johannes Sjölund (wulax); base by Stephen");
            D("          Challener (Redshrike)  ·  CC-BY-SA 3.0");
            L("CraftPix.net — free game assets");
            L("Dungeon Crawl Stone Soup — David E. Gervais");
            y += 6f;

            H("Font & Audio");
            L("Ark Pixel font — TakWolf  (SIL OFL 1.1)");
            L("Audio — Kenney, JaggedStone, yd, MintoDog  (CC0)");
            y += 8f;

            D("Full licenses & attribution: ATTRIBUTIONS.md");
            D("LPC-derived sprites are licensed CC-BY-SA 3.0.");

            if (Button(new Rect(px + (pw - 160) * 0.5f, py + ph - 50f, 160, 38), "Back", true))
                _screen = Page.Main;
        }

        // ── 控件 / 绘制工具 ───────────────────────────────────────────────────

        private bool Button(Rect r, string label, bool enabled, bool accent = false, bool danger = false)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Color bg = !enabled ? new Color(0.1f, 0.1f, 0.14f, 0.8f)
                     : danger   ? (hover ? new Color(0.55f, 0.16f, 0.16f) : new Color(0.32f, 0.1f, 0.1f))
                     : accent   ? (hover ? new Color(0.5f, 0.34f, 0.12f) : new Color(0.28f, 0.2f, 0.07f))
                     :            (hover ? new Color(0.2f, 0.22f, 0.3f) : new Color(0.13f, 0.14f, 0.2f));
            Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0, 0, 0, 0.3f));
            Fill(r, bg);
            Color edge = !enabled ? new Color(0.25f, 0.25f, 0.3f, 0.4f)
                       : danger   ? new Color(0.9f, 0.4f, 0.35f, 0.8f)
                       : accent   ? new Color(0.95f, 0.7f, 0.3f, 0.85f)
                       :            new Color(0.5f, 0.6f, 0.85f, 0.7f);
            Fill(new Rect(r.x, r.y, r.width, 2), edge);
            Fill(new Rect(r.x, r.yMax - 2, r.width, 2), edge);

            Color tc = !enabled ? new Color(0.45f, 0.45f, 0.5f)
                     : accent   ? new Color(1f, 0.9f, 0.6f)
                     : danger   ? new Color(1f, 0.85f, 0.8f)
                     :            new Color(0.9f, 0.92f, 0.98f);
            _btnStyle.normal.textColor = tc;
            GUI.Label(r, label, _btnStyle);

            GUI.enabled = enabled;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked && enabled;
        }

        private void Panel(float px, float py, float pw, float ph, string title)
        {
            Fill(new Rect(px - 6, py - 6, pw + 12, ph + 12), new Color(0, 0, 0, 0.55f));
            Fill(new Rect(px, py, pw, ph), new Color(0.09f, 0.09f, 0.13f, 0.97f));
            Fill(new Rect(px, py, pw, 2), new Color(0.95f, 0.7f, 0.3f, 0.7f));
            GUI.Label(new Rect(px, py + 12, pw, 26), title, Center(17, new Color(0.98f, 0.88f, 0.4f)));
            Fill(new Rect(px + 18, py + 40, pw - 36, 1), new Color(0.3f, 0.3f, 0.4f));
        }

        // ── 样式 ─────────────────────────────────────────────────────────────

        private GUIStyle _smallStyle2;
        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            int big = Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Screen.height * 0.058f), 30, 56);
            _titleStyle = new GUIStyle(GUI.skin.label)
            { fontSize = big, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _subStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _subStyle.normal.textColor = new Color(0.85f, 0.82f, 0.7f);
            _btnStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _smallStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            _smallStyle.normal.textColor = new Color(0.5f, 0.5f, 0.58f);
            _smallStyle2 = new GUIStyle(GUI.skin.label)
            { fontSize = 12, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            _smallStyle2.normal.textColor = new Color(0.62f, 0.6f, 0.66f);
        }

        private static GUIStyle Mk(int size, TextAnchor a, Color c)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = a };
            s.normal.textColor = c; return s;
        }
        private static GUIStyle Left(int s, Color c)   => Mk(s, TextAnchor.MiddleLeft, c);
        private static GUIStyle Right(int s, Color c)  => Mk(s, TextAnchor.MiddleRight, c);
        private static GUIStyle Center(int s, Color c) => Mk(s, TextAnchor.MiddleCenter, c);

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
    }
}
