using Game.Core;
using Game.Data;
using Game.Dev;
using UnityEngine;

namespace Game.UI
{
    /// Main menu / start lobby — tabs: 英雄选择 | 战绩 | 操控说明 | 设置
    public class MenuController : MonoBehaviour
    {
        private enum Tab { HeroSelect, Records, Controls, Settings }

        private PersistentState _persistent;
        private HeroData[]      _heroes;
        private int             _selectedHeroIndex;
        private Tab             _currentTab = Tab.HeroSelect;

        private void Start()
        {
            _persistent = PersistentState.Load();
            BuildHeroPool();
            EnsureStarterUnlocked();
        }

        // ── Hero Pool ────────────────────────────────────────────────────────

        private void BuildHeroPool()
        {
            _heroes = new[]
            {
                MakeHero("Warrior", "雷昂·铁誓",
                    "战士 · 边境守卫世家出身，沉稳直接。在熔岩深处直面祖辈的罪责。",
                    100f, 14f, 5f, 5.5f, 1.0f, 0,   new Color(0.4f, 0.85f, 1f),
                    HeroSkillType.WarCry,         10f, "战吼",
                    HeroPassiveType.BattlefieldWill,   "战场意志"),

                MakeHero("Ranger", "艾薇拉·风痕",
                    "游侠 · 冷静独立的独行者，侦察队唯一的生还者。被遗忘的记忆等待解封。",
                    70f,  12f, 0f, 8.0f, 1.4f, 30,  new Color(0.8f, 1f,   0.5f),
                    HeroSkillType.ShadowStep,     6f,  "暗影步",
                    HeroPassiveType.ComboStrike,       "连击之道"),

                MakeHero("Mage", "赛琳娜·星烬",
                    "法师 · 王国学院法师，理性而执着于知识。为追查导师的界心研究而来。",
                    55f,  22f, 0f, 5.0f, 0.8f, 60,  new Color(1f,   0.6f, 1f),
                    HeroSkillType.ArcaneSurge,    8f,  "奥术涌动",
                    HeroPassiveType.ManaAmplification, "法力增幅"),

                MakeHero("Paladin", "奥斯汀·晨誓",
                    "圣骑士 · 虔诚正直的晨誓教会骑士，将亲手揭开教会掩盖的真相。",
                    110f, 11f, 6f, 5.0f, 0.9f, 100, new Color(1f,   0.9f, 0.4f),
                    HeroSkillType.HolyLight,      12f, "圣光术",
                    HeroPassiveType.SacredOath,        "神圣誓约"),

                MakeHero("Hunter", "诺兰·灰羽",
                    "猎手 · 沉默敏锐的荒野猎人，信兽多于信人。循着虚空污染的痕迹而来。",
                    65f,  16f, 0f, 7.0f, 1.2f, 150, new Color(1f,   0.55f, 0.3f),
                    HeroSkillType.PrecisionShot,  7f,  "精准射击",
                    HeroPassiveType.EagleEye,          "鹰眼"),
            };
        }

        private static HeroData MakeHero(
            string name, string displayName, string desc,
            float hp, float atk, float def, float ms, float asp,
            int cost, Color tint,
            HeroSkillType skillType, float skillCd, string skillName,
            HeroPassiveType passiveType, string passiveName)
        {
            var h = ScriptableObject.CreateInstance<HeroData>();
            h.heroName          = name;
            h.displayName       = displayName;
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

        private void StartRun()
        {
            if (_selectedHeroIndex < 0 || _selectedHeroIndex >= _heroes.Length) return;
            var hero = _heroes[_selectedHeroIndex];
            if (!_persistent.IsHeroUnlocked(hero.heroName)) return;
            GameManager.Instance.StartRun(hero);
        }

        // ── OnGUI ────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            // 背景
            FillRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.07f, 0.07f, 0.11f));
            FillRect(new Rect(0, 0, Screen.width, 3), new Color(0.55f, 0.45f, 0.15f));
            FillRect(new Rect(0, Screen.height - 3, Screen.width, 3), new Color(0.55f, 0.45f, 0.15f));

            DrawHeader();
            DrawTabBar();

            switch (_currentTab)
            {
                case Tab.HeroSelect: DrawHeroSelect(); break;
                case Tab.Records:    DrawRecords();    break;
                case Tab.Controls:   DrawControls();   break;
                case Tab.Settings:   DrawSettings();   break;
            }

            DrawStartButton();
        }

        // ── Header ───────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            GUI.Label(new Rect(0, 10, Screen.width, 52), "三界余烬",
                MkLabel(44, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.98f, 0.9f, 0.35f)));
            GUI.Label(new Rect(0, 58, Screen.width, 18), "Embers of Three Realms  ·  深入三层地牢，找回界心碎片",
                MkLabel(12, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.5f, 0.5f, 0.58f)));
            GUI.Label(new Rect(0, 80, Screen.width, 20), $"◈  解锁货币: {_persistent.UnlockCurrency}",
                MkLabel(14, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(1f, 0.88f, 0.22f)));
        }

        // ── Tab Bar ──────────────────────────────────────────────────────────

        private void DrawTabBar()
        {
            const float tabY  = 108f;
            const float tabH  = 30f;
            const float tabW  = 150f;
            const float tabGap = 4f;
            const int   tabCount = 4;
            float totalW = tabCount * tabW + (tabCount - 1) * tabGap;
            float startX = (Screen.width - totalW) * 0.5f;

            string[] labels = { "英雄选择", "战  绩", "操控说明", "设  置" };
            Tab[]    values = { Tab.HeroSelect, Tab.Records, Tab.Controls, Tab.Settings };

            FillRect(new Rect(0, tabY + tabH - 1, Screen.width, 1), new Color(0.22f, 0.22f, 0.32f));

            for (int i = 0; i < tabCount; i++)
            {
                float tx    = startX + i * (tabW + tabGap);
                bool  active = _currentTab == values[i];

                FillRect(new Rect(tx, tabY, tabW, tabH),
                    active ? new Color(0.16f, 0.22f, 0.38f) : new Color(0.1f, 0.1f, 0.16f));

                if (active)
                    FillRect(new Rect(tx, tabY + tabH - 2, tabW, 2), new Color(0.45f, 0.75f, 1f));

                if (GUI.Button(new Rect(tx, tabY, tabW, tabH), GUIContent.none, GUIStyle.none))
                    _currentTab = values[i];

                GUI.Label(new Rect(tx, tabY, tabW, tabH), labels[i],
                    MkLabel(13, TextAnchor.MiddleCenter,
                        active ? FontStyle.Bold : FontStyle.Normal,
                        active ? new Color(0.45f, 0.75f, 1f) : new Color(0.65f, 0.65f, 0.72f)));
            }
        }

        // ── 英雄选择 ─────────────────────────────────────────────────────────

        private void DrawHeroSelect()
        {
            const float cardW = 200f, cardH = 288f, gap = 10f;
            float totalW = _heroes.Length * cardW + (_heroes.Length - 1) * gap;
            float startX = (Screen.width - totalW) * 0.5f;
            const float cardY = 148f;

            for (int i = 0; i < _heroes.Length; i++)
            {
                var  h        = _heroes[i];
                bool unlocked = _persistent.IsHeroUnlocked(h.heroName);
                bool selected = i == _selectedHeroIndex;
                var  rect     = new Rect(startX + i * (cardW + gap), cardY, cardW, cardH);

                Color cardBg = !unlocked ? new Color(0.2f, 0.12f, 0.12f)
                             : selected  ? new Color(0.13f, 0.24f, 0.42f)
                                         : new Color(0.12f, 0.13f, 0.2f);
                FillRect(rect, cardBg);

                // 选中边框
                if (selected)
                {
                    Color bc = new Color(0.45f, 0.75f, 1f);
                    FillRect(new Rect(rect.x,        rect.y,        rect.width, 2), bc);
                    FillRect(new Rect(rect.x,        rect.yMax - 2, rect.width, 2), bc);
                    FillRect(new Rect(rect.x,        rect.y,        2,          rect.height), bc);
                    FillRect(new Rect(rect.xMax - 2, rect.y,        2,          rect.height), bc);
                }

                // 英雄头像 72×72
                float ps = 72f;
                var   pr = new Rect(rect.x + (cardW - ps) * 0.5f, rect.y + 12f, ps, ps);
                FillRect(pr, new Color(0.08f, 0.08f, 0.14f));
                var portrait = HeroSprites.Get(h.heroName);
                if (portrait != null)
                    GUI.DrawTexture(pr, portrait.texture);
                else
                    FillRect(pr, h.tintColor * 0.4f);
                FillRect(new Rect(pr.x, pr.yMax - 3f, ps, 3), h.tintColor * 0.85f);

                // 锁定遮罩
                if (!unlocked)
                    FillRect(new Rect(pr.x, pr.y, ps, ps), new Color(0f, 0f, 0f, 0.55f));

                float iy = rect.y + 92f;

                GUI.Label(new Rect(rect.x + 8, iy, cardW - 16, 22),
                    string.IsNullOrEmpty(h.displayName) ? h.heroName : h.displayName,
                    MkLabel(16, TextAnchor.MiddleCenter, FontStyle.Bold,
                        unlocked ? Color.white : new Color(0.55f, 0.45f, 0.45f)));

                var descS = MkLabel(10, TextAnchor.UpperLeft, FontStyle.Normal, new Color(0.7f, 0.7f, 0.72f));
                descS.wordWrap = true;
                GUI.Label(new Rect(rect.x + 8, iy + 24, cardW - 16, 38), h.description, descS);

                GUI.Label(new Rect(rect.x + 8, iy + 64, cardW - 16, 18),
                    $"♥ {h.baseMaxHP:0}   ⚔ {h.baseAttack:0}   🛡 {h.baseDefense:0}   ⚡ {h.baseMoveSpeed:0.0}",
                    MkLabel(10, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.7f, 0.85f, 1f)));

                GUI.Label(new Rect(rect.x + 8, iy + 82, cardW - 16, 18),
                    $"[F] {h.heroSkillName}  CD:{h.heroSkillCooldown:0}s",
                    MkLabel(10, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(1f, 0.82f, 0.3f)));

                GUI.Label(new Rect(rect.x + 8, iy + 100, cardW - 16, 18),
                    $"◆ {h.heroPassiveName}",
                    MkLabel(10, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.5f, 1f, 0.6f)));

                // 解锁标签（锁定时显示费用）
                if (!unlocked)
                {
                    GUI.Label(new Rect(rect.x + 8, iy + 120, cardW - 16, 18),
                        $"🔒 需 {h.unlockCost} ◈ 解锁",
                        MkLabel(10, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.75f, 0.65f, 0.4f)));
                }

                // 操作按钮
                var btnRect = new Rect(rect.x + 8, rect.y + cardH - 40, cardW - 16, 32);
                if (unlocked)
                {
                    FillRect(btnRect, selected ? new Color(0.18f, 0.38f, 0.7f) : new Color(0.15f, 0.15f, 0.22f));
                    if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
                        _selectedHeroIndex = i;
                    GUI.Label(btnRect, selected ? "✓  已选择" : "选  择",
                        MkLabel(13, TextAnchor.MiddleCenter, FontStyle.Bold,
                            selected ? new Color(0.5f, 0.8f, 1f) : new Color(0.68f, 0.68f, 0.75f)));
                }
                else
                {
                    bool canAfford = _persistent.UnlockCurrency >= h.unlockCost;
                    FillRect(btnRect, canAfford ? new Color(0.38f, 0.28f, 0.06f) : new Color(0.16f, 0.1f, 0.1f));
                    GUI.enabled = canAfford;
                    if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
                        _persistent.TryUnlockHero(h.heroName, h.unlockCost);
                    GUI.Label(btnRect, $"解锁  {h.unlockCost} ◈",
                        MkLabel(12, TextAnchor.MiddleCenter, FontStyle.Bold,
                            canAfford ? new Color(1f, 0.85f, 0.3f) : new Color(0.45f, 0.38f, 0.3f)));
                    GUI.enabled = true;
                }
            }
        }

        // ── 战绩 ─────────────────────────────────────────────────────────────

        private void DrawRecords()
        {
            float panW = Mathf.Min(520f, Screen.width - 40f);
            float panX = (Screen.width - panW) * 0.5f;
            const float panY = 148f;
            float panH = Screen.height - panY - 70f;

            FillRect(new Rect(panX, panY, panW, panH), new Color(0.1f, 0.1f, 0.16f));
            FillRect(new Rect(panX, panY, panW, 2), new Color(0.45f, 0.75f, 1f, 0.4f));

            GUI.Label(new Rect(panX, panY + 8, panW, 28), "历  史  战  绩",
                MkLabel(20, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.98f, 0.9f, 0.35f)));

            FillRect(new Rect(panX + 20, panY + 42, panW - 40, 1), new Color(0.28f, 0.28f, 0.38f));

            var stats = new (string label, string value)[]
            {
                ("总出战次数",    _persistent.TotalRuns.ToString()),
                ("通关次数",      _persistent.TotalVictories.ToString()),
                ("胜  率",        _persistent.TotalRuns > 0
                                      ? $"{100f * _persistent.TotalVictories / _persistent.TotalRuns:0.0}%"
                                      : "—"),
                ("到达最深层",    _persistent.BestFloor > 0 ? $"第 {_persistent.BestFloor} 层" : "—"),
                ("累计击杀数",    $"{_persistent.TotalKills:N0}"),
                ("累计造成伤害",  $"{_persistent.TotalDamage:N0}"),
            };

            float rowH = 40f;
            float gy   = panY + 50f;
            for (int i = 0; i < stats.Length; i++)
            {
                float ry = gy + i * rowH;
                FillRect(new Rect(panX + 20, ry, panW - 40, rowH - 2),
                    i % 2 == 0 ? new Color(0.13f, 0.14f, 0.21f) : new Color(0.1f, 0.1f, 0.16f));

                GUI.Label(new Rect(panX + 32, ry, 200, rowH), stats[i].label,
                    MkLabel(13, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.72f, 0.72f, 0.8f)));
                GUI.Label(new Rect(panX + 32, ry, panW - 64, rowH), stats[i].value,
                    MkLabel(15, TextAnchor.MiddleRight, FontStyle.Bold, new Color(0.95f, 0.95f, 1f)));
            }

            float msgY = gy + stats.Length * rowH + 16f;
            FillRect(new Rect(panX + 20, msgY - 4, panW - 40, 1), new Color(0.25f, 0.25f, 0.35f));
            string msg = _persistent.TotalVictories == 0
                ? "尚无通关记录——深渊正等待着你的挑战。"
                : $"已完成 {_persistent.TotalVictories} 次通关，深渊的秘密正逐渐向你揭示。";
            var msgS = MkLabel(12, TextAnchor.MiddleCenter, FontStyle.Italic, new Color(0.52f, 0.52f, 0.58f));
            msgS.wordWrap = true;
            GUI.Label(new Rect(panX + 20, msgY + 4, panW - 40, 30), msg, msgS);
        }

        // ── 操控说明 ─────────────────────────────────────────────────────────

        private void DrawControls()
        {
            float panW = Mathf.Min(580f, Screen.width - 40f);
            float panX = (Screen.width - panW) * 0.5f;
            const float panY = 148f;
            float panH = Screen.height - panY - 70f;

            FillRect(new Rect(panX, panY, panW, panH), new Color(0.1f, 0.1f, 0.16f));
            FillRect(new Rect(panX, panY, panW, 2), new Color(0.45f, 0.75f, 1f, 0.4f));

            GUI.Label(new Rect(panX, panY + 8, panW, 28), "操  控  说  明",
                MkLabel(20, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.98f, 0.9f, 0.35f)));

            FillRect(new Rect(panX + 20, panY + 42, panW - 40, 1), new Color(0.28f, 0.28f, 0.38f));

            var bindings = new (string key, string action)[]
            {
                ("WASD",    "移动角色"),
                ("鼠标左键", "普通攻击（弓可蓄力，最长 1.5s，伤害 ×1 ~ ×2.5）"),
                ("Q",       "切换武器槽位（共 2 个槽位）"),
                ("R",       "武器主动技能（蓝 / 紫稀有度解锁）"),
                ("F",       "英雄主动技能"),
                ("鼠标滚轮", "缩放视角"),
                ("ESC",     "返回主菜单"),
            };

            float rowH = 36f;
            float gy   = panY + 50f;
            for (int i = 0; i < bindings.Length; i++)
            {
                float ry = gy + i * rowH;
                FillRect(new Rect(panX + 20, ry, panW - 40, rowH - 2),
                    i % 2 == 0 ? new Color(0.13f, 0.14f, 0.21f) : new Color(0.1f, 0.1f, 0.16f));

                // 按键徽章
                float chipW = 82f;
                FillRect(new Rect(panX + 28, ry + 6, chipW, rowH - 14), new Color(0.2f, 0.2f, 0.3f));
                FillRect(new Rect(panX + 28, ry + 6, chipW, 1), new Color(0.45f, 0.45f, 0.55f));
                GUI.Label(new Rect(panX + 28, ry + 6, chipW, rowH - 14), bindings[i].key,
                    MkLabel(11, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.45f, 0.75f, 1f)));

                GUI.Label(new Rect(panX + 120, ry, panW - 140, rowH), bindings[i].action,
                    MkLabel(12, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.78f, 0.78f, 0.84f)));
            }

            // 游戏技巧
            float tipY = gy + bindings.Length * rowH + 12f;
            FillRect(new Rect(panX + 20, tipY, panW - 40, 1), new Color(0.28f, 0.28f, 0.38f));
            GUI.Label(new Rect(panX + 28, tipY + 6, panW - 56, 20), "游戏技巧",
                MkLabel(12, TextAnchor.MiddleLeft, FontStyle.Bold, new Color(0.98f, 0.9f, 0.35f)));

            var tips = new[]
            {
                "· 重复拾取同一武器可自动升级，满级后转化为附魔加成",
                "· 精英敌人（第二波起出现）掉落更丰厚的奖励",
                "· 蓝/紫武器带有武器技能（R 键），合理利用可大幅提升效率",
                "· 每层通关额外获得 50 解锁货币，可在大厅解锁新英雄",
            };
            var tipS = MkLabel(11, TextAnchor.UpperLeft, FontStyle.Normal, new Color(0.65f, 0.65f, 0.7f));
            tipS.wordWrap = true;
            float ty = tipY + 28f;
            foreach (var t in tips)
            {
                GUI.Label(new Rect(panX + 28, ty, panW - 56, 22), t, tipS);
                ty += 22f;
            }
        }

        // ── 设置 ─────────────────────────────────────────────────────────────

        private void DrawSettings()
        {
            float panW = Mathf.Min(500f, Screen.width - 40f);
            float panX = (Screen.width - panW) * 0.5f;
            const float panY = 148f;
            float panH = Screen.height - panY - 70f;

            FillRect(new Rect(panX, panY, panW, panH), new Color(0.1f, 0.1f, 0.16f));
            FillRect(new Rect(panX, panY, panW, 2), new Color(0.45f, 0.75f, 1f, 0.4f));

            GUI.Label(new Rect(panX, panY + 8, panW, 28), "设  置",
                MkLabel(20, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.98f, 0.9f, 0.35f)));

            FillRect(new Rect(panX + 20, panY + 42, panW - 40, 1), new Color(0.28f, 0.28f, 0.38f));

            float fy = panY + 54f;

            // 主音量
            GUI.Label(new Rect(panX + 28, fy, 160, 28), "主音量",
                MkLabel(13, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.8f, 0.8f, 0.86f)));
            float vol    = AudioListener.volume;
            float newVol = GUI.HorizontalSlider(
                new Rect(panX + 192, fy + 9, panW - 240, 12), vol, 0f, 1f);
            if (!Mathf.Approximately(newVol, vol)) AudioListener.volume = newVol;
            GUI.Label(new Rect(panX + panW - 52, fy, 40, 28), $"{newVol * 100:0}%",
                MkLabel(11, TextAnchor.MiddleRight, FontStyle.Normal, new Color(0.65f, 0.65f, 0.7f)));

            fy += 38f;

            // 全屏模式
            GUI.Label(new Rect(panX + 28, fy, 160, 28), "全屏模式",
                MkLabel(13, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.8f, 0.8f, 0.86f)));
            bool fs    = Screen.fullScreen;
            bool newFs = GUI.Toggle(new Rect(panX + 192, fy + 5, 18, 18), fs, "");
            if (newFs != fs) Screen.fullScreen = newFs;
            GUI.Label(new Rect(panX + 216, fy, 100, 28), fs ? "开启" : "关闭",
                MkLabel(12, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.6f, 0.6f, 0.65f)));

            fy += 38f;

            // 分辨率（只读信息）
            GUI.Label(new Rect(panX + 28, fy, 160, 28), "当前分辨率",
                MkLabel(13, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.8f, 0.8f, 0.86f)));
            GUI.Label(new Rect(panX + 192, fy, panW - 220, 28), $"{Screen.width} × {Screen.height}",
                MkLabel(12, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.6f, 0.6f, 0.65f)));

            fy += 46f;
            FillRect(new Rect(panX + 20, fy, panW - 40, 1), new Color(0.22f, 0.22f, 0.32f));
            fy += 12f;

            // 存档路径
            GUI.Label(new Rect(panX + 28, fy, panW - 56, 18), "存档路径",
                MkLabel(11, TextAnchor.MiddleLeft, FontStyle.Bold, new Color(0.55f, 0.55f, 0.62f)));
            var pathS = MkLabel(9, TextAnchor.UpperLeft, FontStyle.Normal, new Color(0.4f, 0.4f, 0.46f));
            pathS.wordWrap = true;
            GUI.Label(new Rect(panX + 28, fy + 20, panW - 56, 36),
                Application.persistentDataPath, pathS);

            fy += 66f;
            FillRect(new Rect(panX + 20, fy, panW - 40, 1), new Color(0.22f, 0.22f, 0.32f));
            fy += 14f;

            // 重置存档
            float resetW = 220f;
            float resetX = panX + (panW - resetW) * 0.5f;
            FillRect(new Rect(resetX, fy, resetW, 30), new Color(0.3f, 0.1f, 0.1f));
            if (GUI.Button(new Rect(resetX, fy, resetW, 30), GUIContent.none, GUIStyle.none))
            {
                _persistent = new PersistentState();
                _persistent.Save();
                EnsureStarterUnlocked();
            }
            GUI.Label(new Rect(resetX, fy, resetW, 30), "重置存档（清空所有进度）",
                MkLabel(11, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(1f, 0.55f, 0.5f)));
        }

        // ── 开始按钮（底部常驻）──────────────────────────────────────────────

        private void DrawStartButton()
        {
            bool heroReady = _selectedHeroIndex >= 0
                          && _selectedHeroIndex < _heroes.Length
                          && _persistent.IsHeroUnlocked(_heroes[_selectedHeroIndex].heroName);

            const float btnW = 270f, btnH = 44f;
            float btnX = (Screen.width - btnW) * 0.5f;
            float btnY = Screen.height - btnH - 12f;

            FillRect(new Rect(btnX - 6, btnY - 6, btnW + 12, btnH + 12), new Color(0f, 0f, 0f, 0.35f));
            FillRect(new Rect(btnX, btnY, btnW, btnH),
                heroReady ? new Color(0.16f, 0.3f, 0.55f) : new Color(0.1f, 0.1f, 0.16f));

            if (heroReady)
            {
                FillRect(new Rect(btnX,         btnY,          btnW, 2), new Color(0.45f, 0.75f, 1f));
                FillRect(new Rect(btnX,         btnY + btnH - 2, btnW, 2), new Color(0.45f, 0.75f, 1f));
            }

            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), GUIContent.none, GUIStyle.none) && heroReady)
                StartRun();

            string label = heroReady ? "▶  开始冒险" : "前往「英雄选择」选择角色";
            GUI.Label(new Rect(btnX, btnY, btnW, btnH), label,
                MkLabel(heroReady ? 18 : 12, TextAnchor.MiddleCenter, FontStyle.Bold,
                    heroReady ? Color.white : new Color(0.4f, 0.4f, 0.46f)));
        }

        // ── GUI 工具 ─────────────────────────────────────────────────────────

        private static Texture2D _whitePixel;
        private static Texture2D WhitePixel
        {
            get
            {
                if (_whitePixel != null) return _whitePixel;
                _whitePixel = new Texture2D(1, 1);
                _whitePixel.SetPixel(0, 0, Color.white);
                _whitePixel.Apply();
                _whitePixel.hideFlags = HideFlags.HideAndDontSave;
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
    }
}
