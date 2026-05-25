using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Core
{
    [System.Serializable]
    public class PersistentState
    {
        public List<string> UnlockedHeroIds = new List<string>();
        public int UnlockCurrency = 0;

        // Run records
        public int TotalRuns      = 0;
        public int TotalVictories = 0;
        public int BestFloor      = 0;
        public int TotalKills     = 0;
        public int TotalDamage    = 0;

        // ── 叙事进度（阶段一脚手架，供后续剧情系统使用）────────────────
        /// 跨周目解锁的真相旗标 id
        public List<string> TruthFlags = new List<string>();
        /// 每个交互物的累计调查次数（全局：所有英雄相加）
        public List<InvestigationEntry> Investigations = new List<InvestigationEntry>();

        /// 每个交互物 × 英雄 的累计调查次数（仅本英雄的足迹）
        public List<HeroInvestigationEntry> HeroInvestigations = new List<HeroInvestigationEntry>();

        [System.Serializable]
        public class InvestigationEntry
        {
            public string objectId;
            public int    count;
        }

        [System.Serializable]
        public class HeroInvestigationEntry
        {
            public string objectId;
            public string heroKey;
            public int    count;
        }

        private const string FileName = "save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool IsHeroUnlocked(string heroId) => UnlockedHeroIds.Contains(heroId);

        public bool TryUnlockHero(string heroId, int cost)
        {
            if (UnlockedHeroIds.Contains(heroId)) return false;
            if (UnlockCurrency < cost) return false;
            UnlockCurrency -= cost;
            UnlockedHeroIds.Add(heroId);
            Save();
            return true;
        }

        public void AddCurrency(int amount)
        {
            UnlockCurrency += amount;
            Save();
        }

        public void RecordRunResult(int floor, bool victory, int kills, int damage)
        {
            TotalRuns++;
            if (victory) TotalVictories++;
            if (floor > BestFloor) BestFloor = floor;
            TotalKills  += kills;
            TotalDamage += damage;
            Save();
        }

        // ── 叙事进度访问器 ─────────────────────────────────────────────
        public bool HasTruthFlag(string flag) => TruthFlags.Contains(flag);

        public void AddTruthFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || TruthFlags.Contains(flag)) return;
            TruthFlags.Add(flag);
            Save();
        }

        public int GetInvestigationCount(string objectId)
        {
            foreach (var e in Investigations)
                if (e.objectId == objectId) return e.count;
            return 0;
        }

        /// 记录一次调查，返回该交互物的累计调查次数（全局：所有英雄相加）
        public int RecordInvestigation(string objectId)
        {
            foreach (var e in Investigations)
                if (e.objectId == objectId) { e.count++; Save(); return e.count; }
            Investigations.Add(new InvestigationEntry { objectId = objectId, count = 1 });
            Save();
            return 1;
        }

        /// 查询某英雄对某交互物的累计调查次数
        public int GetHeroInvestigationCount(string objectId, string heroKey)
        {
            if (string.IsNullOrEmpty(heroKey)) return 0;
            foreach (var e in HeroInvestigations)
                if (e.objectId == objectId && e.heroKey == heroKey) return e.count;
            return 0;
        }

        /// 记录一次「该英雄」对该交互物的调查，返回其专属计数
        public int RecordHeroInvestigation(string objectId, string heroKey)
        {
            if (string.IsNullOrEmpty(heroKey)) return 0;
            foreach (var e in HeroInvestigations)
                if (e.objectId == objectId && e.heroKey == heroKey)
                { e.count++; Save(); return e.count; }
            HeroInvestigations.Add(new HeroInvestigationEntry {
                objectId = objectId, heroKey = heroKey, count = 1
            });
            Save();
            return 1;
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(SavePath, json);
        }

        public static PersistentState Load()
        {
            if (!File.Exists(SavePath)) return new PersistentState();
            try
            {
                var json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PersistentState>(json) ?? new PersistentState();
            }
            catch
            {
                return new PersistentState();
            }
        }
    }
}
