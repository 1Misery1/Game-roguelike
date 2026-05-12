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
        [SerializeField] private float arenaHalfWidth  = 8f;
        [SerializeField] private float arenaHalfHeight = 4.5f;
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
        private float FloorScale => 1f + (CurrentFloor - 1) * 0.3f;

        private State _state = State.Menu;
        private PersistentState _persistent;
        private HeroData[] _heroes;
        private int _selectedHeroIndex = 0;

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
                    120f, 12f, 3f, 5f,   1.0f, 0,   new Color(0.4f, 0.85f, 1f),
                    HeroSkillType.WarCry,        10f, "战吼",
                    HeroPassiveType.BattlefieldWill,  "战场意志"),

                MakeHero("Ranger", "敏捷游侠，连击可叠加攻击加成。",
                    85f,  11f, 0f, 7f,   1.3f, 30,  new Color(0.8f, 1f,   0.5f),
                    HeroSkillType.ShadowStep,    6f,  "影步",
                    HeroPassiveType.ComboStrike,      "连击"),

                MakeHero("Mage", "玻璃炮，技能后下次普攻伤害翻倍。",
                    70f,  20f, 0f, 4.5f, 0.8f, 60,  new Color(1f,   0.6f, 1f),
                    HeroSkillType.ArcaneSurge,   8f,  "奥术迸发",
                    HeroPassiveType.ManaAmplification,"魔力增幅"),

                MakeHero("Paladin", "圣骑士，击杀敌人回复5HP。",
                    130f, 10f, 5f, 4.5f, 0.9f, 100, new Color(1f,   0.9f, 0.4f),
                    HeroSkillType.HolyLight,     12f, "神圣之光",
                    HeroPassiveType.SacredOath,       "神圣誓约"),

                MakeHero("Hunter", "猎人，永久爆击率+20%、爆伤+30%。",
                    80f,  15f, 0f, 6f,   1.1f, 150, new Color(1f,   0.55f, 0.3f),
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
            if (_player != null) _player.transform.position = new Vector3(-arenaHalfWidth + 0.8f, 0f, 0f);
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
                _player.transform.position = new Vector3(-arenaHalfWidth + 0.8f, 0f, 0f);

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
                StartCoroutine(FlashRoutine(sr, Color.white, 0.06f));
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
                    StartCoroutine(FlashRoutine(bossSr, Color.white, 0.08f));
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
                float x = Random.Range(-4f, 6f), y = Random.Range(-2.5f, 2.5f);
                var pos = new Vector3(x, y, 0f);
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
            });
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
            });
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
            int base_ = 3 + CurrentFloor; // Floor1=4, Floor2=5, Floor3=6
            return Random.Range(base_, base_ + 2);
        }

        // ── 场景分层辅助方法 ──────────────────────────────────────────────

        // 各层楼敌人出现权重（索引0-7 → 骷髅/小兵/弓箭手/蝙蝠/盾士/毒蜘蛛/暗影刺客/爆炎恶魔）
        private float[] GetFloorEnemyWeights()
        {
            switch (CurrentFloor)
            {
                case 1:  return new float[] { 1f, 2f, 1f, 1f, 3f, 1f, 1f, 4f }; // 炼狱：盾士+爆炎恶魔高发
                case 2:  return new float[] { 4f, 1f, 3f, 3f, 1f, 1f, 1f, 1f }; // 霜境：骷髅+弓手+蝙蝠高发
                default: return new float[] { 1f, 3f, 1f, 1f, 1f, 2f, 4f, 2f }; // 混沌：暗影刺客+毒蜘蛛高发
            }
        }

        // 各层楼精英怪出现概率（15% / 30% / 55%）
        private float GetFloorEliteChance()
        {
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
            if (_arenaRoot == null) return;
            var c = GetFloorWallColor();
            foreach (Transform child in _arenaRoot.transform)
            {
                if (child.name == "FloorBackground") continue; // 背景单独处理
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = c;
            }
            // 重建背景纹理（新楼层主题）
            var oldBg = _arenaRoot.transform.Find("FloorBackground");
            if (oldBg != null) Destroy(oldBg.gameObject);
            float bgW = arenaHalfWidth * 2f + 6f;
            float bgH = arenaHalfHeight * 2f + 6f;
            FloorBackground.Create(CurrentFloor, _arenaRoot.transform, bgW, bgH);
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

        // 将 count 只敌人横向铺开，混入精英；全部死亡后调用 onAllDead
        private void SpawnRoomWave(int count, System.Action onAllDead)
        {
            if (_player == null) return;
            float eliteChance = GetFloorEliteChance();

            int remaining      = count;
            System.Action dec  = () => { remaining--; if (remaining <= 0) onAllDead(); };

            bool spawnedElite  = false;
            for (int i = 0; i < count; i++)
            {
                float x   = Random.Range(-4f, 6f);
                float y   = Random.Range(-2.5f, 2.5f);
                var   pos = new Vector3(x, y, 0f);

                if (!spawnedElite && Random.value < eliteChance)
                {
                    spawnedElite = true;
                    SpawnEliteEnemy(pos, dec);
                }
                else
                {
                    SpawnRandomNormalEnemy(pos, dec);
                }
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
            doorGO.transform.position   = new Vector3(arenaHalfWidth - 0.4f, 0f, 0f);
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
            _player.transform.position   = new Vector3(-arenaHalfWidth + 0.8f, 0f, 0f);
            _player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = _player.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = hero.tintColor;
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
                StartCoroutine(FlashRoutine(sr, Color.white, 0.06f));
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
            cam.orthographic    = true;
            cam.orthographicSize = 5.5f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGO.AddComponent<AudioListener>();
        }

        private void BuildArena()
        {
            _arenaRoot = new GameObject("Arena");
            // 先生成背景（排在最底层）
            float bgW = arenaHalfWidth * 2f + 6f;
            float bgH = arenaHalfHeight * 2f + 6f;
            FloorBackground.Create(CurrentFloor, _arenaRoot.transform, bgW, bgH);
            var wallColor = GetFloorWallColor();
            MakeWall(new Vector2(0f,  arenaHalfHeight + 0.25f), new Vector2(arenaHalfWidth * 2f + 0.5f, 0.5f), wallColor);
            MakeWall(new Vector2(0f, -arenaHalfHeight - 0.25f), new Vector2(arenaHalfWidth * 2f + 0.5f, 0.5f), wallColor);
            MakeWall(new Vector2( arenaHalfWidth + 0.25f, 0f),  new Vector2(0.5f, arenaHalfHeight * 2f + 0.5f), wallColor);
            MakeWall(new Vector2(-arenaHalfWidth - 0.25f, 0f),  new Vector2(0.5f, arenaHalfHeight * 2f + 0.5f), wallColor);
        }

        private void MakeWall(Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(_arenaRoot.transform, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeUnitSquareSprite();
            sr.color        = color;
            sr.sortingOrder = 0;
            go.AddComponent<BoxCollider2D>();
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

        private void FillRect(Rect rect, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhitePixel);
            GUI.color = prev;
        }

        // --------------------------------------------------------------------
        //  GUI
        // --------------------------------------------------------------------

        private void OnGUI()
        {
            switch (_state)
            {
                case State.Menu:          DrawMenu();                                                break;
                case State.Playing:       DrawHUD();                                                break;
                case State.FloorComplete: DrawFloorComplete();                                      break;
                case State.Victory:       DrawEndScreen("VICTORY!",  new Color(1f, 0.9f, 0.2f), true);  break;
                case State.Death:         DrawEndScreen("YOU DIED",  new Color(1f, 0.3f, 0.3f), false); break;
            }
        }

        private void DrawMenu()
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.08f, 0.08f, 0.12f));

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal   = { textColor = new Color(0.95f, 0.95f, 0.4f) }
            };
            GUI.Label(new Rect(0, 30, Screen.width, 60), "2D ROGUELIKE", title);

            var info = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, 90, Screen.width, 28), $"Unlock Currency: {_persistent.UnlockCurrency}", info);

            float cardW = 195f, cardH = 210f, gap = 10f;
            float totalW = _heroes.Length * cardW + (_heroes.Length - 1) * gap;
            float startX = (Screen.width - totalW) * 0.5f;
            float y      = 135f;

            for (int i = 0; i < _heroes.Length; i++)
            {
                var h        = _heroes[i];
                bool unlocked = _persistent.IsHeroUnlocked(h.heroName);
                bool selected = i == _selectedHeroIndex;

                var rect = new Rect(startX + i * (cardW + gap), y, cardW, cardH);
                Color bg = !unlocked ? new Color(0.28f, 0.15f, 0.15f)
                         : selected  ? new Color(0.2f, 0.45f, 0.75f)
                                     : new Color(0.18f, 0.22f, 0.3f);
                FillRect(rect, bg);
                FillRect(new Rect(rect.x + 8, rect.y + 8, 34, 34), h.tintColor);

                var nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                GUI.Label(new Rect(rect.x + 50, rect.y + 8, cardW - 58, 26), h.heroName, nameStyle);

                var descStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
                GUI.Label(new Rect(rect.x + 8, rect.y + 48, cardW - 16, 36), h.description, descStyle);

                var statStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.8f, 0.9f, 1f) } };
                GUI.Label(new Rect(rect.x + 8, rect.y + 88, cardW - 16, 18),
                    $"HP {h.baseMaxHP:0}  ATK {h.baseAttack:0}  SPD {h.baseMoveSpeed:0.0}", statStyle);

                var skillStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(1f, 0.85f, 0.4f) } };
                GUI.Label(new Rect(rect.x + 8, rect.y + 108, cardW - 16, 18),
                    $"[F] {h.heroSkillName}  CD:{h.heroSkillCooldown:0}s", skillStyle);

                var passiveStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.6f, 1f, 0.7f) } };
                GUI.Label(new Rect(rect.x + 8, rect.y + 126, cardW - 16, 18),
                    $"[被动] {h.heroPassiveName}", passiveStyle);

                var btnRect = new Rect(rect.x + 8, rect.y + cardH - 36, cardW - 16, 28);
                if (unlocked)
                {
                    if (GUI.Button(btnRect, selected ? "✓ 已选择" : "选择"))
                        _selectedHeroIndex = i;
                }
                else
                {
                    bool affordable = _persistent.UnlockCurrency >= h.unlockCost;
                    GUI.enabled = affordable;
                    if (GUI.Button(btnRect, $"解锁 ({h.unlockCost})"))
                        _persistent.TryUnlockHero(h.heroName, h.unlockCost);
                    GUI.enabled = true;
                }
            }

            var startBtn = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            bool canStart = _selectedHeroIndex >= 0 && _persistent.IsHeroUnlocked(_heroes[_selectedHeroIndex].heroName);
            GUI.enabled = canStart;
            if (GUI.Button(new Rect(Screen.width / 2f - 140, y + cardH + 20, 280, 50), "开始冒险", startBtn))
                StartRun();
            GUI.enabled = true;

            var hint = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.6f, 0.6f, 0.7f) } };
            GUI.Label(new Rect(0, Screen.height - 28, Screen.width, 20),
                "Save file: " + Application.persistentDataPath, hint);
        }

        private void DrawHUD()
        {
            var label = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            string roomType = _currentRoomIndex < _floorRooms.Count ? _floorRooms[_currentRoomIndex] : "—";
            string roomName = GetRoomDisplayName(roomType);
            GUI.Label(new Rect(10, 10, 700, 26),
                $"[{GetFloorName()}] {CurrentFloor}/{maxFloor}层 · {_currentRoomIndex + 1}/{_floorRooms.Count}间 · {roomName} · 难度×{FloorScale:0.0}", label);
            GUI.Label(new Rect(10, 34, 800, 26),
                "WASD移动 · Space/左键普攻 · R/右键武器技能 · F英雄技能 · Q切换武器 · E购买/装备 · 绿门进入下一间", label);
            if (_playerHealth != null)
            {
                GUI.Label(new Rect(10, 58, 560, 26),
                    $"HP: {Mathf.CeilToInt(_playerHealth.Current)} / {Mathf.CeilToInt(_playerHealth.Max)}   金币: {RunCoins}",
                    label);
            }
            DrawHeroSkillHUD(label);

            DrawWeaponHUD();

            DrawBossHPBar();

            DrawTalentStatus();

            if (_pendingTalent != null) DrawTalentReplacementOverlay();

            if (Time.time < _bannerUntil && !string.IsNullOrEmpty(_bannerMessage))
            {
                var bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                    normal   = { textColor = new Color(1f, 0.9f, 0.4f) }
                };
                GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 40), _bannerMessage, bannerStyle);
            }
        }

        private static string WeaponSpecialLabel(WeaponInstance wi)
        {
            if (wi.Data.lifeStealRate > 0f)   return $"  吸血{wi.Data.lifeStealRate * 100:0}%";
            if (wi.Data.hpCostPerAttack > 0f) return $"  耗血{wi.Data.hpCostPerAttack:0}/次";
            return "";
        }

        private void DrawWeaponHUD()
        {
            if (_player == null) return;
            var handler = _player.GetComponent<PlayerWeaponHandler>();
            if (handler == null) return;

            float panelX = Screen.width - 420f;
            float panelY = 10f;
            float panelW = 410f;
            float panelH = handler.ActiveWeapon?.Data?.HasSkill == true ? 132f : 92f;

            FillRect(new Rect(panelX - 6, panelY - 4, panelW + 12, panelH + 8), new Color(0f, 0f, 0f, 0.55f));

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                normal   = { textColor = new Color(0.9f, 0.9f, 0.6f) }
            };
            GUI.Label(new Rect(panelX, panelY, panelW, 20), "── 武器栏 (Q切换) ──", titleStyle);

            for (int i = 0; i < 2; i++)
            {
                var wi      = handler.Slots[i];
                bool active = handler.ActiveSlotIndex == i;
                float baseY = panelY + 20f + i * 36f;

                Color slotColor = wi == null ? new Color(0.5f, 0.5f, 0.5f)
                                : WeaponData.GetRarityColor(wi.Data.rarity);
                if (!active) slotColor *= 0.65f;

                string prefix = active ? "▶" : "  ";

                if (wi == null)
                {
                    var emptyStyle = new GUIStyle(GUI.skin.label)
                        { fontSize = 12, normal = { textColor = slotColor } };
                    GUI.Label(new Rect(panelX, baseY, panelW, 20), $"{prefix} [{i + 1}] 空", emptyStyle);
                }
                else
                {
                    // 第一行：名称 + 类型 + 伤害 + 攻速
                    var nameStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize  = active ? 13 : 12,
                        fontStyle = active ? FontStyle.Bold : FontStyle.Normal,
                        normal    = { textColor = slotColor }
                    };
                    GUI.Label(new Rect(panelX, baseY, panelW, 20),
                        $"{prefix} [{i + 1}] {wi.ShortName}  {wi.CategoryLabel}  {wi.EffectiveDamage:0}伤  {wi.Data.attackSpeed:0.0}/s",
                        nameStyle);

                    // 第二行：HP加成 + 特殊词条 + 升级/附魔进度
                    string upgradeStr = $"升{wi.UpgradeLevel}/{wi.Data.maxUpgradeLevel}";
                    if (wi.Data.CanEnchant) upgradeStr += $" 附{wi.EnchantLevel}/{wi.Data.maxEnchantLevel}";
                    string subLine = $"   HP+{wi.HPBonus:0}{WeaponSpecialLabel(wi)}  [{upgradeStr}]";
                    var subStyle = new GUIStyle(GUI.skin.label)
                        { fontSize = 11, normal = { textColor = slotColor * 0.85f } };
                    GUI.Label(new Rect(panelX, baseY + 18f, panelW, 16), subLine, subStyle);
                }
            }

            var active_wi = handler.ActiveWeapon;
            if (active_wi?.Data?.HasSkill == true)
            {
                float  skillY    = panelY + 92f;
                string skillName = active_wi.Data.skill.skillName;
                float  cdRem     = handler.SkillCooldownRemaining;
                bool   ready     = handler.SkillReady;

                string skillLabel = ready
                    ? $"技能: {skillName} [就绪!] (R/右键)"
                    : $"技能: {skillName} CD: {cdRem:0.0}s";

                Color skillColor = ready ? new Color(0.5f, 1f, 0.5f) : new Color(0.7f, 0.7f, 1f);
                var skillStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12, normal = { textColor = skillColor }
                };
                GUI.Label(new Rect(panelX, skillY, panelW, 20), skillLabel, skillStyle);

                float barW = panelW - 4f;
                float barY = skillY + 20f;
                FillRect(new Rect(panelX, barY, barW, 8), new Color(0.3f, 0.3f, 0.3f));
                float fill = 1f - handler.SkillCooldownRatio;
                FillRect(new Rect(panelX, barY, barW * fill, 8), ready ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.3f, 0.5f, 1f));
            }
        }

        private void DrawHeroSkillHUD(GUIStyle baseLabel)
        {
            if (_player == null) return;
            var skillHandler = _player.GetComponent<HeroActiveSkillHandler>();
            if (skillHandler == null || skillHandler.SkillType == HeroSkillType.None) return;

            float panelX = 10f;
            float panelY = 82f;
            float panelW = 220f;
            float panelH = 38f;

            FillRect(new Rect(panelX - 4, panelY - 2, panelW + 8, panelH + 4), new Color(0f, 0f, 0f, 0.5f));

            bool   ready      = skillHandler.IsReady;
            float  cdRem      = skillHandler.CooldownRemaining;
            string skillLabel = ready
                ? $"[F] {skillHandler.SkillName}  就绪!"
                : $"[F] {skillHandler.SkillName}  CD: {cdRem:0.0}s";

            Color skillColor = ready ? new Color(1f, 0.85f, 0.1f) : new Color(0.7f, 0.65f, 0.4f);
            var style = new GUIStyle(baseLabel) { fontSize = 13, normal = { textColor = skillColor } };
            GUI.Label(new Rect(panelX, panelY, panelW, 20), skillLabel, style);

            float barW = panelW;
            FillRect(new Rect(panelX, panelY + 20f, barW, 8), new Color(0.3f, 0.3f, 0.3f));
            float fill = 1f - skillHandler.CooldownRatio;
            FillRect(new Rect(panelX, panelY + 20f, barW * fill, 8),
                ready ? new Color(1f, 0.8f, 0.1f) : new Color(0.5f, 0.45f, 0.2f));
        }

        private void DrawTalentStatus()
        {
            if (_activeTalents.Count == 0) return;
            float panelX = 10f;
            float panelY = 126f;
            float panelW = 240f;
            const float rowH = 34f;
            float panelH = _activeTalents.Count * rowH + 20f;
            FillRect(new Rect(panelX - 4, panelY - 4, panelW + 8, panelH + 4), new Color(0f, 0f, 0f, 0.5f));

            var header = new GUIStyle(GUI.skin.label)
                { fontSize = 11, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };
            GUI.Label(new Rect(panelX, panelY, panelW, 16), "── 天赋 ──", header);

            for (int i = 0; i < _activeTalents.Count; i++)
            {
                var at    = _activeTalents[i];
                float rowY = panelY + 18f + i * rowH;
                string dur = at.IsPermanent ? "∞" : $"{at.RoomsLeft}房";
                Color  c   = at.IsPermanent ? new Color(0.9f, 0.9f, 0.6f) : new Color(1f, 0.7f, 0.3f);

                var nameStyle = new GUIStyle(GUI.skin.label)
                    { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = c } };
                GUI.Label(new Rect(panelX, rowY, panelW, 18),
                    $"[{i + 1}] {at.Data.talentName}  ({dur})", nameStyle);

                var descStyle = new GUIStyle(GUI.skin.label)
                    { fontSize = 11, normal = { textColor = new Color(0.75f, 0.75f, 0.75f) } };
                GUI.Label(new Rect(panelX + 8f, rowY + 16f, panelW - 8f, 16),
                    at.Data.description, descStyle);
            }
        }

        private void DrawTalentReplacementOverlay()
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.72f));

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = new Color(1f, 0.85f, 0.2f) }
            };
            GUI.Label(new Rect(0, cy - 100f, Screen.width, 36),
                $"天赋已满！新天赋：{_pendingTalent.talentName}", titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, cy - 60f, Screen.width, 26), "选择替换哪一个（或取消放弃新天赋）", subStyle);

            float btnW = 280f;
            float btnH = 46f;
            var   btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };

            for (int i = 0; i < _activeTalents.Count; i++)
            {
                var at  = _activeTalents[i];
                string dur = at.IsPermanent ? "永久" : $"剩{at.RoomsLeft}房";
                float  y   = cy - 10f + i * (btnH + 10f);
                if (GUI.Button(new Rect(cx - btnW * 0.5f, y, btnW, btnH),
                    $"替换：{at.Data.talentName} ({dur})", btnStyle))
                {
                    ReplaceTalentAt(i);
                }
            }

            float cancelY = cy - 10f + _activeTalents.Count * (btnH + 10f) + 10f;
            if (GUI.Button(new Rect(cx - btnW * 0.5f, cancelY, btnW, 38),
                "取消（放弃新天赋）", btnStyle))
            {
                _pendingTalent = null;
            }
        }

        private void DrawBossHPBar()
        {
            if (_bossHealth == null) return;
            float ratio = Mathf.Clamp01(_bossHealth.Current / _bossHealth.Max);
            float barW  = Screen.width * 0.5f;
            float barH  = 18f;
            float barX  = (Screen.width - barW) * 0.5f;
            float barY  = Screen.height - 46f;

            FillRect(new Rect(barX - 4, barY - 24, barW + 8, barH + 30), new Color(0f, 0f, 0f, 0.65f));

            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(1f, 0.35f, 0.35f) }
            };
            GUI.Label(new Rect(barX, barY - 22f, barW, 20f), _bossName ?? "BOSS", nameStyle);

            FillRect(new Rect(barX, barY, barW, barH), new Color(0.22f, 0.08f, 0.08f));
            FillRect(new Rect(barX, barY, barW * ratio, barH), new Color(0.85f, 0.15f, 0.15f));

            var hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12, alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            GUI.Label(new Rect(barX, barY, barW, barH),
                $"{Mathf.CeilToInt(_bossHealth.Current)} / {Mathf.CeilToInt(_bossHealth.Max)}", hpStyle);
        }

        private void DrawFloorComplete()
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.65f));

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = new Color(0.35f, 1f, 0.5f) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 70), $"第 {CurrentFloor} 层「{GetFloorName()}」清除！", titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 30),
                $"已获得 +{clearReward} 解锁货币   当前金币: {RunCoins}", subStyle);

            var nextFloor = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = new Color(1f, 0.8f, 0.5f) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.39f, Screen.width, 26),
                $"下一层难度: ×{1f + CurrentFloor * 0.3f:0.0}  (敌人HP↑ ATK↑)", nextFloor);

            // 当前HP状态
            if (_playerHealth != null)
            {
                float ratio    = _playerHealth.Ratio;
                Color hpColor  = ratio > 0.5f ? new Color(0.3f, 1f, 0.4f)
                               : ratio > 0.25f ? new Color(1f, 0.85f, 0.1f)
                                               : new Color(1f, 0.35f, 0.35f);
                var hpStyle = new GUIStyle(GUI.skin.label)
                    { fontSize = 17, alignment = TextAnchor.MiddleCenter, normal = { textColor = hpColor } };
                GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 28),
                    $"生命值: {Mathf.CeilToInt(_playerHealth.Current)} / {Mathf.CeilToInt(_playerHealth.Max)}  ({ratio * 100:0}%)", hpStyle);
            }

            // 花钱回血按钮
            int   healCost  = 30 + CurrentFloor * 10;
            bool  canHeal   = RunCoins >= healCost && _playerHealth != null && _playerHealth.Ratio < 0.999f;
            float healBtnX  = Screen.width / 2f - 175f;
            float healBtnY  = Screen.height * 0.52f;
            var   healStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            GUI.enabled = canHeal;
            if (GUI.Button(new Rect(healBtnX, healBtnY, 350, 38),
                $"花费 {healCost} 金币  回复50%最大生命值", healStyle))
            {
                RunCoins -= healCost;
                _playerHealth?.Heal(_playerHealth.Max * 0.5f);
            }
            GUI.enabled = true;

            float btnX = Screen.width / 2f - 140f;
            float btnY = Screen.height * 0.60f;
            var btn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            if (GUI.Button(new Rect(btnX, btnY, 280, 50), $"进入第 {CurrentFloor + 1} 层 ▶", btn))
                AdvanceFloor();
            if (GUI.Button(new Rect(btnX, btnY + 60f, 280, 50), "返回主菜单", btn))
                EnterMenu();
        }

        private void DrawEndScreen(string title, Color color, bool victory)
        {
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));

            var bigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 56, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal   = { textColor = color }
            };
            GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 80), title, bigStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = Color.white }
            };
            if (victory)
                GUI.Label(new Rect(0, Screen.height * 0.30f, Screen.width, 30),
                    $"全部 {maxFloor} 层通关！   +{clearReward} 解锁货币   (合计: {_persistent.UnlockCurrency})", subStyle);

            float statsY = Screen.height * (victory ? 0.38f : 0.30f);
            var statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15, alignment = TextAnchor.MiddleCenter,
                normal   = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            GUI.Label(new Rect(0, statsY,       Screen.width, 24), $"通关层数: {CurrentFloor} / {maxFloor}", statStyle);
            GUI.Label(new Rect(0, statsY + 26f, Screen.width, 24), $"击杀敌人: {_enemiesKilled}", statStyle);
            GUI.Label(new Rect(0, statsY + 52f, Screen.width, 24), $"总输出伤害: {Mathf.RoundToInt(_totalDamageDealt):N0}", statStyle);
            GUI.Label(new Rect(0, statsY + 78f, Screen.width, 24), $"剩余金币: {RunCoins}", statStyle);

            float btnX = Screen.width / 2f - 140f;
            float btnY = Screen.height * (victory ? 0.62f : 0.58f);
            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (GUI.Button(new Rect(btnX, btnY,       280, 42), "再次挑战", btnStyle)) StartRun();
            if (GUI.Button(new Rect(btnX, btnY + 54f, 280, 42), "返回主菜单", btnStyle)) EnterMenu();
        }
    }
}
