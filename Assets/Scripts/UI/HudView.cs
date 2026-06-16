using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// Combat HUD. GameBootstrap pushes data each frame via Set*; the top-left column
    /// auto-stacks via layout groups, and chip lists rebuild only when their content changes.
    public class HudView : MonoBehaviour
    {
        /// 左侧栏一行：左竖条 + 标题 + 副标题。
        public struct Chip { public Color color; public string title; public string subtitle; }
        /// 武器槽快照（文本由 GameBootstrap 预先算好，HUD 不碰武器内部逻辑）。
        public struct WeaponSlot
        {
            public bool   occupied;
            public bool   active;
            public Sprite icon;
            public Color  color;
            public string line1;
            public string line2;
        }

        private Canvas _canvas;

        // 顶栏
        private Text _topInfo;
        // Boss 血条
        private GameObject _bossGroup; private Text _bossName, _bossHpText; private Image _bossFill;
        // 玩家血条
        private Image _hpFill; private Text _hpText;
        // 金币 / 污染
        private Text _goldText;
        private GameObject _corrGroup; private Text _corrTier, _corrValue; private Image _corrFill;
        // 英雄技能
        private GameObject _heroSkillGroup; private Image _heroSkillFill; private Text _heroSkillText;
        // 武器面板
        private GameObject _weaponPanel;
        private SlotWidgets[] _slots;
        private GameObject _wSkillGroup; private Image _wSkillFill; private Text _wSkillText;
        // 左侧栏
        private Transform _talentsPanel, _itemsPanel, _synergiesPanel;
        private string _talentsSig, _itemsSig, _synergiesSig;
        // 横幅
        private GameObject _bannerGroup; private Text _bannerText;

        private class SlotWidgets
        {
            public GameObject highlight, accent;
            public Image icon;
            public Text  line1, line2;
        }

        private void Awake() => Build();

        public void SetVisible(bool on)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(on);
        }

        // ── 构建（一次性）────────────────────────────────────────────────────
        private void Build()
        {
            _canvas = UIFactory.CreateOverlayCanvas("HudCanvas", sortingOrder: 100);
            _canvas.transform.SetParent(transform, false);
            var root = _canvas.transform;

            BuildTopBar(root);
            BuildBossBar(root);
            BuildHpBar(root);
            BuildGoldCorruption(root);
            BuildHeroSkill(root);
            BuildWeaponPanel(root);
            BuildLeftColumn(root);
            BuildBanner(root);
        }

        private void BuildTopBar(Transform root)
        {
            var bar = UIFactory.Image("TopBar", root, new Color(0f, 0f, 0f, 0.62f));
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, 26f);
            rt.anchoredPosition = Vector2.zero;

            _topInfo = UIFactory.Label("Info", bar.transform, "", 13, TextAnchor.MiddleLeft,
                FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f));
            UIFactory.Stretch(_topInfo.rectTransform, left: 12f);

            var hint = UIFactory.Label("Hint", bar.transform,
                "WASD Move  Space/LMB Attack  R/RMB Skill  F Hero Skill  Q Swap Weapon  E Interact",
                11, TextAnchor.MiddleRight, FontStyle.Normal, new Color(0.52f, 0.52f, 0.58f));
            UIFactory.Stretch(hint.rectTransform, right: 12f);
        }

        private void BuildBossBar(Transform root)
        {
            var g = UIFactory.Rect("BossBar", root);
            g.anchorMin = g.anchorMax = new Vector2(0.5f, 1f); g.pivot = new Vector2(0.5f, 1f);
            g.sizeDelta = new Vector2(620f, 56f); g.anchoredPosition = new Vector2(0f, -34f);   // 下移,避开顶栏
            _bossGroup = g.gameObject;

            var bg = UIFactory.Image("Bg", g, new Color(0f, 0f, 0f, 0.72f));
            UIFactory.Stretch(bg.rectTransform);
            var top = UIFactory.Image("Accent", g, new Color(0.8f, 0.2f, 0.2f, 0.7f));
            Top(top.rectTransform, 0f, 0f, 620f, 2f);

            _bossName = UIFactory.Label("Name", g, "BOSS", 14, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(1f, 0.35f, 0.35f));
            Top(_bossName.rectTransform, 10f, 6f, 600f, 18f);

            _bossFill = Bar(g, "Bar", new Color(0.2f, 0.06f, 0.06f), new Color(0.85f, 0.2f, 0.12f),
                out var barHolder);
            Top(barHolder, 10f, 28f, 600f, 20f);

            _bossHpText = UIFactory.Label("Hp", barHolder.parent, "", 11, TextAnchor.MiddleCenter,
                FontStyle.Normal, Color.white);
            Top(_bossHpText.rectTransform, 10f, 28f, 600f, 20f);

            _bossGroup.SetActive(false);
        }

        private void BuildHpBar(Transform root)
        {
            _hpFill = Bar(root, "HpBar", new Color(0.16f, 0.05f, 0.05f),
                new Color(0.18f, 0.82f, 0.28f), out var holder);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(420f, 28f); holder.anchoredPosition = new Vector2(0f, 22f);
            AddOutline(holder.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.8f));

            _hpText = UIFactory.Label("Hp", holder, "", 13, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            UIFactory.Stretch(_hpText.rectTransform);
        }

        private void BuildGoldCorruption(Transform root)
        {
            // 金币：血条左侧（与血条底对齐）
            var gold = UIFactory.Image("Gold", root, new Color(0f, 0f, 0f, 0.65f));
            var grt = gold.rectTransform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0f); grt.pivot = new Vector2(1f, 0f);
            grt.sizeDelta = new Vector2(150f, 30f); grt.anchoredPosition = new Vector2(-220f, 21f);
            _goldText = UIFactory.Label("Txt", gold.transform, "◈  0", 16, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(1f, 0.88f, 0.2f));
            UIFactory.Stretch(_goldText.rectTransform);

            // 污染：堆在金币上方
            var corr = UIFactory.Image("Corruption", root, new Color(0f, 0f, 0f, 0.65f));
            var crt = corr.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f); crt.pivot = new Vector2(1f, 0f);
            crt.sizeDelta = new Vector2(150f, 30f); crt.anchoredPosition = new Vector2(-220f, 57f);
            _corrGroup = corr.gameObject;

            _corrTier = UIFactory.Label("Tier", corr.transform, "", 13, TextAnchor.MiddleLeft, FontStyle.Bold);
            Top(_corrTier.rectTransform, 6f, 0f, 72f, 30f);
            _corrValue = UIFactory.Label("Val", corr.transform, "", 11, TextAnchor.MiddleCenter, FontStyle.Normal);
            Top(_corrValue.rectTransform, 78f, 2f, 66f, 14f);
            _corrFill = Bar(corr.transform, "Bar", new Color(1f, 1f, 1f, 0.10f), Color.white, out var cbar);
            Top(cbar, 78f, 20f, 66f, 5f);

            _corrGroup.SetActive(false);
        }

        private void BuildHeroSkill(Transform root)
        {
            _heroSkillFill = Bar(root, "HeroSkill", new Color(0.16f, 0.14f, 0.08f),
                new Color(0.95f, 0.78f, 0.12f), out var holder);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(260f, 26f); holder.anchoredPosition = new Vector2(0f, 58f);
            AddOutline(holder.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.72f));
            _heroSkillGroup = holder.gameObject;

            _heroSkillText = UIFactory.Label("Txt", holder, "", 13, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            UIFactory.Stretch(_heroSkillText.rectTransform);

            _heroSkillGroup.SetActive(false);
        }

        private void BuildWeaponPanel(Transform root)
        {
            const float PW = 190f;   // 贴底放在血条右侧
            var p = UIFactory.Image("WeaponPanel", root, new Color(0f, 0f, 0f, 0.72f));
            var prt = p.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(1f, 0f); prt.pivot = new Vector2(1f, 0f);
            prt.sizeDelta = new Vector2(PW, 46f); prt.anchoredPosition = new Vector2(-8f, 8f);
            _weaponPanel = p.gameObject;

            var top = UIFactory.Image("Accent", p.transform, new Color(0.5f, 0.5f, 0.6f, 0.4f));
            Top(top.rectTransform, 0f, 0f, PW, 2f);

            _slots = new SlotWidgets[2];
            for (int i = 0; i < 2; i++)
            {
                var s = new SlotWidgets();
                float slotY = i * 16f;   // 2 槽各 15 高,共 0~31

                var hl = UIFactory.Image($"Hl{i}", p.transform, new Color(0.18f, 0.22f, 0.38f, 0.7f));
                Top(hl.rectTransform, 0f, slotY, PW, 15f);
                s.highlight = hl.gameObject;

                var ac = UIFactory.Image($"Ac{i}", p.transform, new Color(0.45f, 0.75f, 1f));
                Top(ac.rectTransform, 0f, slotY + 1f, 2f, 13f);
                s.accent = ac.gameObject;

                s.icon = UIFactory.Image($"Icon{i}", p.transform, new Color(0.08f, 0.08f, 0.1f));
                Top(s.icon.rectTransform, 3f, slotY + 2f, 12f, 12f);

                s.line1 = UIFactory.Label($"L1_{i}", p.transform, "", 9, TextAnchor.MiddleLeft);
                Top(s.line1.rectTransform, 16f, slotY + 0f, PW - 18f, 9f);
                s.line2 = UIFactory.Label($"L2_{i}", p.transform, "", 7, TextAnchor.MiddleLeft);
                Top(s.line2.rectTransform, 16f, slotY + 8f, PW - 18f, 8f);

                _slots[i] = s;
            }

            _wSkillFill = Bar(p.transform, "WSkill", new Color(0.12f, 0.12f, 0.18f),
                new Color(0.25f, 0.4f, 0.85f), out var sbar);
            Top(sbar, 0f, 32f, PW, 13f);   // 技能条放在两槽之下(32~45),刚好落在 46 内
            _wSkillGroup = sbar.gameObject;
            _wSkillText = UIFactory.Label("WSkillTxt", p.transform, "", 8, TextAnchor.MiddleCenter,
                FontStyle.Normal, Color.white);
            Top(_wSkillText.rectTransform, 0f, 32f, PW, 13f);

            _weaponPanel.SetActive(false);
        }

        private void BuildLeftColumn(Transform root)
        {
            var col = UIFactory.Rect("LeftColumn", root);
            col.anchorMin = col.anchorMax = new Vector2(0f, 1f); col.pivot = new Vector2(0f, 1f);
            col.anchoredPosition = new Vector2(8f, -30f);
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f; vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;  vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
            var fit = col.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _talentsPanel   = MakeColumnPanel(col, "Talents");
            _itemsPanel     = MakeColumnPanel(col, "Items");
            _synergiesPanel = MakeColumnPanel(col, "Synergies");
        }

        private Transform MakeColumnPanel(Transform parent, string name)
        {
            var panel = UIFactory.Rect(name, parent);
            var le = panel.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 258f;
            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f; vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panel.gameObject.SetActive(false);
            return panel;
        }

        private void BuildBanner(Transform root)
        {
            var g = UIFactory.Rect("Banner", root);
            g.anchorMin = new Vector2(0.2f, 1f); g.anchorMax = new Vector2(0.8f, 1f);
            g.pivot = new Vector2(0.5f, 1f); g.sizeDelta = new Vector2(0f, 48f);
            g.anchoredPosition = new Vector2(0f, -78f);
            _bannerGroup = g.gameObject;

            var bg = UIFactory.Image("Bg", g, new Color(0f, 0f, 0f, 0.62f));
            UIFactory.Stretch(bg.rectTransform);
            var top = UIFactory.Image("Accent", g, new Color(0.95f, 0.8f, 0.2f, 0.8f));
            var trt = top.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f); trt.sizeDelta = new Vector2(0f, 2f); trt.anchoredPosition = Vector2.zero;

            _bannerText = UIFactory.Label("Txt", g, "", 22, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(1f, 0.92f, 0.35f), wrap: true);
            UIFactory.Stretch(_bannerText.rectTransform, left: 16f, right: 16f, top: 6f, bottom: 6f);

            _bannerGroup.SetActive(false);
        }

        // ── 每帧推送 ──────────────────────────────────────────────────────────

        public void SetTopBar(string info) { _topInfo.text = info; }

        public void SetHp(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            Color fill = ratio > 0.6f ? Color.Lerp(new Color(0.85f, 0.7f, 0.08f), new Color(0.18f, 0.82f, 0.28f), (ratio - 0.6f) / 0.4f)
                       : ratio > 0.25f ? Color.Lerp(new Color(0.88f, 0.18f, 0.1f), new Color(0.85f, 0.7f, 0.08f), (ratio - 0.25f) / 0.35f)
                       :                 new Color(0.88f, 0.12f, 0.08f);
            SetFill(_hpFill, ratio);
            _hpFill.color = fill;
            _hpText.text = $"♥  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        public void SetGold(int coins) { _goldText.text = $"◈  {coins}"; }

        public void SetCorruption(int corruption)
        {
            if (corruption <= 0) { _corrGroup.SetActive(false); return; }
            _corrGroup.SetActive(true);

            const int MaxShown = 12;
            string tier; Color col; float ratio;
            if (corruption < 5)       { tier = "Tainted";   col = new Color(0.78f, 0.62f, 0.92f); ratio = corruption / (float)MaxShown; }
            else if (corruption < 10) { tier = "Sinking";   col = new Color(0.62f, 0.32f, 0.85f); ratio = corruption / (float)MaxShown; }
            else                      { tier = "Possessed"; col = new Color(0.92f, 0.18f, 0.85f); ratio = 1f; }

            _corrTier.text = $"◊ {tier}"; _corrTier.color = col;
            _corrValue.text = $"Void {corruption}";
            _corrValue.color = new Color(col.r, col.g, col.b, 0.95f);
            _corrFill.color = col; SetFill(_corrFill, ratio);
        }

        public void SetHeroSkill(bool visible, string skillName, bool ready, float cdRemaining, float cooldownRatio)
        {
            _heroSkillGroup.SetActive(visible);
            if (!visible) return;
            SetFill(_heroSkillFill, 1f - cooldownRatio);
            _heroSkillFill.color = ready ? new Color(0.95f, 0.78f, 0.12f) : new Color(0.32f, 0.42f, 0.85f);
            _heroSkillText.text = ready ? $"[F] {skillName}   ✦ Ready!" : $"[F] {skillName}   CD {cdRemaining:0.0}s";
            _heroSkillText.color = ready ? new Color(1f, 0.96f, 0.45f) : Color.white;
        }

        public void SetWeapon(bool visible, WeaponSlot slot0, WeaponSlot slot1,
                              bool skillVisible, bool skillReady, float skillFill, string skillLabel)
        {
            if (_weaponPanel == null || _slots == null) return;   // 防御:画布未完整构建时不抛异常中断整个 HUD 刷新
            _weaponPanel.SetActive(visible);
            if (!visible) return;

            if (_slots[0] != null) ApplySlot(_slots[0], slot0);
            if (_slots[1] != null) ApplySlot(_slots[1], slot1);

            _wSkillGroup.SetActive(skillVisible);
            _wSkillText.gameObject.SetActive(skillVisible);
            if (skillVisible)
            {
                SetFill(_wSkillFill, skillFill);
                _wSkillFill.color = skillReady ? new Color(0.25f, 0.8f, 0.28f) : new Color(0.25f, 0.4f, 0.85f);
                _wSkillText.text = skillLabel;
                _wSkillText.color = skillReady ? new Color(0.55f, 1f, 0.55f) : new Color(0.75f, 0.8f, 1f);
            }
        }

        private void ApplySlot(SlotWidgets w, WeaponSlot data)
        {
            w.highlight.SetActive(data.active);
            w.accent.SetActive(data.active);

            if (data.occupied && data.icon != null) { w.icon.sprite = data.icon; w.icon.color = Color.white; }
            else { w.icon.sprite = null; w.icon.color = data.occupied ? data.color * 0.5f : new Color(0.2f, 0.2f, 0.22f); }

            w.line1.text  = data.line1;
            w.line1.color = data.color;
            w.line1.fontStyle = data.active ? FontStyle.Bold : FontStyle.Normal;

            w.line2.text  = data.line2 ?? "";
            w.line2.color = data.color * 0.82f;
            w.line2.gameObject.SetActive(!string.IsNullOrEmpty(data.line2));
        }

        public void SetTalents(List<Chip> chips)
            => RebuildChips(_talentsPanel, ref _talentsSig, chips, null, default, new Color(0.95f, 0.85f, 0.4f, 0.6f));

        public void SetItems(List<Chip> chips)
            => RebuildChips(_itemsPanel, ref _itemsSig, chips, $"✦ Items ({chips.Count})",
                new Color(0.92f, 0.84f, 0.55f), new Color(0.92f, 0.78f, 0.40f, 0.6f));

        public void SetSynergies(List<Chip> chips)
            => RebuildChips(_synergiesPanel, ref _synergiesSig, chips, $"★ Synergies ({chips.Count})",
                new Color(1f, 0.78f, 0.92f), new Color(1f, 0.55f, 0.85f, 0.7f));

        public void SetBoss(bool visible, string bossName, float current, float max)
        {
            _bossGroup.SetActive(visible);
            if (!visible) return;
            _bossName.text = string.IsNullOrEmpty(bossName) ? "BOSS" : bossName;
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            SetFill(_bossFill, ratio);
            _bossFill.color = Color.Lerp(new Color(0.75f, 0.12f, 0.12f), new Color(0.95f, 0.35f, 0.1f), ratio);
            _bossHpText.text = $"{Mathf.CeilToInt(current):N0} / {Mathf.CeilToInt(max):N0}";
        }

        public void SetBanner(bool visible, string message)
        {
            _bannerGroup.SetActive(visible);
            if (visible) _bannerText.text = message;
        }

        // ── chip 面板重建（仅内容变化时）────────────────────────────────────
        private static readonly StringBuilder _sb = new StringBuilder();
        private void RebuildChips(Transform panel, ref string sig, List<Chip> chips,
                                  string header, Color headerColor, Color accentColor)
        {
            _sb.Clear();
            if (header != null) _sb.Append(header).Append('|');
            foreach (var c in chips) _sb.Append(c.title).Append('~').Append(c.subtitle).Append('|');
            string newSig = _sb.ToString();
            if (newSig == sig) return;
            sig = newSig;

            for (int i = panel.childCount - 1; i >= 0; i--) Destroy(panel.GetChild(i).gameObject);

            if (chips.Count == 0) { panel.gameObject.SetActive(false); return; }
            panel.gameObject.SetActive(true);

            // 顶部强调线
            var line = UIFactory.Image("TopLine", panel, accentColor);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

            if (header != null)
            {
                var h = UIFactory.Label("Header", panel, header, 12, TextAnchor.MiddleLeft, FontStyle.Bold, headerColor);
                h.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
            }

            foreach (var c in chips)
            {
                var chip = UIFactory.Rect("Chip", panel);
                chip.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

                var ac = UIFactory.Image("Accent", chip, c.color);
                var art = ac.rectTransform;
                art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(0f, 1f);
                art.pivot = new Vector2(0f, 0.5f); art.sizeDelta = new Vector2(3f, 0f); art.anchoredPosition = Vector2.zero;

                bool hasSub = !string.IsNullOrEmpty(c.subtitle);
                var title = UIFactory.Label("Title", chip, c.title, 12,
                    hasSub ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft, FontStyle.Bold, c.color);
                var trt = title.rectTransform;
                trt.anchorMin = new Vector2(0f, hasSub ? 0.45f : 0f); trt.anchorMax = new Vector2(1f, 1f);
                trt.offsetMin = new Vector2(8f, 0f); trt.offsetMax = Vector2.zero;

                if (hasSub)
                {
                    var sub = UIFactory.Label("Sub", chip, c.subtitle, 10, TextAnchor.UpperLeft,
                        FontStyle.Normal, new Color(c.color.r * 0.8f, c.color.g * 0.8f, c.color.b * 0.85f));
                    var srt = sub.rectTransform;
                    srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0.45f);
                    srt.offsetMin = new Vector2(8f, 0f); srt.offsetMax = Vector2.zero;
                }
            }
        }

        // ── 工具 ──────────────────────────────────────────────────────────────

        // 进度条：bg 容器 + Filled 横向填充层。返回 fill（设 fillAmount/color），out 出 bg 的 RectTransform。
        private static Image Bar(Transform parent, string name, Color bgColor, Color fillColor,
                                 out RectTransform holder)
        {
            var bg = UIFactory.Image(name, parent, bgColor);
            holder = bg.rectTransform;
            var fill = UIFactory.Image("Fill", holder, fillColor);
            UIFactory.Stretch(fill.rectTransform);
            return fill;
        }

        // 进度条填充：用锚点右边界表示比例（不依赖 sprite —— Image.Type.Filled 在无 sprite 时 fillAmount 会被忽略）。
        private static void SetFill(Image fill, float amount)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddOutline(Graphic g, Color color)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color; o.effectDistance = new Vector2(1f, 1f);
        }

        // Place relative to the parent's top-left corner, with y growing downward.
        private static void Top(RectTransform rt, float x, float yFromTop, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
        }
    }
}
