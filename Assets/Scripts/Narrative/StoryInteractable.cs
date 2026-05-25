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

        // 最近一次 Interact 时的全局计数缓存（用于 fallback 真相旗解锁）
        private int _lastGlobalCount;

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
            // 注意 count 参数现在是「本英雄的累计次数」（per-hero）
            // 真相旗 fallback 用全局计数 → 通过 _lastGlobalCount 访问
            BuildDialogue = (hero, heroCount) => BuildLinesFromData(_data, hero, heroCount);
            OnResolved    = (hero, heroCount) => ResolveFromData(_data, hero, heroCount, _lastGlobalCount);
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

            // 全局计数（用于真相旗 fallback）
            _lastGlobalCount = gm != null && gm.Persistent != null
                ? gm.Persistent.RecordInvestigation(ObjectId)
                : 1;

            // 每英雄计数（用于分支匹配，保证换英雄重玩时仍能从 count=1 看起）
            int heroCount = gm != null && gm.Persistent != null
                ? gm.Persistent.RecordHeroInvestigation(ObjectId, hero != null ? hero.heroName : "")
                : 1;

            var lines = BuildDialogue != null ? BuildDialogue(hero, heroCount) : null;
            if (lines == null || lines.Count == 0) return;

            _busy = true;
            DialogueBox.Get().Play(lines, () =>
            {
                OnResolved?.Invoke(hero, heroCount);
                _busy = false;
            });
        }

        // ── 数据驱动求值 ─────────────────────────────────────────────────────

        private static List<DialogueLine> BuildLinesFromData(StoryInteractableData d, HeroData hero, int count)
        {
            string heroKey  = hero != null ? hero.heroName : "";
            string heroName = hero != null && !string.IsNullOrEmpty(hero.displayName)
                                  ? hero.displayName : "冒险者";
            int runs = GameManager.Instance != null && GameManager.Instance.Persistent != null
                          ? GameManager.Instance.Persistent.TotalVictories : 0;

            var result = new List<DialogueLine>();
            foreach (var br in d.branches)
            {
                if (br == null) continue;
                if (count < Mathf.Max(1, br.minCount)) continue;
                if (br.maxCount > 0 && count > br.maxCount) continue;
                if (br.minRunCount > 0 && runs < br.minRunCount) continue;
                if (br.maxRunCount > 0 && runs > br.maxRunCount) continue;
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

        private static void ResolveFromData(StoryInteractableData d, HeroData hero, int heroCount, int globalCount)
        {
            var gm = GameManager.Instance;
            string heroKey = hero != null ? hero.heroName : "";

            foreach (var f in d.runStoryFlags)
                if (!string.IsNullOrEmpty(f)) gm?.Run?.SetStoryFlag(f);

            foreach (var ta in d.truthAwards)
            {
                if (ta == null || string.IsNullOrEmpty(ta.flag)) continue;

                bool isOwnHero  = string.IsNullOrEmpty(ta.requireHero) || ta.requireHero == heroKey;
                // fallback 现在按全局计数：任意英雄顺序累计 N 次都能解锁
                bool isFallback = ta.fallbackCount > 0 && globalCount >= ta.fallbackCount;
                if (!isOwnHero && !isFallback) continue;

                gm?.Persistent?.AddTruthFlag(ta.flag);
            }

            foreach (var item in d.grantStoryItems)
                if (!string.IsNullOrEmpty(item)) gm?.Run?.AddStoryItem(item);

            if (d.addCorruption != 0)
                gm?.Run?.AddCorruption(d.addCorruption);

            if (!string.IsNullOrEmpty(d.bannerText))
                GameBootstrap.PostBanner(d.bannerText);
        }
    }
}
