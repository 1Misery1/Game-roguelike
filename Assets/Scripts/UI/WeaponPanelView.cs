using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// Reusable weapon panel widget: two weapon slots + a weapon-skill bar. Shared by the
    /// combat HUD (Dungeon) and the training-arena HUD. Build() constructs it under a parent
    /// canvas; the owner pushes a snapshot each frame through SetWeapon().
    public class WeaponPanelView
    {
        /// Snapshot of one weapon slot (text is pre-computed by the caller; the widget never
        /// touches weapon internals).
        public struct WeaponSlot
        {
            public bool   occupied;
            public bool   active;
            public Sprite icon;
            public Color  color;
            public string line1;
            public string line2;
        }

        private class SlotWidgets
        {
            public GameObject highlight, accent;
            public Image icon;
            public Text  line1, line2;
        }

        private GameObject    _panel;
        private SlotWidgets[]  _slots;
        private GameObject     _skillGroup;
        private Image          _skillFill;
        private Text           _skillText;

        /// Build the panel under the given parent (a canvas). Hidden until SetWeapon(true, ...).
        public void Build(Transform root)
        {
            const float PW = 190f;   // bottom-right, beside the HP bar
            const float PH = 64f;    // two 25-px weapon rows (0-50) + skill bar below (50-63)
            var p = UIFactory.Image("WeaponPanel", root, new Color(0f, 0f, 0f, 0.72f));
            var prt = p.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(1f, 0f); prt.pivot = new Vector2(1f, 0f);
            prt.sizeDelta = new Vector2(PW, PH); prt.anchoredPosition = new Vector2(-8f, 8f);
            _panel = p.gameObject;

            var top = UIFactory.Image("Accent", p.transform, new Color(0.5f, 0.5f, 0.6f, 0.4f));
            Top(top.rectTransform, 0f, 0f, PW, 2f);

            _slots = new SlotWidgets[2];
            for (int i = 0; i < 2; i++)
            {
                var s = new SlotWidgets();
                float slotY = i * 25f;   // two 25-px rows: slot 0 = 0-25, slot 1 = 25-50

                var hl = UIFactory.Image($"Hl{i}", p.transform, new Color(0.18f, 0.22f, 0.38f, 0.7f));
                Top(hl.rectTransform, 0f, slotY, PW, 25f);
                s.highlight = hl.gameObject;

                var ac = UIFactory.Image($"Ac{i}", p.transform, new Color(0.45f, 0.75f, 1f));
                Top(ac.rectTransform, 0f, slotY + 1f, 2f, 23f);
                s.accent = ac.gameObject;

                s.icon = UIFactory.Image($"Icon{i}", p.transform, new Color(0.08f, 0.08f, 0.1f));
                Top(s.icon.rectTransform, 3f, slotY + 2f, 24f, 24f);

                s.line1 = UIFactory.Label($"L1_{i}", p.transform, "", 9, TextAnchor.MiddleLeft);
                Top(s.line1.rectTransform, 30f, slotY + 2f, PW - 32f, 9f);
                s.line2 = UIFactory.Label($"L2_{i}", p.transform, "", 8, TextAnchor.MiddleLeft);
                Top(s.line2.rectTransform, 30f, slotY + 13f, PW - 32f, 8f);

                _slots[i] = s;
            }

            _skillFill = Bar(p.transform, "WSkill", new Color(0.12f, 0.12f, 0.18f),
                new Color(0.25f, 0.4f, 0.85f), out var sbar);
            Top(sbar, 0f, 50f, PW, 13f);   // skill bar below the two weapon rows (50-63)
            _skillGroup = sbar.gameObject;
            _skillText = UIFactory.Label("WSkillTxt", p.transform, "", 8, TextAnchor.MiddleCenter,
                FontStyle.Normal, Color.white);
            Top(_skillText.rectTransform, 0f, 50f, PW, 13f);

            _panel.SetActive(false);
        }

        public void SetWeapon(bool visible, WeaponSlot slot0, WeaponSlot slot1,
                              bool skillVisible, bool skillReady, float skillFill, string skillLabel)
        {
            if (_panel == null || _slots == null) return;   // not built yet — never break a HUD refresh
            _panel.SetActive(visible);
            if (!visible) return;

            if (_slots[0] != null) ApplySlot(_slots[0], slot0);
            if (_slots[1] != null) ApplySlot(_slots[1], slot1);

            _skillGroup.SetActive(skillVisible);
            _skillText.gameObject.SetActive(skillVisible);
            if (skillVisible)
            {
                SetFill(_skillFill, skillFill);
                _skillFill.color = skillReady ? new Color(0.25f, 0.8f, 0.28f) : new Color(0.25f, 0.4f, 0.85f);
                _skillText.text = skillLabel;
                _skillText.color = skillReady ? new Color(0.55f, 1f, 0.55f) : new Color(0.75f, 0.8f, 1f);
            }
        }

        private static void ApplySlot(SlotWidgets w, WeaponSlot data)
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

        // ── helpers (local copies so the widget is self-contained) ─────────────
        private static Image Bar(Transform parent, string name, Color bgColor, Color fillColor, out RectTransform holder)
        {
            var bg = UIFactory.Image(name, parent, bgColor);
            holder = bg.rectTransform;
            var fill = UIFactory.Image("Fill", holder, fillColor);
            UIFactory.Stretch(fill.rectTransform);
            return fill;
        }

        private static void SetFill(Image fill, float amount)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Top(RectTransform rt, float x, float yFromTop, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
        }
    }
}
