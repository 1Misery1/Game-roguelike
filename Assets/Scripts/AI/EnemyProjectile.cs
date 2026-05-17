using Game.Combat;
using Game.Dev;
using UnityEngine;

namespace Game.AI
{
    public class EnemyProjectile : MonoBehaviour
    {
        private Vector2    _dir;
        private float      _speed;
        private float      _maxRangeSq;
        private DamageInfo _damageInfo;
        private System.Action<IDamageable> _onHit;
        private Vector2    _startPos;

        public static EnemyProjectile Spawn(
            Vector3 startPos, Vector2 dir, float speed, float maxRange,
            DamageInfo dmg, ProjectileType projType, float size, Transform parent,
            System.Action<IDamageable> onHit = null)
        {
            var go = new GameObject("EnemyProjectile");
            go.transform.SetParent(parent, true);
            go.transform.position   = startPos;
            go.transform.localScale = new Vector3(size, size, 1f);
            go.transform.rotation   = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SkillSprites.GetProjectile(projType);
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.5f;
            col.isTrigger = true;

            var proj         = go.AddComponent<EnemyProjectile>();
            proj._dir        = dir.normalized;
            proj._speed      = speed;
            proj._maxRangeSq = maxRange * maxRange;
            proj._damageInfo = dmg;
            proj._onHit      = onHit;
            proj._startPos   = startPos;

            return proj;
        }

        private void Update()
        {
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
            if (((Vector2)transform.position - _startPos).sqrMagnitude >= _maxRangeSq)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_damageInfo.Source != null && other.gameObject == _damageInfo.Source) return;
            if (other.GetComponent<EnemyTag>() != null) return;

            var d = other.GetComponent<IDamageable>();
            if (d == null) return;

            d.TakeDamage(_damageInfo);
            _onHit?.Invoke(d);
            Destroy(gameObject);
        }
    }
}
