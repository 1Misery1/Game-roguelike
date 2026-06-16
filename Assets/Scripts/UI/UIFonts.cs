using UnityEngine;

namespace Game.UI
{
    /// Central UI font entry point: all UI text prefers the Ark Pixel font.
    /// Load order: Resources/Fonts/ark-pixel-12px → system CJK font fallback (YaHei / SimHei).
    /// Scaling is handled by the CanvasScaler (see UIFactory), so no manual scaling helper is needed.
    public static class UIFonts
    {
        static Font _ark;
        static Font _fallback;
        static bool _arkTried;

        /// 主 UI 字体（方舟像素体 / 中文兜底）
        public static Font UI
        {
            get
            {
                if (!_arkTried)
                {
                    _ark      = Resources.Load<Font>("Fonts/ark-pixel-12px");
                    _arkTried = true;
                }
                if (_ark != null) return _ark;
                if (_fallback == null)
                    _fallback = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体" }, 22);
                return _fallback;
            }
        }
    }
}
