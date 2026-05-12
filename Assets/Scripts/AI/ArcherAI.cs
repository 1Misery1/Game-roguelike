using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 腐败弓箭手：保持距离，对玩家进行远程射击
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class ArcherAI : MonoBehaviour
    {
        public Transform target;
        public float preferredDistance = 6f;   // 保持距离
        public float attackRange       = 9f;   // 射程
        public float attackInterval    = 2.0f;
        public float projectileDamage  = 15f;

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private float _lastAttackTime;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody2D>();
            _stats = GetComponent<CharacterStats>();
        }

        private void FixedUpdate()
        {
            if (target == null) return;
            float dist  = Vector2.Distance(transform.position, target.position);
            Vector2 dir = ((Vector2)target.position - _rb.position).normalized;
            float speed = _stats.Get(StatType.MoveSpeed);

            // 超出保持距离则靠近，太近则后退
            if (dist > preferredDistance + 1.5f)
                _rb.MovePosition(_rb.position + dir * speed * Time.fixedDeltaTime);
            else if (dist < preferredDistance - 1.5f)
                _rb.MovePosition(_rb.position - dir * speed * Time.fixedDeltaTime);
        }

        private void Update()
        {
            if (target == null) return;
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackRange && Time.time >= _lastAttackTime + attackInterval)
            {
                Shoot();
                _lastAttackTime = Time.time;
            }
        }

        private void Shoot()
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float atk   = _stats.Get(StatType.Attack);
            EnemyProjectile.Spawn(
                transform.position, dir, speed: 8f, attackRange,
                new DamageInfo { Amount = projectileDamage + atk, Type = DamageType.Physical, Source = gameObject },
                new Color(0.95f, 0.8f, 0.2f), size: 0.25f, transform.parent);
        }
    }
}
