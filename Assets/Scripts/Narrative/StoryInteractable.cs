using System.Collections.Generic;
using Game.Core;
using Game.Data;
using Game.Dev;
using Game.Player;
using UnityEngine;

namespace Game.Narrative
{
    /// 剧情交互物：玩家进入触发器时，按当前英雄与累计调查次数播放对话。
    /// 具体内容由生成方（如 GameBootstrap）通过 BuildDialogue / OnResolved 注入，
    /// 因此本组件本身与任何特定剧情解耦，可复用于全部 10 个交互物。
    [RequireComponent(typeof(Collider2D))]
    public class StoryInteractable : MonoBehaviour
    {
        /// 唯一 id，用于 PersistentState 的跨周目调查次数统计
        public string ObjectId = "story_object";

        /// (当前英雄, 本次为第几次调查) -> 对话行列表
        public System.Func<HeroData, int, List<DialogueLine>> BuildDialogue;

        /// 对话播放结束回调：(当前英雄, 本次为第几次调查)
        public System.Action<HeroData, int> OnResolved;

        private bool _busy;
        private bool _awaitingCombatEnd; // 战斗中进入触发器：待战斗结束后触发一次

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            // 战斗期间不可交互，记录待触发；否则立即交互
            if (GameBootstrap.CombatInProgress) _awaitingCombatEnd = true;
            else                                TryInteract();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _awaitingCombatEnd = false;
        }

        private void Update()
        {
            // 玩家在战斗中走上交互物，战斗结束后补触发一次（仅一次，不会循环）
            if (_awaitingCombatEnd && !GameBootstrap.CombatInProgress)
            {
                _awaitingCombatEnd = false;
                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (_busy || DialogueBox.IsActive) return;
            Interact();
        }

        private void Interact()
        {
            var gm   = GameManager.Instance;
            var hero = gm != null && gm.Run != null ? gm.Run.Hero : null;

            // 记录调查次数（跨周目）；编辑器直接运行时 GameManager 可能为空
            int count = gm != null && gm.Persistent != null
                ? gm.Persistent.RecordInvestigation(ObjectId)
                : 1;

            var lines = BuildDialogue != null ? BuildDialogue(hero, count) : null;
            if (lines == null || lines.Count == 0) return;

            _busy = true;
            DialogueBox.Get().Play(lines, () =>
            {
                OnResolved?.Invoke(hero, count);
                _busy = false;
            });
        }
    }
}
