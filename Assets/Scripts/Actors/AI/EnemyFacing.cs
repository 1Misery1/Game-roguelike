using UnityEngine;

namespace Game.AI
{
    // Manages which direction an enemy faces: updates the SpriteRenderer (flipX + front/back sprite)
    // toward the player with a turn cooldown. Exposes IsBackExposed() for backstab detection.
    public class EnemyFacing : MonoBehaviour
    {
        public float turnCooldown = 0.6f;

        public Vector2 FacingDir { get; private set; } = Vector2.down;

        private float          _nextTurnTime;
        private SpriteRenderer _sr;
        private Sprite         _frontSprite;
        private Sprite         _backSprite;
        private Transform      _target;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            var p = GameObject.FindWithTag("Player");
            if (p != null) _target = p.transform;
            ApplyFacing();
            _nextTurnTime = Time.time + turnCooldown;
        }

        public void SetSprites(Sprite front, Sprite back)
        {
            _frontSprite = front;
            _backSprite  = back;
            ApplyFacing();
        }

        private void Update()
        {
            if (_target == null || Time.time < _nextTurnTime) return;
            float dx = _target.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f) return; // 同一垂直线，保持当前朝向
            FacingDir     = dx > 0f ? Vector2.right : Vector2.left;
            _nextTurnTime = Time.time + turnCooldown;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (_sr == null) return;
            _sr.flipX = FacingDir.x < 0f;
            if (_frontSprite != null)
                _sr.sprite = _frontSprite;
        }

        // True when the given world position is behind this entity
        // (attacker is in the opposite direction of facing → backstab opportunity).
        public bool IsBackExposed(Vector2 worldPos)
        {
            Vector2 toAttacker = ((Vector2)worldPos - (Vector2)transform.position).normalized;
            return Vector2.Dot(FacingDir, toAttacker) < -0.5f;
        }
    }
}
