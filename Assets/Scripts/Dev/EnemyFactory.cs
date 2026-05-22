using Game.AI;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.Dev
{
    // Enemy factory: creates enemy GameObjects and configures all components.
    // Caller is responsible for subscribing to OnDied events (count / coin drop / Destroy).
    public static class EnemyFactory
    {
        // ── Normal enemies ──────────────────────────────────────

        // Skeleton: HP25, ATK8, DEF0, SPD4.5 | basic chase, 3 coins
        public static GameObject SpawnSkeleton(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Skeleton", pos, 0.7f, new Color(0.85f, 0.85f, 0.75f),
                hp: 25f, atk: 8f, def: 0f, spd: 4.5f, parent: parent, EnemyType.Skeleton);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Skeleton;
            var ai  = go.AddComponent<ChaseAI>();
            ai.target          = player;
            ai.stoppingDistance = 0.85f;
            ai.attackInterval  = 2.0f;
            ai.contactDamage   = 8f;
            return go;
        }

        // Soldier: HP38, ATK12, DEF2, SPD4.0 | melee, 4 coins
        public static GameObject SpawnSoldier(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Soldier", pos, 0.75f, new Color(0.5f, 0.75f, 0.4f),
                hp: 38f, atk: 12f, def: 2f, spd: 4.0f, parent: parent, EnemyType.Soldier);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Soldier;
            var ai  = go.AddComponent<ChaseAI>();
            ai.target          = player;
            ai.stoppingDistance = 0.9f;
            ai.attackInterval  = 1.8f;
            ai.contactDamage   = 10f;
            return go;
        }

        // Archer: HP20, ATK0, DEF0, SPD3.0 | ranged, 4 coins
        public static GameObject SpawnArcher(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Archer", pos, 0.65f, new Color(0.6f, 0.85f, 0.35f),
                hp: 20f, atk: 0f, def: 0f, spd: 3.0f, parent: parent, EnemyType.Archer);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Archer;
            var ai  = go.AddComponent<ArcherAI>();
            ai.target           = player;
            ai.preferredDistance = 7f;
            ai.attackRange      = 9f;
            ai.attackInterval   = 2.5f;
            ai.projectileDamage = 12f;
            return go;
        }

        // Bat: HP18, DEF0, SPD7.0 | orbit dash, 3 coins
        public static GameObject SpawnBat(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Bat", pos, 0.55f, new Color(0.35f, 0.2f, 0.5f),
                hp: 18f, atk: 0f, def: 0f, spd: 7.0f, parent: parent, EnemyType.Bat);
            go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Bat;
            var ai  = go.AddComponent<BatAI>();
            ai.target        = player;
            ai.orbitRadius   = 4.5f;
            ai.orbitSpeed    = 3.5f;
            ai.dashSpeed     = 16f;
            ai.dashDuration  = 0.35f;
            ai.dashCooldown  = 4.0f;
            ai.dashDamage    = 10f;
            return go;
        }

        // Shield Guard: HP52, ATK12, DEF5, SPD3.5 | shield damage reduction, 6 coins
        public static GameObject SpawnShieldGuard(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Shield Guard", pos, 0.8f, new Color(0.3f, 0.5f, 0.8f),
                hp: 52f, atk: 12f, def: 5f, spd: 3.5f, parent: parent, EnemyType.ShieldGuard);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.ShieldGuard;
            var ai  = go.AddComponent<ShieldGuardAI>();
            ai.target          = player;
            ai.attackRange     = 1.1f;
            ai.attackInterval  = 2.0f;
            ai.contactDamage   = 12f;
            ai.shieldInterval  = 8f;
            ai.shieldDuration  = 3f;
            ai.shieldReduction = 0.8f;
            return go;
        }

        // Poison Spider: HP25, ATK10, DEF0, SPD5.5 | contact poison DoT, leaves poison pool on death, 3 coins
        public static GameObject SpawnPoisonSpider(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Poison Spider", pos, 0.5f, new Color(0.2f, 0.55f, 0.1f),
                hp: 25f, atk: 10f, def: 0f, spd: 5.5f, parent: parent, EnemyType.PoisonSpider);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.PoisonSpider;
            var ai  = go.AddComponent<PoisonSpiderAI>();
            ai.target             = player;
            ai.stoppingDistance   = 0.7f;
            ai.attackInterval     = 1.5f;
            ai.contactDamage      = 8f;
            ai.poisonTickDamage   = 5f;
            ai.poisonTicks        = 4;
            ai.poisonTickInterval = 0.6f;
            return go;
        }

        // Shadow Assassin: HP38, ATK22, DEF0, SPD5.0 | stealth + blink burst, 5 coins
        public static GameObject SpawnShadowAssassin(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Shadow Assassin", pos, 0.6f, new Color(0.2f, 0.1f, 0.3f),
                hp: 38f, atk: 22f, def: 0f, spd: 5.0f, parent: parent, EnemyType.ShadowAssassin);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.ShadowAssassin;
            var ai  = go.AddComponent<ShadowAssassinAI>();
            ai.target           = player;
            ai.preferredMinDist = 5f;
            ai.preferredMaxDist = 8f;
            ai.blinkCooldown    = 6f;
            ai.burstDamage      = 22f;
            ai.retreatDistance  = 6f;
            return go;
        }

        // Explosive Demon: HP28, ATK0, DEF0, SPD3.8 | proximity/death AoE explosion, 4 coins
        public static GameObject SpawnExplosiveDemon(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Explosive Demon", pos, 0.65f, new Color(0.9f, 0.4f, 0.1f),
                hp: 28f, atk: 0f, def: 0f, spd: 3.8f, parent: parent, EnemyType.ExplosiveDemon);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.ExplosiveDemon;
            var ai  = go.AddComponent<ExplosiveDemonAI>();
            ai.target           = player;
            ai.stoppingDistance = 0.8f;
            ai.fuseRange        = 1.2f;
            ai.fuseDuration     = 1.5f;
            ai.explosionRadius  = 3f;
            ai.explosionDamage  = 32f;
            return go;
        }

        // ── Elite ──────────────────────────────────────────────────

        // Commander: HP110, ATK16, DEF4, SPD3.5 | greatsword AoE + aura, 15 coins
        public static GameObject SpawnCommander(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Commander", pos, 0.9f, new Color(0.9f, 0.6f, 0.2f),
                hp: 110f, atk: 16f, def: 4f, spd: 3.5f, parent: parent, EnemyType.Commander);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Commander;
            var ai  = go.AddComponent<CommanderAI>();
            ai.target        = player;
            ai.attackRange   = 2.2f;
            ai.attackInterval = 2.5f;
            ai.attackDamage  = 16f;
            ai.auraRadius    = 8f;
            ai.auraCooldown  = 10f;
            ai.auraDuration  = 8f;
            ai.auraHpBonus   = 0.5f;
            ai.auraAtkBonus  = 0.3f;
            return go;
        }

        // Witch: HP80, ATK14, DEF0, SPD3.0 | staff attack + bat summon, 15 coins
        public static GameObject SpawnWitch(Vector3 pos, Transform player, Transform parent,
            System.Func<Vector3, GameObject> spawnBatCallback)
        {
            var go = MakeBase("Witch", pos, 0.75f, new Color(0.75f, 0.3f, 0.9f),
                hp: 80f, atk: 14f, def: 0f, spd: 3.0f, parent: parent, EnemyType.Witch);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Witch;
            var ai  = go.AddComponent<WitchAI>();
            ai.target              = player;
            ai.preferredDistance   = 6f;
            ai.attackRange         = 8f;
            ai.attackInterval      = 3.0f;
            ai.attackDamage        = 14f;
            ai.summonCooldown      = 8f;
            ai.summonCount         = 2;
            ai.SpawnBatCallback    = spawnBatCallback;
            return go;
        }

        // ── Boss ──────────────────────────────────────────────────

        // Hell Giant: HP320, ATK28, DEF8, SPD2.5 | lava + stomp, two-phase buff
        public static GameObject SpawnHellGiant(Vector3 pos, Transform player, Transform parent,
            System.Func<Vector3, float, float, float, GameObject> spawnLavaCallback)
        {
            var go = MakeBase("Hell Giant", pos, 1.2f, new Color(0.7f, 0.12f, 0.08f),
                hp: 320f, atk: 28f, def: 8f, spd: 2.5f, parent: parent, EnemyType.HellGiant);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.HellGiant;
            go.GetComponent<SpriteRenderer>().sortingOrder = 6;

            var ai = go.AddComponent<HellGiantAI>();
            ai.target             = player;
            ai.attackRange        = 2.0f;
            ai.attackInterval     = 2.0f;
            ai.attackDamage       = 28f;
            ai.lavaCooldown       = 6f;
            ai.lavaDamagePerSec   = 6f;
            ai.lavaLifetime       = 5f;
            ai.lavaRadius         = 2.5f;
            ai.lavaCount_P1       = 2;
            ai.lavaCount_P2       = 3;
            ai.slamCooldown       = 10f;
            ai.slamRadius         = 4f;
            ai.slamDamage         = 24f;
            ai.slamKnockback      = 12f;
            ai.slamStunDuration   = 0.8f;
            ai.phase2SpeedBonus   = 0.5f;
            ai.SpawnLavaCallback  = spawnLavaCallback;
            return go;
        }

        // Poison Shaman: HP105, ATK16, DEF0, SPD3.0 | poison bolt + buff spiders + poison puddle, 15 coins
        public static GameObject SpawnPoisonShaman(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Poison Shaman", pos, 0.85f, new Color(0.3f, 0.7f, 0.2f),
                hp: 105f, atk: 16f, def: 0f, spd: 3.0f, parent: parent, EnemyType.PoisonShaman);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.PoisonShaman;
            var ai  = go.AddComponent<PoisonShamanAI>();
            ai.target             = player;
            ai.preferredMinDist   = 5f;
            ai.preferredMaxDist   = 8f;
            ai.boltCooldown       = 3f;
            ai.boltRange          = 8f;
            ai.boltDamage         = 14f;
            ai.poisonTickDamage   = 4f;
            ai.poisonTicks        = 3;
            ai.poisonTickInterval = 0.7f;
            ai.spiderBuffCooldown = 6f;
            ai.spiderAuraRadius   = 8f;
            ai.spiderBuffDuration = 5f;
            ai.puddleCooldown     = 8f;
            return go;
        }

        // Necromancer: HP115, ATK12, DEF2, SPD2.8 | soul drain + raise skeletons, 15 coins
        public static GameObject SpawnNecromancer(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Necromancer", pos, 0.8f, new Color(0.3f, 0.1f, 0.5f),
                hp: 115f, atk: 12f, def: 2f, spd: 2.8f, parent: parent, EnemyType.Necromancer);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.Necromancer;
            var ai  = go.AddComponent<NecromancerAI>();
            ai.target          = player;
            ai.preferredMinDist = 5f;
            ai.preferredMaxDist = 8f;
            ai.drainCooldown   = 3f;
            ai.drainRange      = 7f;
            ai.drainDamage     = 14f;
            ai.drainHealRatio  = 0.6f;
            ai.summonCooldown  = 10f;
            ai.summonCount_P1  = 1;
            ai.summonCount_P2  = 2;
            return go;
        }

        // Frost Lich: HP480, ATK20, DEF5, SPD1.8 | ranged frost + ice volley + frost nova, phase 2 cooldown reduction
        public static GameObject SpawnFrostLich(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Frost Lich", pos, 1.1f, new Color(0.45f, 0.75f, 1f),
                hp: 480f, atk: 20f, def: 5f, spd: 1.8f, parent: parent, EnemyType.FrostLich);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.FrostLich;
            go.GetComponent<SpriteRenderer>().sortingOrder = 6;

            var ai = go.AddComponent<FrostLichAI>();
            ai.target              = player;
            ai.attackRange         = 8f;
            ai.attackInterval      = 3f;
            ai.attackDamage        = 16f;
            ai.novaCooldown        = 5f;
            ai.novaRadius          = 3.5f;
            ai.novaDamage          = 16f;
            ai.novaStun            = 0.5f;
            ai.volleyCooldown      = 4f;
            ai.volleyDamage        = 12f;
            ai.volleyRange         = 10f;
            ai.volleyCount_P1      = 3;
            ai.volleyCount_P2      = 5;
            ai.preferredMinDist    = 4f;
            ai.preferredMaxDist    = 7f;
            ai.phase2CdMultiplier  = 0.7f;
            return go;
        }

        // Chaos Lord: HP700, ATK35, DEF12, SPD3.0 | melee sweep + chaos burst + summon legion, phase 2 speed boost
        public static GameObject SpawnChaosLord(Vector3 pos, Transform player, Transform parent)
        {
            var go = MakeBase("Chaos Lord", pos, 1.4f, new Color(0.5f, 0.1f, 0.7f),
                hp: 700f, atk: 35f, def: 12f, spd: 3.0f, parent: parent, EnemyType.ChaosLord);
            var tag = go.AddComponent<EnemyTag>(); tag.type = EnemyType.ChaosLord;
            go.GetComponent<SpriteRenderer>().sortingOrder = 6;

            var ai = go.AddComponent<ChaosLordAI>();
            ai.target               = player;
            ai.attackRange          = 2.5f;
            ai.attackInterval       = 1.5f;
            ai.attackDamage         = 35f;
            ai.attackKnockback      = 10f;
            ai.burstCooldown        = 7f;
            ai.burstRadius          = 5f;
            ai.burstDamage          = 24f;
            ai.burstKnockback       = 14f;
            ai.burstPulses          = 3;
            ai.summonCooldown       = 12f;
            ai.summonCount_P1       = 2;
            ai.summonCount_P2       = 3;
            ai.phase2SpeedBonus     = 0.8f;
            ai.phase2BurstCooldown  = 4.5f;
            ai.phase2SummonCooldown = 8f;
            return go;
        }

        // ── Lava pool ─────────────────────────────────────────────

        public static GameObject SpawnLavaPool(Vector3 pos, float dps, float lifetime, float radius,
            Transform parent, GameObject owner)
        {
            var go = new GameObject("LavaPool");
            go.transform.SetParent(parent, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GetPoolSprite();
            sr.color        = new Color(1f, 0.5f, 0.12f, 0.9f);
            sr.sortingOrder = 3;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.5f;  // localScale放大处理实际范围
            col.isTrigger = true;

            var lava         = go.AddComponent<LavaPool>();
            lava.damagePerSecond = dps;
            lava.lifetime        = lifetime;
            lava.owner           = owner;
            return go;
        }

        // Poison puddle: green, reuses LavaPool component, true damage
        public static GameObject SpawnPoisonPool(Vector3 pos, float dps, float lifetime, float radius,
            Transform parent, GameObject owner)
        {
            var go = SpawnLavaPool(pos, dps, lifetime, radius, parent, owner);
            if (go != null) go.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.85f, 0.18f, 0.85f);
            return go;
        }

        // 圆形地面池贴图（径向渐变 + 噪点），可被 SpriteRenderer.color 着色为熔岩/毒液
        private static Sprite _poolSprite;
        private static Sprite GetPoolSprite()
        {
            if (_poolSprite != null) return _poolSprite;
            const int sz = 64;
            const float cx = (sz - 1) * 0.5f, cy = (sz - 1) * 0.5f, rad = sz * 0.5f;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[sz * sz];
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / rad;
                if (d >= 1f) { px[y * sz + x] = new Color32(0, 0, 0, 0); continue; }

                // 噪点（多频正弦叠加）让池体不均匀，模拟翻涌的熔岩纹理
                float n = Mathf.Sin(x * 0.9f + y * 0.5f) * Mathf.Sin(x * 0.35f - y * 0.7f);
                float value = Mathf.Clamp01(0.62f + n * 0.22f + (1f - d) * 0.30f);
                // 边缘柔化淡出，中心更不透明
                float alpha = Mathf.Clamp01((1f - d) * 1.6f) * 0.95f;

                byte v = (byte)(value * 255f);
                px[y * sz + x] = new Color32(v, v, v, (byte)(alpha * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _poolSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
            return _poolSprite;
        }

        // ── Internal utilities ────────────────────────────────────

        private static GameObject MakeBase(string name, Vector3 pos, float size, Color color,
            float hp, float atk, float def, float spd, Transform parent,
            EnemyType spriteType = (EnemyType)(-1))
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(size, size, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            var customSprite = (int)spriteType >= 0 ? EnemySprites.Get(spriteType) : null;
            sr.sprite       = customSprite != null ? customSprite : GetSquareSprite();
            sr.color        = customSprite != null ? Color.white : color;
            sr.sortingOrder = 5;

            if ((int)spriteType >= 0 && customSprite != null)
            {
                var facing = go.AddComponent<EnemyFacing>();
                facing.SetSprites(customSprite, EnemySprites.GetBack(spriteType));
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;

            var col    = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            var stats = go.AddComponent<CharacterStats>();
            stats.SetBase(StatType.MaxHP,       hp);
            stats.SetBase(StatType.Attack,      atk);
            stats.SetBase(StatType.Defense,     def);
            stats.SetBase(StatType.MoveSpeed,   spd * 0.8f);
            stats.SetBase(StatType.AttackSpeed, 1f);

            go.AddComponent<Health>();
            return go;
        }

        private static Sprite _square;
        private static Sprite GetSquareSprite()
        {
            if (_square != null) return _square;
            const int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var px = new Color[sz * sz];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
            return _square;
        }
    }
}
