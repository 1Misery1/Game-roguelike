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

            // 本周目是否已做过该交互物的抉择？（用任一 choice 的 runStoryFlags 作为已选标记）
            bool choiceAlreadyMade = false;
            if (_data != null && _data.choices != null && gm?.Run != null)
            {
                foreach (var c in _data.choices)
                {
                    if (c == null || c.runStoryFlags == null) continue;
                    foreach (var f in c.runStoryFlags)
                        if (!string.IsNullOrEmpty(f) && gm.Run.HasStoryFlag(f))
                        { choiceAlreadyMade = true; break; }
                    if (choiceAlreadyMade) break;
                }
            }

            _busy = true;
            DialogueBox.Get().Play(lines, () =>
            {
                // 主对话结束：如果数据有 choices 且本周目尚未选择过 → 弹出选项；否则直接结算
                if (!choiceAlreadyMade && _data != null && _data.choices != null && _data.choices.Count > 0)
                {
                    string title = !string.IsNullOrEmpty(_data.choiceTitle) ? _data.choiceTitle
                                  : !string.IsNullOrEmpty(_data.bannerText) ? _data.bannerText
                                  : "选择";
                    var labels = new List<string>();
                    var descs  = new List<string>();
                    foreach (var c in _data.choices)
                    {
                        labels.Add(c != null ? c.label : "...");
                        descs .Add(c != null ? c.description : "");
                    }
                    ChoiceBox.Get().Show(title, labels, descs, idx =>
                    {
                        OnPlayerPickedChoice(hero, heroCount, idx);
                    });
                }
                else
                {
                    OnResolved?.Invoke(hero, heroCount);
                    _busy = false;
                }
            });
        }

        /// 玩家在 ChoiceBox 上点选后回调：先播 followLines，再叠加选项奖励，最后跑主结算
        private void OnPlayerPickedChoice(HeroData hero, int heroCount, int idx)
        {
            if (_data == null || idx < 0 || idx >= _data.choices.Count)
            {
                OnResolved?.Invoke(hero, heroCount);
                _busy = false;
                return;
            }
            var ch = _data.choices[idx];

            // 构造选项追加台词（同样支持 {hero}/{heroKey} 占位符）
            var followLines = new List<DialogueLine>();
            string heroKey  = hero != null ? hero.heroName : "";
            string heroName = hero != null && !string.IsNullOrEmpty(hero.displayName)
                                  ? hero.displayName : "冒险者";
            if (ch.followLines != null)
            {
                foreach (var ln in ch.followLines)
                {
                    if (ln == null) continue;
                    string spk = Resolve(ln.speaker,     heroName, heroKey);
                    string por = Resolve(ln.portraitKey, heroName, heroKey);
                    string txt = Resolve(ln.text,        heroName, heroKey);
                    if (string.IsNullOrEmpty(por)) por = null;
                    followLines.Add(new DialogueLine(spk, por, txt));
                }
            }

            System.Action applyEffects = () =>
            {
                ApplyChoiceEffects(ch, hero, heroKey);
                // 再跑主结算（不会重复发授旗：AddTruthFlag 自动去重）
                OnResolved?.Invoke(hero, heroCount);
                _busy = false;
            };

            if (followLines.Count > 0) DialogueBox.Get().Play(followLines, applyEffects);
            else                        applyEffects();
        }

        private void ApplyChoiceEffects(StoryChoice ch, HeroData hero, string heroKey)
        {
            var gm = GameManager.Instance;
            foreach (var f in ch.runStoryFlags)
                if (!string.IsNullOrEmpty(f)) gm?.Run?.SetStoryFlag(f);

            foreach (var ta in ch.truthAwards)
            {
                if (ta == null || string.IsNullOrEmpty(ta.flag)) continue;
                bool isOwn      = string.IsNullOrEmpty(ta.requireHero) || ta.requireHero == heroKey;
                bool isFallback = ta.fallbackCount > 0 && _lastGlobalCount >= ta.fallbackCount;
                if (!isOwn && !isFallback) continue;
                gm?.Persistent?.AddTruthFlag(ta.flag);
            }

            foreach (var item in ch.grantStoryItems)
                if (!string.IsNullOrEmpty(item)) gm?.Run?.AddStoryItem(item);

            if (ch.addCorruption != 0) gm?.Run?.AddCorruption(ch.addCorruption);
            if (!string.IsNullOrEmpty(ch.bannerOverride))
                GameBootstrap.PostBanner(ch.bannerOverride);
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
