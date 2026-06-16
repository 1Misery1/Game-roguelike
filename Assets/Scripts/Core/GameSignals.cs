using System;

namespace Game.Core
{
    // 跨层运行时信号中枢：让中/底层代码无需反向依赖 Bootstrap 即可
    // 上报横幅消息、读写战斗进行状态。Bootstrap 订阅 BannerRequested 做实际 UI。
    public static class GameSignals
    {
        // 当前房间是否处于战斗（有未清空的刷怪波次）。由 Bootstrap 写、各处读。
        public static bool CombatInProgress { get; set; }

        // 对话框是否打开（打开时冻结玩家输入）。由 DialogueBox 写、PlayerController 读。
        public static bool DialogueActive { get; set; }

        // 选项弹窗是否打开。由 ChoiceBox 写；暂停菜单据此避让 ESC。
        public static bool ChoiceActive { get; set; }

        // 横幅消息请求；Bootstrap 在启动时订阅并渲染。
        public static event Action<string> BannerRequested;

        public static void PostBanner(string message) => BannerRequested?.Invoke(message);
    }
}
