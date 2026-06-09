using System.Collections;
using Game.Data;
using UnityEngine;

namespace Game.Combat
{
    // 给目标施加「限时」StatModifier，并把到期移除的计时协程跑在「目标自身」上。
    // 关键点：减速这类 debuff 的施法源（坠落冰锥 / 虚空格 / 火柱）会在很短时间内销毁，
    // 若把移除协程挂在施法源上，源一销毁协程即中止 → 减速永久残留。挂在目标上即可避免。
    public static class TimedModifier
    {
        public static void Apply(CharacterStats stats, StatType stat, ModifierOp op,
                                 float value, float duration)
        {
            if (stats == null) return;
            // 每次施加都用独立 key 对象，互不干扰；到期各自精确移除。
            var key = new object();
            stats.AddModifier(new StatModifier(stat, op, value, key));
            var runner = stats.gameObject.AddComponent<TimedModifierRunner>();
            runner.Begin(stats, key, duration);
        }
    }

    // 一次性自毁的计时器组件（挂在目标上）。每个 debuff 一个实例，到期移除对应修饰器并自销毁。
    public sealed class TimedModifierRunner : MonoBehaviour
    {
        public void Begin(CharacterStats stats, object key, float duration)
        {
            StartCoroutine(Run(stats, key, duration));
        }

        private IEnumerator Run(CharacterStats stats, object key, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (stats != null) stats.RemoveModifiersFrom(key);
            Destroy(this);
        }
    }
}
