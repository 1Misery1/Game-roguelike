using UnityEngine;

namespace Game.Dev
{
    /// 统一 UI 字体入口：项目所有 IMGUI 文字优先使用方舟像素体。
    /// 加载顺序：Resources/Fonts/ark-pixel-12px → 系统中文字体兜底（雅黑/黑体）
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

        /// 给所有 GUI 控件统一刷上像素字体（在 OnGUI 开头调用一次）
        public static void ApplyToSkin()
        {
            if (GUI.skin == null) return;
            var f = UI;
            if (f == null) return;
            if (GUI.skin.label  != null) GUI.skin.label.font  = f;
            if (GUI.skin.button != null) GUI.skin.button.font = f;
            if (GUI.skin.box    != null) GUI.skin.box.font    = f;
            if (GUI.skin.toggle != null) GUI.skin.toggle.font = f;
            if (GUI.skin.textArea  != null) GUI.skin.textArea.font  = f;
            if (GUI.skin.textField != null) GUI.skin.textField.font = f;
        }
    }
}
