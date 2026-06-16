using Game.Art;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Factories;
using Game.Player;
using Game.UI;
using UnityEngine;

namespace Game.Bootstrap
{
    /// 训练场（白盒练武场）。挂在 Training 场景里唯一的引导物体上，运行时搭好一切：
    /// 灰色白盒地面 + 边墙、当前附身英雄的真身、几个无限血稻草人、跟随相机、返回营地的出口。
    /// 这里只用来试招，不开启正式 Run，也不会进入地牢。
    public class TrainingBootstrap : MonoBehaviour
    {
        [SerializeField] private float bgWorldWidth = 26f;  // 手绘底图世界宽度(4:3,高度自动)
        [SerializeField] private float playHalfW    = 9.6f; // 可走区域半宽(隐形边墙)
        [SerializeField] private float playHalfH    = 7.2f; // 可走区域半高
        [SerializeField] private int   dummyCount   = 4;

        private TrainingLayout _layout;       // 可视化布局锚点（有则用其坐标，无则用内置值）
        private GameObject _player;
        private Camera     _cam;
        private Vector3    _exitPos;
        private Vector3    _gatePos;          // 右下角大门(玩家进场/离场处)
        private float      _bgHalfW, _bgHalfH; // 背景半尺寸(相机夹取边界)
        private bool       _nearExit;
        private HeroData   _hero;             // 本次试练的英雄(返回营地时据此恢复附身)

        private string _msg = "";
        private float  _msgUntil;
        private Game.UI.TrainingHudView _hud;

        private void Start()
        {
            gameObject.AddComponent<DamageNumbers>();   // 伤害数字单例

            _layout = FindObjectOfType<TrainingLayout>();
            if (_layout != null)   // 用布局锚点覆盖可走边界
            { playHalfW = _layout.boundaryHalf.x; playHalfH = _layout.boundaryHalf.y; }

            _hero = ResolveHero();
            BuildArena();
            SpawnPlayer(_hero);
            SpawnDummies();
            EnsureCamera();

            Flash("Training Arena. Space/Left-click to attack, R weapon skill, F hero skill. The dummy has infinite HP — practice freely.", 7f);
        }

        // ── 解析要试练的英雄 ───────────────────────────────────────────
        private HeroData ResolveHero()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.TrainingHero != null) return gm.TrainingHero;
            if (gm != null && gm.Run != null && gm.Run.Hero != null) return gm.Run.Hero;
            var db = Resources.Load<HeroDatabase>("Heroes/HeroDatabase");
            if (db != null && db.heroes != null && db.heroes.Length > 0) return db.heroes[0];
            return null;
        }

        // ── 手绘演武场底图 + 隐形边墙 + 右下角大门 ─────────────────────
        private void BuildArena()
        {
            var root = new GameObject("TrainingArena");

            // 背景：手绘演武场大厅
            var bgSprite = Resources.Load<Sprite>("Training/TrainingMap");
            if (bgSprite != null)
            {
                var bg = new GameObject("TrainingMap");
                bg.transform.SetParent(root.transform, false);
                bg.transform.position = Vector3.zero;
                var bsr = bg.AddComponent<SpriteRenderer>();
                bsr.sprite       = bgSprite;
                bsr.sortingOrder = -100;
                float nativeW = Mathf.Max(0.0001f, bgSprite.bounds.size.x);
                float scale   = bgWorldWidth / nativeW;
                bg.transform.localScale = new Vector3(scale, scale, 1f);
                _bgHalfW = bgWorldWidth * 0.5f;
                _bgHalfH = bgSprite.bounds.size.y * scale * 0.5f;
            }
            else
            {
                BuildWhiteboxFloor(root.transform);  // 兜底
                _bgHalfW = playHalfW + 1f;
                _bgHalfH = playHalfH + 1f;
            }

            // 四面隐形边墙（仅碰撞，挡住玩家；画中石墙负责视觉）
            float t = 0.8f;
            MakeWall(root.transform, new Vector3(0f,  playHalfH, 0f), new Vector2(playHalfW * 2f + t, t));
            MakeWall(root.transform, new Vector3(0f, -playHalfH, 0f), new Vector2(playHalfW * 2f + t, t));
            MakeWall(root.transform, new Vector3(-playHalfW, 0f, 0f), new Vector2(t, playHalfH * 2f));
            MakeWall(root.transform, new Vector3( playHalfW, 0f, 0f), new Vector2(t, playHalfH * 2f));

            // 右下角大门 + 出口垫：走上去按 E 返回营地（布局锚点可覆盖位置）
            _gatePos = (_layout != null && _layout.HasReturnPoint)
                ? _layout.returnPoint.position
                : new Vector3(playHalfW - 1.8f, -playHalfH + 0.9f, 0f);
            _exitPos = _gatePos;
            var exit = MakeQuad(root.transform, "ExitPad", _exitPos, new Vector2(1f, 1f),
                                new Color(0.95f, 0.7f, 0.35f, 0.28f), -90);
            exit.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
        }

        // 兜底白盒地面（仅在底图缺失时使用）
        private void BuildWhiteboxFloor(Transform root)
        {
            const float tile = 2f;
            int cols = Mathf.CeilToInt(playHalfW * 2f / tile);
            int rows = Mathf.CeilToInt(playHalfH * 2f / tile);
            for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                bool even = (x + y) % 2 == 0;
                var c = even ? new Color(0.22f, 0.22f, 0.26f) : new Color(0.27f, 0.27f, 0.32f);
                var pos = new Vector3(-playHalfW + tile * (x + 0.5f), -playHalfH + tile * (y + 0.5f), 0f);
                MakeQuad(root, "Floor", pos, new Vector2(tile, tile), c, -10);
            }
        }

        private void MakeWall(Transform parent, Vector3 pos, Vector2 size)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private GameObject MakeQuad(Transform parent, string name, Vector3 pos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = UnitSquare();
            sr.color        = color;
            sr.sortingOrder = order;
            return go;
        }

        // ── 木桩：摆在中央较场上,一排面向玩家 ──────────────────────────
        private void SpawnDummies()
        {
            // 布局锚点优先：每个非空锚点生成一个假人
            if (_layout != null && _layout.dummySpawns != null && _layout.DummyCount > 0)
            {
                int k = 0;
                foreach (var t in _layout.dummySpawns)
                {
                    if (t == null) continue;
                    MakeDummy("StrawDummy_" + k++, t.position);
                }
                return;
            }

            // 回退：按数量横向铺开
            int n = Mathf.Max(1, dummyCount);
            float span = playHalfW * 1.0f;            // 横向铺开范围
            for (int i = 0; i < n; i++)
            {
                float fx = n == 1 ? 0f : Mathf.Lerp(-span * 0.5f, span * 0.5f, i / (float)(n - 1));
                MakeDummy("StrawDummy_" + i, new Vector3(fx, 0.2f, 0f));
            }
        }

        private void MakeDummy(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
            go.AddComponent<TrainingDummy>();
        }

        // ── 玩家真身（与地牢一致的装配，简化版）───────────────────────
        private void SpawnPlayer(HeroData hero)
        {
            if (hero == null)
            {
                Debug.LogWarning("[TrainingBootstrap] No hero to spawn.");
                return;
            }

            _player = new GameObject("Player_" + hero.heroName);
            _player.tag                  = "Player";
            // 出生点：布局锚点优先；否则自右下角大门进场(门边、面朝场内)
            _player.transform.position   = (_layout != null && _layout.HasPlayerSpawn)
                ? _layout.playerSpawn.position
                : _gatePos + Vector3.up * 1.8f;
            _player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = _player.AddComponent<SpriteRenderer>();
            var heroSpr = HeroSprites.Get(hero.heroName);
            sr.sprite       = heroSpr;
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

            var health = _player.AddComponent<Health>();
            health.IFrameDuration = 0.4f;

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
            controller.SetFacingSprites(HeroSprites.Get(hero.heroName), HeroSprites.GetBack(hero.heroName));

            _player.AddComponent<PlayerStateReporter>();

            var (slot0, slot1) = WeaponLibrary.GetStarterWeapons(hero.heroName);
            weaponHandler.EquipWeapon(slot0, 0);
            weaponHandler.EquipWeapon(slot1, 1);
            health.Heal(health.Max);
        }

        // ── 相机 ───────────────────────────────────────────────────────
        private void EnsureCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            _cam.orthographic     = true;
            _cam.orthographicSize = 6.5f;
            _cam.backgroundColor  = new Color(0.04f, 0.03f, 0.05f);
            _cam.clearFlags       = CameraClearFlags.SolidColor;
        }

        private void LateUpdate()
        {
            if (_cam == null || _player == null) return;
            float hh = _cam.orthographicSize;
            float hw = hh * _cam.aspect;
            float cx = (_bgHalfW > hw) ? Mathf.Clamp(_player.transform.position.x, -_bgHalfW + hw, _bgHalfW - hw) : 0f;
            float cy = (_bgHalfH > hh) ? Mathf.Clamp(_player.transform.position.y, -_bgHalfH + hh, _bgHalfH - hh) : 0f;
            _cam.transform.position = new Vector3(cx, cy, -10f);
        }

        private void Update()
        {
            if (_player != null)
            {
                _nearExit = ((Vector2)(_player.transform.position - _exitPos)).sqrMagnitude <= 1.6f * 1.6f;
                if ((_nearExit && Input.GetKeyDown(KeyCode.E)) || Input.GetKeyDown(KeyCode.Escape))
                    ReturnToHub();
            }
            EnsureHud();
            _hud.SetExitPrompt(_nearExit);
            _hud.SetBanner(Time.unscaledTime < _msgUntil && !string.IsNullOrEmpty(_msg), _msg);
        }

        private void EnsureHud()
        {
            if (_hud != null) return;
            _hud = new GameObject("TrainingHud").AddComponent<Game.UI.TrainingHudView>();
            _hud.SetControls(Controls);
        }

        private void ReturnToHub()
        {
            // 确保有持久 GameManager 来携带"返回练武场"状态——即便是单独打开 Training 场景
            // 直接测试(没有从营地进入),返回营地也能恢复附身态并落在练武场门旁。
            var gm = GameManager.Instance;
            if (gm == null)
            {
                var go = new GameObject("GameManager");
                gm = go.AddComponent<GameManager>();   // Awake 内设 Instance + DontDestroyOnLoad
            }
            if (gm.TrainingHero == null && _hero != null) gm.SetTrainingHero(_hero);
            gm.ReturnToHub();
        }

        private void Flash(string msg, float dur) { _msg = msg; _msgUntil = Time.unscaledTime + dur; }

        // 控制提示行（key, action）—— 完整列出训练场可用操作
        private static readonly string[,] Controls =
        {
            { "WASD",            "Move" },
            { "Space / LMB",     "Attack" },
            { "R / RMB",         "Weapon Skill" },
            { "F",               "Hero Skill" },
            { "Q",               "Swap Weapon" },
            { "E",               "Interact" },
            { "Esc",             "Return to Camp" },
        };


        private static Sprite _unit;
        private static Sprite UnitSquare()
        {
            if (_unit != null) return _unit;
            const int s = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _unit = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _unit;
        }
    }
}
