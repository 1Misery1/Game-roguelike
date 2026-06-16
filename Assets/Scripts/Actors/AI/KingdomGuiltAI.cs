using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Player;
using UnityEngine;
namespace Game.AI
{
    /// 隐藏 Boss「王国之罪」—— 王国累积罪行具现。
    ///
    /// 与混沌领主完全不同的战斗主题：
    ///   王令封锁 (DecreeWalls)     — 3 道按王令降下的封锁墙，True 伤害
    ///   亡魂控诉 (Wraiths)          — 4 个静止亡魂在玩家周围脉冲伤害 6 秒
    ///   加冕之失 (CrownToll)        — 单次大圆 True 伤害，伤害随玩家虚空污染值放大
    ///
    /// 受击 50% HP 触发 Phase 2：移速 +0.5，全 CD ×0.7。
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class KingdomGuiltAI : MonoBehaviour
    {
        public Transform target;

        [Header("Melee")]
        public float meleeRange     = 2.2f;
        public float meleeDamage    = 35f;
        public float meleeKnockback = 8f;
        public float meleeCooldown  = 1.8f;

        [Header("Decree Walls")]
        public float decreeCooldown      = 8f;
        public float decreeWallDamage    = 35f;   // True
        public float decreeWallWidth     = 1.6f;
        public float decreeWindupSeconds = 1.0f;

        [Header("Wraith Indictment")]
        public float wraithCooldown       = 11f;
        public int   wraithCount          = 4;
        public float wraithDamagePerPulse = 16f;
        public float wraithRadius         = 1.5f;
        public float wraithLifetime       = 6f;

        [Header("Encircle — 围绕玩家的延时环形范围攻击")]
        public float encircleCooldown  = 5.5f;   // 主要攻击：高频
        public int   encircleCount     = 6;      // 围成一圈的爆点数（留出可冲刺的缝隙）
        public float encircleRing      = 3.0f;   // 爆点距玩家中心的半径
        public float encircleAoeRadius = 1.15f;  // 每个爆点的伤害半径（缝隙约 0.7，可穿）
        public float encircleWindup    = 1.05f;  // 预警延时（玩家可躲）
        public float encircleDamage    = 28f;

        [Header("Crown Toll (corruption-scaled)")]
        public float crownTollCooldown        = 14f;
        public float crownTollBaseDamage      = 25f;
        public float crownTollCorruptionMult  = 3f;
        public float crownTollRadius          = 8f;
        public float crownTollWindup          = 1.6f;

        [Header("Phase 2 (≤50% HP)")]
        public float phase2SpeedBonus    = 0.5f;
        public float phase2CooldownScale = 0.7f;

        Rigidbody2D    _rb;
        CharacterStats _stats;
        Health         _health;
        float _lastMelee    = -100f;
        float _lastDecree   = -100f;
        float _lastWraith   = -100f;
        float _lastCrown    = -100f;
        float _lastEncircle = -100f;
        bool  _busy;
        bool  _phase2;

        void Awake()
        {
            _rb     = GetComponent<Rigidbody2D>();
            _stats  = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            // 最终 Boss 钉在房间中心：改为运动学刚体 → 不被击退、也不主动移动
            if (_rb != null) { _rb.bodyType = RigidbodyType2D.Kinematic; _rb.velocity = Vector2.zero; }
        }

        void Update()
        {
            if (target == null) return;
            CheckPhase2();
            if (_busy) return;

            float k = _phase2 ? phase2CooldownScale : 1f;
            if      (Time.time >= _lastEncircle + encircleCooldown * k)
                StartCoroutine(CastEncircle());
            else if (Time.time >= _lastCrown  + crownTollCooldown * k)
                StartCoroutine(CastCrownToll());
            else if (Time.time >= _lastDecree + decreeCooldown    * k)
                StartCoroutine(CastDecreeWalls());
            else if (Time.time >= _lastWraith + wraithCooldown    * k)
                CastWraiths();

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= meleeRange && Time.time >= _lastMelee + meleeCooldown)
                DoMelee();
        }

        // 固定 Boss：不追击、不移动（站在房间中心释放范围攻击）
        void FixedUpdate() { }

        void CheckPhase2()
        {
            if (_phase2 || _health == null) return;
            if (_health.Current <= _health.Max * 0.5f)
            {
                _phase2 = true;
                _stats.AddModifier(new StatModifier(StatType.MoveSpeed, ModifierOp.Flat,
                    phase2SpeedBonus, "kg_phase2"));
                GameSignals.PostBanner("Silence is our shield.");
            }
        }

        void DoMelee()
        {
            _lastMelee = Time.time;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, meleeRange))
            {
                if (col.gameObject == gameObject) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo {
                    Amount = meleeDamage, Type = DamageType.Physical, Source = gameObject
                });
                ApplyKnockback(col, meleeKnockback);
            }
        }

        IEnumerator CastDecreeWalls()
        {
            _busy = true;
            _lastDecree = Time.time;
            GameSignals.PostBanner("By royal decree — seal it…");

            float baseX = target != null ? target.position.x : 0f;
            float[] xs  = { baseX - 3.5f, baseX, baseX + 3.5f };
            var warnings = new List<GameObject>();
            foreach (var x in xs)
                warnings.Add(SpawnRect(new Vector2(x, transform.position.y),
                    decreeWallWidth, 6f, new Color(0.92f, 0.78f, 0.40f, 0.32f)));

            yield return new WaitForSeconds(decreeWindupSeconds);
            foreach (var w in warnings) if (w != null) Destroy(w);

            foreach (var x in xs)
            {
                var wall = SpawnRect(new Vector2(x, transform.position.y),
                    decreeWallWidth, 6f, new Color(0.30f, 0.18f, 0.05f, 0.85f));
                DamageBox(new Vector2(x, transform.position.y),
                    decreeWallWidth, 6f, decreeWallDamage);
                if (wall != null) Destroy(wall, 0.45f);
            }
            _busy = false;
        }

        void DamageBox(Vector2 center, float w, float h, float dmg)
        {
            foreach (var col in Physics2D.OverlapBoxAll(center, new Vector2(w, h), 0f))
            {
                if (col.gameObject == gameObject) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo {
                    Amount = dmg, Type = DamageType.True, Source = gameObject, BypassIFrames = true
                });
            }
        }

        void CastWraiths()
        {
            _lastWraith = Time.time;
            GameSignals.PostBanner("Remember our names.");
            if (target == null) return;

            for (int i = 0; i < wraithCount; i++)
            {
                float ang   = i * Mathf.PI * 2f / wraithCount + Random.value * 0.5f;
                Vector2 pos = (Vector2)target.position +
                              new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 3.5f;
                var go = new GameObject("Wraith");
                go.transform.SetParent(transform.parent, true);
                go.transform.position   = pos;
                go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = MakeSquareSprite();
                sr.color        = new Color(0.85f, 0.85f, 0.95f, 0.72f);
                sr.sortingOrder = 4;

                var w = go.AddComponent<KingdomGuiltWraith>();
                w.damagePerPulse = wraithDamagePerPulse;
                w.radius         = wraithRadius;
                w.lifetime       = wraithLifetime;
                w.owner          = gameObject;
            }
        }

        IEnumerator CastCrownToll()
        {
            _busy = true;
            _lastCrown = Time.time;
            int corruption = GameManager.Instance?.Run?.VoidCorruption ?? 0;
            float dmg = crownTollBaseDamage + corruption * crownTollCorruptionMult;

            GameSignals.PostBanner($"You crowned us too.  (Coronation's Loss charging → {dmg:0} true damage)");

            // 圆形预警（撑大+闪烁）→ 命中闪光（特效）
            AoeTelegraph.Spawn(transform.position, crownTollRadius, crownTollWindup, 0.4f,
                new Color(1f, 0.5f, 0.15f, 0.45f), new Color(1f, 0.8f, 0.3f, 0.85f), 6);
            yield return new WaitForSeconds(crownTollWindup);
            Flash(transform.position, crownTollRadius);

            foreach (var col in Physics2D.OverlapCircleAll(transform.position, crownTollRadius))
            {
                if (col.gameObject == gameObject) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo {
                    Amount = dmg, Type = DamageType.True, Source = gameObject, BypassIFrames = true
                });
            }
            _busy = false;
        }

        // 围绕玩家的「延时环形范围攻击」：在玩家四周布下一圈预警圆，windup 后同时炸开（带命中闪光）。
        // 中心留有安全位 → 玩家可向圈内/缝隙处闪避。Boss 全程不动。
        IEnumerator CastEncircle()
        {
            _busy = true;
            _lastEncircle = Time.time;
            GameSignals.PostBanner("The realm closes in.");
            if (target == null) { _busy = false; yield break; }

            Vector2 center = target.position;            // 起手时锁定玩家位置
            var spots = new List<Vector2>(encircleCount);
            float baseAng = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < encircleCount; i++)
            {
                float ang = baseAng + i * Mathf.PI * 2f / encircleCount;
                spots.Add(center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * encircleRing);
            }

            // 预警 + 特效（橙红撑大闪烁）
            foreach (var s in spots)
                AoeTelegraph.Spawn(s, encircleAoeRadius, encircleWindup, 0.35f,
                    new Color(0.95f, 0.45f, 0.15f, 0.5f), new Color(1f, 0.85f, 0.4f, 0.85f), 6);

            yield return new WaitForSeconds(encircleWindup);

            foreach (var s in spots)
            {
                Flash(s, encircleAoeRadius);             // 命中闪光特效
                foreach (var col in Physics2D.OverlapCircleAll(s, encircleAoeRadius))
                {
                    if (col.gameObject == gameObject) continue;
                    col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo {
                        Amount = encircleDamage, Type = DamageType.Magical, Source = gameObject
                    });
                }
            }
            _busy = false;
        }

        // 命中瞬间的明亮闪光特效（warnTime=0 → 直接命中色并快速淡出）
        static void Flash(Vector2 pos, float radius)
        {
            AoeTelegraph.Spawn(pos, radius, 0f, 0.22f,
                new Color(1f, 0.96f, 0.72f, 0.9f), new Color(1f, 0.96f, 0.72f, 0.9f), 8);
        }

        void ApplyKnockback(Collider2D col, float force)
        {
            var rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || col.gameObject == gameObject) return;
            Vector2 dir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }

        // ── Visual helpers ────────────────────────────────────────────────
        static Sprite _square;
        static Sprite MakeSquareSprite()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(2, 2);
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            return _square;
        }

        GameObject SpawnRect(Vector2 pos, float w, float h, Color col)
        {
            var go = new GameObject("KG_Effect");
            go.transform.SetParent(transform.parent, true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeSquareSprite();
            sr.color        = col;
            sr.sortingOrder = 4;
            return go;
        }
    }

    /// 「亡魂控诉」召唤出的静止亡魂；每秒对范围内造成魔法伤害
    public class KingdomGuiltWraith : MonoBehaviour
    {
        public float damagePerPulse = 16f;
        public float radius         = 1.5f;
        public float lifetime       = 6f;
        public GameObject owner;

        float _life;
        float _pulse;
        const float PulseInterval = 1f;

        void Update()
        {
            _life += Time.deltaTime;
            if (_life >= lifetime) { Destroy(gameObject); return; }

            _pulse += Time.deltaTime;
            if (_pulse >= PulseInterval)
            {
                _pulse -= PulseInterval;
                foreach (var col in Physics2D.OverlapCircleAll(transform.position, radius))
                {
                    if (col.gameObject == gameObject) continue;
                    if (owner != null && col.gameObject == owner) continue;
                    col.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo {
                        Amount = damagePerPulse, Type = DamageType.Magical, Source = gameObject
                    });
                }
            }
        }
    }
}
