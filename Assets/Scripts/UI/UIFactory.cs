using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Game.UI
{
    /// Factory for code-built UI (Canvas / Text / Image / Button). Uses legacy Text
    /// (crisp bitmap pixel font); the CanvasScaler references 960x540 and matches height,
    /// so sizes are authored in base px and scaling is left to the scaler.
    public static class UIFactory
    {
        static readonly Vector2 RefResolution = new Vector2(960f, 540f);   // reference resolution (height match)

        /// Create a screen-space overlay canvas (with its own CanvasScaler / GraphicRaycaster).
        /// sortingOrder controls layering: HUD < dialogue < popup < summary.
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder, bool dontDestroy = false)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RefResolution;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 1f;   // 1 = match height fully (equivalent to UIFonts.Scale = height/540)

            EnsureEventSystem();
            if (dontDestroy) Object.DontDestroyOnLoad(go);
            return canvas;
        }

        /// Ensure the scene has an EventSystem (new Input System module) so Buttons receive clicks.
        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ── Basic widgets ─────────────────────────────────────────────────────

        /// Empty RectTransform container.
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// Solid colour block (background / divider / bar fill).
        public static Image Image(string name, Transform parent, Color color, bool raycast = false)
        {
            var rt  = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color         = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// Sprite image.
        public static Image Sprite(string name, Transform parent, Sprite sprite, bool raycast = false)
        {
            var img = Image(name, parent, Color.white, raycast);
            img.sprite = sprite;
            if (sprite == null) img.color = new Color(1, 1, 1, 0);
            return img;
        }

        /// Pixel-font text. fontSize is in base px (the CanvasScaler scales it by resolution).
        public static Text Label(string name, Transform parent, string text, int fontSize,
                                 TextAnchor anchor = TextAnchor.UpperLeft,
                                 FontStyle style = FontStyle.Normal, Color? color = null,
                                 bool wrap = false)
        {
            var rt = Rect(name, parent);
            var t  = rt.gameObject.AddComponent<Text>();
            t.font               = UIFonts.UI;
            t.text               = text;
            t.fontSize           = fontSize;
            t.alignment          = anchor;
            t.fontStyle          = style;
            t.color              = color ?? Color.white;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            t.raycastTarget      = false;
            t.supportRichText    = false;
            return t;
        }

        /// Transparent hit-target button + optional background; onClick takes a delegate directly.
        public static Button Button(string name, Transform parent, System.Action onClick,
                                    Color? bg = null)
        {
            var img = Image(name, parent, bg ?? new Color(0, 0, 0, 0), raycast: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        // ── RectTransform placement helpers ───────────────────────────────────

        /// Stretch to fill the parent (with optional per-edge insets).
        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0,
                                            float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// Position by anchor (0..1) with a fixed size; pivot defaults to centre.
        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 size,
                                           Vector2 anchoredPos, Vector2? pivot = null)
        {
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.pivot            = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;
            return rt;
        }
    }
}
