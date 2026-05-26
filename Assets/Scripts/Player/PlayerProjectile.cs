using Game.AI;
using Game.Combat;
using Game.Data;
using Game.Dev;
using UnityEngine;

namespace Game.Player
{
    // 玩家普攻投射物：飞行至命中敌人/墙体或达到最大射程时再结算伤害
    // 单体模式：碰到首个敌人即造成伤害并销毁
    // AOE模式 (aoeRadius>0)：碰到敌人/墙或飞完射程时，在落点 OverlapCircleAll 群体伤害
    public class PlayerProjectile : MonoBehaviour
    {
        private Vector2    _dir;
        private float      _speed;
        private float      _maxRangeSq;
        private Vector2    _startPos;
        private DamageInfo _damage;
        private float      _aoeRadius;
        private GameObject _source;
        private bool       _detonated;

        public static PlayerProjectile Spawn(
            ProjectileType type, Vector3 startPos, Vector2 dir,
            float speed, float maxRange, float size, Transform parent,
            DamageInfo damage, float aoeRadius, GameObject source)
        {
            if (dir == Vector2.zero) dir = Vector2.right;
            dir = dir.normalized;

            var go = new GameObject("PProj_" + type);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position   = startPos;
            go.transform.localScale = Vector3.one * size;
            go.transform.rotation   = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SkillSprites.GetProjectile(type);
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.35f;
            col.isTrigger = true;

            var proj = go.AddComponent<PlayerProjectile>();
            proj._dir        = dir;
            proj._speed      = speed;
            proj._maxRangeSq = maxRange * maxRange;
            proj._startPos   = startPos;
            proj._damage     = damage;
            proj._aoeRadius  = aoeRadius;
            proj._source     = source;
            return proj;
        }

        private void Update()
        {
            if (_detonated) return;
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_detonated) return;
            if (other.gameObject == _source) return;

            // 玩家投射物只关心带 EnemyTag 的敌人；忽略其他触发器/拾取物
            if (other.GetComponent<EnemyTag>() == null) return;

            if (_aoeRadius > 0f)
            {
                Detonate();
            }
            else
            {
                other.GetComponent<IDamageable>()?.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }

        private void Detonate()
        {
            if (_detonated) return;
            _detonated = true;

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
