using System.Collections.Generic;
using Game.Data;

namespace Game.Core
{
    public class RunState
    {
        public HeroData Hero { get; private set; }
        public readonly List<WeaponInstance> Weapons = new List<WeaponInstance>();
        public readonly List<TalentData>     Talents  = new List<TalentData>();
        public readonly List<BuffData>       Buffs    = new List<BuffData>();

        public int CurrentFloor = 1;
        public int Coins = 0;
        public int UnlockCurrencyEarned = 0;
        public bool IsActive { get; private set; }

        // ── 叙事状态（阶段一脚手架，供后续剧情系统使用）────────────────
        /// 虚空污染值；剧情抉择会增减，影响结局分支
        public int VoidCorruption = 0;
        /// 本局持有的剧情道具 id（如「烧焦的工匠名册」「寒镜碎片」）
        public readonly List<string>    StoryItems = new List<string>();
        /// 本局已触发的剧情旗标 id
        public readonly HashSet<string> StoryFlags = new HashSet<string>();

        public void Begin(HeroData hero)
        {
            Hero = hero;
            Weapons.Clear();
            Talents.Clear();
            Buffs.Clear();
            CurrentFloor = 1;
            Coins = 0;
            UnlockCurrencyEarned = 0;
            VoidCorruption = 0;
            StoryItems.Clear();
            StoryFlags.Clear();
            IsActive = true;
        }

        public void End()
        {
            IsActive = false;
            Weapons.Clear();
            Talents.Clear();
            Buffs.Clear();
            StoryItems.Clear();
            StoryFlags.Clear();
        }

        public void AddWeapon(WeaponInstance w) => Weapons.Add(w);
        public void AddTalent(TalentData t)     => Talents.Add(t);
        public void AddBuff(BuffData b)          => Buffs.Add(b);

        // ── 叙事状态访问器 ─────────────────────────────────────────────
        public void AddCorruption(int amount)
        {
            VoidCorruption += amount;
            if (VoidCorruption < 0) VoidCorruption = 0;
        }

        public bool HasStoryItem(string id) => StoryItems.Contains(id);

        public void AddStoryItem(string id)
        {
            if (!string.IsNullOrEmpty(id) && !StoryItems.Contains(id))
                StoryItems.Add(id);
        }

        public bool HasStoryFlag(string flag) => StoryFlags.Contains(flag);

        public void SetStoryFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag)) StoryFlags.Add(flag);
        }
    }
}
