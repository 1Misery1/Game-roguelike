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

namespace Game.Dev
{
    /// Single-scene state machine: Menu -> Playing -> FloorComplete -> Playing... -> Victory/Death -> Menu.
    /// Supports multi-floor runs with enemy scaling and weapon drops.
    public class GameBootstrap : MonoBehaviour
    {
        // Static bounds (written by MapBuilder, read by enemy AI)
        public static float ArenaHalfW { get; private set; } = 16f;
        public static float ArenaHalfH { get; private set; } = 10f;

        // 当前房间是否仍处于战斗中（读取方：可交互物，战斗期间禁止交互）
        public static bool CombatInProgress { get; private set; }
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
        private MapBuilder.MapInfo _mapInfo;
        private int _mapVariant;
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
            // 创建摄像机必须在 Awake 完成，否则第一帧渲染前 Unity 会报 "no cameras rendering"
            EnsureCamera();
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

        /// 过场开始时触发的全局 hook：动画系统可订阅此事件按当前结局类型播放
        /// (isTrueEnding, durationSeconds) -> 由订阅者使用
        public static event System.Action<bool, float> OnEndingCutsceneStart;
        /// 过场结束（自然完成或玩家跳过）时触发
        public static event System.Action OnEndingCutsceneEnd;

        /// 三档结局
        public enum EndingTier { Normal, Truth, Crown }

        private EndingTier _endingTier;
        private bool       _isTrueEnding;   // 兼容旧逻辑：Truth/Crown 都视作 true
        private int        _endingTruthCount;
        private float      _cutsceneStartTime;
        private float      _cutsceneDuration;

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
            _state = State.EndingCutscene;
            _cutsceneDuration =
                _endingTier == EndingTier.Crown ? CrownEndingCutsceneDuration :
                _endingTier == EndingTier.Truth ? TrueEndingCutsceneDuration  :
                                                  NormalEndingCutsceneDuration;
            _cutsceneStartTime = Time.unscaledTime;
            OnEndingCutsceneStart?.Invoke(_isTrueEnding, _cutsceneDuration);
            StartCoroutine(EndingCutsceneRoutine());
        }

        private System.Collections.IEnumerator EndingCutsceneRoutine()
        {
            // unscaledTime 等待，便于过场期间任意调整 Time.timeScale（如 0 让世界冻结）
            float endAt = Time.unscaledTime + _cutsceneDuration;
            while (Time.unscaledTime < endAt && _state == State.EndingCutscene)
                yield return null;
            FinishEndingCutscene();
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
            var   pool       = GetFloorRoomPool();
            float total      = 0f;
            foreach (var e in pool) total += e.weight;

            // Combat room count increases per floor: Floor1=4, Floor2=5, Floor3=6
            int combatCount  = nonBossRoomCount + (CurrentFloor - 1);
            var rooms        = new List<string>();
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
            // Reset player to left spawn point
            if (_player != null)
                _player.transform.position = _mapInfo.PlayerSpawn;

            if (index >= _floorRooms.Count)
            {
                TriggerVictory();
                return;
            }

            if (index > 0) OnNewRoomEntered(); // timed-talent countdown (room 0 excluded)

            var type = _floorRooms[index];
            _currentRoomRoot = new GameObject($"Room_{index}_{type}");
            CombatInProgress = false; // 新房间默认非战斗；有刷怪波次时由 SpawnRoomWave 置位
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
            go.transform.position = _mapInfo.PlayerSpawn + offsetFromSpawn;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>();   // 大小在 ApplyData 中由 data 写入
            var si = go.AddComponent<StoryInteractable>();
            si.Data = data;                     // 触发 ApplyData：scale/color/collider/dialogue
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
                go.transform.position   = new Vector3(x, -1.5f, 0f);
                go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = MakeUnitSquareSprite();
                sr.color        = WeaponData.GetRarityColor(weapon.Data.rarity);
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
                pickups[i].transform.position = new Vector3(-3.5f + 3.5f * i, 0f, 0f);

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

            int floorIdx   = Mathf.Clamp(CurrentFloor - 1, 0, ShopRarityTable.Length - 1);
            int[] rarities = ShopRarityTable[floorIdx];
            float priceScale = 1f + (CurrentFloor - 1) * 0.3f;

            // 6 weapons in a row
            for (int i = 0; i < rarities.Length; i++)
            {
                float x   = -5.5f + i * 2.2f;
                int   ri  = rarities[i];
                int   price = Mathf.RoundToInt(WeaponBasePrice[ri] * priceScale);
                SpawnShopWeaponPedestal(new Vector3(x, 1.5f, 0f), GetWeaponOfRarity(ri), price);
            }

            // Forge, talent, and enchant pedestals in the lower row
            int uses = Mathf.Min(CurrentFloor, 2);
            int forgePrice   = Mathf.RoundToInt((20 + CurrentFloor * 5) * priceScale);
            int enchantPrice = Mathf.RoundToInt((30 + CurrentFloor * 5) * priceScale);
            int talentPrice  = Mathf.RoundToInt(30 * priceScale);

            SpawnActionPedestal(new Vector3(-3f, -1.5f, 0f), ActionPedestal.ActionType.Forge,   forgePrice,   uses);
            SpawnTalentDrawPedestal(new Vector3(0f, -1.5f, 0f), talentPrice);
            SpawnActionPedestal(new Vector3( 3f, -1.5f, 0f), ActionPedestal.ActionType.Enchant, enchantPrice, uses);

            int potionPrice = Mathf.RoundToInt(25 * priceScale);
            SpawnHealthPotionPedestal(new Vector3(0f, -3.0f, 0f), potionPrice);
        }

        private void SpawnActionPedestal(Vector3 pos, ActionPedestal.ActionType actionType, int price, int uses)
        {
            bool isForge = actionType == ActionPedestal.ActionType.Forge;
            var go = new GameObject(isForge ? "ForgeAltar" : "EnchantAltar");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = isForge ? new Color(1f, 0.65f, 0.2f) : new Color(0.55f, 0.3f, 1f);
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
            var go = new GameObject("HealthPotion");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = new Color(0.2f, 0.92f, 0.38f);
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

        private void SpawnShopWeaponPedestal(Vector3 pos, WeaponInstance weapon, int price)
        {
            var go = new GameObject("ShopWeapon_" + weapon.Data.weaponName);
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
            var go = new GameObject("TalentDraw");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = new Color(0.75f, 0.3f, 0.95f);
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
            var boss   = EnemyFactory.SpawnHellGiant(new Vector3(0f, 2.5f, 0f),
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
            var boss = EnemyFactory.SpawnFrostLich(new Vector3(0f, 2.5f, 0f),
                           _player.transform, _currentRoomRoot.transform);
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // Floor 3: Chaos Lord — Chaos Burst + summon legion
        private void BuildFloor3Boss()
        {
            ShowBanner("BOSS — Chaos Lord appears! The final battle!");
            var boss   = EnemyFactory.SpawnChaosLord(new Vector3(0f, 2.5f, 0f),
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

                // 最终层 Boss 击败：若已满足隐藏 Boss 条件且尚未召唤过 → 进入「王国之罪」战
                if (CurrentFloor >= maxFloor)
                {
                    if (!_hiddenBossSpawned && IsHiddenBossUnlocked()) SpawnHiddenBoss();
                    else                                                TriggerVictory();
                }
                else
                {
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
            ShowBanner("「怪物穿着王冠，坐在地上。」");

            // 战前对话铺垫：用现有 DialogueBox 播放 4 句铭文
            var lines = new System.Collections.Generic.List<Game.Narrative.DialogueLine> {
                new Game.Narrative.DialogueLine("旁白",     null, "回廊深处，断裂的王座缓缓抬起。它从来不是一座座椅。"),
                new Game.Narrative.DialogueLine("王国之罪", null, "「你们想杀死怪物。」"),
                new Game.Narrative.DialogueLine("王国之罪", null, "「可怪物不是从虚空来的。」"),
                new Game.Narrative.DialogueLine("王国之罪", null, "「怪物穿着王冠，坐在地上。」"),
            };
            Game.Narrative.DialogueBox.Get().Play(lines, () =>
            {
                // 复用 ChaosLord 实体 + AI，整体放大 1.6×，HP 与伤害 2.5×
                var boss   = EnemyFactory.SpawnChaosLord(new Vector3(0f, 2.5f, 0f),
                                 _player.transform, _currentRoomRoot.transform);
                boss.name  = "Kingdom_Guilt";
                boss.transform.localScale *= 1.6f;
                var bossAI = boss.GetComponent<ChaosLordAI>();
                bossAI.SpawnMinionCallback = pos => SpawnRandomNormalEnemy(pos, () => { });
                ScaleEnemyStats(boss, FloorScale * 2.5f);

                _bossHealth = boss.GetComponent<Health>();
                _bossName   = "王国之罪";
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

            string dur = talent.roomDuration > 0 ? $" ({talent.roomDuration}房)" : "";
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
        private float[] GetFloorEnemyWeights()
        {
            // First two rooms only spawn basic enemies, no dangerous special enemies
            if (_currentRoomIndex <= 1)
                return new float[] { 4f, 4f, 2f, 3f, 0f, 0f, 0f, 0f };

            // From room 3 onward, floor-theme enemies gradually appear
            bool earlyRoom = _currentRoomIndex <= 2;
            switch (CurrentFloor)
            {
                case 1:
                    return earlyRoom
                        ? new float[] { 2f, 3f, 2f, 2f, 1f, 1f, 0f, 1f }   // ShieldGuard/ExplosiveDemon weight suppressed
                        : new float[] { 1f, 2f, 1f, 1f, 3f, 1f, 1f, 4f };  // Inferno full roster
                case 2:
                    return earlyRoom
                        ? new float[] { 4f, 1f, 3f, 3f, 0f, 0f, 0f, 0f }
                        : new float[] { 4f, 1f, 3f, 3f, 1f, 1f, 1f, 1f };  // Frost Realm full roster
                default:
                    return earlyRoom
                        ? new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 2f, 1f }
                        : new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 4f, 2f };  // Chaos Abyss full roster
            }
        }

        // Elite enemy spawn chance per floor (first two rooms never spawn elites)
        private float GetFloorEliteChance()
        {
            if (_currentRoomIndex <= 1) return 0f;
            switch (CurrentFloor)
            {
                case 1:  return 0.15f;
                case 2:  return 0.30f;
                default: return 0.55f;
            }
        }

        // Combat room pool per floor (includes theme-specific special rooms)
        private (string type, float weight)[] GetFloorRoomPool()
        {
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
        private void SetFloorBackground()
        {
            if (Camera.main == null) return;
            switch (CurrentFloor)
            {
                case 1:  Camera.main.backgroundColor = new Color(0.15f, 0.05f, 0.03f); break; // Inferno
                case 2:  Camera.main.backgroundColor = new Color(0.03f, 0.06f, 0.14f); break; // Frost Realm
                default: Camera.main.backgroundColor = new Color(0.07f, 0.03f, 0.11f); break; // Chaos Abyss
            }
        }

        // Rebuild arena with new floor colors (called on floor transition)
        private void UpdateArenaColors()
        {
            if (_arenaRoot != null) { Destroy(_arenaRoot); _arenaRoot = null; }
            _arenaRoot  = new GameObject("Arena");
            _mapVariant = Random.Range(0, 3);
            _mapInfo    = LoadRoomPrefab(CurrentFloor, _mapVariant, _arenaRoot.transform);
            ArenaHalfW  = _mapInfo.HalfW;
            ArenaHalfH  = _mapInfo.HalfH;
        }

        private string GetFloorName()
        {
            switch (CurrentFloor)
            {
                case 1:  return "Inferno";
                case 2:  return "Frost Realm";
                default: return "Chaos Abyss";
            }
        }

        private string GetFloorNarrative()
        {
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
                case "Monster":    return "战斗";
                case "Talent":     return "天赋";
                case "Coin":       return "金币";
                case "Shop":       return "商店";
                case "Boss":       return "BOSS";
                case "HellTrial":  return "熔炉试炼";
                case "FrostGrave": return "霜墓密室";
                case "ChaosRift":  return "混沌裂隙";
                default:           return type;
            }
        }

        // ── 地形杀生成 ────────────────────────────────────────────────────

        // 按楼层在当前房间生成地形危险区域（随房间 root 销毁）
        private void SpawnFloorHazards()
        {
            if (_currentRoomRoot == null) return;
            switch (CurrentFloor)
            {
                case 1: SpawnFlamePillars();   break;
                case 2: SpawnIceSpikeTraps();  break;
                case 3: SpawnVoidRifts();      break;
            }
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
            foreach (var p in positions)
            {
                var go = new GameObject("FlamePillar");
                go.transform.SetParent(root, true);
                go.transform.position   = new Vector3(p.x, p.y, 0f);
                go.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                var lavaSpr     = LoadHazardSprite("Tiles/lava_hazard");
                sr.sprite       = lavaSpr != null ? lavaSpr : MakeUnitSquareSprite();
                sr.color        = new Color(0.45f, 0.08f, 0.04f, 0.80f);
                sr.sortingOrder = 3;

                go.AddComponent<FlamePillar>();
                go.AddComponent<Game.AI.NavHazardRegistrar>().radius = 0.9f;
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
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("IceSpikeTrap");
                go.transform.SetParent(root, true);
                go.transform.position   = new Vector3(candidates[i].x, candidates[i].y, 0f);
                go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = MakeUnitSquareSprite();
                sr.color        = new Color(0.25f, 0.45f, 0.80f, 0.10f);
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
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("VoidRift");
                go.transform.SetParent(root, true);
                go.transform.position   = new Vector3(candidates[i].x, candidates[i].y, 0f);
                go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = MakeUnitSquareSprite();
                sr.color        = new Color(0.55f, 0.05f, 0.75f, 0.90f);
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

        // 分两波刷怪（55%/45%）；每波从房间四壁边缘生成，第一波不出精英
        private void SpawnRoomWave(int totalCount, System.Action onAllDead, bool multiWave = true)
        {
            if (_player == null) return;

            // 战斗开始：禁止可交互物交互，直到全部敌人清场
            CombatInProgress = true;
            System.Action onCleared = () =>
            {
                CombatInProgress = false;
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
                ShowBanner("第二波来袭！");
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
            ShowBanner("精英怪出现！");
            RegisterEnemy(elite, 15, onDied);
        }

        // 15% 概率在当前房间生成神秘祭坛（可选互动）
        private void MaybeAddAltar()
        {
            if (Random.value >= 0.15f) return;

            var go = new GameObject("AltarPedestal");
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.position   = new Vector3(-6f, 2.8f, 0f);
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = new Color(0.75f, 0.3f, 0.95f);
            sr.sortingOrder = 7;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.7f;
            col.isTrigger = true;

            var mystery = go.AddComponent<MysteryPedestal>();
            mystery.OnResolved += HandleAltar;
            ShowBanner("神秘祭坛出现！（可选择互动）");
        }

        private void HandleAltar(MysteryOutcome outcome)
        {
            switch (outcome)
            {
                case MysteryOutcome.Lucky:
                    RunCoins += 25;
                    ShowBanner("祭坛祝福：+25 金币！");
                    break;
                case MysteryOutcome.Gift:
                    var gift = GenerateRandomTalent();
                    ApplyTalentToPlayer(gift);
                    ShowBanner($"祭坛馈赠：天赋 [{gift.talentName}]！");
                    break;
                case MysteryOutcome.Heal:
                    if (_playerHealth != null) _playerHealth.Heal(9999f);
                    ShowBanner("祭坛治愈：满血复活！");
                    break;
                case MysteryOutcome.Cursed:
                    if (_player != null)
                    {
                        var stats = _player.GetComponent<CharacterStats>();
                        stats?.AddModifier(new StatModifier(StatType.MaxHP, ModifierOp.PercentMul, -0.15f, "Altar_Curse"));
                    }
                    ShowBanner("祭坛诅咒：最大生命 -15%！");
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
                    ShowBanner($"天赋 [{at.Data.talentName}] 已到期消失");
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
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = new Color(0.25f, 0.95f, 0.35f);
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
        }

        private void SpawnWeaponPedestal(Vector3 pos, WeaponInstance weapon)
        {
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
                ShowBanner($"已装备: {w.ShortName}");
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
                    ShowBanner($"获得重复武器！{existing.ShortName} 自动升级！");
                    return true;
                }
                if (existing.TryEnchant())
                {
                    handler.RefreshWeaponHPBonus(i);
                    ShowBanner($"获得重复武器！{existing.ShortName} 自动附魔！");
                    return true;
                }
                ShowBanner($"获得重复武器！{existing.ShortName} 已满级，丢弃");
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

        private TalentPickup SpawnTalentOrb(TalentData data, Color color)
        {
            var go = new GameObject("Talent_" + data.talentName);
            go.transform.SetParent(_currentRoomRoot.transform, true);
            go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
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
            _arenaRoot  = new GameObject("Arena");
            _mapVariant = Random.Range(0, 3);
            _mapInfo    = LoadRoomPrefab(CurrentFloor, _mapVariant, _arenaRoot.transform);
            ArenaHalfW  = _mapInfo.HalfW;
            ArenaHalfH  = _mapInfo.HalfH;
        }

        private MapBuilder.MapInfo LoadRoomPrefab(int floor, int variant, Transform parent)
        {
            string letter = _variantLetters[variant % 3];
            string path   = $"Rooms/Floor{floor}/Room_F{floor}{letter}";
            var prefab    = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[GameBootstrap] Prefab 未找到：{path}，回退到程序化生成");
                return MapBuilder.Build(floor, variant, parent);
            }

            var rows = MapBuilder.GetMap(floor, variant);
            NavGrid.Build(rows);
            MapBuilder.SetupPhysics();
            FloorBackground.Create(floor, parent, MapBuilder.TileW + 4f, MapBuilder.TileH + 4f);

            var roomGO = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            var meta   = roomGO.GetComponent<Game.Dungeon.RoomMetadata>();
            if (meta != null) return meta.ToMapInfo();

            return new MapBuilder.MapInfo
            {
                HalfW       = MapBuilder.TileW * 0.5f,
                HalfH       = MapBuilder.TileH * 0.5f,
                PlayerSpawn = new Vector3(-MapBuilder.TileW * 0.5f + 2.5f, 0f, 0f),
                DoorPos     = new Vector3( MapBuilder.TileW * 0.5f - 0.5f, 0f, 0f),
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

        /// 外部脚本（如数据驱动的 StoryInteractable）转发横幅显示
        public static void PostBanner(string message)
        {
            if (_instance != null) _instance.ShowBanner(message);
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

        private void FillRect(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, WhitePixel);
            GUI.color = prev;
        }

        // ── 横条进度条 ──────────────────────────────────────────────
        private void DrawBar(float x, float y, float w, float h, float ratio,
                             Color bg, Color fill, Color border)
        {
            FillRect(new Rect(x - 1, y - 1, w + 2, h + 2), border);
            FillRect(new Rect(x, y, w, h), bg);
            if (ratio > 0f) FillRect(new Rect(x, y, w * Mathf.Clamp01(ratio), h), fill);
        }

        // ─────────────────────────────────────────────────────────────
        //  OnGUI 入口
        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            switch (_state)
            {
                case State.Playing:        DrawHUD();                                                  break;
                case State.FloorComplete:  DrawFloorComplete();                                        break;
                case State.EndingCutscene: DrawEndingCutscene();                                       break;
                case State.Victory:        DrawVictoryScreen();                                        break;
                case State.Death:          DrawEndScreen("阵亡",  new Color(1f, 0.3f,  0.3f), false); break;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  战斗 HUD
        // ─────────────────────────────────────────────────────────────
        private void DrawHUD()
        {
            DrawTopBar();
            DrawPlayerHPBar();
            DrawGoldDisplay();
            DrawHeroSkillHUD();
            DrawWeaponHUD();
            DrawTalentStatus();
            DrawBossHPBar();

            // 提示横幅
            if (Time.time < _bannerUntil && !string.IsNullOrEmpty(_bannerMessage))
            {
                float bY = Screen.height * 0.15f;
                FillRect(new Rect(Screen.width * 0.2f, bY - 4, Screen.width * 0.6f, 44), new Color(0f, 0f, 0f, 0.62f));
                FillRect(new Rect(Screen.width * 0.2f, bY - 4, Screen.width * 0.6f, 2), new Color(0.95f, 0.8f, 0.2f, 0.8f));
                GUI.Label(new Rect(0, bY, Screen.width, 36),
                    _bannerMessage,
                    MkLabel(22, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(1f, 0.92f, 0.35f)));
            }

            if (_pendingTalent != null) DrawTalentReplacementOverlay();
        }

        // 顶部细条：楼层/房间信息 + 操作提示
        private void DrawTopBar()
        {
            FillRect(new Rect(0, 0, Screen.width, 28), new Color(0f, 0f, 0f, 0.62f));
            string roomT = _currentRoomIndex < _floorRooms.Count ? _floorRooms[_currentRoomIndex] : "—";
            string info = $"「{GetFloorName()}」 · 第{_currentRoomIndex + 1}/{_floorRooms.Count}间 · {GetRoomDisplayName(roomT)} · 难度×{FloorScale:0.00}";
            GUI.Label(new Rect(10, 3, Screen.width * 0.55f, 22), info,
                MkLabel(13, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f)));
            GUI.Label(new Rect(0, 3, Screen.width - 10, 22),
                "WASD移动  Space/鼠标左键攻击  R/右键技能  F英雄技能  Q换武器  E互动  绿门进入下一间",
                MkLabel(11, TextAnchor.MiddleRight, FontStyle.Normal, new Color(0.52f, 0.52f, 0.58f)));
        }

        // 底部中央：玩家血量条
        private void DrawPlayerHPBar()
        {
            if (_playerHealth == null) return;
            float barW = 480f;
            float barH = 28f;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = Screen.height - 48f;

            float ratio = _playerHealth.Ratio;
            Color fill = ratio > 0.6f ? Color.Lerp(new Color(0.85f, 0.7f, 0.08f), new Color(0.18f, 0.82f, 0.28f), (ratio - 0.6f) / 0.4f)
                       : ratio > 0.25f ? Color.Lerp(new Color(0.88f, 0.18f, 0.1f), new Color(0.85f, 0.7f, 0.08f), (ratio - 0.25f) / 0.35f)
                       :                 new Color(0.88f, 0.12f, 0.08f);

            // 外框
            FillRect(new Rect(barX - 2, barY - 2, barW + 4, barH + 4), new Color(0f, 0f, 0f, 0.8f));
            FillRect(new Rect(barX - 1, barY - 1, barW + 2, barH + 2), new Color(0.35f, 0.35f, 0.35f, 0.6f));

            DrawBar(barX, barY, barW, barH, ratio,
                new Color(0.16f, 0.05f, 0.05f),
                fill,
                new Color(0f, 0f, 0f, 0f));

            // 血量分段刻度线
            int segs = 5;
            for (int s = 1; s < segs; s++)
            {
                float sx = barX + barW * s / segs;
                FillRect(new Rect(sx - 0.5f, barY, 1, barH), new Color(0f, 0f, 0f, 0.35f));
            }

            // 血量文字
            var hpS = MkLabel(13, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            GUI.Label(new Rect(barX, barY, barW, barH),
                $"♥  {Mathf.CeilToInt(_playerHealth.Current)} / {Mathf.CeilToInt(_playerHealth.Max)}", hpS);
        }

        // 血条左侧金币
        private void DrawGoldDisplay()
        {
            float barW = 480f;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = Screen.height - 48f;
            float goldX = barX - 128f;
            FillRect(new Rect(goldX - 4, barY - 2, 120, 32), new Color(0f, 0f, 0f, 0.65f));
            GUI.Label(new Rect(goldX, barY + 4, 114, 22),
                $"◈  {RunCoins}",
                MkLabel(16, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(1f, 0.88f, 0.2f)));
        }

        // 武器 HUD（右下角）
        private void DrawWeaponHUD()
        {
            if (_player == null) return;
            var handler = _player.GetComponent<PlayerWeaponHandler>();
            if (handler == null) return;

            float panelW = 360f;
            float panelX = Screen.width - panelW - 8f;
            bool hasSkill = handler.ActiveWeapon?.Data?.HasSkill == true;
            float panelH = hasSkill ? 118f : 92f;
            float panelY = Screen.height - panelH - 8f;

            FillRect(new Rect(panelX - 6, panelY - 6, panelW + 12, panelH + 12), new Color(0f, 0f, 0f, 0.72f));
            FillRect(new Rect(panelX - 6, panelY - 6, panelW + 12, 2), new Color(0.5f, 0.5f, 0.6f, 0.4f));

            for (int i = 0; i < 2; i++)
            {
                var wi      = handler.Slots[i];
                bool active = handler.ActiveSlotIndex == i;
                float slotY = panelY + i * 44f;
                float slotH = 40f;

                // 激活背景高亮
                if (active) FillRect(new Rect(panelX, slotY, panelW, slotH), new Color(0.18f, 0.22f, 0.38f, 0.7f));

                // 左侧激活竖条
                if (active) FillRect(new Rect(panelX, slotY + 4, 3, slotH - 8), new Color(0.45f, 0.75f, 1f));

                Color rc = wi == null ? new Color(0.45f, 0.45f, 0.5f) : WeaponData.GetRarityColor(wi.Data.rarity);
                if (!active) rc *= 0.65f;

                // 武器图标 30×30
                var iconR = new Rect(panelX + 7f, slotY + 5f, 30f, 30f);
                if (wi != null)
                {
                    FillRect(iconR, new Color(0.08f, 0.08f, 0.1f));
                    var spr = WeaponSprites.Get(wi.Data.weaponName);
                    if (spr != null) GUI.DrawTexture(iconR, spr.texture);
                    else FillRect(iconR, rc * 0.5f);
                }
                else FillRect(iconR, new Color(0.2f, 0.2f, 0.22f));

                float tx = panelX + 41f;
                float tw = panelW - 45f;

                if (wi == null)
                {
                    GUI.Label(new Rect(tx, slotY + 11, tw, 18),
                        $"{(active ? "▶ " : "   ")}槽位 {i + 1}  [空]",
                        MkLabel(12, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.38f, 0.38f, 0.42f)));
                }
                else
                {
                    GUI.Label(new Rect(tx, slotY + 2, tw, 18),
                        $"{(active ? "▶ " : "   ")}{wi.ShortName}  {wi.EffectiveDamage:0}伤  {wi.Data.attackSpeed:0.0}/s",
                        MkLabel(active ? 13 : 11, TextAnchor.MiddleLeft, active ? FontStyle.Bold : FontStyle.Normal, rc));
                    string upg = wi.Data.CanEnchant
                        ? $"锻+{wi.UpgradeLevel}/{wi.Data.maxUpgradeLevel} 附+{wi.EnchantLevel}/{wi.Data.maxEnchantLevel}"
                        : $"锻+{wi.UpgradeLevel}/{wi.Data.maxUpgradeLevel}";
                    GUI.Label(new Rect(tx, slotY + 22, tw, 16),
                        $"HP+{wi.HPBonus:0}  {upg}{WeaponSpecialLabel(wi)}",
                        MkLabel(10, TextAnchor.MiddleLeft, FontStyle.Normal, rc * 0.82f));
                }
            }

            if (hasSkill)
            {
                float skillY = panelY + 92f;
                bool  ready  = handler.SkillReady;
                float fill   = 1f - handler.SkillCooldownRatio;
                Color barFill = ready ? new Color(0.25f, 0.8f, 0.28f) : new Color(0.25f, 0.4f, 0.85f);
                DrawBar(panelX, skillY, panelW, 22f, fill,
                    new Color(0.12f, 0.12f, 0.18f), barFill, new Color(0f, 0f, 0f, 0f));
                string skillLbl = ready
                    ? $"[R] {handler.ActiveWeapon.Data.skill.skillName}  ✦ 就绪!"
                    : $"[R] {handler.ActiveWeapon.Data.skill.skillName}  冷却 {handler.SkillCooldownRemaining:0.0}s";
                GUI.Label(new Rect(panelX, skillY, panelW, 22),
                    skillLbl,
                    MkLabel(11, TextAnchor.MiddleCenter, FontStyle.Normal, ready ? new Color(0.55f, 1f, 0.55f) : new Color(0.75f, 0.8f, 1f)));
            }
        }

        // 英雄技能 HUD（左下，HP条左侧上方）
        private void DrawHeroSkillHUD()
        {
            if (_player == null) return;
            var sk = _player.GetComponent<HeroActiveSkillHandler>();
            if (sk == null || sk.SkillType == HeroSkillType.None) return;

            float barW = 200f;
            float barX = (Screen.width - 480f) * 0.5f - barW - 14f;
            float barY = Screen.height - 48f;

            FillRect(new Rect(barX - 4, barY - 2, barW + 8, 32), new Color(0f, 0f, 0f, 0.65f));

            bool  ready   = sk.IsReady;
            float fill    = 1f - sk.CooldownRatio;
            Color bFill   = ready ? new Color(0.9f, 0.75f, 0.1f) : new Color(0.55f, 0.45f, 0.18f);
            DrawBar(barX, barY + 14f, barW, 12f, fill,
                new Color(0.22f, 0.18f, 0.06f), bFill, new Color(0f, 0f, 0f, 0f));
            GUI.Label(new Rect(barX, barY + 2, barW, 14),
                ready ? $"[F] {sk.SkillName}  ✦ 就绪!" : $"[F] {sk.SkillName}  {sk.CooldownRemaining:0.0}s",
                MkLabel(11, TextAnchor.MiddleCenter, FontStyle.Bold,
                    ready ? new Color(1f, 0.9f, 0.25f) : new Color(0.7f, 0.62f, 0.35f)));
        }

        // 天赋状态（左侧竖排小标签）
        private void DrawTalentStatus()
        {
            if (_activeTalents.Count == 0) return;
            float chipW = 200f;
            float chipH = 28f;
            float chipX = 8f;
            float startY = 36f;
            FillRect(new Rect(chipX - 4, startY - 4, chipW + 8, _activeTalents.Count * (chipH + 4) + 4), new Color(0f, 0f, 0f, 0.55f));
            for (int i = 0; i < _activeTalents.Count; i++)
            {
                var at  = _activeTalents[i];
                float y = startY + i * (chipH + 4);
                Color c = at.IsPermanent ? new Color(0.95f, 0.88f, 0.35f) : new Color(1f, 0.68f, 0.28f);
                FillRect(new Rect(chipX, y, 3, chipH), c);
                GUI.Label(new Rect(chipX + 6, y, chipW - 6, 16),
                    $"{at.Data.talentName}",
                    MkLabel(12, TextAnchor.MiddleLeft, FontStyle.Bold, c));
                GUI.Label(new Rect(chipX + 6, y + 14, chipW - 6, 13),
                    at.IsPermanent ? at.Data.description : $"{at.Data.description}  [{at.RoomsLeft}间]",
                    MkLabel(10, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.75f, 0.75f, 0.75f)));
            }
        }

        // Boss血量条（顶部居中，宽大醒目）
        private void DrawBossHPBar()
        {
            if (_bossHealth == null) return;
            float barW  = Mathf.Min(600f, Screen.width * 0.52f);
            float barH  = 20f;
            float barX  = (Screen.width - barW) * 0.5f;
            float barY  = 36f;
            float ratio = Mathf.Clamp01(_bossHealth.Current / _bossHealth.Max);

            FillRect(new Rect(barX - 8, barY - 26, barW + 16, barH + 34), new Color(0f, 0f, 0f, 0.72f));
            FillRect(new Rect(barX - 8, barY - 26, barW + 16, 2), new Color(0.8f, 0.2f, 0.2f, 0.7f));

            GUI.Label(new Rect(0, barY - 22, Screen.width, 18),
                _bossName ?? "BOSS",
                MkLabel(14, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(1f, 0.35f, 0.35f)));

            DrawBar(barX, barY, barW, barH, ratio,
                new Color(0.2f, 0.06f, 0.06f),
                Color.Lerp(new Color(0.75f, 0.12f, 0.12f), new Color(0.95f, 0.35f, 0.1f), ratio),
                new Color(0.5f, 0.1f, 0.1f, 0.8f));

            // 分段刻度
            for (int s = 1; s <= 4; s++)
                FillRect(new Rect(barX + barW * s / 5f - 0.5f, barY, 1, barH), new Color(0f, 0f, 0f, 0.45f));

            var hpS = MkLabel(11, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white);
            GUI.Label(new Rect(barX, barY, barW, barH),
                $"{Mathf.CeilToInt(_bossHealth.Current):N0} / {Mathf.CeilToInt(_bossHealth.Max):N0}", hpS);
        }

        // 天赋替换覆盖层
        private void DrawTalentReplacementOverlay()
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.75f));
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.Label(new Rect(0, cy - 105, Screen.width, 38),
                $"天赋槽已满  ·  新天赋：{_pendingTalent.talentName}",
                MkLabel(24, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(1f, 0.88f, 0.22f)));
            GUI.Label(new Rect(0, cy - 62, Screen.width, 26),
                _pendingTalent.description,
                MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.88f, 0.88f, 0.88f)));
            GUI.Label(new Rect(0, cy - 30, Screen.width, 22),
                "选择要替换的天赋，或取消放弃新天赋",
                MkLabel(14, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f)));

            float btnW = 300f, btnH = 46f;
            var btnS = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            for (int i = 0; i < _activeTalents.Count; i++)
            {
                var at = _activeTalents[i];
                string dur = at.IsPermanent ? "永久" : $"剩{at.RoomsLeft}间";
                if (GUI.Button(new Rect(cx - btnW * 0.5f, cy + 10 + i * (btnH + 8), btnW, btnH),
                    $"替换：{at.Data.talentName}  ({dur})", btnS))
                    ReplaceTalentAt(i);
            }
            float cancelY = cy + 10 + _activeTalents.Count * (btnH + 8) + 12;
            if (GUI.Button(new Rect(cx - btnW * 0.5f, cancelY, btnW, 38), "取消  (放弃新天赋)", btnS))
                _pendingTalent = null;
        }

        // 层间过渡画面
        private void DrawFloorComplete()
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.06f, 0.04f, 0.88f));
            FillRect(new Rect(Screen.width * 0.1f, Screen.height * 0.12f, Screen.width * 0.8f, 3), new Color(0.3f, 0.95f, 0.45f, 0.7f));

            GUI.Label(new Rect(0, Screen.height * 0.15f, Screen.width, 70),
                $"第 {CurrentFloor} 层  ·  「{GetFloorName()}」  通关！",
                MkLabel(46, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.35f, 1f, 0.5f)));

            GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, 28),
                $"获得  +{clearReward}  解锁货币    当前金币: {RunCoins}",
                MkLabel(18, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white));

            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 24),
                $"下一层难度倍率: ×{1f + CurrentFloor * 0.25f:0.00}  (敌人HP与攻击力同步提升)",
                MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(1f, 0.78f, 0.45f)));

            if (_playerHealth != null)
            {
                float r = _playerHealth.Ratio;
                Color hc = r > 0.5f ? new Color(0.25f, 0.95f, 0.4f) : r > 0.25f ? new Color(1f, 0.85f, 0.12f) : new Color(1f, 0.3f, 0.3f);
                GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 26),
                    $"当前生命: {Mathf.CeilToInt(_playerHealth.Current)} / {Mathf.CeilToInt(_playerHealth.Max)}  ({r * 100:0}%)",
                    MkLabel(16, TextAnchor.MiddleCenter, FontStyle.Normal, hc));

                // 回血按钮
                int hCost = 30 + CurrentFloor * 10;
                GUI.enabled = RunCoins >= hCost && r < 0.999f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 175, Screen.height * 0.49f, 350, 40),
                    $"花费 {hCost} 金币  回复 50% 生命值",
                    new GUIStyle(GUI.skin.button) { fontSize = 14 }))
                {
                    RunCoins -= hCost;
                    _playerHealth.Heal(_playerHealth.Max * 0.5f);
                }
                GUI.enabled = true;
            }

            var btn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            float btnCx = Screen.width * 0.5f;
            if (GUI.Button(new Rect(btnCx - 145, Screen.height * 0.57f, 290, 52), $"进入第 {CurrentFloor + 1} 层  ▶", btn))
                AdvanceFloor();
            if (GUI.Button(new Rect(btnCx - 145, Screen.height * 0.57f + 62, 290, 46), "返回主菜单", btn))
                ReturnToMenu();
        }

        // 结算/死亡画面
        // 周目结局过场基线动画：纯黑幕 fade-in → 静默 hold → fade-out。
        // 真实动画接入后由订阅 OnEndingCutsceneStart 的系统覆盖；本占位提供
        // 最小可信的视觉过渡，避免战斗画面突然切到结算 UI。
        //
        // 时间线（progress = elapsed / duration）：
        //   0.00 — 0.15  : 黑幕淡入（alpha 0 → 1）
        //   0.15 — 0.30  : 标题淡入
        //   0.30 — 0.85  : 标题保持（hold）
        //   0.85 — 1.00  : 整体淡出（黑幕 + 标题 alpha 1 → 0）
        private void DrawEndingCutscene()
        {
            float elapsed  = Mathf.Max(0f, Time.unscaledTime - _cutsceneStartTime);
            float progress = _cutsceneDuration > 0f ? Mathf.Clamp01(elapsed / _cutsceneDuration) : 1f;

            // 黑幕 alpha：开头淡入，结尾淡出
            float blackAlpha =
                progress < 0.15f ? Mathf.SmoothStep(0f, 1f, progress / 0.15f) :
                progress > 0.85f ? Mathf.SmoothStep(1f, 0f, (progress - 0.85f) / 0.15f) :
                                   1f;
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, blackAlpha));

            // 标题 alpha：中段淡入并保持，结尾随黑幕同步淡出
            float titleAlpha =
                progress < 0.15f ? 0f :
                progress < 0.30f ? Mathf.SmoothStep(0f, 1f, (progress - 0.15f) / 0.15f) :
                progress < 0.85f ? 1f :
                                   Mathf.SmoothStep(1f, 0f, (progress - 0.85f) / 0.15f);

            if (titleAlpha > 0.001f)
            {
                string title;
                Color  baseTint;
                switch (_endingTier)
                {
                    case EndingTier.Crown:
                        title    = "「王冠落地，名字归来。」";
                        baseTint = new Color(1f,    0.86f, 0.55f);
                        break;
                    case EndingTier.Truth:
                        title    = "「记住地下之名。」";
                        baseTint = new Color(0.92f, 0.78f, 1f);
                        break;
                    default:
                        title    = "「世界暂时合上了眼。」";
                        baseTint = new Color(0.85f, 0.85f, 0.78f);
                        break;
                }
                var tint = new Color(baseTint.r, baseTint.g, baseTint.b, titleAlpha);
                GUI.Label(new Rect(0, Screen.height * 0.46f, Screen.width, 60), title,
                    MkLabel(28, TextAnchor.MiddleCenter, FontStyle.Italic, tint));
            }

            // 极淡的跳过提示（仅在黑幕完全展开时显示）
            if (blackAlpha > 0.95f && progress < 0.85f)
            {
                GUI.Label(new Rect(0, Screen.height * 0.88f, Screen.width, 22),
                    "（空格 / 回车 跳过）",
                    MkLabel(12, TextAnchor.MiddleCenter, FontStyle.Normal,
                        new Color(0.45f, 0.45f, 0.50f, 0.6f)));
            }

            // 跳过键
            var e = Event.current;
            bool skipReq =
                (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Space ||
                                                  e.keyCode == KeyCode.Return ||
                                                  e.keyCode == KeyCode.Escape)) ||
                (e.type == EventType.MouseDown && e.button == 0);
            if (skipReq) FinishEndingCutscene();
        }

        // 三档结局画面
        private void DrawVictoryScreen()
        {
            switch (_endingTier)
            {
                case EndingTier.Crown:
                    DrawEndScreen("真相·王冠", new Color(1f, 0.86f, 0.55f, 1f), true,
                        "「怪物穿着王冠，坐在地上——而你将它推下了王座。」",
                        $"王国之罪已被揭穿。  （已知真相 {_endingTruthCount} / 10）");
                    break;
                case EndingTier.Truth:
                    DrawEndScreen("真相·余烬", new Color(0.92f, 0.78f, 1f, 1f), true,
                        "「世界暂时得救。」",
                        $"地下的名字，再无人能忽视。  （已知真相 {_endingTruthCount} / 10）");
                    break;
                default:
                    DrawEndScreen("胜利", new Color(1f, 0.92f, 0.2f, 1f), true,
                        "「世界暂时得救。」",
                        $"但地下的名字，仍无人记得。  （已知真相 {_endingTruthCount} / 10）");
                    break;
            }
        }

        private void DrawEndScreen(string title, Color color, bool victory, string subtitle = null, string footnote = null)
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.72f));
            GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 86),
                title, MkLabel(62, TextAnchor.MiddleCenter, FontStyle.Bold, color));

            if (victory)
            {
                if (!string.IsNullOrEmpty(subtitle))
                    GUI.Label(new Rect(0, Screen.height * 0.215f, Screen.width, 30), subtitle,
                        MkLabel(20, TextAnchor.MiddleCenter, FontStyle.Italic, new Color(0.92f, 0.86f, 0.7f)));

                GUI.Label(new Rect(0, Screen.height * 0.265f, Screen.width, 30),
                    $"全部 {maxFloor} 层通关完成！  +{clearReward} 解锁货币  (合计: {_persistent.UnlockCurrency})",
                    MkLabel(18, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white));

                if (!string.IsNullOrEmpty(footnote))
                    GUI.Label(new Rect(0, Screen.height * 0.305f, Screen.width, 28), footnote,
                        MkLabel(16, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.78f, 0.78f, 0.85f)));
            }

            // 死亡时显示倒计时
            if (!victory)
            {
                float remaining = Mathf.Max(0f, _deathReturnAt - Time.time);
                GUI.Label(new Rect(0, Screen.height * 0.27f, Screen.width, 26),
                    $"{remaining:0.0} 秒后自动返回主菜单…",
                    MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Italic, new Color(0.75f, 0.55f, 0.55f)));
            }

            float sy = Screen.height * (victory ? 0.35f : 0.32f);
            var ss = MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.82f, 0.82f, 0.82f));
            GUI.Label(new Rect(0, sy,       Screen.width, 24), $"到达楼层: {CurrentFloor} / {maxFloor}", ss);
            GUI.Label(new Rect(0, sy + 28,  Screen.width, 24), $"击杀数: {_enemiesKilled}", ss);
            GUI.Label(new Rect(0, sy + 56,  Screen.width, 24), $"总伤害: {Mathf.RoundToInt(_totalDamageDealt):N0}", ss);
            GUI.Label(new Rect(0, sy + 84,  Screen.width, 24), $"剩余金币: {RunCoins}", ss);

            float btnY = Screen.height * (victory ? 0.60f : 0.58f);
            var bs = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (!victory)
            {
                if (GUI.Button(new Rect(Screen.width * 0.5f - 140, btnY, 280, 44), "立即返回主菜单", bs))
                    ReturnToMenu();
            }
            else
            {
                if (GUI.Button(new Rect(Screen.width * 0.5f - 140, btnY,       280, 44), "再次挑战", bs))  RestartRun();
                if (GUI.Button(new Rect(Screen.width * 0.5f - 140, btnY + 56f, 280, 44), "返回主菜单", bs)) ReturnToMenu();
            }
        }

        // 样式工厂（避免大量重复 new GUIStyle）
        private static GUIStyle MkLabel(int size, TextAnchor align, FontStyle style, Color color)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize  = size,
                alignment = align,
                fontStyle = style,
            };
            s.normal.textColor = color;
            return s;
        }

        private static string WeaponSpecialLabel(WeaponInstance wi)
        {
            if (wi.Data.lifeStealRate > 0f)   return $"  吸血{wi.Data.lifeStealRate * 100:0}%";
            if (wi.Data.hpCostPerAttack > 0f) return $"  耗血{wi.Data.hpCostPerAttack:0}/次";
            return "";
        }
    }
}
