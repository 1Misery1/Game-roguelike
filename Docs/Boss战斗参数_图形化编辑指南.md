# Boss 战斗参数 · 图形化编辑指南

3 个楼层 Boss + 隐藏 Boss「王国之罪」的核心数值已全部数据化。Inspector 直接调，无需改 C#。

---

## 入口

```
Assets/Resources/Bosses/
   ├── HellGiant.asset     — Floor 1 Boss
   ├── FrostLich.asset     — Floor 2 Boss
   ├── ChaosLord.asset     — Floor 3 Boss
   └── KingdomGuilt.asset  — 隐藏 Boss「王国之罪」
```

双击任一 `.asset` → Inspector 编辑。运行时由 `BossStatsRegistry` 缓存查表。

---

## Inspector 字段

| 字段 | 作用 |
|---|---|
| `Boss Id` | 内部 ID（必填，不可与其他 Boss 重复）。代码按此字符串匹配：`hell_giant` / `frost_lich` / `chaos_lord` / `kingdom_guilt` |
| `Display Name` | HUD 显示名 |
| `Max HP` | 基础 HP（不含 FloorScale 周目倍率） |
| `Attack` | 基础攻击 |
| `Defense` | 基础防御 |
| `Move Speed` | 基础移动速度 |
| `Visual Scale` | 整体缩放（白盒尺寸；ChaosLord 默认 1.4，KingdomGuilt 默认 2.24 ≈ ChaosLord × 1.6） |
| `Tint Color` | 精灵着色 |

### 当前默认值（与原代码 1:1）

| Boss | HP | ATK | DEF | SPD | Scale |
|---|---|---|---|---|---|
| Hell Giant | 320 | 28 | 8 | 2.5 | 1.2 |
| Frost Lich | 480 | 20 | 5 | 1.8 | 1.1 |
| Chaos Lord | 700 | 35 | 12 | 3.0 | 1.4 |
| **王国之罪** | **1750** | **88** | **18** | 3.0 | **2.24** |

---

## 容错

- 任意 `.asset` 缺失 / `bossId` 不匹配 → 回退到代码内置默认值（与原硬编码完全一致）
- 删除整个 `Bosses/` 目录也不会崩溃

---

## 还未数据化的内容

技能层参数（CD / 半径 / 伤害值 / 阶段 2 行为等）仍在各自 AI 脚本里：

- `HellGiantAI`：岩浆池 CD/半径/数量、地震伤害/击退/眩晕
- `FrostLichAI`：射击 CD、霜爆 nova、冰刺齐射数量
- `ChaosLordAI`：混沌爆发、召唤军团、阶段 2 CD
- `KingdomGuiltAI`：王令封锁、亡魂控诉、加冕之失（按虚空污染放大）

如需技能也数据化，可在 `BossStatsData` 上扩字段，或为每个 Boss 做单独的 `BossSkillData`。

---

## 重置回默认

```
执行 Assets/Editor/Tools/CreateBossStatsDefault.cs → Execute
```

会重新生成 4 个资产。

---

## 相关文件

| 文件 | 作用 |
|---|---|
| `Assets/Scripts/Dev/BossStatsData.cs` | SO 数据定义 |
| `Assets/Scripts/Dev/BossStatsRegistry.cs` | 按 bossId 查表（带缓存） |
| `Assets/Scripts/Dev/EnemyFactory.cs` | SpawnHellGiant / SpawnFrostLich / SpawnChaosLord 读 SO |
| `Assets/Scripts/Dev/GameBootstrap.cs` | `SpawnHiddenBoss` 用 KingdomGuilt SO 显式覆盖统计 |
| `Assets/Editor/Tools/CreateBossStatsDefault.cs` | 重置工具 |

---

最后更新：2026-05-25
