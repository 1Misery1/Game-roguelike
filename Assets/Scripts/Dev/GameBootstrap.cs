using System.Collections.Generic;
using Game.AI;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Dungeon;
using Game.Player;
using Game.Systems;
using UnityEngine;

namespace Game.Dev
{
    /// Single-scene state machine: Menu -> Playing -> FloorComplete -> Playing... -> Victory/Death -> Menu.
    /// Supports multi-floor runs with enemy scaling and weapon drops.
    public class GameBootstrap : MonoBehaviour
    {
        // 静态边界（由 MapBuilder 写入，供敌人AI读取）
        public static float ArenaHalfW { get; private set; } = 16f;
        public static float ArenaHalfH { get; private set; } = 10f;
        [SerializeField] private int   clearReward     = 50;
        [SerializeField] private int   nonBossRoomCount = 4;
        [SerializeField] private int   maxFloor        = 3;

        private enum State { Menu, Playing, FloorComplete, Victory, Death }

        // 战斗房间权重（Shop 单独固定插入，Boss 固定最后）
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

        private State _state = State.Menu;
        private PersistentState _persistent;
        private HeroData[] _heroes;
        private int _selectedHeroIndex = 0;
        private MapBuilder.MapInfo _mapInfo;
        private int _mapVariant;

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
            public System.Action OnRoomEntered; // 每进入房间触发的额外效果
        }
        private readonly List<ActiveTalent> _activeTalents = new List<ActiveTalent>();
        private TalentData _pendingTalent;
        private const int  MaxTalents = 2;

        // --------------------------------------------------------------------
        //  Startup
        // --------------------------------------------------------------------

        private void Start()
        {
            _persistent    = PersistentState.Load();
            BuildHeroPool();
            EnsureStarterUnlocked();
            EnsureCamera();
            _damageNumbers = gameObject.AddComponent<DamageNumbers>();
            EnterMenu();
        }

        private void BuildHeroPool()
        {
            _heroes = new[]
            {
                MakeHero("Warrior", "坚韧近战战士，擅长正面对抗。",
                    100f, 14f, 5f, 5.5f, 1.0f, 0,   new Color(0.4f, 0.85f, 1f),
                    HeroSkillType.WarCry,        10f, "战吼",
                    HeroPassiveType.BattlefieldWill,  "战场意志"),

                MakeHero("Ranger", "敏捷游侠，连击可叠加攻击加成。",
                    70f,  12f, 0f, 8.0f, 1.4f, 30,  new Color(0.8f, 1f,   0.5f),
                    HeroSkillType.ShadowStep,    6f,  "影步",
                    HeroPassiveType.ComboStrike,      "连击"),

                MakeHero("Mage", "玻璃炮，技能后下次普攻伤害翻倍。",
                    55f,  22f, 0f, 5.0f, 0.8f, 60,  new Color(1f,   0.6f, 1f),
                    HeroSkillType.ArcaneSurge,   8f,  "奥术迸发",
                    HeroPassiveType.ManaAmplification,"魔力增幅"),

                MakeHero("Paladin", "圣骑士，击杀敌人回复5HP。",
                    110f, 11f, 6f, 5.0f, 0.9f, 100, new Color(1f,   0.9f, 0.4f),
                    HeroSkillType.HolyLight,     12f, "神圣之光",
                    HeroPassiveType.SacredOath,       "神圣誓约"),

                MakeHero("Hunter", "猎人，永久爆击率+20%、爆伤+30%。",
                    65f,  16f, 0f, 7.0f, 1.2f, 150, new Color(1f,   0.55f, 0.3f),
                    HeroSkillType.PrecisionShot, 7f,  "精准射击",
                    HeroPassiveType.EagleEye,         "鹰眼"),
            };
        }

        private HeroData MakeHero(
            string name, string desc,
            float hp, float atk, float def, float ms, float asp,
            int cost, Color tint,
            HeroSkillType skillType, float skillCd, string skillName,
            HeroPassiveType passiveType, string passiveName)
        {
            var h = ScriptableObject.CreateInstance<HeroData>();
            h.heroName          = name;
            h.description       = desc;
            h.baseMaxHP         = hp;
            h.baseAttack        = atk;
            h.baseDefense       = def;
            h.baseMoveSpeed     = ms;
            h.baseAttackSpeed   = asp;
            h.unlockCost        = cost;
            h.tintColor         = tint;
            h.unlockedByDefault = cost == 0;
            h.heroSkillType     = skillType;
            h.heroSkillCooldown = skillCd;
            h.heroSkillName     = skillName;
            h.heroPassiveType   = passiveType;
            h.heroPassiveName   = passiveName;
            return h;
        }

        private void EnsureStarterUnlocked()
        {
            if (_heroes.Length == 0) return;
            var starter = _heroes[0].heroName;
            if (!_persistent.IsHeroUnlocked(starter))
            {
                _persistent.UnlockedHeroIds.Add(starter);
                _persistent.Save();
            }
        }

        // --------------------------------------------------------------------
        //  State transitions
        // --------------------------------------------------------------------

        private void EnterMenu()
        {
            CleanupRun();
            _state = State.Menu;
        }

        private void StartRun()
        {
            if (_selectedHeroIndex < 0 || _selectedHeroIndex >= _heroes.Length) return;
            var hero = _heroes[_selectedHeroIndex];
            if (!_persistent.IsHeroUnlocked(hero.heroName)) return;

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

        private void TriggerVictory()
        {
            _state = State.Victory;
            _persistent.AddCurrency(clearReward);
            _persistent.Save();
        }

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

        private void TriggerDeath()
        {
            _state = State.Death;
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

            // 战斗房间数随层数递增：Floor1=4, Floor2=5, Floor3=6
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

            // 商店随机插入（不放在第一个和最后一个位置）
            int shopPos = rooms.Count > 1 ? Random.Range(1, rooms.Count) : 0;
            rooms.Insert(shopPos, "Shop");

            rooms.Add("Boss");
            return rooms;
        }

        private void LoadRoom(int index)
        {
            if (_currentRoomRoot != null) Destroy(_currentRoomRoot);
            _currentRoomIndex = index;
            // 玩家重置到左侧起点
            if (_player != null)
                _player.transform.position = _mapInfo.PlayerSpawn;

            if (index >= _floorRooms.Count)
            {
                TriggerVictory();
                return;
            }

            if (index > 0) OnNewRoomEntered(); // 限时天赋倒计时（第0房不计入）

            var type = _floorRooms[index];
            _currentRoomRoot = new GameObject($"Room_{index}_{type}");
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
        }

        // --------------------------------------------------------------------
        //  Room builders
        // --------------------------------------------------------------------

        private GameObject SpawnRandomNormalEnemy(Vector3 pos, System.Action onDied)
        {
            if (_player == null) return null;
            // 按楼层主题加权随机选择敌人类型
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

        // 通用：挂载视觉回调 + 特殊死亡效果 + 金币/死亡事件
        private void RegisterEnemy(GameObject enemy, int baseCoins, System.Action onDied)
        {
            // 设置物理层，让敌人被实心墙阻挡
            enemy.layer = 8;
            var col = enemy.GetComponent<CircleCollider2D>();
            if (col != null) col.isTrigger = false;
            ScaleEnemyStats(enemy, FloorScale);
            AttachVisualCallbacks(enemy);
            AttachSpecialDeathEffect(enemy);
            int c  = Mathf.RoundToInt(baseCoins * FloorScale);
            var hp = enemy.GetComponent<Health>();
            hp.OnDamaged += dmg => { _totalDamageDealt += dmg.Amount; };
            hp.OnDied += () => { RunCoins += c; PlayerPassiveEvents.RaisePlayerKilledEnemy(); _enemiesKilled++; };
            hp.OnDied += () => Destroy(enemy);
            hp.OnDied += onDied;
        }

        // 受击：白色闪光 + 浮动伤害数字
        private void AttachVisualCallbacks(GameObject enemy)
        {
            var sr = enemy.GetComponent<SpriteRenderer>();
            var hp = enemy.GetComponent<Health>();
            var tr = enemy.transform;
            if (sr == null) return;
            hp.OnDamaged += dmg =>
            {
                StartCoroutine(FlashRoutine(sr, new Color(1f, 0.35f, 0.35f), 0.14f));
                if (tr != null) DamageNumbers.Instance?.Show(tr.position, dmg.Amount, dmg.IsCrit);
            };
        }

        // 特殊死亡效果（在 Destroy 之前触发，可安全读取 transform）
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
                if (col.GetComponent<EnemyTag>() != null) continue; // 不伤害其他敌人
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = 40f, Type = DamageType.True, Source = null
                });
            }
            DamageNumbers.Instance?.Show(pos, 40f, false);
        }

        private void BuildMonsterRoom()
        {
            ShowBanner("消灭所有敌人 → 三选一武器奖励");
            MaybeAddAltar();
            SpawnFloorHazards();
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, () =>
            {
                ShowBanner("战斗胜利！选择一把武器！");
                DropWeaponChoices();
                OpenRightDoor();
            });
        }

        // 生成3把武器供玩家选一把，选后全部消失
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

            // 选中任何一把时销毁全部选项
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
                    ShowBanner($"已选择: {chosen.ShortName}");
                };
            }
        }


        private void BuildTalentRoom()
        {
            ShowBanner("消灭所有敌人 → 选择一个天赋");
            MaybeAddAltar();
            SpawnFloorHazards();
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, DropTalentChoices);
        }

        // (name, desc, stat, op, value, color, roomDuration)  -1 = permanent
        private static readonly (string name, string desc, StatType stat, ModifierOp op, float value, Color color, int rooms)[] TalentPool =
        {
            // ── 永久天赋 ────────────────────────────────────────────
            ("强力",   "+20% 攻击力",         StatType.Attack,            ModifierOp.PercentMul, 0.20f, new Color(1f,   0.40f, 0.40f), -1),
            ("疾风",   "+25% 移动速度",       StatType.MoveSpeed,         ModifierOp.PercentMul, 0.25f, new Color(0.4f, 0.90f, 1f  ), -1),
            ("活力",   "+50 最大生命",        StatType.MaxHP,             ModifierOp.Flat,       50f,   new Color(1f,   0.90f, 0.30f), -1),
            ("守护",   "+5 防御",             StatType.Defense,           ModifierOp.Flat,        5f,   new Color(0.4f, 0.70f, 1f  ), -1),
            ("鹰眼",   "+15% 暴击率",         StatType.CritRate,          ModifierOp.Flat,       0.15f, new Color(1f,   0.85f, 0.20f), -1),
            ("致命",   "+25% 暴击伤害",       StatType.CritDamage,        ModifierOp.Flat,       0.25f, new Color(1f,   0.50f, 0.10f), -1),
            ("狂战",   "+15% 攻击速度",       StatType.AttackSpeed,       ModifierOp.PercentMul, 0.15f, new Color(1f,   0.30f, 0.60f), -1),
            ("奥能",   "+30% 技能强度",       StatType.SkillPower,        ModifierOp.PercentMul, 0.30f, new Color(0.7f, 0.40f, 1f  ), -1),
            ("敏捷",   "+10% 冷却缩减",       StatType.CooldownReduction, ModifierOp.Flat,       0.10f, new Color(0.5f, 1f,   0.90f), -1),
            ("财富",   "+30% 金币获取",       StatType.CoinGain,          ModifierOp.PercentMul, 0.30f, new Color(1f,   0.85f, 0.10f), -1),
            ("铁壁",   "+10 防御",            StatType.Defense,           ModifierOp.Flat,       10f,   new Color(0.3f, 0.60f, 1f  ), -1),
            ("泰坦",   "+100 最大生命",       StatType.MaxHP,             ModifierOp.Flat,      100f,   new Color(0.9f, 0.40f, 0.40f), -1),
            ("冲劲",   "+20% 移动速度",       StatType.MoveSpeed,         ModifierOp.PercentMul, 0.20f, new Color(0.3f, 0.95f, 0.50f), -1),
            ("猛力",   "+30% 攻击力",         StatType.Attack,            ModifierOp.PercentMul, 0.30f, new Color(1f,   0.20f, 0.20f), -1),
            // ── 永久天赋（扩充）────────────────────────────────────────
            ("血性",   "+80 最大生命",        StatType.MaxHP,             ModifierOp.Flat,       80f,   new Color(0.9f, 0.30f, 0.30f), -1),
            ("专注",   "+12% 冷却缩减",       StatType.CooldownReduction, ModifierOp.Flat,       0.12f, new Color(0.4f, 1f,   0.85f), -1),
            ("灵动",   "+20% 攻击速度",       StatType.AttackSpeed,       ModifierOp.PercentMul, 0.20f, new Color(0.8f, 0.55f, 1f  ), -1),
            ("战意",   "+35% 暴击伤害",       StatType.CritDamage,        ModifierOp.Flat,       0.35f, new Color(1f,   0.40f, 0.05f), -1),
            ("铁身",   "+8 防御",             StatType.Defense,           ModifierOp.Flat,        8f,   new Color(0.5f, 0.65f, 1f  ), -1),
            ("贪财",   "+40% 金币获取",       StatType.CoinGain,          ModifierOp.PercentMul, 0.40f, new Color(1f,   0.90f, 0.05f), -1),
            ("不屈",   "+150 最大生命",       StatType.MaxHP,             ModifierOp.Flat,      150f,   new Color(0.9f, 0.20f, 0.20f), -1),
            ("万法",   "+50% 技能强度",       StatType.SkillPower,        ModifierOp.PercentMul, 0.50f, new Color(0.65f,0.30f, 1f  ), -1),
            ("破天",   "+25% 攻击力",         StatType.Attack,            ModifierOp.PercentMul, 0.25f, new Color(1f,   0.35f, 0.35f), -1),
            ("神速",   "+35% 移动速度",       StatType.MoveSpeed,         ModifierOp.PercentMul, 0.35f, new Color(0.3f, 0.95f, 1f  ), -1),
            ("锋芒",   "+20% 暴击率",         StatType.CritRate,          ModifierOp.Flat,       0.20f, new Color(1f,   0.80f, 0.10f), -1),
            ("神盾",   "+15 防御",            StatType.Defense,           ModifierOp.Flat,       15f,   new Color(0.25f,0.55f, 1f  ), -1),
            // ── 限时天赋（持续若干个房间后消失）──────────────────────
            ("爆发",   "+80% 攻击力 (3房)",   StatType.Attack,            ModifierOp.PercentMul, 0.80f, new Color(1f,   0.05f, 0.05f),  3),
            ("急速",   "+50% 攻速 (3房)",     StatType.AttackSpeed,       ModifierOp.PercentMul, 0.50f, new Color(0.9f, 0.55f, 1f  ),  3),
            ("护甲",   "+25 防御 (4房)",      StatType.Defense,           ModifierOp.Flat,       25f,   new Color(0.5f, 0.80f, 1f  ),  4),
            ("暴走",   "+35% 暴击率 (3房)",   StatType.CritRate,          ModifierOp.Flat,       0.35f, new Color(1f,   0.95f, 0.05f),  3),
            ("血怒",   "+120% 攻击力 (2房)",  StatType.Attack,            ModifierOp.PercentMul, 1.20f, new Color(1f,   0.02f, 0.02f),  2),
            ("极速",   "+80% 攻速 (2房)",     StatType.AttackSpeed,       ModifierOp.PercentMul, 0.80f, new Color(0.7f, 0.30f, 1f  ),  2),
            ("魔爆",   "+60% 技能强度 (3房)", StatType.SkillPower,        ModifierOp.PercentMul, 0.60f, new Color(0.55f,0.15f, 1f  ),  3),
            ("钢壁",   "+50 防御 (2房)",      StatType.Defense,           ModifierOp.Flat,       50f,   new Color(0.4f, 0.75f, 1f  ),  2),
            // 特殊效果天赋（stat modifier value=0，效果由OnRoomEntered实现）
            ("生机",   "每进入新房间回复 5% 最大生命值", StatType.MaxHP, ModifierOp.Flat, 0f,   new Color(0.3f, 1f,   0.55f), -1),
            ("生息",   "每进入新房间回复 10 点生命值",   StatType.MaxHP, ModifierOp.Flat, 0f,   new Color(0.4f, 1f,   0.70f), -1),
            ("商道",   "每进入新房间获得 8 金币",        StatType.CoinGain, ModifierOp.Flat, 0f, new Color(1f,   0.88f, 0.20f), -1),
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
            ShowBanner($"消灭所有敌人 → 获得 {reward} 金币");
            MaybeAddAltar();
            SpawnFloorHazards();
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, () =>
            {
                RunCoins += reward;
                ShowBanner($"战斗胜利！获得 {reward} 金币！");
                OpenRightDoor();
            });
        }

        // 按稀有度分组的武器工厂（用于商店品质分层）
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
            ShowBanner("商店 — 靠近后按 E 购买 (可略过)");
            OpenRightDoor();

            int floorIdx   = Mathf.Clamp(CurrentFloor - 1, 0, ShopRarityTable.Length - 1);
            int[] rarities = ShopRarityTable[floorIdx];
            float priceScale = 1f + (CurrentFloor - 1) * 0.3f;

            // 6 把武器一排
            for (int i = 0; i < rarities.Length; i++)
            {
                float x   = -5.5f + i * 2.2f;
                int   ri  = rarities[i];
                int   price = Mathf.RoundToInt(WeaponBasePrice[ri] * priceScale);
                SpawnShopWeaponPedestal(new Vector3(x, 1.5f, 0f), GetWeaponOfRarity(ri), price);
            }

            // 锻造台、天赋台、附魔台 排在下方
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
                        string verb = isForge ? "锻造" : "附魔";
                        ShowBanner($"{verb}成功！{w.ShortName}");
                        return true;
                    }
                }
                ShowBanner(isForge ? "所有武器已达最高锻造等级！" : "没有可附魔的武器（需蓝/紫品质）！");
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
                ShowBanner($"喝下血药！回复 {Mathf.RoundToInt(heal)} 点生命值！");
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
                ShowBanner($"已购买并装备: {w.ShortName}  (-{price}金币)");
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
                ShowBanner($"抽取到天赋：{t.talentName}！");
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

        // 第一层：地狱巨人 — 岩浆 + 重踏
        private void BuildFloor1Boss()
        {
            ShowBanner("BOSS — 地狱巨人降临！");
            var boss   = EnemyFactory.SpawnHellGiant(new Vector3(0f, 2.5f, 0f),
                             _player.transform, _currentRoomRoot.transform, null);
            var bossAI = boss.GetComponent<HellGiantAI>();
            var roomTr = _currentRoomRoot.transform;
            bossAI.SpawnLavaCallback = (pos, dps, lt, r) =>
                EnemyFactory.SpawnLavaPool(pos, dps, lt, r, roomTr, boss);
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // 第二层：霜魂巫妖 — 冰霜新星 + 冰锥齐射
        private void BuildFloor2Boss()
        {
            ShowBanner("BOSS — 霜魂巫妖出现！当心冰霜！");
            var boss = EnemyFactory.SpawnFrostLich(new Vector3(0f, 2.5f, 0f),
                           _player.transform, _currentRoomRoot.transform);
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // 第三层：混沌领主 — 混沌爆发 + 召唤军团
        private void BuildFloor3Boss()
        {
            ShowBanner("BOSS — 混沌领主现身！终局之战！");
            var boss   = EnemyFactory.SpawnChaosLord(new Vector3(0f, 2.5f, 0f),
                             _player.transform, _currentRoomRoot.transform);
            var bossAI = boss.GetComponent<ChaosLordAI>();
            bossAI.SpawnMinionCallback = pos => SpawnRandomNormalEnemy(pos, () => { });
            ScaleEnemyStats(boss, FloorScale);
            RegisterBossEvents(boss);
        }

        // Boss通用事件：受击闪烁、浮动伤害、死亡结算
        private void RegisterBossEvents(GameObject boss)
        {
            var bossSr = boss.GetComponent<SpriteRenderer>();
            var bossHp = boss.GetComponent<Health>();
            var bossTr = boss.transform;
            _bossHealth = bossHp;
            _bossName   = boss.name;
            if (bossSr != null)
                bossHp.OnDamaged += dmg =>
                {
                    _totalDamageDealt += dmg.Amount;
                    StartCoroutine(FlashRoutine(bossSr, new Color(1f, 0.35f, 0.35f), 0.14f));
                    if (bossTr != null) DamageNumbers.Instance?.Show(bossTr.position, dmg.Amount, dmg.IsCrit);
                };
            bossHp.OnDied += () =>
            {
                _enemiesKilled++;
                _bossHealth = null;
                _bossName   = null;
                PlayerPassiveEvents.RaisePlayerKilledEnemy();
                Destroy(boss);
                if (CurrentFloor >= maxFloor) TriggerVictory();
                else                          TriggerFloorComplete();
            };
        }

        // ── 场景专属特殊房间 ──────────────────────────────────────────────

        // 熔炉试炼（第1层专属）：预置岩浆池 + 多敌人 → 武器奖励
        private void BuildHellTrialRoom()
        {
            ShowBanner("【熔炉试炼】岩浆蔓延！消灭所有敌人 → 精选武器奖励");
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
                ShowBanner("熔炉试炼完成！选择一把精选武器！");
                DropWeaponChoices();
                OpenRightDoor();
            });
        }

        // 霜墓密室（第2层专属）：玩家移速 -25% → 清怪解除冰封 → 天赋+金币奖励
        private void BuildFrostGraveRoom()
        {
            ShowBanner("【霜墓密室】冰封之地！移速-25%，消灭亡灵 → 天赋+金币奖励");
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
                ShowBanner($"冰封解除！获得 {bonus} 金币，选择一个天赋继续前进！");
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

        // 混沌裂隙（第3层专属）：双倍敌人 → 2.5秒后第二波 → 武器+大量金币
        private void BuildChaosRiftRoom()
        {
            ShowBanner("【混沌裂隙】虚空撕裂！敌人倍增，消灭后引发余波！奖励丰厚！");
            MaybeAddAltar();
            int count = GetRoomEnemyCount() * 2;
            SpawnRoomWave(count, () =>
            {
                ShowBanner("第一波清除！混沌余波即将袭来……");
                StartCoroutine(ChaosRiftSecondWave());
            }, multiWave: false);
        }

        private System.Collections.IEnumerator ChaosRiftSecondWave()
        {
            yield return new WaitForSeconds(2.5f);
            if (_state != State.Playing) yield break;
            ShowBanner("【混沌余波】第二波入侵！");
            int count = GetRoomEnemyCount();
            SpawnRoomWave(count, () =>
            {
                int goldBonus = 50 + CurrentFloor * 15;
                RunCoins     += goldBonus;
                ShowBanner($"混沌平息！获得 {goldBonus} 金币 + 武器选择！");
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
                ShowBanner("天赋已满（上限2个）！请在左下角选择替换");
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
            ShowBanner($"获得天赋：{talent.talentName}{dur}");
        }

        private System.Action GetTalentRoomEffect(string talentName)
        {
            switch (talentName)
            {
                case "生机": return () => _playerHealth?.Heal((_playerHealth?.Max ?? 0f) * 0.05f);
                case "生息": return () => _playerHealth?.Heal(10f);
                case "商道": return () => RunCoins += 8;
                default:    return null;
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
            if (_currentRoomIndex == 0) return 2;  // 第一间始终只有2只，让玩家熟悉操作
            if (_currentRoomIndex == 1) return 3;  // 第二间固定3只
            int base_ = 3 + CurrentFloor; // Floor1=4, Floor2=5, Floor3=6
            return Random.Range(base_, base_ + 2);
        }

        // ── 场景分层辅助方法 ──────────────────────────────────────────────

        // 各层楼敌人出现权重（索引0-7 → 骷髅/小兵/弓箭手/蝙蝠/盾士/毒蜘蛛/暗影刺客/爆炎恶魔）
        private float[] GetFloorEnemyWeights()
        {
            // 前两间只刷基础小怪，不出现危险特殊敌人
            if (_currentRoomIndex <= 1)
                return new float[] { 4f, 4f, 2f, 3f, 0f, 0f, 0f, 0f };

            // 第3间起逐步引入层主题敌人
            bool earlyRoom = _currentRoomIndex <= 2;
            switch (CurrentFloor)
            {
                case 1:
                    return earlyRoom
                        ? new float[] { 2f, 3f, 2f, 2f, 1f, 1f, 0f, 1f }   // 盾士/爆炎恶魔权重压低
                        : new float[] { 1f, 2f, 1f, 1f, 3f, 1f, 1f, 4f };  // 炼狱完全体
                case 2:
                    return earlyRoom
                        ? new float[] { 4f, 1f, 3f, 3f, 0f, 0f, 0f, 0f }
                        : new float[] { 4f, 1f, 3f, 3f, 1f, 1f, 1f, 1f };  // 霜境完全体
                default:
                    return earlyRoom
                        ? new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 2f, 1f }
                        : new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 4f, 2f };  // 混沌完全体
            }
        }

        // 各层楼精英怪出现概率（前两间永远不出精英）
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

        // 各层楼战斗房间池（包含主题特殊房间）
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

        // 各层楼墙壁颜色
        private Color GetFloorWallColor()
        {
            switch (CurrentFloor)
            {
                case 1:  return new Color(0.42f, 0.16f, 0.08f); // 炼狱：暗红岩壁
                case 2:  return new Color(0.10f, 0.20f, 0.40f); // 霜境：冰蓝石壁
                default: return new Color(0.25f, 0.08f, 0.38f); // 混沌：腐败紫壁
            }
        }

        // 设置相机背景色以匹配楼层主题
        private void SetFloorBackground()
        {
            if (Camera.main == null) return;
            switch (CurrentFloor)
            {
                case 1:  Camera.main.backgroundColor = new Color(0.15f, 0.05f, 0.03f); break; // 炼狱
                case 2:  Camera.main.backgroundColor = new Color(0.03f, 0.06f, 0.14f); break; // 霜境
                default: Camera.main.backgroundColor = new Color(0.07f, 0.03f, 0.11f); break; // 混沌
            }
        }

        // 更新竞技场墙壁颜色并重建背景（楼层切换时调用）
        private void UpdateArenaColors()
        {
            // 新楼层：销毁旧地图，随机选一张新地图重建
            if (_arenaRoot != null) { Destroy(_arenaRoot); _arenaRoot = null; }
            _arenaRoot  = new GameObject("Arena");
            _mapVariant = Random.Range(0, 3);
            _mapInfo    = MapBuilder.Build(CurrentFloor, _mapVariant, _arenaRoot.transform);
            ArenaHalfW  = _mapInfo.HalfW;
            ArenaHalfH  = _mapInfo.HalfH;
        }

        private string GetFloorName()
        {
            switch (CurrentFloor)
            {
                case 1:  return "灼热炼狱";
                case 2:  return "霜境幽域";
                default: return "混沌深渊";
            }
        }

        private string GetFloorNarrative()
        {
            switch (CurrentFloor)
            {
                case 1:  return "【灼热炼狱】岩浆涌动，炎魔与铁甲把守通道，小心爆炸与熔岩！";
                case 2:  return "【霜境幽域】严寒入骨，骸骨复活，精英概率大幅提升！";
                default: return "【混沌深渊】虚空破碎，精英肆虐——终局之战，混沌领主在等着你！";
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
                sr.sprite       = MakeUnitSquareSprite();
                sr.color        = new Color(0.45f, 0.08f, 0.04f, 0.80f);
                sr.sortingOrder = 3;

                go.AddComponent<FlamePillar>();
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
            }
        }

        // 分两波刷怪（55%/45%）；每波从房间四壁边缘生成，第一波不出精英
        private void SpawnRoomWave(int totalCount, System.Action onAllDead, bool multiWave = true)
        {
            if (_player == null) return;

            if (!multiWave || totalCount <= 2)
            {
                // 数量 ≤2 或明确禁用时直接单波
                SpawnWaveGroup(totalCount, false, onAllDead);
                return;
            }

            int w1 = Mathf.CeilToInt(totalCount * 0.55f);
            int w2 = totalCount - w1;

            SpawnWaveGroup(w1, false, () =>
            {
                if (w2 <= 0) { onAllDead(); return; }
                ShowBanner("第二波来袭！");
                SpawnWaveGroup(w2, true, onAllDead);
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
            stats.SetBase(StatType.MoveSpeed,   hero.baseMoveSpeed);
            stats.SetBase(StatType.AttackSpeed, hero.baseAttackSpeed);

            _playerHealth = _player.AddComponent<Health>();
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
            _player.AddComponent<PlayerController>();
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

        private void BuildArena()
        {
            _arenaRoot  = new GameObject("Arena");
            _mapVariant = Random.Range(0, 3);
            _mapInfo    = MapBuilder.Build(CurrentFloor, _mapVariant, _arenaRoot.transform);
            ArenaHalfW  = _mapInfo.HalfW;
            ArenaHalfH  = _mapInfo.HalfH;
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
                case State.Menu:          DrawMenu();                                                                   break;
                case State.Playing:       DrawHUD();                                                                   break;
                case State.FloorComplete: DrawFloorComplete();                                                         break;
                case State.Victory:       DrawEndScreen("胜利!", new Color(1f, 0.92f, 0.2f), true);                  break;
                case State.Death:         DrawEndScreen("阵亡", new Color(1f, 0.3f, 0.3f), false);                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  主菜单 — 英雄选择
        // ─────────────────────────────────────────────────────────────
        private void DrawMenu()
        {
            // 渐变背景
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.07f, 0.07f, 0.11f));
            FillRect(new Rect(0, 0, Screen.width, 4), new Color(0.55f, 0.45f, 0.15f));
            FillRect(new Rect(0, Screen.height - 4, Screen.width, 4), new Color(0.55f, 0.45f, 0.15f));

            // 标题
            var titleS = MkLabel(52, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.98f, 0.9f, 0.35f));
            GUI.Label(new Rect(0, 22, Screen.width, 68), "深渊·轮回", titleS);
            var subtitleS = MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.65f, 0.65f, 0.7f));
            GUI.Label(new Rect(0, 82, Screen.width, 24), "Roguelike Dungeon  ·  选择英雄开始冒险", subtitleS);

            float currencyY = 112f;
            var currS = MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(1f, 0.88f, 0.22f));
            GUI.Label(new Rect(0, currencyY, Screen.width, 22), $"◈  解锁货币: {_persistent.UnlockCurrency}", currS);

            // 英雄卡片
            const float cardW = 210f, cardH = 280f, gap = 14f;
            float totalW = _heroes.Length * cardW + (_heroes.Length - 1) * gap;
            float startX = (Screen.width - totalW) * 0.5f;
            float cardY  = 148f;

            for (int i = 0; i < _heroes.Length; i++)
            {
                var h = _heroes[i];
                bool unlocked = _persistent.IsHeroUnlocked(h.heroName);
                bool selected = i == _selectedHeroIndex;
                var rect = new Rect(startX + i * (cardW + gap), cardY, cardW, cardH);

                // 卡片背景
                Color cardBg = !unlocked ? new Color(0.22f, 0.12f, 0.12f)
                             : selected  ? new Color(0.14f, 0.26f, 0.44f)
                                         : new Color(0.14f, 0.15f, 0.22f);
                FillRect(rect, cardBg);

                // 选中边框
                if (selected)
                {
                    FillRect(new Rect(rect.x, rect.y, rect.width, 2), new Color(0.45f, 0.75f, 1f));
                    FillRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), new Color(0.45f, 0.75f, 1f));
                    FillRect(new Rect(rect.x, rect.y, 2, rect.height), new Color(0.45f, 0.75f, 1f));
                    FillRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), new Color(0.45f, 0.75f, 1f));
                }

                // 英雄头像（72×72）
                float portraitSize = 72f;
                var portraitRect = new Rect(rect.x + (cardW - portraitSize) * 0.5f, rect.y + 12f, portraitSize, portraitSize);
                FillRect(portraitRect, new Color(0.08f, 0.08f, 0.14f));
                var portrait = HeroSprites.Get(h.heroName);
                if (portrait != null)
                    GUI.DrawTexture(portraitRect, portrait.texture);
                else
                    FillRect(portraitRect, h.tintColor * 0.5f);
                // 头像底部颜色条
                FillRect(new Rect(rect.x + (cardW - portraitSize) * 0.5f, rect.y + 12f + portraitSize - 4f, portraitSize, 4), h.tintColor * 0.8f);

                float iy = rect.y + 92f;
                // 英雄名
                GUI.Label(new Rect(rect.x + 8, iy, cardW - 16, 24),
                    h.heroName, MkLabel(18, TextAnchor.MiddleCenter, FontStyle.Bold, unlocked ? Color.white : new Color(0.6f, 0.5f, 0.5f)));
                // 简介
                var descS = MkLabel(11, TextAnchor.UpperLeft, FontStyle.Normal, new Color(0.78f, 0.78f, 0.78f));
                descS.wordWrap = true;
                GUI.Label(new Rect(rect.x + 8, iy + 26, cardW - 16, 34), h.description, descS);
                // 属性行
                GUI.Label(new Rect(rect.x + 8, iy + 62, cardW - 16, 18),
                    $"♥{h.baseMaxHP:0}  ⚔{h.baseAttack:0}  🛡{h.baseDefense:0}  ⚡{h.baseMoveSpeed:0.0}",
                    MkLabel(11, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.75f, 0.88f, 1f)));
                // 主动技能
                GUI.Label(new Rect(rect.x + 8, iy + 82, cardW - 16, 18),
                    $"[F] {h.heroSkillName}  CD{h.heroSkillCooldown:0}s",
                    MkLabel(11, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(1f, 0.85f, 0.35f)));
                // 被动
                GUI.Label(new Rect(rect.x + 8, iy + 100, cardW - 16, 18),
                    $"◆ {h.heroPassiveName}",
                    MkLabel(11, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.55f, 1f, 0.65f)));

                // 按钮
                var btnRect = new Rect(rect.x + 8, rect.y + cardH - 36, cardW - 16, 28);
                if (unlocked)
                {
                    if (GUI.Button(btnRect, selected ? "✓ 已选择" : "选择"))
                        _selectedHeroIndex = i;
                }
                else
                {
                    bool canAfford = _persistent.UnlockCurrency >= h.unlockCost;
                    GUI.enabled = canAfford;
                    if (GUI.Button(btnRect, $"解锁  {h.unlockCost} ◈"))
                        _persistent.TryUnlockHero(h.heroName, h.unlockCost);
                    GUI.enabled = true;
                }
            }

            // 开始按钮
            float startY = cardY + cardH + 18f;
            bool canStart = _selectedHeroIndex >= 0 && _persistent.IsHeroUnlocked(_heroes[_selectedHeroIndex].heroName);
            GUI.enabled = canStart;
            var startS = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            if (GUI.Button(new Rect(Screen.width * 0.5f - 150, startY, 300, 52), "▶  开始冒险", startS))
                StartRun();
            GUI.enabled = true;

            var pathS = MkLabel(11, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.4f, 0.4f, 0.45f));
            GUI.Label(new Rect(0, Screen.height - 24, Screen.width, 20), Application.persistentDataPath, pathS);
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
                EnterMenu();
        }

        // 结算/死亡画面
        private void DrawEndScreen(string title, Color color, bool victory)
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.72f));
            GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 86),
                title, MkLabel(62, TextAnchor.MiddleCenter, FontStyle.Bold, color));

            if (victory)
                GUI.Label(new Rect(0, Screen.height * 0.27f, Screen.width, 30),
                    $"全部 {maxFloor} 层通关完成！  +{clearReward} 解锁货币  (合计: {_persistent.UnlockCurrency})",
                    MkLabel(18, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white));

            float sy = Screen.height * (victory ? 0.35f : 0.28f);
            var ss = MkLabel(15, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.82f, 0.82f, 0.82f));
            GUI.Label(new Rect(0, sy,       Screen.width, 24), $"到达楼层: {CurrentFloor} / {maxFloor}", ss);
            GUI.Label(new Rect(0, sy + 28,  Screen.width, 24), $"击杀数: {_enemiesKilled}", ss);
            GUI.Label(new Rect(0, sy + 56,  Screen.width, 24), $"总伤害: {Mathf.RoundToInt(_totalDamageDealt):N0}", ss);
            GUI.Label(new Rect(0, sy + 84,  Screen.width, 24), $"剩余金币: {RunCoins}", ss);

            float btnY = Screen.height * (victory ? 0.60f : 0.56f);
            var bs = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (GUI.Button(new Rect(Screen.width * 0.5f - 140, btnY,       280, 44), "再次挑战", bs)) StartRun();
            if (GUI.Button(new Rect(Screen.width * 0.5f - 140, btnY + 56f, 280, 44), "返回主菜单", bs)) EnterMenu();
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
