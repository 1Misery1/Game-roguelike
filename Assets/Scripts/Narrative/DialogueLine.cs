namespace Game.Narrative
{
    /// 一句对话。旁白行将 Speaker 设为「旁白」、PortraitKey 设为 null。
    public struct DialogueLine
    {
        /// 说话者显示名
        public readonly string Speaker;
        /// 头像键（HeroSprites 的 heroName，如 "Warrior"）；null = 无头像
        public readonly string PortraitKey;
        /// 对话正文
        public readonly string Text;

        public DialogueLine(string speaker, string portraitKey, string text)
        {
            Speaker     = speaker;
            PortraitKey = portraitKey;
            Text        = text;
        }
    }
}
