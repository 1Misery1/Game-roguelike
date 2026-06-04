using System.Collections.Generic;
using Game.Core;
using Game.Data;
using Game.Art;
using UnityEngine;

namespace Game.UI
{
    /// 大厅运行时控制器（场景驱动）。
    /// 场景里把布局摆好（地板/篝火/台座/伴侣坐姿/名册行/封印裂缝…都是真实 GameObject），
    /// 本脚本仅做：1) 输入移动+E 交互  2) 按存档状态切换可见性/颜色  3) HUD 显示。
    /// 不再运行时 new GameObject 或 Texture2D。
    public class HubController : MonoBehaviour
    {
        [Header("Data / References")]
        [SerializeField] private HeroDatabase heroDatabase;
        [SerializeField] private Transform    player;
        [SerializeField] private Camera       cam;

        [Header("Companion seats (按 HeroDatabase 顺序拖入)")]
        [SerializeField] private GameObject[] companionSeats; // 5 个，已解锁就启用

        [Header("Memorial name rows (按上→下顺序拖入 10 行)")]
        [SerializeField] private GameObject[] memorialNameRows; // 10 行
        [SerializeField] private SpriteRenderer memorialBody;   // 满 10 行时点亮

        [Header("Lift seal crack (按存档进度伸缩 / 调透明度)")]
        [SerializeField] private Transform sealCrack;
        [SerializeField] private SpriteRenderer sealCrackSr;
        [SerializeField] private float sealCrackBaseWidth = 1.5f;

        [Header("Tuning")]
        [SerializeField] private float   moveSpeed      = 6f;
        [SerializeField] private float   interactRadius = 1.7f;
        [SerializeField] private float   playerRadius   = 0.42f;   // 碰撞检测半径
        [SerializeField] private LayerMask obstacleMask = ~0;       // 障碍体积所在层(默认全部)
        [SerializeField] private Vector2 boundsMin = new Vector2(-11.5f, -5.5f);
        [SerializeField] private Vector2 boundsMax = new Vector2( 11.5f,  5.5f);

        [Header("Camera follow (视角锁定在当前操控的角色上)")]
        [Tooltip("越小越拉近、越聚焦角色；越大看到的房间越多")]
        [SerializeField] private float   camOrthoSize = 5.5f;
        [SerializeField] private float   camLerp      = 8f;          // 跟随平滑
        [SerializeField] private Vector2 camMin = new Vector2(-12f, -7.2f); // 相机中心夹紧范围(房间四壁)
        [SerializeField] private Vector2 camMax = new Vector2( 12f,  6.5f);

        [Header("Ghost / Unlock visuals (压暗,贴合被远处火光照亮的氛围)")]
        [SerializeField] private Color ghostTint = new Color(0.55f, 0.7f, 0.92f, 0.6f);
        [SerializeField] private Color unlockedTint = new Color(0.82f, 0.82f, 0.9f, 1f);
        [SerializeField] private Color awokenTint = new Color(1f, 0.93f, 0.78f, 1f); // 觉醒魂体的暖金点亮
        [SerializeField] private Color pedestalLockedTint = new Color(0.28f, 0.28f, 0.36f, 1f);
        [SerializeField] private Color pedestalUnlockedTint = new Color(0.58f, 0.56f, 0.64f, 1f);

        private HeroData[]      _heroes;
        private PersistentState _persistent;
        private int             _selectedHeroIndex;
        private bool            _facingRight = true;
        private SpriteRenderer  _playerSr;
        private Sprite          _playerGhostSprite;        // 余烬游魂原图(未附身时)
        private float           _playerGhostHeight = 2f;   // 游魂/真身统一显示高度
        private readonly List<HubStation> _stations = new List<HubStation>();
        private HubStation _near;

        private string _msg = "";
        private float  _msgUntil;
        private GUIStyle _centerStyle;

        private const float TruthTotal = 10f;
        private const float FloorTotal = 3f;

        private void Start()
        {
            if (heroDatabase == null)
                heroDatabase = Resources.Load<HeroDatabase>("Heroes/HeroDatabase");
            _heroes = heroDatabase != null && heroDatabase.heroes != null
                ? heroDatabase.heroes : new HeroData[0];

            _persistent = GameManager.Instance != null ? GameManager.Instance.Persistent : PersistentState.Load();
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic     = true;
                cam.orthographicSize = camOrthoSize;
            }
            if (player != null) _playerSr = player.GetComponent<SpriteRenderer>();

            _stations.AddRange(FindObjectsOfType<HubStation>());
            if (_playerSr != null)
            {
                _playerGhostSprite = _playerSr.sprite;            // 记下「余烬游魂」原图
                _playerGhostHeight = _playerSr.bounds.size.y;      // 与真身换装时保持等高
            }
            EnsureStoryUnlocks();
            _selectedHeroIndex = -1;                 // 开局默认游魂态
            bool fromTraining = TryResumeFromTraining();  // 若从练武场归来:以英雄态立于门旁
            ApplyPedestalUnlockVisuals();
            ApplyCompanionSeats();
            ApplyMemorialRows();
            ApplyLiftSeal();
            ApplyPlayerVisual();
            SnapCamera();
            const string ghostGuide = "你是一缕余烬游魂。走向台前已觉醒的残魂,按 E 附身,便能化作他的身躯下潜。";
            if (fromTraining && _selectedHeroIndex >= 0)
                Flash($"你自练武场归来,仍以 {_heroes[_selectedHeroIndex].displayName} 之身,立于门旁。", 4f);
            else if (!IntroController.HasSeen)
                IntroController.Play(onComplete: () => Flash(ghostGuide, 7f));  // 首次入营:先看开场,再给引导
            else
                Flash(ghostGuide, 7f);
        }

        // 从练武场返回:沿用刚才操控的英雄(英雄态),并把玩家落在练武场门旁。
        // 返回 true 表示确实是从练武场归来且成功附身。
        private bool TryResumeFromTraining()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.ReturningFromTraining) return false;
            gm.ReturningFromTraining = false;                 // 消费一次性标志
            if (gm.TrainingHero == null) return false;

            int idx = System.Array.FindIndex(_heroes,
                h => h != null && h.heroName == gm.TrainingHero.heroName);
            if (idx < 0 || !_persistent.IsHeroUnlocked(_heroes[idx].heroName)) return false;

            _selectedHeroIndex = idx;
            var door = FindStation(HubStationKind.TrainingDoor);
            if (door != null && player != null)
            {
                var p = door.transform.position;
                player.position = new Vector3(p.x, p.y + 1.0f, player.position.z); // 门内侧一点,正对房间
            }
            return true;
        }

        private HubStation FindStation(HubStationKind kind)
        {
            foreach (var s in _stations) if (s.kind == kind) return s;
            return null;
        }

        // 剧情解锁:某英雄的「专属真相」一旦在地底被揭开(写入存档 TruthFlags),
        // 这缕残魂即觉醒为可选状态。requiredTruthFlag 为空者(主角)开局即可用。
        private void EnsureStoryUnlocks()
        {
            if (_heroes.Length == 0) return;
            bool changed = false;
            foreach (var h in _heroes)
            {
                if (h == null) continue;
                bool qualifies = string.IsNullOrEmpty(h.requiredTruthFlag)
                                 || _persistent.HasTruthFlag(h.requiredTruthFlag);
                if (qualifies && !_persistent.IsHeroUnlocked(h.heroName))
                {
                    _persistent.UnlockedHeroIds.Add(h.heroName);
                    changed = true;
                }
            }
            if (changed) _persistent.Save();
            // 开局不自动附身:玩家先以「余烬游魂」形态游荡,需走到台座主动附身。
            // (_selectedHeroIndex 的初值在 Start 里设定,以便区分「从练武场归来」的情形)
        }

        // 把某个 SpriteRenderer 的图换成英雄「游戏内真身」精灵,并保持原显示高度不变。
        private void SwapToRealHero(Transform holder, string heroName)
        {
            if (holder == null) return;
            var sr = holder.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            var real = HeroSprites.Get(heroName);
            if (real == null) return;
            float worldH = sr.bounds.size.y;                 // 当前(残魂)世界显示高度
            sr.sprite = real;
            sr.color   = Color.white;
            float localH = real.bounds.size.y;               // 真身在 scale=1 下的高度
            if (localH > 0.0001f && worldH > 0.0001f)
            {
                float k = worldH / localH;
                holder.localScale = new Vector3(k, k, 1f);
            }
        }

        // 台座视觉态：已解锁=实体全色；未解锁=残魂(半透蓝白 + 上下漂浮)
        private void ApplyPedestalUnlockVisuals()
        {
            foreach (var s in _stations)
            {
                if (s.kind != HubStationKind.HeroPedestal) continue;
                if (s.heroIndex < 0 || s.heroIndex >= _heroes.Length) continue;
                bool unlocked = _persistent.IsHeroUnlocked(_heroes[s.heroIndex].heroName);

                // 底座颜色(找 Base 子物体;兼容 anchor 自身有 SR 的旧场景)
                var baseT = s.transform.Find("Base");
                var baseSr = baseT != null ? baseT.GetComponent<SpriteRenderer>()
                                           : s.GetComponent<SpriteRenderer>();
                if (baseSr != null) baseSr.color = unlocked ? pedestalUnlockedTint : pedestalLockedTint;

                // 台座上的人物 (子物体 "Figure")
                var fig = s.transform.Find("Figure");
                if (fig != null)
                {
                    var sr = fig.GetComponent<SpriteRenderer>();
                    var ghost = fig.GetComponent<GhostFloat>();
                    if (unlocked)
                    {
                        // 已觉醒:手绘魂体点亮为暖金、停止漂浮(不再用低分辨率程序化真身)
                        if (sr != null) sr.color = awokenTint;
                        if (ghost != null) ghost.enabled = false;
                    }
                    else
                    {
                        // 未觉醒:半透蓝白残魂 + 上下漂浮
                        if (sr != null) sr.color = ghostTint;
                        if (ghost != null) ghost.enabled = true;
                    }
                }
            }
        }

        // 已解锁的英雄出现在火边的预置座位上
        private void ApplyCompanionSeats()
        {
            if (companionSeats == null) return;
            int n = Mathf.Min(companionSeats.Length, _heroes.Length);
            for (int i = 0; i < n; i++)
            {
                if (companionSeats[i] == null) continue;
                bool unlocked = _persistent.IsHeroUnlocked(_heroes[i].heroName);
                // 已附身的那位:其肉身已被玩家操控,火边座位消失,避免出现两个分身。
                bool seated = unlocked && i != _selectedHeroIndex;
                companionSeats[i].SetActive(seated);
                if (seated)
                {
                    // 火边已觉醒的同伴显示其游戏内「真身」像素角色(人物而非鬼魂),
                    // 与玩家附身后的形象一致;台座上未唤醒者才保留残魂。
                    var spr = companionSeats[i].transform.Find("Sprite");
                    SwapToRealHero(spr != null ? spr : companionSeats[i].transform,
                                   _heroes[i].heroName);
                }
            }
        }

        // 名册碑：每记起 1 条真相点亮 1 行；满 10 行碑体高亮
        private void ApplyMemorialRows()
        {
            int truths = _persistent.TruthFlags != null ? _persistent.TruthFlags.Count : 0;
            if (memorialNameRows != null)
            {
                for (int i = 0; i < memorialNameRows.Length; i++)
                    if (memorialNameRows[i] != null)
                        memorialNameRows[i].SetActive(i < truths);
            }
            if (memorialBody != null && truths >= TruthTotal)
                memorialBody.color = new Color(0.78f, 0.86f, 1f);
        }

        // 升降井封印裂缝：BestFloor / FloorTotal 控制宽度与亮度
        private void ApplyLiftSeal()
        {
            if (sealCrack == null) return;
            float prog = Mathf.Clamp01(_persistent.BestFloor / FloorTotal);
            var ls = sealCrack.localScale;
            ls.x = sealCrackBaseWidth * (0.25f + 0.75f * prog);
            sealCrack.localScale = ls;
            if (sealCrackSr != null)
                sealCrackSr.color = new Color(1f, 0.5f + 0.4f * prog, 0.25f, 0.35f + 0.55f * prog);
        }

        // 未附身=显示独立的余烬游魂;附身后=切换为该英雄的游戏内真身建模,游魂消失。
        private void ApplyPlayerVisual()
        {
            if (_playerSr == null || player == null) return;
            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _heroes.Length)
            {
                // 附身:玩家化作该英雄的游戏内真身(像素角色,人物而非魂体),保持统一显示高度
                SwapToRealHero(player, _heroes[_selectedHeroIndex].heroName);
            }
            else
            {
                _playerSr.sprite = _playerGhostSprite;
                _playerSr.color  = new Color(0.72f, 0.86f, 1f, 0.95f);
                if (_playerGhostSprite != null)
                {
                    float lh = _playerGhostSprite.bounds.size.y;
                    if (lh > 0.0001f)
                    {
                        float k = _playerGhostHeight / lh;
                        player.localScale = new Vector3(k, k, 1f);
                    }
                }
            }
            FitPlayerShadow();
        }

        // 玩家脚下贴地影：因游魂/真身切换会改 player.localScale，需反向补偿，
        // 让阴影的世界大小与脚下偏移保持恒定（与火边伙伴的阴影一致），不随换身忽大忽小。
        private void FitPlayerShadow()
        {
            if (player == null) return;
            var sh = player.Find("Shadow");
            if (sh == null) return;
            float ps = player.localScale.x;
            if (ps < 0.0001f) return;
            const float shadowWorldScale = 0.40f; // 椭圆约 1 单位宽
            const float shadowWorldOffY  = -0.72f; // 落在脚下的世界偏移
            sh.localScale    = new Vector3(shadowWorldScale / ps, shadowWorldScale / ps, 1f);
            var lp = sh.localPosition;
            sh.localPosition = new Vector3(lp.x, shadowWorldOffY / ps, lp.z);
        }

        // 取某英雄的手绘魂体精灵(挂在其火边伙伴座位上的 Ghost_职业)。缺失时退回通用游魂图。
        private Sprite HeroGhostSprite(int idx)
        {
            if (companionSeats != null && idx >= 0 && idx < companionSeats.Length && companionSeats[idx] != null)
            {
                var sp = companionSeats[idx].transform.Find("Sprite");
                var sr = sp != null ? sp.GetComponent<SpriteRenderer>()
                                    : companionSeats[idx].GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return _playerGhostSprite;
        }

        private void Update()
        {
            if (IntroController.IsActive) return;   // 开场动画播放期间冻结营地操作
            if (player == null) return;

            var dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // 左右转向。英雄真身:朝右 flipX=false(与游戏内一致)。
            // 余烬游魂贴图默认朝向与英雄相反,故游魂态额外反转,避免左右朝向反了。
            if      (dir.x >  0.01f) _facingRight = true;
            else if (dir.x < -0.01f) _facingRight = false;
            if (_playerSr != null)
            {
                bool ghost = _selectedHeroIndex < 0;
                _playerSr.flipX = ghost ? _facingRight : !_facingRight;
            }

            Vector2 cur   = player.position;
            Vector2 delta = dir * moveSpeed * Time.deltaTime;

            // 分轴推进 + 障碍碰撞:撞到障碍体积时该轴不前进(可沿墙滑动)。
            Vector2 tryX = new Vector2(cur.x + delta.x, cur.y);
            if (!Blocked(tryX)) cur.x = tryX.x;
            Vector2 tryY = new Vector2(cur.x, cur.y + delta.y);
            if (!Blocked(tryY)) cur.y = tryY.y;

            cur.x = Mathf.Clamp(cur.x, boundsMin.x, boundsMax.x);
            cur.y = Mathf.Clamp(cur.y, boundsMin.y, boundsMax.y);
            player.position = new Vector3(cur.x, cur.y, player.position.z);

            _near = null;
            float best = interactRadius * interactRadius;
            foreach (var s in _stations)
            {
                float d = ((Vector2)(s.transform.position - player.position)).sqrMagnitude;
                if (d <= best) { best = d; _near = s; }
            }

            if (_near != null && Input.GetKeyDown(KeyCode.E))
                Interact(_near);

            // 魂出:从当前角色离体,变回游魂(角色坐回火边原位)
            if (Input.GetKeyDown(KeyCode.Q))
                UnPossess();
        }

        // 视角锁定:相机平滑跟随当前操控的对象(游魂或附身的英雄),并夹紧到房间四壁内。
        private void LateUpdate()
        {
            if (cam == null || player == null) return;
            Vector3 target = CameraTargetFor(player.position);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1f - Mathf.Exp(-camLerp * Time.deltaTime));
        }

        private Vector3 CameraTargetFor(Vector3 focus)
        {
            float hh = cam.orthographicSize;
            float hw = hh * cam.aspect;
            // 房间比视野窄时不横向平移(维持居中),否则夹紧
            float cx = (camMax.x - camMin.x) > 2f * hw
                ? Mathf.Clamp(focus.x, camMin.x + hw, camMax.x - hw) : (camMin.x + camMax.x) * 0.5f;
            float cy = (camMax.y - camMin.y) > 2f * hh
                ? Mathf.Clamp(focus.y, camMin.y + hh, camMax.y - hh) : (camMin.y + camMax.y) * 0.5f;
            return new Vector3(cx, cy, cam.transform.position.z);
        }

        // 瞬移/换身后立即把相机对准目标,避免横跨房间的拉丝。
        private void SnapCamera()
        {
            if (cam == null || player == null) return;
            cam.transform.position = CameraTargetFor(player.position);
        }

        // 离体:玩家在当前位置变回游魂,被附身的英雄重新坐回火边座位。
        private void UnPossess()
        {
            if (_selectedHeroIndex < 0) return;
            var hero = _heroes[_selectedHeroIndex];
            _selectedHeroIndex = -1;
            ApplyPlayerVisual();     // 当前位置变回游魂
            ApplyCompanionSeats();   // 该英雄重新出现在火边原座位
            Flash($"你离体而出,化回游魂。{hero.displayName} 重新坐回火边。", 3.5f);
        }

        // 目标位置是否与场景里的障碍体积重叠。
        private bool Blocked(Vector2 pos)
        {
            return Physics2D.OverlapCircle(pos, playerRadius, obstacleMask) != null;
        }

        private void Interact(HubStation s)
        {
            switch (s.kind)
            {
                case HubStationKind.HeroPedestal: InteractHero(s.heroIndex); break;
                case HubStationKind.QuestBoard:
                    Flash("悬赏:斩杀地底的「怪物」。走向升降井便可开始下潜。", 4f); break;
                case HubStationKind.LiftDoor: BeginDescent(); break;
                case HubStationKind.TrainingDoor: EnterTraining(); break;
                case HubStationKind.Campfire:
                    IntroController.Play(); break;   // 围火重温开场（你是如何来到这里的）
                case HubStationKind.Memorial:
                    Flash($"名册碑 — 已记起真相 {_persistent.TruthFlags.Count} / 10。记住他们的名字。", 5f); break;
                case HubStationKind.Records:
                    Flash($"出征 {_persistent.TotalRuns}  ·  通关 {_persistent.TotalVictories}  ·  最深 {_persistent.BestFloor} 层  ·  记起真相 {_persistent.TruthFlags.Count}/10", 6f); break;
            }
        }

        private void InteractHero(int idx)
        {
            if (idx < 0 || idx >= _heroes.Length) return;
            var hero = _heroes[idx];
            // 纯剧情解锁:未揭开其专属真相前,残魂不会觉醒,无法附身。
            if (!_persistent.IsHeroUnlocked(hero.heroName))
            {
                string line = !string.IsNullOrEmpty(hero.lockedStoryLine)
                    ? hero.lockedStoryLine
                    : $"{hero.displayName} 的残魂还认不出自己。先在地底揭开属于他的真相吧。";
                Flash(line, 7f);
                return;
            }
            _selectedHeroIndex = idx;
            // 附身:游魂钻入火边那具残躯——玩家瞬移到该英雄的火边座位,从那里以真身行动。
            if (companionSeats != null && idx < companionSeats.Length && companionSeats[idx] != null)
            {
                var seatPos = companionSeats[idx].transform.position;
                player.position = new Vector3(seatPos.x, seatPos.y, player.position.z);
            }
            ApplyPlayerVisual();        // 游魂 → 该英雄真身
            ApplyCompanionSeats();      // 隐藏被附身者的座位(原英雄消失),其余已解锁者照常围坐
            SnapCamera();               // 视角立即对准刚附身的真身
            Flash($"附身成功 — 你化作了 {hero.displayName},自火边起身。", 2.5f);
        }

        private void BeginDescent()
        {
            if (_selectedHeroIndex < 0 || _selectedHeroIndex >= _heroes.Length)
            {
                Flash("你仍是无形的游魂。先附身一缕已觉醒的残魂,才能下潜。", 4f);
                return;
            }
            var hero = _heroes[_selectedHeroIndex];
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[HubController] No GameManager in scene; cannot start run.");
                Flash("GameManager 缺失,无法下潜。", 3f);
                return;
            }
            GameManager.Instance.StartRun(hero);
        }

        // 进入训练场(白盒练武场)。需先附身一缕残魂,以其真身入场试招。
        private void EnterTraining()
        {
            if (_selectedHeroIndex < 0 || _selectedHeroIndex >= _heroes.Length)
            {
                Flash("游魂无法挥剑。先附身一缕已觉醒的残魂,再去练武场试招。", 4f);
                return;
            }
            var hero = _heroes[_selectedHeroIndex];
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[HubController] No GameManager in scene; cannot enter training.");
                Flash("GameManager 缺失,无法进入练武场。", 3f);
                return;
            }
            GameManager.Instance.EnterTraining(hero);
        }

        private void Flash(string msg, float dur) { _msg = msg; _msgUntil = Time.unscaledTime + dur; }

        private void OnGUI()
        {
            if (IntroController.IsActive) return;   // 开场动画期间不画营地 HUD，交给 IntroController 全屏接管
            UIFonts.ApplyToSkin();
            if (_centerStyle == null)
                _centerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            GUI.color = new Color(0.95f, 0.85f, 0.55f);
            GUI.Label(new Rect(12, 8, Screen.width - 24, 26), "三界余烬 — 营地");
            GUI.color = Color.white;
            var selName = (_selectedHeroIndex >= 0 && _selectedHeroIndex < _heroes.Length)
                ? _heroes[_selectedHeroIndex].displayName : "(未附身)";
            GUI.Label(new Rect(Screen.width - 360, 10, 348, 22),
                $"英雄: {selName}    真相: {_persistent.TruthFlags.Count}/10");

            foreach (var s in _stations) DrawStationLabel(s);

            if (_near != null)
            {
                var r = new Rect(Screen.width * 0.5f - 220, Screen.height - 64, 440, 26);
                GUI.color = new Color(0f, 0f, 0f, 0.5f); GUI.Box(r, GUIContent.none);
                GUI.color = new Color(1f, 0.95f, 0.7f); GUI.Label(r, $"[E] {PromptFor(_near)}", _centerStyle);
                GUI.color = Color.white;
            }

            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _heroes.Length)
            {
                var h = _heroes[_selectedHeroIndex];
                GUI.Label(new Rect(12, Screen.height - 92, Screen.width - 24, 60),
                    $"{h.displayName}   [Q] 魂出(变回游魂)\nHP {h.baseMaxHP:0}  ATK {h.baseAttack:0}  DEF {h.baseDefense:0}  ·  技能: {h.heroSkillName}  ·  被动: {h.heroPassiveName}");
            }

            if (Time.unscaledTime < _msgUntil && !string.IsNullOrEmpty(_msg))
            {
                var r = new Rect(Screen.width * 0.5f - 360, Screen.height * 0.5f + 120, 720, 28);
                GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.Box(r, GUIContent.none);
                GUI.color = new Color(0.95f, 0.9f, 0.8f); GUI.Label(r, _msg, _centerStyle);
                GUI.color = Color.white;
            }
        }

        private string PromptFor(HubStation s)
        {
            switch (s.kind)
            {
                case HubStationKind.HeroPedestal:
                    if (s.heroIndex < 0 || s.heroIndex >= _heroes.Length) return "选择";
                    var h = _heroes[s.heroIndex];
                    return _persistent.IsHeroUnlocked(h.heroName)
                        ? $"选择 {h.displayName}" : $"凝视 {h.displayName} 的残魂";
                case HubStationKind.QuestBoard: return "查看悬赏";
                case HubStationKind.LiftDoor:   return "下潜";
                case HubStationKind.TrainingDoor: return "进入练武场";
                case HubStationKind.Campfire:   return "围火·重温开场";
                case HubStationKind.Memorial:   return "细看名册";
                case HubStationKind.Records:    return "查看战绩";
            }
            return "交互";
        }

        private void DrawStationLabel(HubStation s)
        {
            if (cam == null) return;
            // 用所有子 renderer 的世界 bounds 拿到 sprite 真实顶端
            var srs = s.GetComponentsInChildren<SpriteRenderer>();
            float topY = s.transform.position.y + 0.5f;
            for (int i = 0; i < srs.Length; i++)
                if (srs[i].sprite != null) topY = Mathf.Max(topY, srs[i].bounds.max.y);
            Vector3 world = new Vector3(s.transform.position.x, topY + 0.25f, 0f);
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return;

            string text = string.IsNullOrEmpty(s.title) ? s.kind.ToString() : s.title;
            if (s.kind == HubStationKind.HeroPedestal && s.heroIndex >= 0 && s.heroIndex < _heroes.Length)
            {
                if (!_persistent.IsHeroUnlocked(_heroes[s.heroIndex].heroName)) text += "  [残魂未醒]";
                else if (s.heroIndex == _selectedHeroIndex) text += "  ◀";
            }

            GUI.color = (s == _near) ? new Color(1f, 0.95f, 0.6f) : new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(sp.x - 90f, Screen.height - sp.y - 16f, 180, 20), text, _centerStyle);
            GUI.color = Color.white;
        }
    }
}
