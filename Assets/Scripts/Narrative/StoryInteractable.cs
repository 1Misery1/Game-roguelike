using System.Collections.Generic;
using Game.Core;
using Game.Data;
using Game.Dev;
using Game.Player;
using UnityEngine;

namespace Game.Narrative
{
    /// 剧情交互物：玩家进入触发器时，按当前英雄与累计调查次数播放对话。
    ///
    /// 两种使用方式：
    ///   ① 推荐：在 Inspector 拖入 StoryInteractableData（ScriptableObject）→ 完全数据驱动。
    ///   ② 代码注入：由生成方设置 BuildDialogue / OnResolved 回调（向后兼容旧代码）。
    [RequireComponent(typeof(Collider2D))]
    public class StoryInteractable : MonoBehaviour
    {
        [Header("数据驱动（推荐）")]
        [Tooltip("拖入剧情数据 SO。设置后会自动覆盖 ObjectId、对话、奖励、外观")]
        [SerializeField] private StoryInteractableData _data;

        [Header("回退（仅当未设置 Data 时用）")]
        [Tooltip("唯一 id，用于 PersistentState 的跨周目调查次数统计")]
        public string ObjectId = "story_object";

        /// (当前英雄, 本次为第几次调查) -> 对话行列表
        public System.Func<HeroData, int, List<DialogueLine>> BuildDialogue;

        /// 对话播放结束回调：(当前英雄, 本次为第几次调查)
        public System.Action<HeroData, int> OnResolved;

        private bool _busy;
        private bool _awaitingCombatEnd;

        /// 公开访问：让外部生成代码（如 GameBootstrap）也能临时换装数据
        public StoryInteractableData Data
        {
            get => _data;
            set { _data = value; ApplyData(); }
        }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            if (_data != null) ApplyData();
        }

        private void ApplyData()
        {
            if (_data == null) return;

            if (!string.IsNullOrEmpty(_data.objectId)) ObjectId = _data.objectId;

            // 外观（白盒）
            transform.localScale = new Vector3(_data.visualScale.x, _data.visualScale.y, 1f);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = _data.tintColor;
            var box = GetComponent<BoxCollider2D>();
            if (box != null) box.size = _data.colliderSize;

            // 数据驱动的对话 / 奖励
            BuildDialogue = (hero, count) => BuildLinesFromData(_data, hero, count);
            OnResolved    = (hero, count) => ResolveFromData(_data, hero, count);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            if (GameBootstrap.CombatInProgress) _awaitingCombatEnd = true;
            else                                TryInteract();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _awaitingCombatEnd = false;
        }

        private void Update()
        {
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

        // ── 数据驱动求值 ─────────────────────────────────────────────────────

        private static List<DialogueLine> BuildLinesFromData(StoryInteractableData d, HeroData hero, int count)
        {
            string heroKey  = hero != null ? hero.heroName : "";
            string heroName = hero != null && !string.IsNullOrEmpty(hero.displayName)
                                  ? hero.displayName : "冒险者";

            var result = new List<DialogueLine>();
            foreach (var br in d.branches)
            {
                if (br == null) continue;
                if (count < Mathf.Max(1, br.minCount)) continue;
                if (br.maxCount > 0 && count > br.maxCount) continue;
                if (!string.IsNullOrEmpty(br.requireHero) && br.requireHero != heroKey) continue;
                if (!string.IsNullOrEmpty(br.forbidHero)  && br.forbidHero  == heroKey) continue;

                foreach (var ln in br.lines)
                {
                    if (ln == null) continue;
                    string spk = Resolve(ln.speaker,     heroName, heroKey);
                    string por = Resolve(ln.portraitKey, heroName, heroKey);
                    string txt = Resolve(ln.text,        heroName, heroKey);
                    if (string.IsNullOrEmpty(por)) por = null;
                    result.Add(new DialogueLine(spk, por, txt));
                }
            }
            return result;
        }

        private static string Resolve(string s, string heroName, string heroKey)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("{hero}", heroName).Replace("{heroKey}", heroKey);
        }

        private static void ResolveFromData(StoryInteractableData d, HeroData hero, int count)
        {
            var gm = GameManager.Instance;
            string heroKey = hero != null ? hero.heroName : "";

            foreach (var f in d.runStoryFlags)
                if (!string.IsNullOrEmpty(f)) gm?.Run?.SetStoryFlag(f);

            foreach (var ta in d.truthAwards)
            {
                if (ta == null || string.IsNullOrEmpty(ta.flag)) continue;
                if (!string.IsNullOrEmpty(ta.requireHero) && ta.requireHero != heroKey) continue;
                gm?.Persistent?.AddTruthFlag(ta.flag);
            }

            if (!string.IsNullOrEmpty(d.bannerText))
                GameBootstrap.PostBanner(d.bannerText);
        }
    }
}
