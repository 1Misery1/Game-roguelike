using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Data;

namespace Game.Systems
{
    /// 道具协同 buff：当玩家齐集指定的多件剧情道具时，额外触发一组属性加成。
    /// 协同激活后通过 RunState.StoryFlags 标记为「synergy_{id}」防止重复发奖。
    public static class StoryItemSynergyDatabase
    {
        public struct SynergyDef
        {
            public string                       id;            // 内部 ID（用作 StoryFlag 前缀）
            public string                       displayName;   // HUD/banner 显示名
            public string                       flavor;        // 单行描述（HUD 副标题用）
            public string[]                     requiredItems; // 需齐集的道具 ID
            public StoryItemDatabase.Effect[]   effects;       // 激活时叠加的加成
        }

        // ── 协同表（顺序无关，只要齐集即触发）─────────────────────────────
        static readonly SynergyDef[] _all = new SynergyDef[]
        {
            // 「燃烧的王冠」 = 怒火残响 + 王座余威 → 双污染叠攻
            new SynergyDef {
                id            = "burning_crown",
                displayName   = "燃烧的王冠",
                flavor        = "怒火坐稳王座  ·  攻击+25%  攻速+0.20",
                requiredItems = new[] { "怒火残响", "王座余威" },
                effects = new[] {
                    Eff(StatType.Attack,      ModifierOp.PercentAdd, 0.25f),
                    Eff(StatType.AttackSpeed, ModifierOp.Flat,       0.20f),
                }
            },
            // 「冰封誓言」 = 寒镜碎片 + 记忆契约 → 双净化叠肉
            new SynergyDef {
                id            = "ice_oath",
                displayName   = "冰封誓言",
                flavor        = "看清的真相成为庄严的契约  ·  防御+12  HP+30",
                requiredItems = new[] { "寒镜碎片", "记忆契约" },
                effects = new[] {
                    Eff(StatType.Defense, ModifierOp.Flat, 12f),
                    Eff(StatType.MaxHP,   ModifierOp.Flat, 30f),
                }
            },
            // 「破坏者的复仇」 = 推翻的勇气 + 怒火残响 → 净化+污染叠暴击
            new SynergyDef {
                id            = "breaker_vengeance",
                displayName   = "破坏者的复仇",
                flavor        = "推翻一切的力量  ·  暴击率+15%  暴伤+30%",
                requiredItems = new[] { "推翻的勇气", "怒火残响" },
                effects = new[] {
                    Eff(StatType.CritRate,   ModifierOp.Flat,       0.15f),
                    Eff(StatType.CritDamage, ModifierOp.PercentAdd, 0.30f),
                }
            },
            // 「克制的对峙」 = 克制 + 对峙记忆 → 净化+污染叠技能
            new SynergyDef {
                id            = "measured_confrontation",
                displayName   = "克制的对峙",
                flavor        = "不动如山的对手  ·  技能+25%  暴击+8%",
                requiredItems = new[] { "克制", "对峙记忆" },
                effects = new[] {
                    Eff(StatType.SkillPower, ModifierOp.PercentAdd, 0.25f),
                    Eff(StatType.CritRate,   ModifierOp.Flat,       0.08f),
                }
            },
            // 「双重虚空」 = 湖底遗物 + 虚空记忆碎片 → 深陷虚空
            new SynergyDef {
                id            = "twin_void",
                displayName   = "双重虚空",
                flavor        = "深渊中拿走了更多  ·  攻击+15%  HP+40",
                requiredItems = new[] { "湖底遗物", "虚空记忆碎片" },
                effects = new[] {
                    Eff(StatType.Attack, ModifierOp.PercentAdd, 0.15f),
                    Eff(StatType.MaxHP,  ModifierOp.Flat,       40f  ),
                }
            },
        };

        public static IEnumerable<SynergyDef> All => _all;

        /// 检查并激活所有当前已满足条件、且尚未激活的协同。
        /// 返回本次新激活的协同列表，调用方可据此 banner 通知玩家。
        public static List<SynergyDef> CheckAndActivate(RunState run, CharacterStats stats)
        {
            var newlyActive = new List<SynergyDef>();
            if (run == null) return newlyActive;

            foreach (var s in _all)
            {
                string flag = "synergy_" + s.id;
                if (run.HasStoryFlag(flag)) continue;

                bool allOwned = true;
                foreach (var req in s.requiredItems)
                {
                    if (!run.HasStoryItem(req)) { allOwned = false; break; }
                }
                if (!allOwned) continue;

                run.SetStoryFlag(flag);
                if (stats != null && s.effects != null)
                {
                    string src = "synergy:" + s.id;
                    foreach (var e in s.effects)
                        stats.AddModifier(new StatModifier(e.stat, e.op, e.value, src));
                }
                newlyActive.Add(s);
            }
            return newlyActive;
        }

        public static bool IsActive(RunState run, string id)
            => run != null && run.HasStoryFlag("synergy_" + id);

        static StoryItemDatabase.Effect Eff(StatType stat, ModifierOp op, float value)
            => new StoryItemDatabase.Effect { stat = stat, op = op, value = value };
    }
}
