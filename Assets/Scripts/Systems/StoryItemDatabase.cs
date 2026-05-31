using System.Collections.Generic;
using Game.Combat;
using Game.Data;

namespace Game.Systems
{
    /// 剧情道具 → 战斗加成映射。
    /// 设计哲学：增加污染的道具 → 数值更高但有副作用；净化道具 → 数值小但纯增益。
    public static class StoryItemDatabase
    {
        public struct Effect
        {
            public StatType   stat;
            public ModifierOp op;
            public float      value;
        }

        public struct ItemDef
        {
            public string   id;
            public string   flavorTag;   // 短摘要，发奖 banner 用
            public Effect[] effects;
        }

        static readonly Dictionary<string, ItemDef> _byId = new Dictionary<string, ItemDef>
        {
            // ── 净化系（选择净化路径获得，数值偏低但纯增益）─────────────────
            ["Frost Mirror Shard"] = new ItemDef {
                id = "Frost Mirror Shard", flavorTag = "DEF +8  HP +20",
                effects = new[] {
                    new Effect { stat = StatType.Defense, op = ModifierOp.Flat, value =  8f },
                    new Effect { stat = StatType.MaxHP,   op = ModifierOp.Flat, value = 20f },
                }
            },
            ["Restraint"] = new ItemDef {
                id = "Restraint", flavorTag = "Crit DMG +25%  CD -10%",
                effects = new[] {
                    new Effect { stat = StatType.CritDamage,        op = ModifierOp.PercentAdd, value = 0.25f },
                    new Effect { stat = StatType.CooldownReduction, op = ModifierOp.Flat,       value = 0.10f },
                }
            },
            ["Memory Pact"] = new ItemDef {
                id = "Memory Pact", flavorTag = "DEF +6  Gold +15%",
                effects = new[] {
                    new Effect { stat = StatType.Defense,  op = ModifierOp.Flat,       value = 6f },
                    new Effect { stat = StatType.CoinGain, op = ModifierOp.PercentAdd, value = 0.15f },
                }
            },
            ["Courage to Overthrow"] = new ItemDef {
                id = "Courage to Overthrow", flavorTag = "Crit +12%  ATK +10",
                effects = new[] {
                    new Effect { stat = StatType.CritRate, op = ModifierOp.Flat, value = 0.12f },
                    new Effect { stat = StatType.Attack,   op = ModifierOp.Flat, value = 10f   },
                }
            },

            // ── 污染系（选择污染路径获得，数值高但身上多一份重量）─────────────
            ["Lakebed Relic"] = new ItemDef {
                id = "Lakebed Relic", flavorTag = "ATK +12  HP +25",
                effects = new[] {
                    new Effect { stat = StatType.Attack, op = ModifierOp.Flat, value = 12f },
                    new Effect { stat = StatType.MaxHP,  op = ModifierOp.Flat, value = 25f },
                }
            },
            ["Memory of Confrontation"] = new ItemDef {
                id = "Memory of Confrontation", flavorTag = "Skill +18%  Crit +5%",
                effects = new[] {
                    new Effect { stat = StatType.SkillPower, op = ModifierOp.PercentAdd, value = 0.18f },
                    new Effect { stat = StatType.CritRate,   op = ModifierOp.Flat,       value = 0.05f },
                }
            },
            ["Echo of Fury"] = new ItemDef {
                id = "Echo of Fury", flavorTag = "ATK +15  ATK SPD +0.15",
                effects = new[] {
                    new Effect { stat = StatType.Attack,      op = ModifierOp.Flat, value = 15f   },
                    new Effect { stat = StatType.AttackSpeed, op = ModifierOp.Flat, value = 0.15f },
                }
            },
            ["Throne's Lingering Might"] = new ItemDef {
                id = "Throne's Lingering Might", flavorTag = "ATK +20%  Skill +20%  Move -0.4 (Heavy)",
                effects = new[] {
                    new Effect { stat = StatType.Attack,     op = ModifierOp.PercentAdd, value =  0.20f },
                    new Effect { stat = StatType.SkillPower, op = ModifierOp.PercentAdd, value =  0.20f },
                    new Effect { stat = StatType.MoveSpeed,  op = ModifierOp.Flat,       value = -0.4f  },
                }
            },
            ["Void Memory Shard"] = new ItemDef {
                id = "Void Memory Shard", flavorTag = "Skill +10%",
                effects = new[] {
                    new Effect { stat = StatType.SkillPower, op = ModifierOp.PercentAdd, value = 0.10f },
                }
            },

            // ── 纯叙事道具（无战斗加成，仅推动剧情，如 FrostAltar 联动）─────────
            // 例：「烧焦的工匠名册」无 effects，但 HasStoryItem 检查仍可生效
            ["Charred Artisan Register"] = new ItemDef {
                id = "Charred Artisan Register", flavorTag = "(Story item: usable at the Frostsleep Altar)",
                effects = new Effect[0]
            },
        };

        public static bool TryGet(string id, out ItemDef def) => _byId.TryGetValue(id, out def);

        /// 把指定道具的全部加成应用到指定 stats；返回 true 表示真的应用了任何效果。
        public static bool Apply(string id, CharacterStats stats)
        {
            if (stats == null || string.IsNullOrEmpty(id)) return false;
            if (!_byId.TryGetValue(id, out var def))      return false;
            if (def.effects == null || def.effects.Length == 0) return false;

            string source = "story_item:" + id;
            foreach (var e in def.effects)
                stats.AddModifier(new StatModifier(e.stat, e.op, e.value, source));
            return true;
        }
    }
}
