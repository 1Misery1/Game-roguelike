using UnityEngine;

namespace Game.Dev
{
    /// 静态查表入口：按 bossId 取得 BossStatsData。
    /// 首次访问时 LoadAll，缓存数组，零运行时开销。
    public static class BossStatsRegistry
    {
        static BossStatsData[] _cache;

        static BossStatsData[] All =>
            _cache ?? (_cache = Resources.LoadAll<BossStatsData>("Bosses"));

        public static BossStatsData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var d in All)
                if (d != null && d.bossId == id) return d;
            return null;
        }

        /// 调试用：强制重载（如运行时改了 SO 想立刻生效）
        public static void Reload() { _cache = null; }
    }
}
