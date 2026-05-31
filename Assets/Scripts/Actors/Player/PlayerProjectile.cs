using System.Collections.Generic;
using Game.AI;
using Game.Combat;
using Game.Data;
using UnityEngine;
using Game.Art;
using Game.VFX;
namespace Game.Player
{
    // 玩家普攻投射物：飞行至命中敌人/墙体或达到最大射程时再结算伤害
    // 单体模式：碰到首个敌人即造成伤害并销毁
    // AOE模式 (aoeRadius>0)：碰到敌人/墙或飞完射程时，在落点 OverlapCircleAll 群体伤害
    public class PlayerProjectile : MonoBehaviour
    {
        private ProjectileType _type;
        private Vector2    _dir;
        private float      _speed;
        private float      _maxRangeSq;
        private Vector2    _startPos;
        private DamageInfo _damage;
        private float      _aoeRadius;
        private GameObject _source;
        private bool       _detonated;
        private bool       _piercing;
        private HashSet<GameObject> _pierced;

        // 真贴图视觉预制体缓存（缺失则回退到程序化精灵）
        private static readonly Dictionary<ProjectileType, GameObject> _visualCache =
            new Dictionary<ProjectileType, GameObject>();

        private static GameObject LoadVisual(ProjectileType t)
        {
            if (_visualCache.TryGetValue(t, out var p)) return p;
            string path = t == ProjectileType.Arrow    ? "FX/Arrow/Arrow"
                        : t == ProjectileType.MagicOrb  ? "FX/MagicOrb/MagicOrb"
                        : null;
            p = path != null ? Resources.Load<GameObject>(path) : null;
            _visualCache[t] = p;
            return p;
        }

        public static PlayerProjectile Spawn(
            ProjectileType type, Vector3 startPos, Vector2 dir,
            float speed, float maxRange, float size, Transform parent,
            DamageInfo damage, float aoeRadius, GameObject source,
            bool piercing = false)
        {
            if (dir == Vector2.zero) dir = Vector2.right;
            dir = dir.normalized;

            var go = new GameObject("PProj_" + type);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position   = startPos;
            go.transform.localScale = Vector3.one * size;
            go.transform.rotation   = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var visual = LoadVisual(type);
            if (visual != null)
            {
                var v = Instantiate(visual, go.transform);
                v.transform.localPosition = Vector3.zero;
                v.transform.localRotation = Quaternion.identity;
                v.transform.localScale    = Vector3.one;
            }
            else
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = SkillSprites.GetProjectile(type);
                sr.color        = Color.white;
                sr.sortingOrder = 7;
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.35f;
            col.isTrigger = true;

            var proj = go.AddComponent<PlayerProjectile>();
            proj._type       = type;
            proj._dir        = dir;
            proj._speed      = speed;
            proj._maxRangeSq = maxRange * maxRange;
            proj._startPos   = startPos;
            proj._damage     = damage;
            proj._aoeRadius  = aoeRadius;
            proj._source     = source;
            proj._piercing   = piercing;
            if (piercing) proj._pierced = new HashSet<GameObject>();
            return proj;
        }

        private const float HitRadius = 0.25f;  // 命中敌人的轮询半径

        private void Update()
        {
            if (_detonated) return;
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

            // 命中敌人：用 OverlapCircle 轮询，避免 kinematic-kinematic 刚体间
            // 默认不触发 OnTriggerEnter2D 导致投射物“穿怪不掉血”
            var cols = Physics2D.OverlapCircleAll(transform.position, HitRadius);
            foreach (var c in cols)
            {
                if (c.gameObject == _source) continue;
                if (c.GetComponent<EnemyTag>() == null) continue;

                // 穿透模式：每个敌人只结算一次，结算后继续飞行，不销毁
                if (_piercing)
                {
                    if (_pierced.Add(c.gameObject))
                        c.GetComponent<IDamageable>()?.TakeDamage(_damage);
                    continue;
                }

                if (_aoeRadius > 0f) { Detonate(); return; }
                c.GetComponent<IDamageable>()?.TakeDamage(_damage);
                Destroy(gameObject);
                return;
            }

            // 达到最大射程：单体直接销毁，AOE 在最终位置爆开
            if (((Vector2)transform.position - _startPos).sqrMagnitude >= _maxRangeSq)
            {
                if (_aoeRadius > 0f) Detonate();
                else Destroy(gameObject);
                return;
            }

            // 撞墙
            if (Physics2D.OverlapCircle(transform.position, 0.18f, 1 << 9) != null)
            {
                if (_aoeRadius > 0f) Detonate();
                else Destroy(gameObject);
            }
        }

        private void Detonate()
        {
            if (_detonated) return;
            _detonated = true;

            // 爆炸视觉：外缘半径 = 实际伤害半径(_aoeRadius)
            if (_type == ProjectileType.MagicOrb)
                MagicBlastFX.Spawn(transform.position, _aoeRadius, transform.parent);

            var mask = ~(1 << 9); // 排除墙体层
            var cols = Physics2D.OverlapCircleAll(transform.position, _aoeRadius, mask);
            foreach (var col in cols)
            {
                if (col.gameObject == _source) continue;
                if (col.GetComponent<EnemyTag>() == null) continue;
                col.GetComponent<IDamageable>()?.TakeDamage(_damage);
            }
            Destroy(gameObject);
        }
    }
}
