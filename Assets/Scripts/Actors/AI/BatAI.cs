using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // Flying Bat — orbits the player, then dashes to strike.
    // Uses EnemyNavigator to route the orbit position around walls.
    // A brief scale-spin telegraph precedes each dash.
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public class BatAI : MonoBehaviour
    {
        public Transform target;
        public float orbitRadius     = 3.5f;
        public float orbitSpeed      = 3.5f;     // rad/s
        public float dashSpeed       = 16f;
        public float dashDuration    = 0.35f;
        public float dashCooldown    = 3.5f;
        public float dashDamage      = 12f;
        public float retreatSpeed    = 8f;
        public float retreatDuration = 0.8f;

        [Header("Dash Telegraph")]
        public float telegraphTime = 0.22f;      // visible "coil" before launching

        private Rigidbody2D    _rb;
        private CharacterStats _stats;
        private EnemyNavigator _nav;

        private float _orbitAngle;
        private float _dashTimer;
        private float _retreatTimer;
        private float _telegraphTimer;
        private float _lastDashTime;
        private Vector2 _dashDir;
        private bool    _hitThisDash;

        private enum State { Orbit, Telegraph, Dash, Retreat }
        private State _state = State.Orbit;

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody2D>();
            _nav = GetComponent<EnemyNavigator>() ?? gameObject.AddComponent<EnemyNavigator>();
        }

        private void Update()
        {
            if (target == null) return;

            switch (_state)
            {
                case State.Orbit:
                    _orbitAngle += orbitSpeed * Time.deltaTime;
                    if (Time.time >= _lastDashTime + dashCooldown)
                        BeginTelegraph();
                    break;

                case State.Telegraph:
                    _telegraphTimer += Time.deltaTime;
                    if (_telegraphTimer >= telegraphTime)
                        BeginDash();
                    break;

                case State.Dash:
                    _dashTimer += Time.deltaTime;
                    if (_dashTimer >= dashDuration)
                        BeginRetreat();
                    break;

                case State.Retreat:
                    _retreatTimer += Time.deltaTime;
                    if (_retreatTimer >= retreatDuration)
                        _state = State.Orbit;
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (target == null) return;

            switch (_state)
            {
                case State.Orbit:
                {
                    // Navigate toward the orbit position rather than flying straight there,
                    // so the bat routes around walls correctly.
                    Vector2 offset  = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * orbitRadius;
                    Vector2 desired = (Vector2)target.position + offset;
                    Vector2 dir     = _nav.GetMoveDirection(desired);
                    _rb.MovePosition(Vector2.MoveTowards(_rb.position,
                        _rb.position + dir * orbitSpeed * Time.fixedDeltaTime,
                        orbitSpeed * Time.fixedDeltaTime));
                    break;
                }

                case State.Telegraph:
                    // Hover in place and spin-scale to signal the incoming dash
                    transform.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(_telegraphTimer * Mathf.PI * 12f));
                    break;

                case State.Dash:
                    _rb.MovePosition(_rb.position + _dashDir * dashSpeed * Time.fixedDeltaTime);
                    break;

                case State.Retreat:
                {
                    Vector2 away = ((Vector2)transform.position - (Vector2)target.position).normalized;
                    _rb.MovePosition(_rb.position + away * retreatSpeed * Time.fixedDeltaTime);
                    break;
                }
            }
        }

        private void BeginTelegraph()
        {
            _state          = State.Telegraph;
            _telegraphTimer = 0f;
            _lastDashTime   = Time.time;
        }

        private void BeginDash()
        {
            _state        = State.Dash;
            _dashDir      = ((Vector2)target.position - (Vector2)transform.position).normalized;
            _dashTimer    = 0f;
            _hitThisDash  = false;
            transform.localScale = Vector3.one; // reset telegraph scale
        }

        private void BeginRetreat()
        {
            _state        = State.Retreat;
            _retreatTimer = 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != State.Dash || _hitThisDash) return;
            if (other.gameObject == gameObject) return;
            var d = other.GetComponent<IDamageable>();
            if (d != null)
            {
                d.TakeDamage(new DamageInfo
                {
                    Amount = dashDamage,
                    Type   = DamageType.Physical,
                    Source = gameObject
                });
                _hitThisDash = true;
                BeginRetreat();
            }
        }
    }
}
