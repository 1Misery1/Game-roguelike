using System.Collections.Generic;
using Game.AI;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Dungeon;
using Game.Narrative;
using Game.Player;
using Game.Systems;
using UnityEngine;
using Game.Art;
using Game.Factories;
using Game.UI;
namespace Game.Bootstrap
{
    /// Single-scene state machine: Menu -> Playing -> FloorComplete -> Playing... -> Victory/Death -> Menu.
    /// Supports multi-floor runs with enemy scaling and weapon drops.
    public class GameBootstrap : MonoBehaviour
    {
        // Static bounds (written by MapBuilder, read by enemy AI)
        public static float ArenaHalfW { get; private set; } = 16f;
        public static float ArenaHalfH { get; private set; } = 10f;

        [SerializeField] private int   clearReward     = 50;
        [SerializeField] private int   nonBossRoomCount = 4;
        [SerializeField] private int   maxFloor        = 3;

        private enum State { Playing, FloorComplete, EndingCutscene, Victory, Death }

        // Combat room weights (Shop inserted at a fixed position, Boss always last)
        private static readonly (string type, float weight)[] RoomPool =
        {
            ("Monster", 5.0f),
            ("Coin",    2.0f),
            ("Talent",  2.0f),
        };

        public int RunCoins      { get; private set; }
        public int CurrentFloor  { get; private set; } = 1;

        // Enemy stats multiply by this each floor
        private float FloorScale => 1f + (CurrentFloor - 1) * 0.25f;

        private State _state = State.Playing;
        private PersistentState _persistent;
        private MapInfo _mapInfo;
        private int _mapVariant;
        private bool _arenaIsRect;          // 当前竞技场几何是否为规整矩形（商店/Boss 房）
        private bool _arenaIsClean;         // 商店房：纯地板+墙壁，无柱/陷阱/岩浆等任何地块
        private HeroData _currentHero;

        private GameObject _arenaRoot;
        private GameObject _player;
        private Health     _playerHealth;
        private GameObject _currentRoomRoot;
        private List<string> _floorRooms = new List<string>();
        private int _currentRoomIndex;

        private DamageNumbers _damageNumbers;
        private string _bannerMessage;
        private float  _bannerUntil;

        // HUD. RefreshHud() pushes data into it each frame.
        private Game.UI.HudView _hud;
        private Game.UI.OverlayView _overlay;   // full-screen overlays (FloorComplete transition + talent replacement)
        private bool _trShown;                   // 天赋替换弹窗当前是否显示
        private State _esShownFor = State.Playing; // 结算屏当前为哪个状态显示(Playing=未显示)
        private readonly List<Game.UI.HudView.Chip> _talentChips  = new List<Game.UI.HudView.Chip>();
        private readonly List<Game.UI.HudView.Chip> _itemChips    = new List<Game.UI.HudView.Chip>();
        private readonly List<Game.UI.HudView.Chip> _synergyChips = new List<Game.UI.HudView.Chip>();

        private Health _bossHealth;
        private string _bossName;
        private int    _enemiesKilled;
        private float  _totalDamageDealt;

        // Talent tracking
        private sealed class ActiveTalent
        {
            public TalentData Data;
            public object     Source   = new object();
            public int        RoomsLeft; // -1 = permanent
            public bool       IsPermanent => RoomsLeft < 0;
            public System.Action OnRoomEntered; // extra effect triggered on each room entry
        }
        private readonly List<ActiveTalent> _activeTalents = new List<ActiveTalent>();
        private TalentData _pendingTalent;
        private const int  MaxTalents = 2;

        // --------------------------------------------------------------------
        //  Startup
        // --------------------------------------------------------------------

        private void Awake()
        {
            _instance = this;
            GameSignals.BannerRequested += ShowBanner;
            // 创建摄像机必须在 Awake 完成，否则第一帧渲染前 Unity 会报 "no cameras rendering"
            EnsureCamera();
        }

        private void OnDestroy()
        {
            GameSignals.BannerRequested -= ShowBanner;
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            _persistent    = PersistentState.Load();
            _damageNumbers = gameObject.AddComponent<DamageNumbers>();

            // 从 GameManager 读取本局英雄；若直接在 Editor 打开此场景则用后备角色
            var hero = GameManager.Instance != null ? GameManager.Instance.Run.Hero : null;
            if (hero == null) hero = MakeFallbackHero();
            StartRun(hero);
        }

        // 编辑器直接运行时的后备英雄（无需经过菜单场景）
        private static HeroData MakeFallbackHero()
        {
            var h = ScriptableObject.CreateInstance<HeroData>();
            h.heroName          = "Warrior";
            h.description       = "Fallback hero for editor testing.";
            h.baseMaxHP         = 100f;
            h.baseAttack        = 14f;
            h.baseDefense       = 5f;
            h.baseMoveSpeed     = 5.5f;
            h.baseAttackSpeed   = 1.0f;
            h.unlockCost        = 0;
            h.tintColor         = new Color(0.4f, 0.85f, 1f);
            h.unlockedByDefault = true;
            h.heroSkillType     = HeroSkillType.WarCry;
            h.heroSkillCooldown = 10f;
            h.heroSkillName     = "War Cry";
            h.heroPassiveType   = HeroPassiveType.BattlefieldWill;
            h.heroPassiveName   = "Battlefield Will";
            return h;
        }

        // --------------------------------------------------------------------
        //  State transitions
        // --------------------------------------------------------------------

        private void ReturnToMenu()
        {
            CleanupRun();
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void RestartRun()
        {
            StartRun(_currentHero != null ? _currentHero : MakeFallbackHero());
        }

        private void StartRun(HeroData hero)
        {
            _currentHero = hero;
            CleanupRun();
            _state       = State.Playing;
            RunCoins     = 0;
            CurrentFloor = 1;
            MapBuilder.ReseedWorld();   // 每局刷新世界种子 → 程序化布局每局不同
            _floorRooms  = GenerateFloor();
            BuildArena();
            SetFloorBackground();
            SpawnPlayer(hero);
            LoadRoom(0);
            ShowBanner(GetFloorNarrative());
        }

        /// 真相结局触发阈值：跨周目累计真相旗 ≥ 此数 → 隐藏真相结局
        private const int TrueEndingTruthThreshold = 4;

        // ── 周目结局过场动画（预留接入位置）─────────────────────────────────────
        /// 普通结局占位动画时长（秒）。后续接入真实过场后可保留此字段作为兜底时长
        public const float NormalEndingCutsceneDuration = 4.0f;
        /// 真相结局占位动画时长（秒）。真相结局动画通常更长
        public const float TrueEndingCutsceneDuration   = 6.5f;
        /// 王冠结局占位动画时长（秒）。击败「王国之罪」后的终章
        public const float CrownEndingCutsceneDuration  = 8.5f;
        // 多帧结局 CG：每超过 1 帧额外增加的过场秒数（每帧展示时长）
        public const float EndingSecondsPerExtraFrame   = 2.2f;

        /// 过场开始时触发的全局 hook：动画系统可订阅此事件按当前结局类型播放
        /// (isTrueEnding, durationSeconds) -> 由订阅者使用
        public static event System.Action<bool, float> OnEndingCutsceneStart;
        /// 过场结束（自然完成或玩家跳过）时触发
        public static event System.Action OnEndingCutsceneEnd;

        /// 三档结局
        public enum EndingTier { Normal, Truth, Crown }

        private EndingTier _endingTier;
        private bool       _isTrueEnding;   // Truth/Crown both count as a true ending
        private int        _endingTruthCount;
        private float      _cutsceneDuration;   // 仅用于结局开始事件通知（音频淡入等）
        private int        _cutsceneFrame;      // 手动推进的当前帧索引
        private float      _cutsceneAlpha;      // 当前帧淡入 0→1（切帧归零再淡入，是淡入不是闪烁）
        private float      _cutsceneReveal;     // 字幕已显现字符数（逐字浮现）

        private void TriggerVictory()
        {
            _persistent.AddCurrency(clearReward);
            _persistent.RecordRunResult(CurrentFloor, true, _enemiesKilled, Mathf.RoundToInt(_totalDamageDealt));
            _endingTruthCount = _persistent.TruthFlags != null ? _persistent.TruthFlags.Count : 0;

            // 三档：击败「王国之罪」 > 真相旗 ≥ 阈值 > 普通
            if (_persistent.HasTruthFlag("truth_final_boss_defeated")) _endingTier = EndingTier.Crown;
            else if (_endingTruthCount >= TrueEndingTruthThreshold)    _endingTier = EndingTier.Truth;
            else                                                        _endingTier = EndingTier.Normal;
            _isTrueEnding = _endingTier != EndingTier.Normal;

            EnterEndingCutscene();
        }

        private void EnterEndingCutscene()
        {
            _state          = State.EndingCutscene;
            _cutsceneFrame  = 0;
            _cutsceneAlpha  = 0f;
            _cutsceneReveal = 0f;
            // 不再自动计时推进：每帧停留，逐字显示字幕，按键才进下一帧 / 结束。
            // 时长仅用于开始事件通知（音频淡入等），按帧数给个估值。
            int frameCount    = Mathf.Max(1, GetEndingFrames(_endingTier).Length);
            _cutsceneDuration = frameCount * 6f;
            OnEndingCutsceneStart?.Invoke(_isTrueEnding, _cutsceneDuration);
        }

        // 每帧推进当前帧的「淡入」与「字幕逐字浮现」（由 LateUpdate 每帧调用一次）
        private void TickEndingCutscene()
        {
            float dt = Time.unscaledDeltaTime;
            _cutsceneAlpha = Mathf.MoveTowards(_cutsceneAlpha, 1f, dt / 0.55f);
            if (_cutsceneAlpha >= 0.55f)
            {
                var caps = GetEndingCaptions(_endingTier);
                int len = _cutsceneFrame < caps.Length ? caps[_cutsceneFrame].Length : 0;
                _cutsceneReveal = Mathf.MoveTowards(_cutsceneReveal, len, dt * 42f);
            }
        }

        // 按键推进：字未显示完→先补完；否则进入下一帧；末帧→结束（同开场动画手感）
        private void AdvanceEndingCutscene()
        {
            var caps = GetEndingCaptions(_endingTier);
            int len = _cutsceneFrame < caps.Length ? caps[_cutsceneFrame].Length : 0;
            if (_cutsceneAlpha > 0.5f && _cutsceneReveal < len - 0.5f) { _cutsceneReveal = len; return; }
            int frameCount = GetEndingFrames(_endingTier).Length;
            if (_cutsceneFrame >= frameCount - 1) { FinishEndingCutscene(); return; }
            _cutsceneFrame++;
            _cutsceneAlpha  = 0f;
            _cutsceneReveal = 0f;
        }

        private void FinishEndingCutscene()
        {
            if (_state != State.EndingCutscene) return;
            OnEndingCutsceneEnd?.Invoke();
            _state = State.Victory;
        }

        /// 公共 API：动画系统主动结束过场（或玩家跳过键）
        public void SkipEndingCutscene() => FinishEndingCutscene();

        private void TriggerFloorComplete()
        {
            _state = State.FloorComplete;
            _persistent.AddCurrency(clearReward);
            _persistent.Save();
        }

        private void AdvanceFloor()
        {
            CurrentFloor++;
            _floorRooms = GenerateFloor();
            if (_currentRoomRoot != null) { Destroy(_currentRoomRoot); _currentRoomRoot = null; }
            if (_player != null) _player.transform.position = _mapInfo.PlayerSpawn;
            SetFloorBackground();
            UpdateArenaColors();
            LoadRoom(0);
            _state = State.Playing;
            ShowBanner(GetFloorNarrative());
        }

        private const float DeathReturnDelay = 4f;
        private float _deathReturnAt;

        private void TriggerDeath()
        {
            _state = State.Death;
            _persistent.RecordRunResult(CurrentFloor, false, _enemiesKilled, Mathf.RoundToInt(_totalDamageDealt));
            _deathReturnAt = Time.time + DeathReturnDelay;
            StartCoroutine(AutoReturnToMenuRoutine());
        }

        private System.Collections.IEnumerator AutoReturnToMenuRoutine()
        {
            yield return new WaitForSeconds(DeathReturnDelay);
            if (_state == State.Death) ReturnToMenu();
        }

        private void CleanupRun()
        {
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);
            if (_arenaRoot != null)       Destroy(_arenaRoot);
            if (_player != null)          Destroy(_player);
            _playerHealth     = null;
            _currentRoomIndex = 0;
            _bannerMessage    = null;
            _bannerUntil      = 0f;
            _bossHealth       = null;
            _bossName         = null;
            _enemiesKilled    = 0;
            _totalDamageDealt = 0f;
            _activeTalents.Clear();
            _pendingTalent    = null;
        }

        // --------------------------------------------------------------------
        //  Floor generation
        // --------------------------------------------------------------------

        private List<string> GenerateFloor()
        {
            var rooms = new List<string>();

            var   pool   = GetFloorRoomPool();
            float total  = 0f;
            foreach (var e in pool) total += e.weight;

            // Combat room count increases per floor: Floor1=4, Floor2=5, Floor3=6
            int combatCount = nonBossRoomCount + (CurrentFloor - 1);
            for (int i = 0; i < combatCount; i++)
            {
                float roll = Random.value * total;
                float acc  = 0f;
                foreach (var e in pool)
                {
                    acc += e.weight;
                    if (roll <= acc) { rooms.Add(e.type); break; }
                }
            }

            // 商店固定在 Boss 前的最后一间，方便进 Boss 前补给
            rooms.Add("Shop");
            rooms.Add("Boss");
            return rooms;
        }

        private void LoadRoom(int index)
        {
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);
            _currentRoomIndex = index;

            if (index >= _floorRooms.Count)
            {
                TriggerVictory();
                return;
            }

            var type = _floorRooms[index];

            // 商店房 / Boss 房用规整矩形，其余房型用本层异形几何；仅在 矩形↔异形 切换时重建竞技场
            bool wantRect  = (type == "Shop" || type == "Boss");
            // 纯地板+外墙、无任何障碍/危险地块：商店；以及最终层 Boss 房（决战场地要空旷）
            bool wantClean = (type == "Shop") || (type == "Boss" && CurrentFloor >= maxFloor);
            if (wantRect != _arenaIsRect || wantClean != _arenaIsClean || _arenaRoot == null)
            {
                RebuildArenaGeometry(wantRect, wantClean);
                _arenaIsRect  = wantRect;
                _arenaIsClean = wantClean;
            }

            // Reset player to left spawn point（用当前房型几何的出生点）
            if (_player != null)
                _player.transform.position = _mapInfo.PlayerSpawn;

            if (index > 0) OnNewRoomEntered(); // timed-talent countdown (room 0 excluded)

            _currentRoomRoot = new GameObject($"Room_{index}_{type}");
            GameSignals.CombatInProgress = false; // 新房间默认非战斗；有刷怪波次时由 SpawnRoomWave 置位
            switch (type)
            {
                case "Monster":    BuildMonsterRoom();    break;
                case "Talent":     BuildTalentRoom();     break;
                case "Coin":       BuildCoinRoom();       break;
                case "Shop":       BuildShopRoom();       break;
                case "Boss":       BuildBossRoom();       break;
                case "HellTrial":  BuildHellTrialRoom();  break;
                case "FrostGrave": BuildFrostGraveRoom(); break;
                case "ChaosRift":  BuildChaosRiftRoom();  break;
            }

            TrySpawnStoryForRoom(CurrentFloor, index);
        }

        // --------------------------------------------------------------------
        //  剧情交互物 —— (floor, index) -> Resources 路径 + 偏移
        // --------------------------------------------------------------------

        // 缓存 Resources/Story/ 下全部交互物数据，避免每次切房间都重新扫描
        private static StoryInteractableData[] _storyDataCache;
        private static StoryInteractableData[] StoryDataAll =>
            _storyDataCache ?? (_storyDataCache = Resources.LoadAll<StoryInteractableData>("Story"));

        /// 扫描 Resources/Story/ 全部 StoryInteractableData，按 spawnFloor/spawnRoomIndex 匹配本房间，
        /// 用 data.spawnOffset 生成。彻底零代码新增交互物：只需 Create → Game → Narrative → Story Interactable
        /// 并填好 SpawnFloor/SpawnRoomIndex/SpawnOffset。
        private void TrySpawnStoryForRoom(int floor, int index)
        {
            _occupiedStoryCells.Clear();   // 每个房间重置「已占用」格，避免剧情物互相重叠
            foreach (var data in StoryDataAll)
            {
                if (data == null) continue;
                if (data.spawnFloor != floor) continue;
                if (data.spawnRoomIndex != index) continue;
                SpawnStoryFromData(data, data.spawnOffset);
            }
        }

        /// 通用：根据 StoryInteractableData 在当前房间生成一个剧情交互物。
        /// 视觉/触发器尺寸均由 data 控制；offset 相对 PlayerSpawn 放置。
        private void SpawnStoryFromData(StoryInteractableData data, Vector3 offsetFromSpawn)
        {
            if (_currentRoomRoot == null || data == null) return;

            var go = new GameObject($"Story_{data.objectId}");
            go.transform.SetParent(_currentRoomRoot.transform);
            // 放大剧情物显示尺寸（细节更清晰）；占位 footprint 同步放大，避免与邻物重叠
            Vector2 vs = data.visualScale * StoryDisplayBoost;
            // 按视觉大小找一块「可走 + 无危险 + 不压石柱/墙」且未被其它剧情物占用的格子
            go.transform.position = NearestSafeStoryCell(_mapInfo.PlayerSpawn + offsetFromSpawn, vs);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.sortingOrder = 5;                // 高于地板/墙(1)/危险格(3)/石柱(4)，剧情物清晰可见

            go.AddComponent<BoxCollider2D>();   // 大小在 ApplyData 中由 data 写入
            var si = go.AddComponent<StoryInteractable>();
            si.Data = data;                     // 触发 ApplyData：scale/color/collider/dialogue

            // 若提供了专属贴图（Resources/Story/Sprites/<objectId>.png）则覆盖白盒
            var spr = Resources.Load<Sprite>("Story/Sprites/" + data.objectId);
            if (spr != null)
            {
                sr.sprite = spr;
                sr.color  = Color.white;
                var bs = spr.bounds.size;   // 等比缩放放入 (放大后的)visualScale 框（不拉伸），触发盒贴合可见范围
                if (bs.x > 0.001f && bs.y > 0.001f)
                {
                    float fit = Mathf.Min(vs.x / bs.x, vs.y / bs.y);
                    go.transform.localScale = new Vector3(fit, fit, 1f);
                    var box = go.GetComponent<BoxCollider2D>();
                    if (box != null) box.size = new Vector2(bs.x, bs.y);
                }
            }
        }

        // 剧情物整体放大系数：贴图更大、细节更清晰（占位与碰撞盒同步放大）
        private const float StoryDisplayBoost = 1.6f;

        // 已被剧情物占用的格（每房间重置），避免多个剧情物落在同一/相邻格
        private readonly HashSet<Vector2Int> _occupiedStoryCells = new HashSet<Vector2Int>();

        // 找一块按 visualScale 大小都「可走、无危险、不压石柱/墙」且未占用的格子；找不到则兜底到最近可走格。
        private Vector3 NearestSafeStoryCell(Vector3 desired, Vector2 visualScale)
        {
            var start = Game.AI.NavGrid.WorldToCell(desired);
            int hw = Mathf.Max(1, Mathf.CeilToInt(visualScale.x * 0.5f - 0.5f));   // footprint 半宽(格)
            int hh = Mathf.Max(1, Mathf.CeilToInt(visualScale.y * 0.5f - 0.5f));   // footprint 半高(格)
            for (int rad = 0; rad < 16; rad++)
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                if (rad > 0 && Mathf.Abs(dx) != rad && Mathf.Abs(dy) != rad) continue;   // 只扫当前环
                int c = start.x + dx, r = start.y + dy;
                if (_occupiedStoryCells.Contains(new Vector2Int(c, r))) continue;
                if (!StoryFootprintClear(c, r, hw, hh)) continue;
                _occupiedStoryCells.Add(new Vector2Int(c, r));
                var w = Game.AI.NavGrid.CellToWorld(new Vector2Int(c, r));
                return new Vector3(w.x, w.y, 0f);
            }
            return NearestWalkableWorld(desired);   // 兜底
        }

        // footprint 内每格都必须可走（非墙/石柱）且无危险格
        private bool StoryFootprintClear(int c, int r, int hw, int hh)
        {
            for (int dy = -hh; dy <= hh; dy++)
            for (int dx = -hw; dx <= hw; dx++)
            {
                if (!Game.AI.NavGrid.IsWalkable(c + dx, r + dy)) return false;
                if (Game.AI.NavGrid.HazardAt(c + dx, r + dy) != 0) return false;
            }
            return true;
        }

        // --------------------------------------------------------------------
        //  Room builders
        // --------------------------------------------------------------------

        private GameObject SpawnRandomNormalEnemy(Vector3 pos, System.Action onDied)
        {
            if (_player == null) return null;
            // Weighted random enemy selection by floor theme
            float[] weights = GetFloorEnemyWeights();
            float   total   = 0f;
            foreach (var v in weights) total += v;
            float roll = Random.value * total;
            float acc  = 0f;
            int   pick = weights.Length - 1;
            for (int i = 0; i < weights.Length; i++) { acc += weights[i]; if (roll <= acc) { pick = i; break; } }

            int coins;
            GameObject enemy;
            var p    = _player.transform;
            var root = _currentRoomRoot.transform;
            switch (pick)
            {
                case 0:  enemy = EnemyFactory.SpawnSkeleton(pos, p, root);        coins = 3; break;
                case 1:  enemy = EnemyFactory.SpawnSoldier(pos, p, root);         coins = 4; break;
                case 2:  enemy = EnemyFactory.SpawnArcher(pos, p, root);          coins = 4; break;
                case 3:  enemy = EnemyFactory.SpawnBat(pos, p, root);             coins = 3; break;
                case 4:  enemy = EnemyFactory.SpawnShieldGuard(pos, p, root);     coins = 6; break;
                case 5:  enemy = EnemyFactory.SpawnPoisonSpider(pos, p, root);    coins = 3; break;
                case 6:  enemy = EnemyFactory.SpawnShadowAssassin(pos, p, root);  coins = 5; break;
                default: enemy = EnemyFactory.SpawnExplosiveDemon(pos, p, root);  coins = 4; break;
            }
            RegisterEnemy(enemy, coins, onDied);
            return enemy;
        }

        // Common: attach visual callbacks + special death effects + coin/death events
        private void RegisterEnemy(GameObject enemy, int baseCoins, System.Action onDied)
        {
            // Set physics layer so enemies are blocked by solid walls
            enemy.layer = 8;
            var col = enemy.GetComponent<CircleCollider2D>();
            if (col != null) col.isTrigger = false;
            ScaleEnemyStats(enemy, FloorScale);
            AttachVisualCallbacks(enemy);
            AttachSpecialDeathEffect(enemy);
            // 受击反馈：白闪 + 击退 + 命中停顿 + HP 着色
            if (enemy.GetComponent<EnemyHitFeedback>() == null)
                enemy.AddComponent<EnemyHitFeedback>();
            // 卡墙兜底：被击退穿模到墙外时拉回最近可走格
            if (enemy.GetComponent<EnemyStuckRecovery>() == null)
                enemy.AddComponent<EnemyStuckRecovery>();
            // 头顶血条
            if (enemy.GetComponent<EnemyHealthBar>() == null)
                enemy.AddComponent<EnemyHealthBar>();
            int c  = Mathf.RoundToInt(baseCoins * FloorScale);
            var hp = enemy.GetComponent<Health>();
            hp.OnDamaged += dmg => { _totalDamageDealt += dmg.Amount; };
            hp.OnDied += () => { RunCoins += c; PlayerPassiveEvents.RaisePlayerKilledEnemy(); _enemiesKilled++; };
            hp.OnDied += () => Destroy(enemy);
            hp.OnDied += onDied;
        }

        // Hit: floating damage number (flash handled by EnemyHitFeedback)
        private void AttachVisualCallbacks(GameObject enemy)
        {
            var hp = enemy.GetComponent<Health>();
            var tr = enemy.transform;
            hp.OnDamaged += dmg =>
            {
                if (tr != null) DamageNumbers.Instance?.Show(tr.position, dmg.Amount, dmg.IsCrit);
            };
        }

        // Special death effect (triggered before Destroy, transform is still valid)
        private void AttachSpecialDeathEffect(GameObject enemy)
        {
            var tag = enemy.GetComponent<EnemyTag>();
            var hp  = enemy.GetComponent<Health>();
            if (tag == null) return;

            switch (tag.type)
            {
                case EnemyType.PoisonSpider:
                    var spiderRoot = _currentRoomRoot;
                    hp.OnDied += () =>
                    {
                        if (enemy == null) return;
                        var parent = spiderRoot != null ? spiderRoot.transform : null;
                        EnemyFactory.SpawnPoisonPool(enemy.transform.position, 4f, 3f, 1f, parent, null);
                    };
                    break;

                case EnemyType.ExplosiveDemon:
                    var demonAI = enemy.GetComponent<ExplosiveDemonAI>();
                    hp.OnDied += () =>
                    {
                        if (demonAI != null && demonAI.HasExploded) return;
                        if (enemy != null) DoExplosionAt(enemy.transform.position);
                    };
                    break;
            }
        }

        private void DoExplosionAt(Vector3 pos)
        {
            foreach (var col in Physics2D.OverlapCircleAll(pos, 3f))
            {
                if (col.GetComponent<EnemyTag>() != null) continue; // don't damage other enemies
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = 40f, Type = DamageType.True, Source = null
                });
            }
            DamageNumbers.Instance?.Show(pos, 40f, false);
        }

        private void BuildMonsterRoom()
        {
            ShowBanner("Defeat all enemies → choose one of three weapons");
            MaybeAddAltar();
            SpawnFloorHazards();
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, () =>
            {
                ShowBanner("Victory! Choose a weapon!");
                DropWeaponChoices();
                OpenRightDoor();
            });
        }

        // Spawn 3 weapon choices; all disappear once the player picks one
        private void DropWeaponChoices()
        {
            var offers = GetRandomWeaponOffers(3);
            var gos    = new GameObject[offers.Length];

            for (int i = 0; i < offers.Length; i++)
            {
                var weapon = offers[i];
                float x  = -3f + i * 3f;
                var go   = new GameObject("WeaponChoice_" + weapon.Data.weaponName);
                go.transform.SetParent(_currentRoomRoot.transform, true);
                go.transform.position   = NearestSafeWorld(new Vector3(x, -1.5f, 0f));
                go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                var wspr = WeaponSprites.Get(weapon.Data.weaponName);   // 真实武器立绘，缺失则回退稀有度方块
                sr.sprite       = wspr != null ? wspr : MakeUnitSquareSprite();
                sr.color        = wspr != null ? Color.white : WeaponData.GetRarityColor(weapon.Data.rarity);
                sr.sortingOrder = 7;

                var col = go.AddComponent<CircleCollider2D>();
                col.radius    = 0.9f;
                col.isTrigger = true;

                var ped          = go.AddComponent<WeaponShopPedestal>();
                ped.Weapon       = weapon;
                ped.Price        = 0;
                ped.GetCoins     = () => 9999;
                ped.SpendCoins   = _ => { };
                ped.ShowMessage  = msg => ShowBanner(msg);
                gos[i]           = go;
            }

            // Destroy all choices when any one is selected
            for (int i = 0; i < gos.Length; i++)
            {
                var ped        = gos[i].GetComponent<WeaponShopPedestal>();
                var allGos     = gos;
                ped.OnPurchased = chosen =>
                {
                    foreach (var g in allGos) if (g != null) Destroy(g);
                    if (_player == null) return;
                    var handler = _player.GetComponent<PlayerWeaponHandler>();
                    if (handler == null) return;
                    if (TryHandleDuplicateWeapon(chosen, handler)) return;
                    handler.EquipWeapon(chosen, handler.ActiveSlotIndex);
                    ShowBanner($"Selected: {chosen.ShortName}");
                };
            }
        }


        private void BuildTalentRoom()
        {
            ShowBanner("Defeat all enemies → choose a talent");
            MaybeAddAltar();
            SpawnFloorHazards();
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, DropTalentChoices);
        }

        // (name, desc, stat, op, value, color, roomDuration)  -1 = permanent
        private static readonly (string name, string desc, StatType stat, ModifierOp op, float value, Color color, int rooms)[] TalentPool =
        {
            // ── Permanent talents ───────────────────────────────────
            ("Power",      "+20% Attack",              StatType.Attack,            ModifierOp.PercentMul, 0.20f, new Color(1f,   0.40f, 0.40f), -1),
            ("Swift",      "+25% Move Speed",          StatType.MoveSpeed,         ModifierOp.PercentMul, 0.25f, new Color(0.4f, 0.90f, 1f  ), -1),
            ("Vitality",   "+50 Max HP",               StatType.MaxHP,             ModifierOp.Flat,       50f,   new Color(1f,   0.90f, 0.30f), -1),
            ("Guard",      "+5 Defense",               StatType.Defense,           ModifierOp.Flat,        5f,   new Color(0.4f, 0.70f, 1f  ), -1),
            ("Keen Eye",   "+15% Crit Rate",           StatType.CritRate,          ModifierOp.Flat,       0.15f, new Color(1f,   0.85f, 0.20f), -1),
            ("Lethal",     "+25% Crit Damage",         StatType.CritDamage,        ModifierOp.Flat,       0.25f, new Color(1f,   0.50f, 0.10f), -1),
            ("Frenzy",     "+15% Attack Speed",        StatType.AttackSpeed,       ModifierOp.PercentMul, 0.15f, new Color(1f,   0.30f, 0.60f), -1),
            ("Arcane",     "+30% Skill Power",         StatType.SkillPower,        ModifierOp.PercentMul, 0.30f, new Color(0.7f, 0.40f, 1f  ), -1),
            ("Agility",    "+10% Cooldown Reduction",  StatType.CooldownReduction, ModifierOp.Flat,       0.10f, new Color(0.5f, 1f,   0.90f), -1),
            ("Fortune",    "+30% Coin Gain",           StatType.CoinGain,          ModifierOp.PercentMul, 0.30f, new Color(1f,   0.85f, 0.10f), -1),
            ("Iron Wall",  "+10 Defense",              StatType.Defense,           ModifierOp.Flat,       10f,   new Color(0.3f, 0.60f, 1f  ), -1),
            ("Titan",      "+100 Max HP",              StatType.MaxHP,             ModifierOp.Flat,      100f,   new Color(0.9f, 0.40f, 0.40f), -1),
            ("Momentum",   "+20% Move Speed",          StatType.MoveSpeed,         ModifierOp.PercentMul, 0.20f, new Color(0.3f, 0.95f, 0.50f), -1),
            ("Might",      "+30% Attack",              StatType.Attack,            ModifierOp.PercentMul, 0.30f, new Color(1f,   0.20f, 0.20f), -1),
            // ── Permanent talents (extended) ────────────────────────
            ("Bloodlust",  "+80 Max HP",               StatType.MaxHP,             ModifierOp.Flat,       80f,   new Color(0.9f, 0.30f, 0.30f), -1),
            ("Focus",      "+12% Cooldown Reduction",  StatType.CooldownReduction, ModifierOp.Flat,       0.12f, new Color(0.4f, 1f,   0.85f), -1),
            ("Nimble",     "+20% Attack Speed",        StatType.AttackSpeed,       ModifierOp.PercentMul, 0.20f, new Color(0.8f, 0.55f, 1f  ), -1),
            ("Battle Will","+35% Crit Damage",         StatType.CritDamage,        ModifierOp.Flat,       0.35f, new Color(1f,   0.40f, 0.05f), -1),
            ("Iron Body",  "+8 Defense",               StatType.Defense,           ModifierOp.Flat,        8f,   new Color(0.5f, 0.65f, 1f  ), -1),
            ("Greed",      "+40% Coin Gain",           StatType.CoinGain,          ModifierOp.PercentMul, 0.40f, new Color(1f,   0.90f, 0.05f), -1),
            ("Unyielding", "+150 Max HP",              StatType.MaxHP,             ModifierOp.Flat,      150f,   new Color(0.9f, 0.20f, 0.20f), -1),
            ("Omnimagic",  "+50% Skill Power",         StatType.SkillPower,        ModifierOp.PercentMul, 0.50f, new Color(0.65f,0.30f, 1f  ), -1),
            ("Skybreak",   "+25% Attack",              StatType.Attack,            ModifierOp.PercentMul, 0.25f, new Color(1f,   0.35f, 0.35f), -1),
            ("Godspeed",   "+35% Move Speed",          StatType.MoveSpeed,         ModifierOp.PercentMul, 0.35f, new Color(0.3f, 0.95f, 1f  ), -1),
            ("Edge",       "+20% Crit Rate",           StatType.CritRate,          ModifierOp.Flat,       0.20f, new Color(1f,   0.80f, 0.10f), -1),
            ("Divine Shield","+15 Defense",            StatType.Defense,           ModifierOp.Flat,       15f,   new Color(0.25f,0.55f, 1f  ), -1),
            // ── Timed talents (expire after N rooms) ─────────────────
            ("Burst",      "+80% Attack (3 rooms)",    StatType.Attack,            ModifierOp.PercentMul, 0.80f, new Color(1f,   0.05f, 0.05f),  3),
            ("Haste",      "+50% Atk Speed (3 rooms)", StatType.AttackSpeed,       ModifierOp.PercentMul, 0.50f, new Color(0.9f, 0.55f, 1f  ),  3),
            ("Armor",      "+25 Defense (4 rooms)",    StatType.Defense,           ModifierOp.Flat,       25f,   new Color(0.5f, 0.80f, 1f  ),  4),
            ("Rampage",    "+35% Crit Rate (3 rooms)", StatType.CritRate,          ModifierOp.Flat,       0.35f, new Color(1f,   0.95f, 0.05f),  3),
            ("Berserk",    "+120% Attack (2 rooms)",   StatType.Attack,            ModifierOp.PercentMul, 1.20f, new Color(1f,   0.02f, 0.02f),  2),
            ("Overdrive",  "+80% Atk Speed (2 rooms)", StatType.AttackSpeed,       ModifierOp.PercentMul, 0.80f, new Color(0.7f, 0.30f, 1f  ),  2),
            ("Spellstorm", "+60% Skill Power (3 rooms)",StatType.SkillPower,       ModifierOp.PercentMul, 0.60f, new Color(0.55f,0.15f, 1f  ),  3),
            ("Steel Wall", "+50 Defense (2 rooms)",    StatType.Defense,           ModifierOp.Flat,       50f,   new Color(0.4f, 0.75f, 1f  ),  2),
            // Special-effect talents (stat modifier value=0, effect implemented in OnRoomEntered)
            ("Renewal",      "Restore 5% max HP on each new room entry",  StatType.MaxHP,    ModifierOp.Flat, 0f, new Color(0.3f, 1f,   0.55f), -1),
            ("Regeneration", "Restore 10 HP on each new room entry",      StatType.MaxHP,    ModifierOp.Flat, 0f, new Color(0.4f, 1f,   0.70f), -1),
            ("Merchant",     "Gain 8 coins on each new room entry",       StatType.CoinGain, ModifierOp.Flat, 0f, new Color(1f,   0.88f, 0.20f), -1),
        };

        private (string name, string desc, StatType stat, ModifierOp op, float value, Color color, int rooms)[]
            PickRandomTalents(int count)
        {
            var indices = new List<int>();
            for (int i = 0; i < TalentPool.Length; i++) indices.Add(i);
            var result  = new (string, string, StatType, ModifierOp, float, Color, int)[Mathf.Min(count, TalentPool.Length)];
            for (int i = 0; i < result.Length; i++)
            {
                int ri    = Random.Range(0, indices.Count);
                result[i] = TalentPool[indices[ri]];
                indices.RemoveAt(ri);
            }
            return result;
        }

        private void DropTalentChoices()
        {
            var picks   = PickRandomTalents(3);
            var pickups = new List<TalentPickup>();
            foreach (var def in picks)
            {
                var talent            = ScriptableObject.CreateInstance<TalentData>();
                talent.talentName     = def.name;
                talent.description    = def.desc;
                talent.roomDuration   = def.rooms;
                talent.modifiers.Add(new StatModifierEntry { stat = def.stat, op = def.op, value = def.value });
                pickups.Add(SpawnTalentOrb(talent, def.color));
            }
            for (int i = 0; i < pickups.Count; i++)
                pickups[i].transform.position = NearestSafeWorld(new Vector3(-3.5f + 3.5f * i, 0f, 0f));

            var snapshot = new List<TalentPickup>(pickups);
            foreach (var p in snapshot)
            {
                var self = p;
                self.OnPicked += chosen =>
                {
                    ApplyTalentToPlayer(chosen);
                    foreach (var other in snapshot)
                        if (other != null && other != self) Destroy(other.gameObject);
                    OpenRightDoor();
                };
            }
        }

        private void BuildCoinRoom()
        {
            int reward = 30 + CurrentFloor * 10;
            ShowBanner($"Defeat all enemies → earn {reward} coins");
            MaybeAddAltar();
            SpawnFloorHazards();
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, () =>
            {
                RunCoins += reward;
                ShowBanner($"Victory! Earned {reward} coins!");
                OpenRightDoor();
            });
        }

        // Weapon factories grouped by rarity (for shop quality tiers)
        private static readonly System.Func<WeaponInstance>[][] WeaponsByRarity =
        {
            new System.Func<WeaponInstance>[] { WeaponLibrary.IronDagger,   WeaponLibrary.IronSword,         WeaponLibrary.IronGreatsword, WeaponLibrary.IronMallet,   WeaponLibrary.WoodenBow,    WeaponLibrary.BoneBow,      WeaponLibrary.WoodStaff    },
            new System.Func<WeaponInstance>[] { WeaponLibrary.SteelDagger,  WeaponLibrary.KnightSword,       WeaponLibrary.CrescentBlade,  WeaponLibrary.WarriorGreatsword, WeaponLibrary.HunterBow, WeaponLibrary.ElfBow,      WeaponLibrary.MagicStaff   },
            new System.Func<WeaponInstance>[] { WeaponLibrary.VenomFang,    WeaponLibrary.HolyBlade,         WeaponLibrary.FrostLance,     WeaponLibrary.ArmorBreaker,   WeaponLibrary.CloudPiercer, WeaponLibrary.ThunderBow, WeaponLibrary.FrostStaff   },
            new System.Func<WeaponInstance>[] { WeaponLibrary.PhantomBlade, WeaponLibrary.DragonAbyssSword,  WeaponLibrary.DoomBlade,      WeaponLibrary.CelestialBow, WeaponLibrary.ChaosWand    },
        };

        private WeaponInstance GetWeaponOfRarity(int rarityIndex)
        {
            var pool = WeaponsByRarity[Mathf.Clamp(rarityIndex, 0, WeaponsByRarity.Length - 1)];
            return pool[Random.Range(0, pool.Length)]();
        }

        private static readonly int[][] ShopRarityTable =
        {
            new[] { 0, 0, 1, 1, 2, 3 }, // Floor 1: WW GG B P
            new[] { 0, 1, 1, 2, 2, 3 }, // Floor 2: W GG BB P
            new[] { 1, 1, 2, 2, 3, 3 }, // Floor 3: GG BB PP
        };

        private static readonly int[] WeaponBasePrice = { 15, 25, 40, 65 };

        private void BuildShopRoom()
        {
            ShowBanner("Shop — approach and press E to buy (can skip)");
            OpenRightDoor();
            DeactivateRoomHazards();   // 商店房不出现任何危险格（任意一层）
            // 商店为「纯地板+墙壁」的规整房间：不再摆放任何石柱/陷阱/岩浆。

            // 可视化布局预制体：Scene/Prefab 视图里可拖拽各锚点；缺失则回退到代码内置坐标。
            var layout = SpawnShopLayout();
            var decor  = Resources.Load<ShopDecorData>("Shop/Default");   // 仅取武器大小/发光等数据

            // 商人：预制体自带贴图；无布局时回退到运行时贴图/方块
            if (layout == null || layout.shopkeeper == null)
            {
                if (SpawnShopSprite("Shop/Sprites/shopkeeper", new Vector3(0f, 4.5f, 0f), worldHeight: 3.6f, order: 6) == null)
                    SpawnShopkeeper(new Vector3(0f, 3.2f, 0f));
            }

            int floorIdx   = Mathf.Clamp(CurrentFloor - 1, 0, ShopRarityTable.Length - 1);
            int[] rarities = ShopRarityTable[floorIdx];
            float priceScale = 1f + (CurrentFloor - 1) * 0.3f;

            // 武器陈列：优先用布局锚点（货架贴图已在预制体里）；缺失回退到等距排布 + 即时货架
            Vector3 wStart  = new Vector3(-5.5f, 1.5f, 0f);
            float   wSpace  = 2.2f;
            for (int i = 0; i < rarities.Length; i++)
            {
                Vector3 slot;
                if (layout != null && layout.weaponSlots != null &&
                    i < layout.weaponSlots.Length && layout.weaponSlots[i] != null)
                {
                    slot = layout.weaponSlots[i].position;
                }
                else
                {
                    slot = wStart + new Vector3(i * wSpace, 0f, 0f);
                    SpawnShelfSegment(new Vector3(slot.x, slot.y - 0.15f, 0f), wSpace * 0.92f, 1.7f);
                }
                int   ri    = rarities[i];
                int   price = Mathf.RoundToInt(WeaponBasePrice[ri] * priceScale);
                SpawnShopWeaponPedestal(slot, GetWeaponOfRarity(ri), price, decor);
            }

            // Forge / Talent / Enchant / Potion 台座（位置取布局锚点，缺失回退默认坐标）
            int uses = Mathf.Min(CurrentFloor, 2);
            int forgePrice   = Mathf.RoundToInt((20 + CurrentFloor * 5) * priceScale);
            int enchantPrice = Mathf.RoundToInt((30 + CurrentFloor * 5) * priceScale);
            int talentPrice  = Mathf.RoundToInt(30 * priceScale);
            int potionPrice  = Mathf.RoundToInt(25 * priceScale);

            Vector3 pForge   = layout != null ? layout.Pos(layout.forge,      new Vector3(-3f, -1.5f, 0f)) : new Vector3(-3f, -1.5f, 0f);
            Vector3 pTalent  = layout != null ? layout.Pos(layout.talentDraw, new Vector3( 0f, -1.5f, 0f)) : new Vector3( 0f, -1.5f, 0f);
            Vector3 pEnchant = layout != null ? layout.Pos(layout.enchant,    new Vector3( 3f, -1.5f, 0f)) : new Vector3( 3f, -1.5f, 0f);
            Vector3 pPotion  = layout != null ? layout.Pos(layout.potion,     new Vector3( 0f, -3.0f, 0f)) : new Vector3( 0f, -3.0f, 0f);

            SpawnActionPedestal(pForge, ActionPedestal.ActionType.Forge,   forgePrice,   uses);
            SpawnTalentDrawPedestal(pTalent, talentPrice);
            SpawnActionPedestal(pEnchant, ActionPedestal.ActionType.Enchant, enchantPrice, uses);
            SpawnHealthPotionPedestal(pPotion, potionPrice);
        }

        // 实例化商店可视化布局预制体到当前房间（位于房间中心）；缺失返回 null。
        private ShopLayout SpawnShopLayout()
        {
            var prefab = Resources.Load<GameObject>("Shop/ShopLayout");
            if (prefab == null || _currentRoomRoot == null) return null;
            var inst = Instantiate(prefab, _currentRoomRoot.transform);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            return inst.GetComponent<ShopLayout>();
        }

        // 在商人外围用石柱摆一个对称图案（1×1 吸附网格；位于上方，不挡下方台座/购买）
        private void SpawnShopPillars()
        {
            if (_currentRoomRoot == null) return;
            var pillarSpr = Resources.Load<Sprite>("Tiles/Pillar");
            Vector2[] spots =
            {
                new Vector2(-6f, 4.5f), new Vector2(6f, 4.5f),   // 两侧（齐商人）
                new Vector2(-6f, 2.5f), new Vector2(6f, 2.5f),   // 两侧下
                new Vector2(-4f, 6.9f), new Vector2(4f, 6.9f),   // 身后外
                new Vector2( 0f, 6.9f),                          // 身后正中（高于商人头顶，不遮挡）
            };
            foreach (var s in spots)
            {
                var go = new GameObject("ShopPillar");
                go.transform.SetParent(_currentRoomRoot.transform, true);
                go.transform.position   = new Vector3(s.x, s.y, 0f);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = pillarSpr != null ? pillarSpr : MakeUnitSquareSprite();
                sr.sortingOrder = 2;
                go.AddComponent<BoxCollider2D>().size = new Vector2(0.85f, 0.85f);
            }
        }

        // 在指定位置放一张商店精灵（按目标世界高度等比缩放）；缺图返回 null。
        private GameObject SpawnShopSprite(string resource, Vector3 pos, float worldHeight, int order)
        {
            var spr = Resources.Load<Sprite>(resource);
            if (spr == null) return null;
            var go = new GameObject(resource.Substring(resource.LastIndexOf('/') + 1));
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position = pos;
            var bs = spr.bounds.size;
            float s = bs.y > 0.001f ? worldHeight / bs.y : 1f;
            go.transform.localScale = new Vector3(s, s, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = order;
            return go;
        }

        // 单段武器货架：在指定位置铺一段货架图（按目标宽高拉伸），渲染在武器之后。缺图回退木板货架。
        private void SpawnShelfSegment(Vector3 pos, float width, float height)
        {
            var spr = Resources.Load<Sprite>("Shop/Sprites/shelf");
            if (spr == null) { SpawnShopShelf(pos, width); return; }
            var go = new GameObject("ShopShelf");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position = pos;
            var bs = spr.bounds.size;
            go.transform.localScale = new Vector3(width  / Mathf.Max(0.001f, bs.x),
                                                  height / Mathf.Max(0.001f, bs.y), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 3;   // 在武器(order 8)之后
        }

        // ── 商店视觉装饰 ─────────────────────────────────────────────────

        /// 数据驱动版：按 ShopDecorGroup 在场景里组装一组像素方块
        private void SpawnDecorGroup(ShopDecorGroup g)
        {
            if (g == null || !g.enabled || g.parts == null || g.parts.Count == 0) return;
            var root = new GameObject(string.IsNullOrEmpty(g.name) ? "DecorGroup" : g.name);
            root.transform.SetParent(_currentRoomRoot.transform, true);
            root.transform.position = g.anchor;
            foreach (var p in g.parts)
            {
                if (p == null) continue;
                MakeShopPart(root.transform, p.localPos, p.size, p.color, p.sortingOrder);
            }
        }

        /// 商店货架：一条木色长板 + 上下两根支架，背景层
        private void SpawnShopShelf(Vector3 center, float width)
        {
            var root = new GameObject("ShopShelf");
            root.transform.SetParent(_currentRoomRoot.transform, true);
            root.transform.position = center;

            // 主板（深木色）
            MakeShopPart(root.transform, new Vector2(0f, 0f),   new Vector2(width, 0.45f),
                new Color(0.36f, 0.22f, 0.12f), order: 4);
            // 板上沿亮色（高光）
            MakeShopPart(root.transform, new Vector2(0f, 0.20f), new Vector2(width, 0.06f),
                new Color(0.62f, 0.42f, 0.22f), order: 5);
            // 板下沿阴影
            MakeShopPart(root.transform, new Vector2(0f, -0.21f), new Vector2(width, 0.05f),
                new Color(0.18f, 0.10f, 0.05f), order: 5);
            // 三根立柱
            for (int i = -1; i <= 1; i++)
            {
                MakeShopPart(root.transform, new Vector2(i * width * 0.36f, -0.55f),
                    new Vector2(0.20f, 1.0f), new Color(0.28f, 0.18f, 0.08f), order: 4);
            }
        }

        /// 商店老板（程序化像素小人，胡萝卜色衣服 + 灰色兜帽 + 柜台）
        private void SpawnShopkeeper(Vector3 pos)
        {
            var root = new GameObject("Shopkeeper");
            root.transform.SetParent(_currentRoomRoot.transform, true);
            root.transform.position = pos;

            // 兜帽（暗灰）
            MakeShopPart(root.transform, new Vector2(0f, 0.55f), new Vector2(1.05f, 0.85f),
                new Color(0.22f, 0.21f, 0.28f), order: 6);
            // 脸（暖肤色）
            MakeShopPart(root.transform, new Vector2(0f, 0.42f), new Vector2(0.55f, 0.45f),
                new Color(0.95f, 0.78f, 0.65f), order: 7);
            // 眼睛（两点）
            MakeShopPart(root.transform, new Vector2(-0.13f, 0.46f), new Vector2(0.08f, 0.10f),
                new Color(0.10f, 0.07f, 0.05f), order: 8);
            MakeShopPart(root.transform, new Vector2( 0.13f, 0.46f), new Vector2(0.08f, 0.10f),
                new Color(0.10f, 0.07f, 0.05f), order: 8);
            // 大胡子
            MakeShopPart(root.transform, new Vector2(0f, 0.27f), new Vector2(0.55f, 0.20f),
                new Color(0.78f, 0.75f, 0.72f), order: 8);
            // 长袍（深紫红）
            MakeShopPart(root.transform, new Vector2(0f, -0.30f), new Vector2(1.15f, 1.10f),
                new Color(0.50f, 0.18f, 0.22f), order: 6);
            // 腰带（金）
            MakeShopPart(root.transform, new Vector2(0f, -0.18f), new Vector2(1.15f, 0.10f),
                new Color(0.92f, 0.78f, 0.30f), order: 7);

            // 头顶招牌（小灯笼+「商店」字感觉用方块）
            MakeShopPart(root.transform, new Vector2(0f, 1.25f), new Vector2(1.5f, 0.32f),
                new Color(0.10f, 0.08f, 0.12f), order: 6);
            MakeShopPart(root.transform, new Vector2(0f, 1.25f), new Vector2(1.42f, 0.22f),
                new Color(0.95f, 0.78f, 0.30f), order: 7);
        }

        // 一个统一的"贴像素方块"工具：返回子物体便于继续装饰
        private GameObject MakeShopPart(Transform parent, Vector2 localPos, Vector2 size, Color color, int order)
        {
            var go = new GameObject("part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            go.transform.localScale    = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = color;
            sr.sortingOrder = order;
            return go;
        }

        private void SpawnActionPedestal(Vector3 pos, ActionPedestal.ActionType actionType, int price, int uses)
        {
            pos = NearestWalkableWorld(pos);   // 异形房：固定坐标可能落墙，吸附到最近可走格
            bool isForge = actionType == ActionPedestal.ActionType.Forge;
            var go = new GameObject(isForge ? "ForgeAltar" : "EnchantAltar");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = isForge ? InteractableSprites.Anvil() : InteractableSprites.EnchantCrystal();
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.8f;
            col.isTrigger = true;

            var pedestal         = go.AddComponent<ActionPedestal>();
            pedestal.action      = actionType;
            pedestal.price       = price;
            pedestal.usesLeft    = uses;
            pedestal.GetCoins    = () => RunCoins;
            pedestal.SpendCoins  = amt => RunCoins -= amt;
            pedestal.ShowMessage = msg => ShowBanner(msg);
            pedestal.TryApplyAction = () =>
            {
                var handler = _player?.GetComponent<PlayerWeaponHandler>();
                if (handler == null) return false;
                // Try active slot first, then other
                for (int i = 0; i < 2; i++)
                {
                    int slotIdx = (handler.ActiveSlotIndex + i) % 2;
                    var w = handler.Slots[slotIdx];
                    if (w == null) continue;
                    bool ok = actionType == ActionPedestal.ActionType.Forge ? w.TryUpgrade() : w.TryEnchant();
                    if (ok)
                    {
                        handler.RefreshWeaponHPBonus(slotIdx);
                        string verb = isForge ? "Forged" : "Enchanted";
                        ShowBanner($"{verb}! {w.ShortName}");
                        return true;
                    }
                }
                ShowBanner(isForge ? "All weapons are already at max forge level!" : "No weapon to enchant (requires Blue/Purple quality)!");
                return false;
            };
        }

        private void SpawnHealthPotionPedestal(Vector3 pos, int price)
        {
            pos = NearestWalkableWorld(pos);   // 异形房：吸附到最近可走格
            var go = new GameObject("HealthPotion");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = InteractableSprites.Potion();
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.8f;
            col.isTrigger = true;

            var pedestal         = go.AddComponent<ActionPedestal>();
            pedestal.action      = ActionPedestal.ActionType.HealthPotion;
            pedestal.price       = price;
            pedestal.usesLeft    = 1;
            pedestal.GetCoins    = () => RunCoins;
            pedestal.SpendCoins  = amt => RunCoins -= amt;
            pedestal.ShowMessage = msg => ShowBanner(msg);
            pedestal.TryApplyAction = () =>
            {
                if (_playerHealth == null) return false;
                float heal = _playerHealth.Max * 0.40f;
                _playerHealth.Heal(heal);
                ShowBanner($"Health potion used! Restored {Mathf.RoundToInt(heal)} HP!");
                return true;
            };
        }

        private void SpawnShopWeaponPedestal(Vector3 pos, WeaponInstance weapon, int price, ShopDecorData decor = null)
        {
            pos = NearestWalkableWorld(pos);   // 异形房：吸附到最近可走格
            var go = new GameObject("ShopWeapon_" + weapon.Data.weaponName);
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = decor != null ? decor.weaponScale : new Vector3(1.1f, 1.1f, 1f);

            // 武器实物精灵作为陈列；rarity 颜色变成发光底座
            var sr = go.AddComponent<SpriteRenderer>();
            var wsprite = WeaponSprites.Get(weapon.Data.weaponName);
            sr.sprite       = wsprite != null ? wsprite : MakeUnitSquareSprite();
            sr.color        = Color.white;
            sr.sortingOrder = decor != null ? decor.weaponSortingOrder : 8;

            // 发光底座（rarity 颜色）—— 货架格里的浅色衬底
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var glowPos   = decor != null ? decor.weaponGlowLocalPos : new Vector2(0f, -0.08f);
            var glowScale = decor != null ? decor.weaponGlowScale    : new Vector2(0.95f, 0.30f);
            float gAlpha  = decor != null ? decor.weaponGlowAlpha    : 0.55f;
            glow.transform.localPosition = new Vector3(glowPos.x, glowPos.y, 0f);
            glow.transform.localScale    = new Vector3(glowScale.x, glowScale.y, 1f);
            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite       = MakeUnitSquareSprite();
            var rc = WeaponData.GetRarityColor(weapon.Data.rarity);
            glowSr.color        = new Color(rc.r, rc.g, rc.b, gAlpha);
            glowSr.sortingOrder = decor != null ? decor.weaponGlowSortingOrder : 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = decor != null ? decor.weaponColliderRadius : 0.85f;
            col.isTrigger = true;

            var pedestal         = go.AddComponent<WeaponShopPedestal>();
            pedestal.Weapon      = weapon;
            pedestal.Price       = price;
            pedestal.GetCoins    = () => RunCoins;
            pedestal.SpendCoins  = amt => RunCoins -= amt;
            pedestal.ShowMessage = msg => ShowBanner(msg);
            pedestal.OnPurchased = w =>
            {
                if (_player == null) return;
                var handler = _player.GetComponent<PlayerWeaponHandler>();
                if (handler == null) return;
                if (TryHandleDuplicateWeapon(w, handler)) return;
                handler.EquipWeapon(w, handler.ActiveSlotIndex);
                ShowBanner($"Purchased and equipped: {w.ShortName}  (-{price} coins)");
            };
        }

        private void SpawnTalentDrawPedestal(Vector3 pos, int price)
        {
            pos = NearestWalkableWorld(pos);   // 异形房：吸附到最近可走格
            var go = new GameObject("TalentDraw");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = InteractableSprites.Tome();
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.9f;
            col.isTrigger = true;

            var shop         = go.AddComponent<ShopPedestal>();
            var drawn        = GenerateRandomTalent();
            shop.talent      = drawn;
            shop.price       = price;
            shop.GetCoins    = () => RunCoins;
            shop.SpendCoins  = amt => RunCoins -= amt;
            shop.OnPurchased = t =>
            {
                ApplyTalentToPlayer(t);
                ShowBanner($"Talent drawn: {t.talentName}!");
                Destroy(go);
            };
        }


        private TalentData GenerateRandomTalent()
        {
            var d             = TalentPool[Random.Range(0, TalentPool.Length)];
            var t             = ScriptableObject.CreateInstance<TalentData>();
            t.talentName      = d.name;
            t.description     = d.desc;
            t.roomDuration    = d.rooms;
            t.modifiers.Add(new StatModifierEntry { stat = d.stat, op = d.op, value = d.value });
            return t;
        }


        private void BuildBossRoom()
        {
            if (_player == null) return;
            switch (CurrentFloor)
            {
                case 1:  BuildFloor1Boss(); break;
                case 2:  BuildFloor2Boss(); break;
                default: BuildFloor3Boss(); break;
            }
        }

        // Floor 1: Hell Giant — lava pools + ground stomp
        private void BuildFloor1Boss()
        {
            ShowBanner("BOSS — Hell Giant descends!");
            var boss   = EnemyFactory.SpawnHellGiant(NearestWalkableWorld(new Vector3(0f, 2.5f, 0f)),
                             _player.transform, _currentRoomRoot.transform, null);
            var bossAI = boss.GetComponent<HellGiantAI>();
            var roomTr = _currentRoomRoot.transform;
            bossAI.SpawnLavaCallback = (pos, dps, lt, r) =>
                EnemyFactory.SpawnLavaPool(pos, dps, lt, r, roomTr, boss);
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // Floor 2: Frost Lich — Frost Nova + ice spike volley
        private void BuildFloor2Boss()
        {
            ShowBanner("BOSS — Frost Lich appears! Beware the frost!");
            var boss = EnemyFactory.SpawnFrostLich(NearestWalkableWorld(new Vector3(0f, 2.5f, 0f)),
                           _player.transform, _currentRoomRoot.transform);
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // Floor 3: Chaos Lord — Chaos Burst + summon legion
        private void BuildFloor3Boss()
        {
            ShowBanner("BOSS — Chaos Lord appears! The final battle!");
            var boss   = EnemyFactory.SpawnChaosLord(NearestWalkableWorld(new Vector3(0f, 2.5f, 0f)),
                             _player.transform, _currentRoomRoot.transform);
            var bossAI = boss.GetComponent<ChaosLordAI>();
            bossAI.SpawnMinionCallback = pos => SpawnRandomNormalEnemy(pos, () => { });
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // Boss common events: hit flash, floating damage, death settlement
        private void RegisterBossEvents(GameObject boss)
        {
            var bossHp = boss.GetComponent<Health>();
            var bossTr = boss.transform;
            _bossHealth = bossHp;
            _bossName   = boss.name;

            // 进入 Boss 战，切到战斗 BGM
            AudioManager.Get().PlayMusic("boss_battle");

            // Boss 受击反馈：只做白闪 + HP 着色，无击退（Boss 体型大不应被弹飞）
            var feedback = boss.AddComponent<EnemyHitFeedback>();
            feedback.KnockbackForce = 0f;

            bossHp.OnDamaged += dmg =>
            {
                _totalDamageDealt += dmg.Amount;
                if (bossTr != null) DamageNumbers.Instance?.Show(bossTr.position, dmg.Amount, dmg.IsCrit);
            };
            bossHp.OnDied += () =>
            {
                _enemiesKilled++;
                _bossHealth = null;
                _bossName   = null;
                PlayerPassiveEvents.RaisePlayerKilledEnemy();
                Destroy(boss);
                DeactivateRoomHazards();   // Boss 战结束：清掉残留岩浆池/危险格

                // 最终层 Boss 击败：若已满足隐藏 Boss 条件且尚未召唤过 → 进入「王国之罪」战
                if (CurrentFloor >= maxFloor)
                {
                    if (!_hiddenBossSpawned && IsHiddenBossUnlocked()) SpawnHiddenBoss();
                    else { AudioManager.Get().PlayMusic("dungeon_ambient_1"); TriggerVictory(); }
                }
                else
                {
                    AudioManager.Get().PlayMusic("dungeon_ambient_1");
                    TriggerFloorComplete();
                }
            };
        }

        // ── 隐藏 Boss「王国之罪」 ───────────────────────────────────────────
        // 触发条件：4 个关键真相旗（设计文档对应 工匠名册 / 教会沉默 / 王室驳回 / 王座罪行）
        private static readonly string[] HiddenBossRequiredFlags = {
            "truth_artisan_ledger",
            "truth_church_silence",
            "truth_royal_rejected_stop",
            "truth_kingdom_guilt",
        };

        private bool _hiddenBossSpawned;

        public bool IsHiddenBossUnlocked()
        {
            if (_persistent == null) return false;
            foreach (var f in HiddenBossRequiredFlags)
                if (!_persistent.HasTruthFlag(f)) return false;
            return true;
        }

        private void SpawnHiddenBoss()
        {
            _hiddenBossSpawned = true;
            ShowBanner("The monster wears the crown, seated upon the ground.");

            // 战前对话铺垫：用现有 DialogueBox 播放 4 句铭文
            var lines = new System.Collections.Generic.List<Game.Narrative.DialogueLine> {
                new Game.Narrative.DialogueLine("Narrator",     null, "Deep in the corridor, the broken throne slowly rises. It was never merely a seat."),
                new Game.Narrative.DialogueLine("Kingdom's Guilt", null, "You wish to slay the monster."),
                new Game.Narrative.DialogueLine("Kingdom's Guilt", null, "But the monster did not come from the Void."),
                new Game.Narrative.DialogueLine("Kingdom's Guilt", null, "The monster wears the crown, seated upon the ground."),
            };
            Game.Narrative.DialogueBox.Get().Play(lines, () =>
            {
                // 复用 ChaosLord 实体外观；统计/缩放/颜色/AI 全部按 KingdomGuilt SO 覆盖
                // 固定生成在房间正中心（KingdomGuiltAI 会冻结刚体，全程不移动）
                var boss   = EnemyFactory.SpawnChaosLord(NearestWalkableWorld(new Vector3(0f, 0f, 0f)),
                                 _player.transform, _currentRoomRoot.transform);
                boss.name  = "Kingdom_Guilt";

                var kg = BossStatsRegistry.Get("kingdom_guilt");
                // ×1.3：在原立绘缩放基础上再增大一号（最终 Boss 体型更具压迫感）
                float kgScaleMul = (kg != null ? (kg.visualScale / 1.4f) : 1.6f) * 1.3f;
                boss.transform.localScale *= kgScaleMul;
                var sr = boss.GetComponent<SpriteRenderer>();
                var kgSprite = LoadKingdomGuiltSprite();
                if (sr != null)
                {
                    if (kgSprite != null)
                    {
                        // 专属立绘：覆盖复用自 ChaosLord 的程序化精灵，保留原色（不做金色叠染）
                        sr.sprite = kgSprite;
                        sr.color  = Color.white;
                        // 单一立绘，禁用「前/背面切换」否则转身会变回占位方块
                        var facing = boss.GetComponent<EnemyFacing>();
                        if (facing != null) Destroy(facing);
                    }
                    else
                    {
                        sr.color = kg != null ? kg.tintColor : new Color(0.92f, 0.78f, 0.30f);
                    }
                }

                // 移除原 ChaosLord AI，挂上自定义 AI
                var oldAI = boss.GetComponent<ChaosLordAI>();
                if (oldAI != null) Destroy(oldAI);
                var bossAI = boss.AddComponent<KingdomGuiltAI>();
                bossAI.target = _player.transform;

                // 覆盖统计：HP/ATK/DEF/SPD 直接用 KG SO 数值（再叠 FloorScale）
                var kgStats = boss.GetComponent<CharacterStats>();
                if (kgStats != null && kg != null)
                {
                    kgStats.SetBase(StatType.MaxHP,     kg.maxHp     * FloorScale);
                    kgStats.SetBase(StatType.Attack,    kg.attack    * FloorScale);
                    kgStats.SetBase(StatType.Defense,   kg.defense);
                    kgStats.SetBase(StatType.MoveSpeed, kg.moveSpeed);
                    boss.GetComponent<Health>()?.Heal(99999f);
                }
                else
                {
                    ScaleEnemyStats(boss, FloorScale * 2.5f);
                }

                _bossHealth = boss.GetComponent<Health>();
                _bossName   = "Kingdom's Guilt";
                AudioManager.Get().PlayMusic("boss_battle");
                var feedback = boss.AddComponent<EnemyHitFeedback>();
                feedback.KnockbackForce = 0f;
                _bossHealth.OnDamaged += dmg =>
                {
                    _totalDamageDealt += dmg.Amount;
                    if (boss != null) DamageNumbers.Instance?.Show(boss.transform.position, dmg.Amount, dmg.IsCrit);
                };
                _bossHealth.OnDied += () =>
                {
                    _enemiesKilled++;
                    _bossHealth = null;
                    _bossName   = null;
                    PlayerPassiveEvents.RaisePlayerKilledEnemy();
                    if (boss != null) Destroy(boss);
                    DeactivateRoomHazards();   // 隐藏 Boss 战结束：清掉残留岩浆池/危险格
                    // 通关隐藏 Boss → 永久记 truth_final_boss_defeated
                    _persistent.AddTruthFlag("truth_final_boss_defeated");
                    TriggerVictory();
                };
            });
        }

        // ── Special floor rooms ────────────────────────────────────────────

        // Forge Trial (Floor 1 exclusive): preset lava pools + more enemies → weapon reward
        private void BuildHellTrialRoom()
        {
            ShowBanner("[Forge Trial] Lava rising! Defeat all enemies → curated weapon reward");
            MaybeAddAltar();
            var root = _currentRoomRoot.transform;
            var lavaPos = new Vector3[] {
                new Vector3(-4.5f,  2.0f, 0f),
                new Vector3( 3.0f, -2.0f, 0f),
                new Vector3( 5.5f,  1.5f, 0f),
            };
            foreach (var lp in lavaPos)
                EnemyFactory.SpawnLavaPool(lp, 7f, 999f, 1.8f, root, null);

            int count = GetRoomEnemyCount() + 2;
            SpawnRoomWave(count, () =>
            {
                ShowBanner("Forge Trial complete! Choose a curated weapon!");
                DropWeaponChoices();
                OpenRightDoor();
            });
        }

        // Frost Grave (Floor 2 exclusive): player move speed -25% → clear enemies to break freeze → talent + coin reward
        private void BuildFrostGraveRoom()
        {
            ShowBanner("[Frost Grave] Frozen ground! -25% move speed, defeat the undead → talent + coin reward");
            MaybeAddAltar();
            if (_player == null) return;
            var stats     = _player.GetComponent<CharacterStats>();
            const string frostKey = "FrostGrave";
            stats?.AddModifier(new StatModifier(StatType.MoveSpeed, ModifierOp.PercentMul, -0.25f, frostKey));

            int count     = GetRoomEnemyCount();
            var p         = _player.transform;
            var root      = _currentRoomRoot.transform;
            int remaining = count;
            System.Action dec = () =>
            {
                remaining--;
                if (remaining > 0) return;
                stats?.RemoveModifiersFrom(frostKey);
                int bonus = 20 + CurrentFloor * 10;
                RunCoins += bonus;
                ShowBanner($"Freeze broken! Earned {bonus} coins — choose a talent to advance!");
                DropTalentChoices();
            };

            for (int i = 0; i < count; i++)
            {
                var pos = RandomEdgeSpawnPos();
                GameObject enemy; int coins;
                switch (Random.Range(0, 3))
                {
                    case 0:  enemy = EnemyFactory.SpawnSkeleton(pos, p, root); coins = 3; break;
                    case 1:  enemy = EnemyFactory.SpawnArcher(pos, p, root);   coins = 4; break;
                    default: enemy = EnemyFactory.SpawnBat(pos, p, root);      coins = 3; break;
                }
                RegisterEnemy(enemy, coins, dec);
            }
        }

        // Chaos Rift (Floor 3 exclusive): double enemies → second wave after 2.5s → weapon + large coin reward
        private void BuildChaosRiftRoom()
        {
            ShowBanner("[Chaos Rift] The void tears open! Doubled enemies, an aftershock follows — rich rewards!");
            MaybeAddAltar();
            int count = GetRoomEnemyCount() * 2;
            SpawnRoomWave(count, () =>
            {
                ShowBanner("First wave cleared! Chaos aftershock incoming…");
                StartCoroutine(ChaosRiftSecondWave());
            }, multiWave: false);
        }

        private System.Collections.IEnumerator ChaosRiftSecondWave()
        {
            yield return new WaitForSeconds(2.5f);
            if (_state != State.Playing) yield break;
            ShowBanner("[Chaos Aftershock] Second wave incoming!");
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, () =>
            {
                int goldBonus = 50 + CurrentFloor * 15;
                RunCoins     += goldBonus;
                ShowBanner($"Chaos subsided! Earned {goldBonus} coins + weapon choice!");
                DropWeaponChoices();
                OpenRightDoor();
            }, multiWave: false);
        }

        private void ApplyTalentToPlayer(TalentData talent)
        {
            if (_player == null || talent == null) return;

            if (_activeTalents.Count >= MaxTalents)
            {
                _pendingTalent = talent;
                ShowBanner("Talent slots full (max 2)! Choose a replacement in the bottom-left");
                return;
            }

            var stats = _player.GetComponent<CharacterStats>();
            var at    = new ActiveTalent { Data = talent, RoomsLeft = talent.roomDuration };
            at.OnRoomEntered = GetTalentRoomEffect(talent.talentName);
            foreach (var entry in talent.modifiers)
                if (entry.value != 0f)
                    stats.AddModifier(new StatModifier(entry.stat, entry.op, entry.value, at.Source));

            _activeTalents.Add(at);

            var hp = _player.GetComponent<Health>();
            hp?.Heal(9999f);

            string dur = talent.roomDuration > 0 ? $" ({talent.roomDuration} rooms)" : "";
            ShowBanner($"Talent acquired: {talent.talentName}{dur}");
        }

        private System.Action GetTalentRoomEffect(string talentName)
        {
            switch (talentName)
            {
                case "Renewal":      return () => _playerHealth?.Heal((_playerHealth?.Max ?? 0f) * 0.05f);
                case "Regeneration": return () => _playerHealth?.Heal(10f);
                case "Merchant":     return () => RunCoins += 8;
                default:             return null;
            }
        }

        private void ReplaceTalentAt(int index)
        {
            if (index < 0 || index >= _activeTalents.Count || _pendingTalent == null) return;
            var old   = _activeTalents[index];
            var stats = _player?.GetComponent<CharacterStats>();
            stats?.RemoveModifiersFrom(old.Source);
            _activeTalents.RemoveAt(index);
            var pending = _pendingTalent;
            _pendingTalent = null;
            ApplyTalentToPlayer(pending);
        }

        // ── 战斗房间公共逻辑 ──────────────────────────────────────

        private int GetRoomEnemyCount()
        {
            if (_currentRoomIndex == 0) return 2;  // first room always 2 enemies to ease the player in
            if (_currentRoomIndex == 1) return 3;  // second room fixed at 3
            int base_ = 3 + CurrentFloor; // Floor1=4, Floor2=5, Floor3=6
            return Random.Range(base_, base_ + 2);
        }

        // ── Floor helper methods ───────────────────────────────────────────

        // Enemy spawn weights per floor (index 0-7 → Skeleton/Soldier/Archer/Bat/ShieldGuard/PoisonSpider/ShadowAssassin/ExplosiveDemon)
        // 优先读 FloorThemeData；缺失时退回旧硬编码表
        private float[] GetFloorEnemyWeights()
        {
            // 前两间始终只有基础小怪（写死给玩家热身）
            if (_currentRoomIndex <= 1)
                return new float[] { 4f, 4f, 2f, 3f, 0f, 0f, 0f, 0f };

            var theme    = GetFloorTheme();
            bool earlyRoom = _currentRoomIndex <= 2;
            if (theme != null)
            {
                var w = earlyRoom ? theme.earlyEnemyWeights : theme.lateEnemyWeights;
                if (w != null && w.Length == 8) return w;
            }

            // 回退：旧硬编码
            switch (CurrentFloor)
            {
                case 1:
                    return earlyRoom
                        ? new float[] { 2f, 3f, 2f, 2f, 1f, 1f, 0f, 1f }
                        : new float[] { 1f, 2f, 1f, 1f, 3f, 1f, 1f, 4f };
                case 2:
                    return earlyRoom
                        ? new float[] { 4f, 1f, 3f, 3f, 0f, 0f, 0f, 0f }
                        : new float[] { 4f, 1f, 3f, 3f, 1f, 1f, 1f, 1f };
                default:
                    return earlyRoom
                        ? new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 2f, 1f }
                        : new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 4f, 2f };
            }
        }

        private float GetFloorEliteChance()
        {
            if (_currentRoomIndex <= 1) return 0f;
            var theme = GetFloorTheme();
            if (theme != null) return theme.eliteChance;
            switch (CurrentFloor)
            {
                case 1:  return 0.15f;
                case 2:  return 0.30f;
                default: return 0.55f;
            }
        }

        private (string type, float weight)[] GetFloorRoomPool()
        {
            var theme = GetFloorTheme();
            if (theme != null && theme.roomPool != null && theme.roomPool.Count > 0)
            {
                var arr = new (string type, float weight)[theme.roomPool.Count];
                for (int i = 0; i < theme.roomPool.Count; i++)
                {
                    var e = theme.roomPool[i];
                    arr[i] = (e != null ? e.type : "Monster",
                              e != null ? e.weight : 1f);
                }
                return arr;
            }
            switch (CurrentFloor)
            {
                case 1:
                    return new (string type, float weight)[]
                        { ("Monster", 6.0f), ("Coin", 2.5f), ("Talent", 1.0f), ("HellTrial",  1.5f) };
                case 2:
                    return new (string type, float weight)[]
                        { ("Monster", 4.0f), ("Coin", 1.5f), ("Talent", 3.0f), ("FrostGrave", 2.0f) };
                default:
                    return new (string type, float weight)[]
                        { ("Monster", 7.0f), ("Coin", 1.0f), ("Talent", 2.0f), ("ChaosRift",  2.5f) };
            }
        }

        // Wall color per floor
        private Color GetFloorWallColor()
        {
            switch (CurrentFloor)
            {
                case 1:  return new Color(0.42f, 0.16f, 0.08f); // Inferno: dark-red rock wall
                case 2:  return new Color(0.10f, 0.20f, 0.40f); // Frost Realm: ice-blue stone wall
                default: return new Color(0.25f, 0.08f, 0.38f); // Chaos Abyss: corrupted purple wall
            }
        }

        // Set camera background color to match the floor theme
        // ── Floor 主题（数据驱动）────────────────────────────────────────
        static FloorThemeData[] _floorThemeCache;
        static FloorThemeData[] FloorThemesAll =>
            _floorThemeCache ?? (_floorThemeCache = Resources.LoadAll<FloorThemeData>("Floors"));

        private FloorThemeData GetFloorTheme()
        {
            foreach (var t in FloorThemesAll)
                if (t != null && t.floorNumber == CurrentFloor) return t;
            return null;
        }

        private void SetFloorBackground()
        {
            if (Camera.main == null) return;
            var theme = GetFloorTheme();
            if (theme != null)
            {
                Camera.main.backgroundColor = theme.cameraBackground;
                return;
            }
            // 回退：旧 switch
            switch (CurrentFloor)
            {
                case 1:  Camera.main.backgroundColor = new Color(0.15f, 0.05f, 0.03f); break;
                case 2:  Camera.main.backgroundColor = new Color(0.03f, 0.06f, 0.14f); break;
                default: Camera.main.backgroundColor = new Color(0.07f, 0.03f, 0.11f); break;
            }
        }

        // Rebuild arena with new floor colors (called on floor transition)
        private void UpdateArenaColors()
        {
            _mapVariant  = Random.Range(0, 3);
            _arenaIsRect = false;            // 新楼层默认异形几何
            RebuildArenaGeometry(forceRect: false);
        }

        private string GetFloorName()
        {
            var theme = GetFloorTheme();
            if (theme != null && !string.IsNullOrEmpty(theme.displayName)) return theme.displayName;
            switch (CurrentFloor)
            {
                case 1:  return "Inferno";
                case 2:  return "Frost Realm";
                default: return "Chaos Abyss";
            }
        }

        private string GetFloorNarrative()
        {
            var theme = GetFloorTheme();
            if (theme != null && !string.IsNullOrEmpty(theme.narrativeBanner)) return theme.narrativeBanner;
            switch (CurrentFloor)
            {
                case 1:  return "[Inferno] Lava surges, demons and iron-clad guards block the path — watch for explosions and magma!";
                case 2:  return "[Frost Realm] Bone-chilling cold, undead rise again, elite spawn rate greatly increased!";
                default: return "[Chaos Abyss] The void shatters, elites run rampant — the final battle awaits, the Chaos Lord is waiting!";
            }
        }

        private static string GetRoomDisplayName(string type)
        {
            switch (type)
            {
                case "Monster":    return "Combat";
                case "Talent":     return "Talent";
                case "Coin":       return "Coins";
                case "Shop":       return "Shop";
                case "Boss":       return "BOSS";
                case "HellTrial":  return "Forge Trial";
                case "FrostGrave": return "Frost Tomb";
                case "ChaosRift":  return "Chaos Rift";
                default:           return type;
            }
        }

        // ── 地形杀生成 ────────────────────────────────────────────────────

        // 当前房间祭坛位置（无祭坛时为 null），供地形杀避让，避免压在祭坛上
        private Vector3? _altarPos;

        // 按楼层在当前房间生成地形危险区域（随房间 root 销毁）
        private void SpawnFloorHazards()
        {
            if (_currentRoomRoot == null) return;
            switch (CurrentFloor)
            {
                case 1: SpawnFlamePillars();                 break;
                case 2: StartCoroutine(FrostStormRoutine()); break;
                case 3: SpawnVoidRifts();                    break;
            }
        }

        // 第2层「霜暴」：战斗期间沿玩家位置周期性从天投放 1×1 冰锥（不旋转，可走位躲开）。
        private System.Collections.IEnumerator FrostStormRoutine()
        {
            var room   = _currentRoomRoot;
            var iceSpr = LoadHazardSprite("Tiles/Hazard_Frost");
            while (!GameSignals.CombatInProgress) yield return null;          // 等战斗开始
            while (GameSignals.CombatInProgress && room != null && _player != null)
            {
                yield return new WaitForSeconds(Random.Range(1.4f, 2.6f));
                if (!GameSignals.CombatInProgress || room == null || _player == null) break;
                Vector2 pp = (Vector2)_player.transform.position
                           + new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(-1.6f, 1.6f));
                var cell = new Vector3(Mathf.Floor(pp.x) + 0.5f, Mathf.Floor(pp.y) + 0.5f, 0f);
                if (!IsHazardSpotValid(cell)) continue;
                var go = new GameObject("FallingIceSpike");
                go.transform.SetParent(room.transform, true);
                go.AddComponent<Game.AI.FallingIceSpike>()
                  .Init(iceSpr != null ? iceSpr : MakeUnitSquareSprite(), cell);
            }
        }

        // 地形杀候选点是否可放置：必须可走地板、非静态危险格('l'/'t')、且不压祭坛
        private bool IsHazardSpotValid(Vector2 p)
        {
            var cell = Game.AI.NavGrid.WorldToCell(p);
            if (!Game.AI.NavGrid.IsWalkable(cell.x, cell.y)) return false;
            if (Game.AI.NavGrid.HazardAt(cell.x, cell.y) != 0) return false;
            if (_altarPos.HasValue && Vector2.Distance(p, (Vector2)_altarPos.Value) < 1.2f) return false;
            return true;
        }

        // 检查以 center 为中心、size×size 的整个 footprint 是否都可放危险（无墙/石柱）。
        // 用于 2×2 火柱 / 3×3 虚空，避免压在石柱/墙之上（问题6）。
        private bool IsHazardAreaClear(Vector2 center, int size)
        {
            if (size <= 1) return IsHazardSpotValid(center);
            float h = (size - 1) * 0.5f;
            for (float dx = -h; dx <= h + 0.01f; dx += 1f)
            for (float dy = -h; dy <= h + 0.01f; dy += 1f)
                if (!IsHazardSpotValid(new Vector2(center.x + dx, center.y + dy))) return false;
            return true;
        }

        // 把一个期望坐标吸附到最近的可走格（墙/石柱不可走）。
        // 用于奖励球等必须可被玩家走到的物体，避免生成在中央墙体里领不到。
        private Vector3 NearestWalkableWorld(Vector3 desired)
        {
            var cell = Game.AI.NavGrid.WorldToCell(desired);
            if (Game.AI.NavGrid.IsWalkable(cell.x, cell.y))
            {
                var w0 = Game.AI.NavGrid.CellToWorld(cell);
                return new Vector3(w0.x, w0.y, 0f);
            }
            for (int rad = 1; rad < 12; rad++)
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                if (Mathf.Abs(dx) != rad && Mathf.Abs(dy) != rad) continue;
                int c = cell.x + dx, r = cell.y + dy;
                if (Game.AI.NavGrid.IsWalkable(c, r))
                {
                    var w = Game.AI.NavGrid.CellToWorld(new Vector2Int(c, r));
                    return new Vector3(w.x, w.y, 0f);
                }
            }
            return desired;
        }

        // 与 NearestWalkableWorld 相同，但额外要求该格「无危险（岩浆/毒池/冰刺等）」。
        // 用于武器/天赋等奖励落点，避免生成在岩浆等地形杀上。找不到安全格则退回最近可走格。
        private Vector3 NearestSafeWorld(Vector3 desired)
        {
            var cell = Game.AI.NavGrid.WorldToCell(desired);
            for (int rad = 0; rad < 14; rad++)
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                if (rad > 0 && Mathf.Abs(dx) != rad && Mathf.Abs(dy) != rad) continue;   // 只扫当前环
                int c = cell.x + dx, r = cell.y + dy;
                if (!Game.AI.NavGrid.IsWalkable(c, r)) continue;
                if (Game.AI.NavGrid.HazardAt(c, r) != 0) continue;
                var w = Game.AI.NavGrid.CellToWorld(new Vector2Int(c, r));
                return new Vector3(w.x, w.y, 0f);
            }
            return NearestWalkableWorld(desired);   // 全是危险/不可走时的兜底
        }

        // 第1层：四角炼狱火柱（周期性喷火，预警橙色闪烁）
        private void SpawnFlamePillars()
        {
            var root = _currentRoomRoot.transform;
            var positions = new Vector2[]
            {
                new Vector2(-5.8f,  2.8f), new Vector2(5.8f,  2.8f),
                new Vector2(-5.8f, -2.8f), new Vector2(5.8f, -2.8f),
            };
            foreach (var p0 in positions)
            {
                var p = new Vector2(Mathf.Round(p0.x), Mathf.Round(p0.y));  // 吸附到 2×2 网格（整数中心 → 占 4 格）
                if (!IsHazardAreaClear(p, 2)) continue;   // 2×2 footprint 不压石柱/墙（问题6）
                var go = new GameObject("FlamePillar");
                go.transform.SetParent(root, true);
                go.transform.position   = new Vector3(p.x, p.y, 0f);
                go.transform.localScale = new Vector3(2f, 2f, 1f);          // 2×2 正方整格

                var sr = go.AddComponent<SpriteRenderer>();
                var lavaSpr     = LoadHazardSprite("Tiles/Hazard_Inferno"); // docs 熔岩危险格
                sr.sprite       = lavaSpr != null ? lavaSpr : MakeUnitSquareSprite();
                sr.color        = lavaSpr != null ? Color.white : new Color(0.45f, 0.08f, 0.04f, 0.80f);
                sr.sortingOrder = 3;

                go.AddComponent<FlamePillar>();
                go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 1.0f;
            }
        }

        // 第2层：随机3-5个霜境冰刺（预警闪烁后激活，造成伤害+减速）
        private void SpawnIceSpikeTraps()
        {
            var root = _currentRoomRoot.transform;
            var candidates = new Vector2[]
            {
                new Vector2(-3.5f,  2.0f), new Vector2(0.5f,  1.5f), new Vector2(4.0f,  2.0f),
                new Vector2(-2.0f, -1.5f), new Vector2(2.5f, -2.0f), new Vector2(5.5f,  0.0f),
            };
            int count = Random.Range(3, candidates.Length);
            // 随机洗牌后取前 count 个
            for (int i = candidates.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
            }
            int placed = 0;
            for (int i = 0; i < candidates.Length && placed < count; i++)
            {
                var cp = new Vector2(Mathf.Floor(candidates[i].x) + 0.5f, Mathf.Floor(candidates[i].y) + 0.5f); // 吸附到格中心（1×1）
                if (!IsHazardSpotValid(cp)) continue;   // 跳过墙体/静态危险格/祭坛
                placed++;
                var go = new GameObject("IceSpikeTrap");
                go.transform.SetParent(root, true);
                go.transform.position   = new Vector3(cp.x, cp.y, 0f);
                go.transform.localScale = new Vector3(1f, 1f, 1f);          // 1×1 正方整格

                var sr = go.AddComponent<SpriteRenderer>();
                var iceSpr      = LoadHazardSprite("Tiles/Hazard_Frost");   // docs 冰面危险格
                sr.sprite       = iceSpr != null ? iceSpr : MakeUnitSquareSprite();
                sr.color        = iceSpr != null ? Color.white : new Color(0.25f, 0.45f, 0.80f, 0.10f);
                sr.sortingOrder = 3;

                go.AddComponent<IceSpikeTrap>();
                go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 0.7f;
            }
        }

        // 第3层：1-2个混沌虚空裂隙（持续减速+真实伤害+周期脉冲）
        private void SpawnVoidRifts()
        {
            var root = _currentRoomRoot.transform;
            var candidates = new Vector2[]
            {
                new Vector2(-1.5f, 1.5f), new Vector2(3.5f, -1.0f),
                new Vector2( 0.5f, 2.5f), new Vector2(5.0f,  1.5f),
            };
            int count = Random.Range(1, 3);
            int placed = 0;
            for (int i = 0; i < candidates.Length && placed < count; i++)
            {
                var cp = new Vector2(Mathf.Floor(candidates[i].x) + 0.5f, Mathf.Floor(candidates[i].y) + 0.5f); // 吸附到格中心
                if (!IsHazardAreaClear(cp, 3)) continue;   // 3×3 footprint 不压石柱/墙（问题6）
                placed++;
                var go = new GameObject("VoidRift");
                go.transform.SetParent(root, true);
                go.transform.position   = new Vector3(cp.x, cp.y, 0f);
                go.transform.localScale = new Vector3(1f, 1f, 1f);          // 潜伏 1×1（VoidRift 周期扩大到 3×3）

                var sr = go.AddComponent<SpriteRenderer>();
                var voidSpr     = LoadHazardSprite("Tiles/Hazard_Chaos");   // docs 虚空危险格
                sr.sprite       = voidSpr != null ? voidSpr : MakeUnitSquareSprite();
                sr.color        = voidSpr != null ? Color.white : new Color(0.55f, 0.05f, 0.75f, 0.90f);
                sr.sortingOrder = 3;

                go.AddComponent<VoidRift>();
                go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 1.4f;
            }
        }

        // 危险区贴图加载（Resources/Tiles/*），失败时回退为纯色方块
        private static readonly Dictionary<string, Sprite> _hazardSpriteCache = new Dictionary<string, Sprite>();
        private static Sprite LoadHazardSprite(string resourceName)
        {
            if (_hazardSpriteCache.TryGetValue(resourceName, out var cached)) return cached;
            Sprite spr = null;
            var tex = Resources.Load<Texture2D>(resourceName);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                spr = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                    new Vector2(0.5f, 0.5f), tex.width);
            }
            _hazardSpriteCache[resourceName] = spr;
            return spr;
        }

        // 战斗胜利后清除一切「定期触发 / 亡语类」危险：火柱、冰刺、虚空裂隙，以及
        // 复用 LavaPool 组件的岩浆池 / 毒蛛绿色毒池（亡语 AoE）。
        // 既清掉动态生成的，也清掉地图 't' 格固定存在的危险，避免清场后仍持续喷发或残留伤害。
        private void DeactivateRoomHazards()
        {
            // 熔岩/虚空危险格：不销毁，转入稳定态（停止喷发、不再伤害，保留可见）
            foreach (var h in FindObjectsByType<Game.AI.FlamePillar>(FindObjectsSortMode.None))  h.Stabilize();
            foreach (var h in FindObjectsByType<Game.AI.VoidRift>(FindObjectsSortMode.None))     h.Stabilize();
            foreach (var h in FindObjectsByType<Game.AI.IceSpikeTrap>(FindObjectsSortMode.None)) Destroy(h.gameObject);
            foreach (var h in FindObjectsByType<Game.AI.LavaPool>(FindObjectsSortMode.None))        Destroy(h.gameObject); // 含绿色毒池
            foreach (var h in FindObjectsByType<Game.AI.FallingIceSpike>(FindObjectsSortMode.None)) Destroy(h.gameObject); // 霜暴冰锥
        }

        // 分两波刷怪（55%/45%）；每波从房间四壁边缘生成，第一波不出精英
        private void SpawnRoomWave(int totalCount, System.Action onAllDead, bool multiWave = true)
        {
            if (_player == null) return;

            // 战斗开始：禁止可交互物交互，直到全部敌人清场
            GameSignals.CombatInProgress = true;
            System.Action onCleared = () =>
            {
                GameSignals.CombatInProgress = false;
                DeactivateRoomHazards();   // 清场后立即停止/清除一切定期触发与亡语类危险
                onAllDead?.Invoke();
            };

            if (!multiWave || totalCount <= 2)
            {
                // 数量 ≤2 或明确禁用时直接单波
                SpawnWaveGroup(totalCount, false, onCleared);
                return;
            }

            int w1 = Mathf.CeilToInt(totalCount * 0.55f);
            int w2 = totalCount - w1;

            SpawnWaveGroup(w1, false, () =>
            {
                if (w2 <= 0) { onCleared(); return; }
                ShowBanner("Second wave incoming!");
                SpawnWaveGroup(w2, true, onCleared);
            });
        }

        // 在房间边缘生成一组敌人（Hades 风格：从四壁出现）
        private void SpawnWaveGroup(int count, bool allowElite, System.Action onAllDead)
        {
            if (_player == null) return;
            int remaining = count;
            System.Action dec = () => { if (--remaining <= 0) onAllDead(); };
            bool spawnedElite = false;
            float eliteChance = allowElite ? GetFloorEliteChance() : 0f;

            for (int i = 0; i < count; i++)
            {
                var pos = RandomEdgeSpawnPos(avoidLeftWall: !allowElite);
                if (!spawnedElite && Random.value < eliteChance)
                {
                    spawnedElite = true;
                    SpawnEliteEnemy(pos, dec);
                }
                else
                    SpawnRandomNormalEnemy(pos, dec);
            }
        }

        // 从场景四壁边缘随机取一个刷怪点（Hades 风格入场）
        private Vector3 RandomEdgeSpawnPos(bool avoidLeftWall = true)
        {
            // 异形房间：从预计算的可达开放格里挑（取若干候选中离玩家最远者，避免贴脸刷怪）
            var dyn = _mapInfo.EnemySpawns;
            if (dyn != null && dyn.Length > 0)
            {
                Vector3 pp   = _player != null ? _player.transform.position : Vector3.zero;
                Vector3 best = dyn[Random.Range(0, dyn.Length)];
                float   bestD = (best - pp).sqrMagnitude;
                for (int i = 0; i < 5; i++)
                {
                    var cand = dyn[Random.Range(0, dyn.Length)];
                    float d  = (cand - pp).sqrMagnitude;
                    if (d > bestD) { best = cand; bestD = d; }
                }
                return best;
            }

            float hw = _mapInfo.HalfW - 2.5f;
            float hh = _mapInfo.HalfH - 2.2f;
            int sides = avoidLeftWall ? 3 : 4;
            switch (Random.Range(0, sides))
            {
                case 0: return new Vector3( _mapInfo.HalfW - 2.5f, Random.Range(-hh, hh), 0f);  // 右
                case 1: return new Vector3(Random.Range(-hw * 0.6f, hw * 0.6f),  _mapInfo.HalfH - 2.2f, 0f);  // 上
                case 2: return new Vector3(Random.Range(-hw * 0.6f, hw * 0.6f), -_mapInfo.HalfH + 2.2f, 0f);  // 下
                default: return new Vector3(-_mapInfo.HalfW + 2.5f, Random.Range(-hh, hh), 0f); // 左
            }
        }

        // 在战斗波次中随机生成一种精英怪
        private void SpawnEliteEnemy(Vector3 pos, System.Action onDied)
        {
            if (_player == null) return;
            var p    = _player.transform;
            var root = _currentRoomRoot.transform;
            GameObject elite;
            switch (Random.Range(0, 4))
            {
                case 0:
                    elite = EnemyFactory.SpawnCommander(pos, p, root);
                    break;
                case 1:
                    elite = EnemyFactory.SpawnWitch(pos, p, root, sp =>
                    {
                        var bat = EnemyFactory.SpawnBat(sp, p, root);
                        RegisterEnemy(bat, 3, () => { });
                        return bat;
                    });
                    break;
                case 2:
                    var shaman = EnemyFactory.SpawnPoisonShaman(pos, p, root);
                    shaman.GetComponent<PoisonShamanAI>().SpawnPoisonPuddleCallback = pp =>
                        EnemyFactory.SpawnPoisonPool(pp, 5f, 4f, 1.5f, root, shaman);
                    elite = shaman;
                    break;
                default:
                    var necro = EnemyFactory.SpawnNecromancer(pos, p, root);
                    necro.GetComponent<NecromancerAI>().SpawnSkeletonCallback = sp =>
                    {
                        var sk = EnemyFactory.SpawnSkeleton(sp, p, root);
                        RegisterEnemy(sk, 3, () => { });
                        return sk;
                    };
                    elite = necro;
                    break;
            }
            ShowBanner("Elite enemy appears!");
            RegisterEnemy(elite, 15, onDied);
        }

        // 15% 概率在当前房间生成神秘祭坛（可选互动）
        private void MaybeAddAltar()
        {
            _altarPos = null;   // 每房重置；下方地形杀据此避让
            if (Random.value >= 0.15f) return;

            var go = new GameObject("AltarPedestal");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = FindSafeAltarPosition();
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            _altarPos = go.transform.position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = InteractableSprites.Altar();
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.7f;
            col.isTrigger = true;

            var mystery = go.AddComponent<MysteryPedestal>();
            mystery.OnResolved += HandleAltar;
            ShowBanner("A mysterious altar appears! (optional)");
        }

        // 找一格 "可走且无地形危险" 的世界坐标作为祭坛位置
        // 优先一组靠墙的候选点；候选点都不行时，从首选点向外螺旋扫描
        private Vector3 FindSafeAltarPosition()
        {
            Vector3[] candidates =
            {
                new Vector3(-6f,  2.8f, 0f),
                new Vector3(-6f, -2.8f, 0f),
                new Vector3( 6f,  2.8f, 0f),
                new Vector3( 6f, -2.8f, 0f),
                new Vector3(-4f,  0f,   0f),
                new Vector3( 4f,  0f,   0f),
                new Vector3( 0f,  3.5f, 0f),
                new Vector3( 0f, -3.5f, 0f),
            };
            foreach (var p in candidates)
            {
                var cell = Game.AI.NavGrid.WorldToCell(p);
                if (Game.AI.NavGrid.IsWalkable(cell.x, cell.y)
                    && Game.AI.NavGrid.HazardAt(cell.x, cell.y) == 0)
                {
                    Vector2 w = Game.AI.NavGrid.CellToWorld(cell);
                    return new Vector3(w.x, w.y, 0f);
                }
            }

            // 兜底：以首选点为圆心向外螺旋扫描，最多 10 格
            var start = Game.AI.NavGrid.WorldToCell(new Vector2(-6f, 2.8f));
            for (int rad = 1; rad < 10; rad++)
            {
                for (int dy = -rad; dy <= rad; dy++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    if (Mathf.Abs(dx) != rad && Mathf.Abs(dy) != rad) continue;
                    int c = start.x + dx, r = start.y + dy;
                    if (Game.AI.NavGrid.IsWalkable(c, r) && Game.AI.NavGrid.HazardAt(c, r) == 0)
                    {
                        Vector2 w = Game.AI.NavGrid.CellToWorld(new Vector2Int(c, r));
                        return new Vector3(w.x, w.y, 0f);
                    }
                }
            }
            return new Vector3(-6f, 2.8f, 0f);
        }

        private void HandleAltar(MysteryOutcome outcome)
        {
            switch (outcome)
            {
                case MysteryOutcome.Lucky:
                    RunCoins += 25;
                    ShowBanner("Altar blessing: +25 coins!");
                    break;
                case MysteryOutcome.Gift:
                    var gift = GenerateRandomTalent();
                    ApplyTalentToPlayer(gift);
                    ShowBanner($"Altar gift: Talent [{gift.talentName}]!");
                    break;
                case MysteryOutcome.Heal:
                    if (_playerHealth != null) _playerHealth.Heal(9999f);
                    ShowBanner("Altar healing: fully restored!");
                    break;
                case MysteryOutcome.Cursed:
                    if (_player != null)
                    {
                        var stats = _player.GetComponent<CharacterStats>();
                        stats?.AddModifier(new StatModifier(StatType.MaxHP, ModifierOp.PercentMul, -0.15f, "Altar_Curse"));
                    }
                    ShowBanner("Altar curse: Max HP -15%!");
                    break;
            }
        }

        // 每进入新房间：限时天赋计数 -1，归零时移除
        private void OnNewRoomEntered()
        {
            if (_player == null) return;
            var stats = _player.GetComponent<CharacterStats>();
            for (int i = _activeTalents.Count - 1; i >= 0; i--)
            {
                var at = _activeTalents[i];
                at.OnRoomEntered?.Invoke(); // 每房效果（如生机回血）
                if (at.IsPermanent) continue;
                at.RoomsLeft--;
                if (at.RoomsLeft <= 0)
                {
                    stats?.RemoveModifiersFrom(at.Source);
                    _activeTalents.RemoveAt(i);
                    ShowBanner($"Talent [{at.Data.talentName}] has expired");
                }
            }
        }

        private void OpenRightDoor()
        {
            if (_currentRoomRoot == null) return;
            if (_currentRoomRoot.transform.Find("Door") != null) return;

            var doorGO = new GameObject("Door");
            doorGO.transform.SetParent(_currentRoomRoot.transform, true);
            doorGO.transform.position   = _mapInfo.DoorPos;
            doorGO.transform.localScale = new Vector3(0.35f, 1.8f, 1f); // 竖向出口

            var sr = doorGO.AddComponent<SpriteRenderer>();
            sr.sprite       = InteractableSprites.Door();
            sr.color        = Color.white;
            sr.sortingOrder = 8;

            var col = doorGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var door      = doorGO.AddComponent<DoorTrigger>();
            int nextIndex = _currentRoomIndex + 1;
            door.OnPlayerEntered += () => LoadRoom(nextIndex);
        }

        // --------------------------------------------------------------------
        //  Spawners
        // --------------------------------------------------------------------

        private void SpawnPlayer(HeroData hero)
        {
            _player = new GameObject("Player_" + hero.heroName);
            _player.tag                  = "Player";
            _player.transform.position   = _mapInfo.PlayerSpawn;
            _player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = _player.AddComponent<SpriteRenderer>();
            var heroSpr = HeroSprites.Get(hero.heroName);
            sr.sprite       = heroSpr != null ? heroSpr : MakeUnitSquareSprite();
            sr.color        = heroSpr != null ? Color.white : hero.tintColor;
            sr.sortingOrder = 10;

            var rb = _player.AddComponent<Rigidbody2D>();
            rb.gravityScale           = 0f;
            rb.freezeRotation         = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = _player.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            var stats = _player.AddComponent<CharacterStats>();
            stats.SetBase(StatType.MaxHP,       hero.baseMaxHP);
            stats.SetBase(StatType.Attack,      hero.baseAttack);
            stats.SetBase(StatType.Defense,     hero.baseDefense);
            stats.SetBase(StatType.MoveSpeed,   hero.baseMoveSpeed * 0.8f);
            stats.SetBase(StatType.AttackSpeed, hero.baseAttackSpeed);

            _playerHealth = _player.AddComponent<Health>();
            _playerHealth.IFrameDuration = 0.4f;
            var playerTr = _player.transform;
            _playerHealth.OnDamaged += dmg =>
            {
                StartCoroutine(FlashRoutine(sr, new Color(1f, 0.4f, 0.4f), 0.18f));
                if (playerTr != null) DamageNumbers.Instance?.Show(playerTr.position + Vector3.up * 0.5f, dmg.Amount, dmg.IsCrit);
                // 受击音效；排除耗血武器对自身造成的 True 伤害（Source 为玩家自身）
                if (dmg.Source != _player) AudioManager.Get().PlaySfx("hurt");
            };
            _playerHealth.OnDied += () =>
            {
                TriggerDeath();
                Destroy(_player);
            };

            if (hero.heroSkillType != HeroSkillType.None)
            {
                var heroSkill = _player.AddComponent<HeroActiveSkillHandler>();
                heroSkill.SkillType = hero.heroSkillType;
                heroSkill.Cooldown  = hero.heroSkillCooldown;
                heroSkill.SkillName = hero.heroSkillName;
            }

            if (hero.heroPassiveType != HeroPassiveType.None)
            {
                var heroPassive = _player.AddComponent<HeroPassiveHandler>();
                heroPassive.PassiveType = hero.heroPassiveType;
            }

            var weaponHandler = _player.AddComponent<PlayerWeaponHandler>();
            weaponHandler.HeroPassive = hero.heroPassiveType;

            var controller = _player.AddComponent<PlayerController>();
            controller.SetFacingSprites(
                HeroSprites.Get(hero.heroName),
                HeroSprites.GetBack(hero.heroName));

            _player.AddComponent<PlayerStateReporter>();

            var (slot0, slot1) = WeaponLibrary.GetStarterWeapons(hero.heroName);
            weaponHandler.EquipWeapon(slot0, 0);
            weaponHandler.EquipWeapon(slot1, 1);

            // 武器 HP 加成会在 Health 之后才提高 MaxHP，导致 _current 卡在基础上限
            // 装备完起手武器后强制顶满 Current
            _playerHealth.Heal(_playerHealth.Max);

            // 开局起播地牢探索 BGM（Boss 战时会切到战斗曲，结束后切回）
            AudioManager.Get().PlayMusic("dungeon_ambient_1");
        }

        private void SpawnWeaponPedestal(Vector3 pos, WeaponInstance weapon)
        {
            pos = NearestWalkableWorld(pos);   // 异形房：吸附到最近可走格
            var go = new GameObject("WeaponPedestal_" + weapon.Data.weaponName);
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = WeaponData.GetRarityColor(weapon.Data.rarity);
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.9f;
            col.isTrigger = true;

            var pedestal       = go.AddComponent<WeaponPedestal>();
            pedestal.Weapon    = weapon;
            pedestal.OnEquipped = w =>
            {
                if (_player == null) return;
                var handler = _player.GetComponent<PlayerWeaponHandler>();
                if (handler == null) return;
                if (TryHandleDuplicateWeapon(w, handler)) { Destroy(go); return; }
                handler.EquipWeapon(w, handler.ActiveSlotIndex);
                ShowBanner($"Equipped: {w.ShortName}");
                Destroy(go);
            };
        }

        private WeaponInstance[] GetRandomWeaponOffers(int count)
        {
            var all = new System.Func<WeaponInstance>[]
            {
                WeaponLibrary.IronDagger,     WeaponLibrary.SteelDagger,
                WeaponLibrary.VenomFang,      WeaponLibrary.PhantomBlade,
                WeaponLibrary.IronSword,      WeaponLibrary.KnightSword,
                WeaponLibrary.CrescentBlade,  WeaponLibrary.HolyBlade,
                WeaponLibrary.DragonAbyssSword,
                WeaponLibrary.IronGreatsword, WeaponLibrary.IronMallet,
                WeaponLibrary.WarriorGreatsword,
                WeaponLibrary.ArmorBreaker,   WeaponLibrary.DoomBlade,
                WeaponLibrary.WoodenBow,      WeaponLibrary.BoneBow,
                WeaponLibrary.HunterBow,      WeaponLibrary.ElfBow,
                WeaponLibrary.CloudPiercer,   WeaponLibrary.ThunderBow,
                WeaponLibrary.CelestialBow,
                WeaponLibrary.WoodStaff,      WeaponLibrary.MagicStaff,
                WeaponLibrary.FrostStaff,     WeaponLibrary.ChaosWand,
            };
            var avail  = new List<int>();
            for (int i = 0; i < all.Length; i++) avail.Add(i);

            var result = new WeaponInstance[Mathf.Min(count, avail.Count)];
            for (int i = 0; i < result.Length; i++)
            {
                int ri = Random.Range(0, avail.Count);
                result[i] = all[avail[ri]]();
                avail.RemoveAt(ri);
            }
            return result;
        }

        // 重复武器检测：若玩家已持有同名武器，自动升级或附魔一次
        private bool TryHandleDuplicateWeapon(WeaponInstance newWeapon, PlayerWeaponHandler handler)
        {
            for (int i = 0; i < handler.Slots.Length; i++)
            {
                var existing = handler.Slots[i];
                if (existing == null) continue;
                if (existing.Data.weaponName != newWeapon.Data.weaponName) continue;

                if (existing.TryUpgrade())
                {
                    handler.RefreshWeaponHPBonus(i);
                    ShowBanner($"Duplicate weapon! {existing.ShortName} auto-upgraded!");
                    return true;
                }
                if (existing.TryEnchant())
                {
                    handler.RefreshWeaponHPBonus(i);
                    ShowBanner($"Duplicate weapon! {existing.ShortName} auto-enchanted!");
                    return true;
                }
                ShowBanner($"Duplicate weapon! {existing.ShortName} maxed, discarded");
                return true;
            }
            return false;
        }

        private void ScaleEnemyStats(GameObject enemy, float scale)
        {
            if (scale <= 1.001f || enemy == null) return;
            var stats = enemy.GetComponent<CharacterStats>();
            if (stats == null) return;
            stats.SetBase(StatType.MaxHP,  stats.Get(StatType.MaxHP)  * scale);
            stats.SetBase(StatType.Attack, stats.Get(StatType.Attack) * scale);
            enemy.GetComponent<Health>()?.Heal(99999f);
        }

        // KingdomGuilt 最终 Boss 专属立绘（Resources/Enemies/KingdomGuilt.png）。
        // PPU 设为高度/2，使整帧≈2 世界单位，与 ChaosLord 程序化精灵基准一致，沿用既有缩放系数。
        private static Sprite _kgSprite;
        private static bool   _kgSpriteTried;
        private static Sprite LoadKingdomGuiltSprite()
        {
            if (_kgSpriteTried) return _kgSprite;
            _kgSpriteTried = true;
            var tex = Resources.Load<Texture2D>("Enemies/KingdomGuilt");
            if (tex == null)
            {
                Debug.LogWarning("[GameBootstrap] 未找到 Enemies/KingdomGuilt 立绘，回退为 ChaosLord 外观。");
                return null;
            }
            tex.filterMode = FilterMode.Bilinear;
            _kgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                      new Vector2(0.5f, 0.5f), tex.height / 2f);
            return _kgSprite;
        }

        // 三档结局 CG，支持「多帧动画」：Resources/Endings/Ending_{tier}_1.._N.png 顺序播放。
        // 若没有编号帧，则回退到单张旧图 Ending_{tier}.png（向后兼容）。缺图时过场为纯黑+标题。
        private const int MaxEndingFrames = 12;
        private static readonly Dictionary<EndingTier, Texture2D[]> _endingFrames =
            new Dictionary<EndingTier, Texture2D[]>();
        private static Texture2D[] GetEndingFrames(EndingTier tier)
        {
            if (_endingFrames.TryGetValue(tier, out var arr)) return arr;
            string baseName = tier == EndingTier.Crown ? "Ending_Crown"
                            : tier == EndingTier.Truth ? "Ending_Truth"
                            :                            "Ending_Normal";
            var list = new List<Texture2D>();
            for (int i = 1; i <= MaxEndingFrames; i++)
            {
                var t = Resources.Load<Texture2D>($"Endings/{baseName}_{i}");
                if (t == null) break;     // 帧必须连续编号
                list.Add(t);
            }
            if (list.Count == 0)          // 回退到单张旧图
            {
                var single = Resources.Load<Texture2D>($"Endings/{baseName}");
                if (single != null) list.Add(single);
            }
            arr = list.ToArray();
            _endingFrames[tier] = arr;    // 可能为空数组，仍缓存
            return arr;
        }

        // 各结局每帧字幕解释（与编号帧一一对应；帧多于字幕时多出的帧不显示字幕）
        private static readonly string[] NormalCaptions =
        {
            "You climb back into a pale, broken dawn.\nThe air still tastes of ash, but the screaming underground has finally gone quiet.",
            "Far above, the black rift in the sky knits slowly shut,\nand the last embers of the disaster drift down like dying snow.",
            "The town stirs again — half-frozen, half-burned, its people none the wiser.\nThey will never know what was buried here, nor what it cost to seal it.",
            "You walk on, the dungeon sealed behind you and the truth still beneath it.\nThe world is saved, for now — and 'for now' is all anyone is ever given.",
        };
        private static readonly string[] TruthCaptions =
        {
            "You stand before the shattered Realmcore,\nits cracked light pulsing like a heart that refuses to stop beating.",
            "Its glow spills the memories the kingdom tried to bury:\nthe forced awakening, the cover-up, the names struck from every record.",
            "Ember-light finds your face, and at last the pieces fall into place.\nYou were never a hero — only the ember they could not put out.",
            "You speak the name beneath the ash and claim it as your own.\nThe three realms exhale, and for the first time in an age, grow still.",
        };
        private static readonly string[] CrownCaptions =
        {
            "Upon the throne sits the crowned beast — the Kingdom's Guilt made flesh,\nfattened on every lie told to keep the disaster hidden.",
            "Your blade meets the beast at last, and the throne hall erupts in sparks.\nEvery lie they buried burns away in the white heat of a single, falling blow.",
            "Your blow lands true. The heavy iron-and-gold crown topples from its brow\nand shatters across the stone, ringing like a cracked and broken bell.",
            "Light pours through the wound in the world, and the realms begin to heal.\nThe crown has fallen; the name returns; the long silence is over at last.",
        };
        private static string[] GetEndingCaptions(EndingTier tier) =>
            tier == EndingTier.Crown ? CrownCaptions :
            tier == EndingTier.Truth ? TruthCaptions :
                                       NormalCaptions;

        private TalentPickup SpawnTalentOrb(TalentData data, Color color)
        {
            var go = new GameObject("Talent_" + data.talentName);
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);   // 大一点的光球

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = InteractableSprites.Orb();    // 放射辉光光球，由下方 sr.color 按天赋色着色
            sr.color        = color;
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.5f;
            col.isTrigger = true;

            var pickup = go.AddComponent<TalentPickup>();
            pickup.talent = data;
            return pickup;
        }

        private CoinPickup SpawnCoinPickup(Vector3 pos, int amount)
        {
            var go = new GameObject("Coin");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = new Color(1f, 0.85f, 0.25f);
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.6f;
            col.isTrigger = true;

            var coin = go.AddComponent<CoinPickup>();
            coin.amount = amount;
            return coin;
        }

        private ShopPedestal SpawnShopPedestal(Vector3 pos, TalentData talent, Color color, int price)
        {
            var go = new GameObject("Shop_" + talent.talentName);
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = color;
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.9f;
            col.isTrigger = true;

            var shop = go.AddComponent<ShopPedestal>();
            shop.talent     = talent;
            shop.price      = price;
            shop.GetCoins   = () => RunCoins;
            shop.SpendCoins = amount => RunCoins -= amount;
            shop.OnPurchased = t => ApplyTalentToPlayer(t);
            return shop;
        }

        // --------------------------------------------------------------------
        //  World setup
        // --------------------------------------------------------------------

        private void EnsureCamera()
        {
            if (Camera.main != null) return;
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5.5f;
            cam.backgroundColor  = new Color(0.12f, 0.12f, 0.18f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGO.AddComponent<AudioListener>();
        }

        // 摄像机跟随玩家，并夹紧到地图边界
        private void LateUpdate()
        {
            // HUD: only shown and refreshed while Playing; hidden in other states (summary / cutscene use overlays).
            EnsureHud();
            _hud.SetVisible(_state == State.Playing);
            if (_state == State.Playing) RefreshHud();

            // uGUI 覆盖层：层间过渡(FloorComplete)。其余状态隐藏。
            EnsureOverlay();
            bool floorComplete = _state == State.FloorComplete;
            _overlay.SetFloorCompleteVisible(floorComplete);
            if (floorComplete) RefreshFloorCompleteOverlay();

            // 天赋替换弹窗(Playing 且有待定天赋时弹出一次,选择/取消后自动收起)
            bool wantTR = _state == State.Playing && _pendingTalent != null;
            if (wantTR && !_trShown)
            {
                var labels = new List<string>();
                foreach (var at in _activeTalents)
                    labels.Add($"Replace: {at.Data.talentName}  ({(at.IsPermanent ? "Permanent" : at.RoomsLeft + " rooms left")})");
                _overlay.ShowTalentReplacement(_pendingTalent.talentName, _pendingTalent.description, labels,
                    idx => ReplaceTalentAt(idx), () => _pendingTalent = null);
                _trShown = true;
            }
            else if (!wantTR && _trShown) { _overlay.HideTalentReplacement(); _trShown = false; }

            // 结算屏(胜利/死亡):进入该状态时构建一次;死亡时每帧刷新倒计时。
            if (_state == State.Victory || _state == State.Death)
            {
                if (_esShownFor != _state) { ShowEndScreenOverlay(); _esShownFor = _state; }
                if (_state == State.Death)
                    _overlay.SetEndScreenCountdown($"Returning to menu in {Mathf.Max(0f, _deathReturnAt - Time.time):0.0}s…");
            }
            else if (_esShownFor != State.Playing) { _overlay.HideEndScreen(); _esShownFor = State.Playing; }

            // 结局过场(uGUI):推进淡入/逐字 + 输入 + 推送显示
            _overlay.SetCutsceneVisible(_state == State.EndingCutscene);
            if (_state == State.EndingCutscene)
            {
                TickEndingCutscene();
                HandleCutsceneInput();
                RefreshCutsceneOverlay();
                return;
            }
            if (_state != State.Playing || _player == null || Camera.main == null) return;
            var cam   = Camera.main;
            float hh  = cam.orthographicSize;
            float hw  = hh * cam.aspect;
            float px  = _player.transform.position.x;
            float py  = _player.transform.position.y;
            float cx  = Mathf.Clamp(px, -ArenaHalfW + hw, ArenaHalfW - hw);
            float cy  = Mathf.Clamp(py, -ArenaHalfH + hh, ArenaHalfH - hh);
            cam.transform.position = new Vector3(cx, cy, cam.transform.position.z);
        }

        private static readonly string[] _variantLetters = { "A", "B", "C" };

        private void BuildArena()
        {
            _mapVariant  = Random.Range(0, 3);
            _arenaIsRect = false;            // 新楼层默认异形几何
            RebuildArenaGeometry(forceRect: false);
        }

        // 按房型重建竞技场几何：商店/Boss → 规整矩形；其余 → 本层异形布局。
        // _mapVariant 同层恒定 + 生成确定性 → 同层每次重建得到一致的异形布局。
        private void RebuildArenaGeometry(bool forceRect, bool clean = false)
        {
            if (_arenaRoot != null) { Destroy(_arenaRoot); _arenaRoot = null; }
            _arenaRoot = new GameObject("Arena");
            _mapInfo   = LoadRoomPrefab(CurrentFloor, _mapVariant, _arenaRoot.transform, forceRect, clean);
            ArenaHalfW = _mapInfo.HalfW;
            ArenaHalfH = _mapInfo.HalfH;
        }

        private MapInfo LoadRoomPrefab(int floor, int variant, Transform parent, bool forceRect = false, bool clean = false)
        {
            // 程序化布局：跳过静态房间预制体，运行时生成（每局不同）。
            // Build 内部用同一份 rows 同时铺设几何并重建 NavGrid → 渲染与寻路同源。
            // forceRect：商店房 / Boss 房强制规整矩形布局。
            if (MapBuilder.Procedural)
            {
                var theme = GetFloorTheme();
                if (theme != null)
                    FloorBackground.Create(theme, parent, MapDims.TileW + 4f, MapDims.TileH + 4f);
                else
                    FloorBackground.Create(floor, parent, MapDims.TileW + 4f, MapDims.TileH + 4f);
                return MapBuilder.Build(floor, variant, parent, createBackground: false, forceRect: forceRect, clean: clean);
            }

            string letter = _variantLetters[variant % 3];
            string path   = $"Rooms/Floor{floor}/Room_F{floor}{letter}";
            var prefab    = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[GameBootstrap] Prefab not found: {path}, falling back to procedural generation");
                return MapBuilder.Build(floor, variant, parent);
            }

            var rows = MapBuilder.GetMap(floor, variant);
            NavGrid.Build(rows);
            MapBuilder.SetupPhysics();
            var fbTheme = GetFloorTheme();
            if (fbTheme != null)
                FloorBackground.Create(fbTheme, parent, MapDims.TileW + 4f, MapDims.TileH + 4f);
            else
                FloorBackground.Create(floor, parent, MapDims.TileW + 4f, MapDims.TileH + 4f);

            var roomGO = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            var meta   = roomGO.GetComponent<Game.Dungeon.RoomMetadata>();
            if (meta != null) return meta.ToMapInfo();

            return new MapInfo
            {
                HalfW       = MapDims.TileW * 0.5f,
                HalfH       = MapDims.TileH * 0.5f,
                PlayerSpawn = new Vector3(-MapDims.TileW * 0.5f + 2.5f, 0f, 0f),
                DoorPos     = new Vector3( MapDims.TileW * 0.5f - 0.5f, 0f, 0f),
            };
        }

        // --------------------------------------------------------------------
        //  Utility
        // --------------------------------------------------------------------

        private System.Collections.IEnumerator FlashRoutine(SpriteRenderer sr, Color flashColor, float duration)
        {
            if (sr == null) yield break;
            var original = sr.color;
            sr.color = flashColor;
            yield return new WaitForSeconds(duration);
            if (sr != null) sr.color = original;
        }

        private void ShowBanner(string message)
        {
            _bannerMessage = message;
            _bannerUntil   = Time.time + 3f;
        }

        private static GameBootstrap _instance;

        private static Sprite _cachedSquare;
        private static Sprite MakeUnitSquareSprite()
        {
            if (_cachedSquare != null) return _cachedSquare;
            const int size = 32;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _cachedSquare = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _cachedSquare;
        }

        private static Texture2D _whitePixel;
        private static Texture2D WhitePixel
        {
            get
            {
                if (_whitePixel == null)
                {
                    _whitePixel = new Texture2D(1, 1);
                    _whitePixel.SetPixel(0, 0, Color.white);
                    _whitePixel.Apply();
                    _whitePixel.hideFlags = HideFlags.HideAndDontSave;
                }
                return _whitePixel;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HUD data push
        // ─────────────────────────────────────────────────────────────
        private void EnsureHud()
        {
            if (_hud != null) return;
            _hud = new GameObject("HudView").AddComponent<Game.UI.HudView>();
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            _overlay = new GameObject("OverlayView").AddComponent<Game.UI.OverlayView>();
            _overlay.SetFloorCompleteCallbacks(
                onHeal: () =>
                {
                    int hCost = 30 + CurrentFloor * 10;
                    if (_playerHealth != null && RunCoins >= hCost && _playerHealth.Ratio < 0.999f)
                    {
                        RunCoins -= hCost;
                        _playerHealth.Heal(_playerHealth.Max * 0.5f);
                    }
                },
                onAdvance: AdvanceFloor,
                onMenu:    ReturnToMenu);
        }

        private void RefreshFloorCompleteOverlay()
        {
            int   hCost   = 30 + CurrentFloor * 10;
            bool  showHp  = _playerHealth != null;
            float cur     = showHp ? _playerHealth.Current : 0f;
            float max     = showHp ? _playerHealth.Max     : 1f;
            float ratio   = showHp ? _playerHealth.Ratio   : 0f;
            bool  canHeal = showHp && RunCoins >= hCost && ratio < 0.999f;
            _overlay.RefreshFloorComplete(GetFloorName(), CurrentFloor, clearReward, RunCoins,
                cur, max, ratio, hCost, canHeal, showHp);
        }

        // Assemble the end-of-run summary screen data.
        private void ShowEndScreenOverlay()
        {
            string stats = $"Floor reached: {CurrentFloor} / {maxFloor}\nKills: {_enemiesKilled}    Total DMG: {Mathf.RoundToInt(_totalDamageDealt):N0}    Coins: {RunCoins}";

            if (_state == State.Death)
            {
                _overlay.ShowEndScreen("Defeated", new Color(1f, 0.3f, 0.3f), false,
                    null, null, null, stats, null, null,
                    null, null, ReturnToMenu);
                return;
            }

            string title, subtitle, footnote; Color color;
            switch (_endingTier)
            {
                case EndingTier.Crown:
                    title = "Truth · Crown"; color = new Color(1f, 0.86f, 0.55f);
                    subtitle = "The monster wears the crown, seated upon the ground — and you cast it down from the throne.";
                    footnote = $"Kingdom's Guilt exposed.  (Truths known {_endingTruthCount} / 10)"; break;
                case EndingTier.Truth:
                    title = "Truth · Embers"; color = new Color(0.92f, 0.78f, 1f);
                    subtitle = "The world is saved, for now.";
                    footnote = $"The name beneath can no longer be ignored.  (Truths known {_endingTruthCount} / 10)"; break;
                default:
                    title = "Victory"; color = new Color(1f, 0.92f, 0.2f);
                    subtitle = "The world is saved, for now.";
                    footnote = $"But the name beneath remains unremembered.  (Truths known {_endingTruthCount} / 10)"; break;
            }
            string cleared = $"All {maxFloor} floors cleared!  +{clearReward} unlock currency  (total: {_persistent.UnlockCurrency})";
            BuildRecapStrings(out string leftRecap, out string rightRecap);

            _overlay.ShowEndScreen(title, color, true, subtitle, cleared, footnote, stats,
                leftRecap, rightRecap, RestartRun, ReturnToMenu, null);
        }

        // 把本周目抉择/道具/协同拼成两列富文本(含颜色),供结算屏显示。
        private void BuildRecapStrings(out string left, out string right)
        {
            left = right = "";
            var run = GameManager.Instance?.Run;
            if (run == null) return;

            string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
            Color pure    = new Color(0.60f, 0.85f, 0.95f);
            Color tainted = new Color(0.93f, 0.62f, 0.95f);
            Color none    = new Color(0.55f, 0.55f, 0.58f);

            var entries = new (string flag, string label, Color color, string tag)[] {
                ("f1_door_struck",             "Sealed Lift Door · Break Open",    tainted, "[Tainted]"),
                ("f1_door_oath",               "Sealed Lift Door · Leave in Silence", pure, "[Pure]"),
                ("f2_lake_witnessed_directly", "Frozen Lake · Gaze Directly",      pure,    "[Pure]"),
                ("f2_lake_shattered",          "Frozen Lake · Shatter",            tainted, "[Tainted]"),
                ("f3_mirror_confronted_self",  "Black Mirror · Confront",          tainted, "[Tainted]"),
                ("f3_mirror_refused_self",     "Black Mirror · Turn Away",         pure,    "[Pure]"),
                ("f3_throne_sat",              "Broken Throne · Sit Upon It",      tainted, "[Tainted]"),
                ("f3_throne_toppled",          "Broken Throne · Topple",           pure,    "[Pure]"),
            };

            var lb = new System.Text.StringBuilder();
            int picked = 0;
            foreach (var e in entries)
            {
                if (!run.HasStoryFlag(e.flag)) continue;
                lb.AppendLine($"<color=#{Hex(e.color)}>·  {e.label}   <i>{e.tag}</i></color>");
                picked++;
            }
            if (picked == 0) lb.AppendLine($"<color=#{Hex(none)}><i>(No choices made this run)</i></color>");
            lb.AppendLine();
            int corr = run.VoidCorruption;
            Color cc = corr >= 10 ? new Color(0.95f, 0.30f, 0.85f) :
                       corr >= 5  ? new Color(0.78f, 0.55f, 0.92f) :
                       corr >= 1  ? new Color(0.78f, 0.78f, 0.92f) :
                                    new Color(0.55f, 0.85f, 0.95f);
            lb.AppendLine($"<color=#{Hex(cc)}><b>Final void corruption: {corr}</b></color>");
            left = lb.ToString();

            var rb = new System.Text.StringBuilder();
            if (run.StoryItems == null || run.StoryItems.Count == 0)
                rb.AppendLine($"<color=#{Hex(none)}><i>(None)</i></color>");
            else
                foreach (var item in run.StoryItems)
                {
                    string flavor = Game.Systems.StoryItemDatabase.TryGet(item, out var def) ? def.flavorTag : "";
                    rb.AppendLine($"<color=#EBD68C>✦ {item}</color>  <color=#AEAEC0><i>{flavor}</i></color>");
                }
            rb.AppendLine();
            rb.AppendLine($"<color=#{Hex(new Color(1f, 0.78f, 0.92f))}><b>── Active Synergies ──</b></color>");
            bool anySyn = false;
            foreach (var s in Game.Systems.StoryItemSynergyDatabase.All)
                if (Game.Systems.StoryItemSynergyDatabase.IsActive(run, s.id))
                {
                    anySyn = true;
                    rb.AppendLine($"<color=#{Hex(new Color(1f, 0.72f, 0.92f))}>★ {s.displayName}</color>");
                    rb.AppendLine($"   <color=#D1AED1><i>{s.flavor}</i></color>");
                }
            if (!anySyn) rb.AppendLine($"<color=#{Hex(none)}><i>(No item synergies triggered)</i></color>");
            right = rb.ToString();
        }

        // ── 结局过场(uGUI 推送)──────────────────────────────────────────────
        private static readonly Dictionary<Texture2D, Sprite> _cgSprites = new Dictionary<Texture2D, Sprite>();
        private static Sprite CutsceneSprite(Texture2D tex)
        {
            if (tex == null) return null;
            if (_cgSprites.TryGetValue(tex, out var s)) return s;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _cgSprites[tex] = s;
            return s;
        }

        private void RefreshCutsceneOverlay()
        {
            var frames     = GetEndingFrames(_endingTier);
            int frameCount = frames.Length;
            float head     = Mathf.Clamp01(_cutsceneAlpha);

            Sprite cg = frameCount > 0 ? CutsceneSprite(frames[Mathf.Clamp(_cutsceneFrame, 0, frameCount - 1)]) : null;

            EndingTitle(out string title, out Color tint);
            var caps = GetEndingCaptions(_endingTier);
            string cap   = _cutsceneFrame < caps.Length ? caps[_cutsceneFrame] : "";
            int shown    = Mathf.Clamp(Mathf.FloorToInt(_cutsceneReveal), 0, cap.Length);
            string hint  = _cutsceneFrame >= frameCount - 1 ? "Click / Space  ▶  End" : "Click / Space  ▶";

            _overlay.RefreshCutscene(cg, head, title, tint, cap.Substring(0, shown),
                frameCount, _cutsceneFrame, hint, shown >= cap.Length && head > 0.9f);
        }

        private void HandleCutsceneInput()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { FinishEndingCutscene(); return; }
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame ||
                    kb.numpadEnterKey.wasPressedThisFrame) { AdvanceEndingCutscene(); return; }
            }
            var ms = UnityEngine.InputSystem.Mouse.current;
            if (ms != null && ms.leftButton.wasPressedThisFrame) AdvanceEndingCutscene();
        }

        private void RefreshHud()
        {
            // 金币 / 虚空污染（最先刷新:即使后续某部件异常,金币显示也不受影响）
            _hud.SetGold(RunCoins);
            _hud.SetCorruption(GameManager.Instance?.Run?.VoidCorruption ?? 0);

            // 玩家血条
            if (_playerHealth != null) _hud.SetHp(_playerHealth.Current, _playerHealth.Max);

            // 顶栏
            string roomT = _currentRoomIndex < _floorRooms.Count ? _floorRooms[_currentRoomIndex] : "—";
            _hud.SetTopBar($"{GetFloorName()} · Room {_currentRoomIndex + 1}/{_floorRooms.Count} · {GetRoomDisplayName(roomT)} · Difficulty ×{FloorScale:0.00}");

            // 英雄技能
            var sk = _player != null ? _player.GetComponent<HeroActiveSkillHandler>() : null;
            if (sk != null && sk.SkillType != HeroSkillType.None)
                _hud.SetHeroSkill(true, sk.SkillName, sk.IsReady, sk.CooldownRemaining, sk.CooldownRatio);
            else
                _hud.SetHeroSkill(false, null, false, 0f, 0f);

            // 武器面板
            var handler = _player != null ? _player.GetComponent<PlayerWeaponHandler>() : null;
            if (handler != null)
            {
                bool   hasSkill = handler.ActiveWeapon?.Data?.HasSkill == true;
                bool   ready    = handler.SkillReady;
                float  fill     = 1f - handler.SkillCooldownRatio;
                string label    = hasSkill
                    ? (ready ? $"[R] {handler.ActiveWeapon.Data.skill.skillName}  ✦ Ready!"
                             : $"[R] {handler.ActiveWeapon.Data.skill.skillName}  CD {handler.SkillCooldownRemaining:0.0}s")
                    : null;
                _hud.SetWeapon(true, BuildWeaponSlot(handler, 0), BuildWeaponSlot(handler, 1),
                               hasSkill, ready, fill, label);
            }
            else _hud.SetWeapon(false, default, default, false, false, 0f, null);

            // 左上角：天赋
            _talentChips.Clear();
            foreach (var at in _activeTalents)
            {
                Color c = at.IsPermanent ? new Color(0.95f, 0.88f, 0.35f) : new Color(1f, 0.68f, 0.28f);
                _talentChips.Add(new Game.UI.HudView.Chip
                {
                    color    = c,
                    title    = at.Data.talentName,
                    subtitle = at.IsPermanent ? at.Data.description : $"{at.Data.description}  [{at.RoomsLeft} rooms]",
                });
            }
            _hud.SetTalents(_talentChips);

            // 左上角：剧情道具
            _itemChips.Clear();
            var run = GameManager.Instance?.Run;
            if (run != null && run.StoryItems != null)
            {
                foreach (var item in run.StoryItems)
                {
                    string flavor = Game.Systems.StoryItemDatabase.TryGet(item, out var def) ? def.flavorTag : null;
                    _itemChips.Add(new Game.UI.HudView.Chip
                    {
                        color = ClassifyStoryItemColor(item), title = $"✦ {item}", subtitle = flavor,
                    });
                }
            }
            _hud.SetItems(_itemChips);

            // 左上角：道具协同
            _synergyChips.Clear();
            if (run != null)
            {
                foreach (var s in Game.Systems.StoryItemSynergyDatabase.All)
                    if (Game.Systems.StoryItemSynergyDatabase.IsActive(run, s.id))
                        _synergyChips.Add(new Game.UI.HudView.Chip
                        {
                            color = new Color(1f, 0.72f, 0.92f), title = $"★ {s.displayName}", subtitle = s.flavor,
                        });
            }
            _hud.SetSynergies(_synergyChips);

            // Boss 血条
            if (_bossHealth != null) _hud.SetBoss(true, _bossName, _bossHealth.Current, _bossHealth.Max);
            else                     _hud.SetBoss(false, null, 0f, 0f);

            // 提示横幅
            bool bannerOn = Time.time < _bannerUntil && !string.IsNullOrEmpty(_bannerMessage);
            _hud.SetBanner(bannerOn, _bannerMessage);
        }

        private Game.UI.HudView.WeaponSlot BuildWeaponSlot(PlayerWeaponHandler handler, int i)
        {
            var wi     = handler.Slots[i];
            bool active = handler.ActiveSlotIndex == i;
            var slot   = new Game.UI.HudView.WeaponSlot { occupied = wi != null, active = active };
            if (wi == null)
            {
                slot.color = new Color(0.38f, 0.38f, 0.42f);
                slot.line1 = $"{(active ? "▶ " : "   ")}Slot {i + 1}  [Empty]";
                slot.line2 = null;
                return slot;
            }
            Color rc = WeaponData.GetRarityColor(wi.Data.rarity);
            if (!active) rc *= 0.65f;
            slot.color = rc;
            slot.icon  = WeaponSprites.Get(wi.Data.weaponName);
            slot.line1 = $"{(active ? "▶ " : "   ")}{wi.ShortName}  {wi.EffectiveDamage:0} dmg  {wi.Data.attackSpeed:0.0}/s";
            string upg = wi.Data.CanEnchant
                ? $"Forge+{wi.UpgradeLevel}/{wi.Data.maxUpgradeLevel} Ench+{wi.EnchantLevel}/{wi.Data.maxEnchantLevel}"
                : $"Forge+{wi.UpgradeLevel}/{wi.Data.maxUpgradeLevel}";
            slot.line2 = $"HP+{wi.HPBonus:0}  {upg}{WeaponSpecialLabel(wi)}";
            return slot;
        }


        // 本周目持有的剧情道具（左侧栏，置于天赋之下）
        // 净化系=青；污染系=紫；纯叙事=米黄
        private static readonly System.Collections.Generic.HashSet<string> _pureItems = new System.Collections.Generic.HashSet<string> {
            "Frost Mirror Shard", "Restraint", "Memory Pact", "Courage to Overthrow"
        };
        private static readonly System.Collections.Generic.HashSet<string> _taintedItems = new System.Collections.Generic.HashSet<string> {
            "Lakebed Relic", "Memory of Confrontation", "Echo of Fury", "Throne's Lingering Might", "Void Memory Shard"
        };

        private static Color ClassifyStoryItemColor(string id)
        {
            if (_pureItems.Contains(id))    return new Color(0.60f, 0.85f, 0.95f);
            if (_taintedItems.Contains(id)) return new Color(0.93f, 0.62f, 0.95f);
            return new Color(0.92f, 0.84f, 0.55f);
        }


        private void EndingTitle(out string title, out Color tint)
        {
            switch (_endingTier)
            {
                case EndingTier.Crown:
                    title = "The crown falls; the name returns."; tint = new Color(1f,    0.86f, 0.55f); break;
                case EndingTier.Truth:
                    title = "Remember the name beneath.";          tint = new Color(0.92f, 0.78f, 1f);    break;
                default:
                    title = "The world closes its eyes, for now."; tint = new Color(0.85f, 0.85f, 0.78f); break;
            }
        }

        private static string WeaponSpecialLabel(WeaponInstance wi)
        {
            if (wi.Data.lifeStealRate > 0f)   return $"  Lifesteal {wi.Data.lifeStealRate * 100:0}%";
            if (wi.Data.hpCostPerAttack > 0f) return $"  HP cost {wi.Data.hpCostPerAttack:0}/hit";
            return "";
        }
    }
}
