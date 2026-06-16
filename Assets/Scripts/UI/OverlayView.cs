using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// Full-screen overlays: floor-complete, talent replacement, victory/death summary, and the ending cutscene. Driven by GameBootstrap per game state.
    public class OverlayView : MonoBehaviour
    {
        private Canvas _canvas;

        // FloorComplete
        private GameObject _fcPanel;
        private Text   _fcTitle, _fcReward, _fcDiff, _fcHp, _fcHealLabel, _fcAdvanceLabel;
        private Button _fcHeal;
        private Action _onHeal, _onAdvance, _onMenu;

        // 天赋替换弹窗(满槽时选择替换/丢弃)
        private GameObject _trPanel, _trContent;

        // 结算屏(胜利/死亡)
        private GameObject _esPanel, _esContent;
        private Text       _esCountdown;

        // 结局过场(逐帧 CG + 逐字字幕)
        private GameObject _csPanel;
        private Image      _csCG, _csTitleBg, _csCapBg, _csCapBorder;
        private Text       _csTitle, _csCaption, _csHint;
        private readonly List<Image> _csDots = new List<Image>();

        private void Awake()
        {
            _canvas = UIFactory.CreateOverlayCanvas("OverlayCanvas", sortingOrder: 700);
            _canvas.transform.SetParent(transform, false);
            BuildFloorComplete(_canvas.transform);
        }

        // ── FloorComplete ─────────────────────────────────────────────────────
        public void SetFloorCompleteCallbacks(Action onHeal, Action onAdvance, Action onMenu)
        {
            _onHeal = onHeal; _onAdvance = onAdvance; _onMenu = onMenu;
        }

        public void SetFloorCompleteVisible(bool on)
        {
            if (_fcPanel != null) _fcPanel.SetActive(on);
        }

        public void RefreshFloorComplete(string floorName, int floor, int reward, int coins,
            float hpCur, float hpMax, float hpRatio, int healCost, bool canHeal, bool showHp)
        {
            _fcTitle.text  = $"Floor {floor}  ·  {floorName}  Cleared!";
            _fcReward.text = $"Gained  +{reward}  unlock currency    Coins: {coins}";
            _fcDiff.text   = $"Next floor difficulty: ×{1f + floor * 0.25f:0.00}  (enemy HP & ATK scale up)";

            _fcHp.gameObject.SetActive(showHp);
            _fcHeal.gameObject.SetActive(showHp);
            if (showHp)
            {
                Color hc = hpRatio > 0.5f ? new Color(0.25f, 0.95f, 0.4f)
                         : hpRatio > 0.25f ? new Color(1f, 0.85f, 0.12f)
                                           : new Color(1f, 0.3f, 0.3f);
                _fcHp.text  = $"HP: {Mathf.CeilToInt(hpCur)} / {Mathf.CeilToInt(hpMax)}  ({hpRatio * 100:0}%)";
                _fcHp.color = hc;
                _fcHealLabel.text    = $"Spend {healCost} coins  restore 50% HP";
                _fcHeal.interactable = canHeal;
            }
            _fcAdvanceLabel.text = $"Enter Floor {floor + 1}  ▶";
        }

        private void BuildFloorComplete(Transform root)
        {
            var panel = UIFactory.Image("FloorComplete", root, new Color(0.04f, 0.06f, 0.04f, 0.92f), raycast: true);
            UIFactory.Stretch(panel.rectTransform);
            _fcPanel = panel.gameObject;
            var t = panel.transform;

            var accent = UIFactory.Image("Accent", t, new Color(0.3f, 0.95f, 0.45f, 0.7f));
            Row(accent.rectTransform, 0.12f, 3f, stretchWidthPad: 0.1f);

            _fcTitle  = CenteredLabel(t, "Title",  "", 28, new Color(0.35f, 1f, 0.5f),   FontStyle.Bold,   0.16f);
            _fcReward = CenteredLabel(t, "Reward", "", 16, Color.white,                   FontStyle.Normal, 0.27f);
            _fcDiff   = CenteredLabel(t, "Diff",   "", 14, new Color(1f, 0.78f, 0.45f),   FontStyle.Normal, 0.34f);
            _fcHp     = CenteredLabel(t, "Hp",     "", 15, Color.white,                   FontStyle.Normal, 0.41f);

            _fcHeal = MakeButton(t, "Heal",    0.49f, () => _onHeal?.Invoke(),    out _fcHealLabel);
            var adv = MakeButton(t, "Advance", 0.58f, () => _onAdvance?.Invoke(), out _fcAdvanceLabel);
            adv.GetComponent<Image>().color = new Color(0.14f, 0.3f, 0.18f, 0.95f);
            var menu = MakeButton(t, "Menu",   0.67f, () => _onMenu?.Invoke(),    out var menuLabel);
            menuLabel.text = "Back to Menu";

            _fcPanel.SetActive(false);
        }

        // ── 天赋替换弹窗 ──────────────────────────────────────────────────────
        public void ShowTalentReplacement(string newName, string newDesc, List<string> replaceLabels,
                                          Action<int> onReplace, Action onCancel)
        {
            if (_trPanel == null)
            {
                var dim = UIFactory.Image("TalentReplace", _canvas.transform, new Color(0f, 0f, 0f, 0.75f), raycast: true);
                UIFactory.Stretch(dim.rectTransform);
                _trPanel = dim.gameObject;
            }
            if (_trContent != null) Destroy(_trContent);
            var content = UIFactory.Rect("Content", _trPanel.transform);
            UIFactory.Stretch(content);
            _trContent = content.gameObject;

            var title = UIFactory.Label("Title", content, $"Talent slots full  ·  New: {newName}", 24,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Color(1f, 0.88f, 0.22f), wrap: true);
            Row(title.rectTransform, 0.30f, 40f, 0.08f);
            var desc = UIFactory.Label("Desc", content, newDesc, 15, TextAnchor.MiddleCenter,
                FontStyle.Normal, new Color(0.88f, 0.88f, 0.88f), wrap: true);
            Row(desc.rectTransform, 0.36f, 30f, 0.12f);
            var hint = UIFactory.Label("Hint", content, "Choose a talent to replace, or cancel to discard the new one",
                14, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f), wrap: true);
            Row(hint.rectTransform, 0.41f, 24f, 0.1f);

            float y = 0.48f;
            for (int i = 0; i < replaceLabels.Count; i++)
            {
                int idx = i;
                var b = MakeButton(content, $"Replace{i}", y, () => onReplace(idx), out var lbl);
                ((RectTransform)b.transform).sizeDelta = new Vector2(460f, 46f);
                lbl.text = replaceLabels[i];
                y += 0.085f;
            }
            var cancel = MakeButton(content, "Cancel", y + 0.03f, () => onCancel(), out var clbl);
            ((RectTransform)cancel.transform).sizeDelta = new Vector2(460f, 46f);
            clbl.text = "Cancel  (discard new talent)";

            _trPanel.SetActive(true);
        }

        public void HideTalentReplacement() { if (_trPanel != null) _trPanel.SetActive(false); }

        // ── 结算屏(胜利/死亡)──────────────────────────────────────────────────
        // leftRecap/rightRecap 为富文本(GameBootstrap 预先拼好,含颜色);死亡时传 null。
        public void ShowEndScreen(string title, Color titleColor, bool victory,
            string subtitle, string clearedLine, string footnote, string statsBlock,
            string leftRecap, string rightRecap,
            Action onTryAgain, Action onMenu, Action onReturnNow)
        {
            if (_esPanel == null)
            {
                var dim = UIFactory.Image("EndScreen", _canvas.transform, new Color(0f, 0f, 0f, 0.78f), raycast: true);
                UIFactory.Stretch(dim.rectTransform);
                _esPanel = dim.gameObject;
            }
            if (_esContent != null) Destroy(_esContent);
            var content = UIFactory.Rect("Content", _esPanel.transform);
            UIFactory.Stretch(content);
            _esContent   = content.gameObject;
            _esCountdown = null;

            var t = UIFactory.Label("Title", content, title, 52, TextAnchor.MiddleCenter, FontStyle.Bold, titleColor);
            Row(t.rectTransform, 0.13f, 76f, 0.02f);

            if (victory)
            {
                if (!string.IsNullOrEmpty(subtitle))
                    Row(UIFactory.Label("Sub", content, subtitle, 20, TextAnchor.MiddleCenter, FontStyle.Italic,
                        new Color(0.92f, 0.86f, 0.7f), wrap: true).rectTransform, 0.215f, 30f, 0.08f);
                Row(UIFactory.Label("Cleared", content, clearedLine, 18, TextAnchor.MiddleCenter, FontStyle.Normal,
                        Color.white, wrap: true).rectTransform, 0.265f, 28f, 0.08f);
                if (!string.IsNullOrEmpty(footnote))
                    Row(UIFactory.Label("Foot", content, footnote, 16, TextAnchor.MiddleCenter, FontStyle.Normal,
                        new Color(0.78f, 0.78f, 0.85f), wrap: true).rectTransform, 0.305f, 26f, 0.1f);
            }
            else
            {
                _esCountdown = UIFactory.Label("Countdown", content, "", 15, TextAnchor.MiddleCenter, FontStyle.Italic,
                    new Color(0.75f, 0.55f, 0.55f));
                Row(_esCountdown.rectTransform, 0.27f, 26f, 0.1f);
            }

            var stats = UIFactory.Label("Stats", content, statsBlock, 15, TextAnchor.UpperCenter, FontStyle.Normal,
                new Color(0.82f, 0.82f, 0.82f), wrap: true);
            stats.supportRichText = true;
            Row(stats.rectTransform, victory ? 0.39f : 0.34f, 54f, 0.1f);

            if (victory)
            {
                Block(content, "LeftHdr",  "── Your Choices ──",    16, TextAnchor.MiddleCenter, 0.10f, 0.46f, 0.44f, 0.47f, new Color(0.95f, 0.86f, 0.55f), bold: true);
                Block(content, "LeftBody", leftRecap,               13, TextAnchor.UpperLeft,     0.11f, 0.46f, 0.48f, 0.80f, new Color(0.85f, 0.85f, 0.85f));
                Block(content, "RightHdr", "── Your Collection ──", 16, TextAnchor.MiddleCenter, 0.54f, 0.90f, 0.44f, 0.47f, new Color(0.95f, 0.86f, 0.55f), bold: true);
                Block(content, "RightBody",rightRecap,              13, TextAnchor.UpperLeft,     0.54f, 0.89f, 0.48f, 0.80f, new Color(0.85f, 0.85f, 0.85f));
            }

            if (victory)
            {
                var b1 = MakeButton(content, "TryAgain", 0.83f, () => onTryAgain?.Invoke(), out var l1); l1.text = "Try Again";
                var r1 = (RectTransform)b1.transform; r1.sizeDelta = new Vector2(280f, 46f); r1.anchoredPosition = new Vector2(-150f, 0f);
                var b2 = MakeButton(content, "Menu", 0.83f, () => onMenu?.Invoke(), out var l2); l2.text = "Back to Menu";
                var r2 = (RectTransform)b2.transform; r2.sizeDelta = new Vector2(280f, 46f); r2.anchoredPosition = new Vector2(150f, 0f);
            }
            else
            {
                var b = MakeButton(content, "ReturnNow", 0.6f, () => onReturnNow?.Invoke(), out var l); l.text = "Return to Menu Now";
            }

            _esPanel.SetActive(true);
        }

        public void SetEndScreenCountdown(string text) { if (_esCountdown != null) _esCountdown.text = text; }
        public void HideEndScreen() { if (_esPanel != null) _esPanel.SetActive(false); }

        // ── 结局过场 ──────────────────────────────────────────────────────────
        public void SetCutsceneVisible(bool on)
        {
            if (on && _csPanel == null) BuildCutscene();
            if (_csPanel != null) _csPanel.SetActive(on);
        }

        public void RefreshCutscene(Sprite cg, float alpha, string title, Color tint,
            string caption, int frameCount, int currentFrame, string hint, bool showHint)
        {
            if (_csPanel == null) return;

            _csCG.enabled = cg != null;
            if (cg != null) { _csCG.sprite = cg; _csCG.color = new Color(1f, 1f, 1f, alpha); }

            _csTitleBg.color = new Color(0f, 0f, 0f, 0.34f * alpha);
            _csTitle.text    = title;
            _csTitle.color   = new Color(tint.r, tint.g, tint.b, alpha);

            _csCapBg.color     = new Color(0f, 0f, 0f, 0.62f * alpha);
            _csCapBorder.color = new Color(0.85f, 0.7f, 0.35f, 0.55f * alpha);
            _csCaption.text    = caption;
            _csCaption.color   = new Color(0.96f, 0.93f, 0.85f, alpha);

            for (int i = 0; i < _csDots.Count; i++)
            {
                bool active = i < frameCount && frameCount > 1;
                _csDots[i].gameObject.SetActive(active);
                if (!active) continue;
                Color c = i == currentFrame ? new Color(1f, 0.85f, 0.4f, 0.95f)
                        : i <  currentFrame ? new Color(0.7f, 0.7f, 0.78f, 0.7f)
                                            : new Color(0.4f, 0.4f, 0.48f, 0.5f);
                _csDots[i].color = new Color(c.r, c.g, c.b, c.a * alpha);
            }

            _csHint.gameObject.SetActive(showHint);
            if (showHint)
            {
                _csHint.text = hint;
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                _csHint.color = new Color(0.8f, 0.85f, 1f, (0.35f + 0.55f * blink) * alpha);
            }
        }

        private void BuildCutscene()
        {
            var panel = UIFactory.Image("Cutscene", _canvas.transform, Color.black, raycast: true);
            UIFactory.Stretch(panel.rectTransform);
            _csPanel = panel.gameObject;
            var t = panel.transform;

            _csCG = UIFactory.Image("CG", t, new Color(1f, 1f, 1f, 0f));
            UIFactory.Stretch(_csCG.rectTransform);   // 铺满(近似 ScaleAndCrop)

            _csTitleBg = UIFactory.Image("TitleBg", t, new Color(0f, 0f, 0f, 0f));
            var tbg = _csTitleBg.rectTransform;
            tbg.anchorMin = new Vector2(0f, 0.44f); tbg.anchorMax = new Vector2(1f, 0.60f);
            tbg.offsetMin = Vector2.zero; tbg.offsetMax = Vector2.zero;

            _csTitle = UIFactory.Label("Title", t, "", 28, TextAnchor.MiddleCenter, FontStyle.Italic, Color.white, wrap: true);
            Row(_csTitle.rectTransform, 0.45f, 60f, 0.05f);

            _csCapBg = UIFactory.Image("CapBg", t, new Color(0f, 0f, 0f, 0f));
            var cbg = _csCapBg.rectTransform;
            cbg.anchorMin = new Vector2(0f, 0f); cbg.anchorMax = new Vector2(1f, 0.30f);
            cbg.offsetMin = Vector2.zero; cbg.offsetMax = Vector2.zero;

            _csCapBorder = UIFactory.Image("CapBorder", t, new Color(0f, 0f, 0f, 0f));
            var cb = _csCapBorder.rectTransform;
            cb.anchorMin = new Vector2(0f, 0.30f); cb.anchorMax = new Vector2(1f, 0.30f);
            cb.pivot = new Vector2(0.5f, 1f); cb.sizeDelta = new Vector2(0f, 2f); cb.anchoredPosition = Vector2.zero;

            _csCaption = UIFactory.Label("Caption", _csCapBg.transform, "", 22, TextAnchor.UpperCenter, FontStyle.Normal, Color.white, wrap: true);
            UIFactory.Stretch(_csCaption.rectTransform, left: 80f, right: 80f, top: 18f, bottom: 30f);

            var dots = UIFactory.Rect("Dots", t);
            dots.anchorMin = dots.anchorMax = new Vector2(0.5f, 0f); dots.pivot = new Vector2(0.5f, 0f);
            dots.anchoredPosition = new Vector2(0f, 8f); dots.sizeDelta = new Vector2(300f, 12f);
            var hlg = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            for (int i = 0; i < 10; i++)
            {
                var d = UIFactory.Image($"Dot{i}", dots, Color.white);
                d.rectTransform.sizeDelta = new Vector2(7f, 7f);
                d.gameObject.SetActive(false);
                _csDots.Add(d);
            }

            _csHint = UIFactory.Label("Hint", t, "", 13, TextAnchor.MiddleRight, FontStyle.Normal, Color.white);
            var hr = _csHint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f); hr.pivot = new Vector2(1f, 0f);
            hr.sizeDelta = new Vector2(300f, 22f); hr.anchoredPosition = new Vector2(-20f, 12f);

            _csPanel.SetActive(false);
        }

        // ── 摆放工具 ──────────────────────────────────────────────────────────
        private static Text CenteredLabel(Transform parent, string name, string text, int size,
                                          Color color, FontStyle style, float yFracFromTop)
        {
            var l = UIFactory.Label(name, parent, text, size, TextAnchor.MiddleCenter, style, color, wrap: true);
            Row(l.rectTransform, yFracFromTop, size * 2.2f);
            return l;
        }

        private Button MakeButton(Transform parent, string name, float yFracFromTop, Action onClick, out Text label)
        {
            var btn = UIFactory.Button(name, parent, onClick, new Color(0.16f, 0.16f, 0.2f, 0.95f));
            var rt  = (RectTransform)btn.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f - yFracFromTop);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 46f);
            rt.anchoredPosition = Vector2.zero;
            btn.colors = new ColorBlock
            {
                normalColor      = new Color(0.16f, 0.16f, 0.2f),
                highlightedColor = new Color(0.28f, 0.28f, 0.36f),
                pressedColor     = new Color(0.32f, 0.32f, 0.42f),
                selectedColor    = new Color(0.28f, 0.28f, 0.36f),
                disabledColor    = new Color(0.10f, 0.10f, 0.12f),
                colorMultiplier  = 1f,
                fadeDuration     = 0.08f,
            };
            label = UIFactory.Label("Label", rt, "", 16, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            UIFactory.Stretch(label.rectTransform);
            return btn;
        }

        // 横排:在面板内某个「自顶向下比例」处水平居中放一行(可全宽拉伸或留边)。
        private static void Row(RectTransform rt, float yFracFromTop, float height, float stretchWidthPad = 0f)
        {
            rt.anchorMin = new Vector2(stretchWidthPad, 1f - yFracFromTop);
            rt.anchorMax = new Vector2(1f - stretchWidthPad, 1f - yFracFromTop);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        // 矩形文本块(按比例锚定 x[min,max] / y(自顶)[top,bot]),支持富文本。
        private static Text Block(Transform parent, string name, string text, int size, TextAnchor anchor,
            float xMin, float xMax, float yTop, float yBot, Color color, bool bold = false)
        {
            var t = UIFactory.Label(name, parent, text, size, anchor, bold ? FontStyle.Bold : FontStyle.Normal, color, wrap: true);
            t.supportRichText = true;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(xMin, 1f - yBot);
            rt.anchorMax = new Vector2(xMax, 1f - yTop);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return t;
        }
    }
}
