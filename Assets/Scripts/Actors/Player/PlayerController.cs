using Game.Combat;
using Game.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats), typeof(PlayerWeaponHandler))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Color attackFlashColor = Color.yellow;
        [SerializeField] private Color skillFlashColor  = new Color(0.5f, 0.5f, 1f);

        private Rigidbody2D            _rb;
        private CharacterStats         _stats;
        private PlayerWeaponHandler    _weapons;
        private HeroActiveSkillHandler _heroSkill;
        private SpriteRenderer         _sr;
        private Color _baseColor;
        private Color _flashColor;
        private float _flashUntil;
        private float _stunUntil;

        // 只保留正面 sprite，朝向通过 flipX 决定
        private Sprite _frontSprite;

        // 由 FixedUpdate 中的水平移动输入驱动，静止时保持上次方向
        private bool _facingRight = true;

        // 弓蓄力状态
        private bool  _chargingBow;
        private float _chargeStart;

        /// GameBootstrap 调用：传入英雄正面 sprite（back 参数保留签名兼容，但不使用）
        public void SetFacingSprites(Sprite front, Sprite back)
        {
            _frontSprite = front;
            if (_sr != null && front != null)
                _sr.sprite = front;
        }

        public void ApplyStun(float duration)
        {
            _stunUntil = Mathf.Max(_stunUntil, Time.time + duration);
        }

        private void Awake()
        {
            _rb            = GetComponent<Rigidbody2D>();
            _rb.gravityScale   = 0f;
            _rb.freezeRotation = true;
            _stats     = GetComponent<CharacterStats>();
            _weapons   = GetComponent<PlayerWeaponHandler>();
            _heroSkill = GetComponent<HeroActiveSkillHandler>();
            _sr        = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        private void Update()
        {
            // 对话进行中：冻结玩家输入（鼠标左键同时用于推进对话，必须屏蔽以免误触发攻击）
            if (Game.Core.GameSignals.DialogueActive) return;

            var kb    = Keyboard.current;
            var mouse = Mouse.current;

            bool stunned = Time.time < _stunUntil;
            Vector2 aimDir = GetAimDirection();

            // 水平朝向由 FixedUpdate 更新，这里只同步 sprite
            ApplyFacing();

            bool isBow = _weapons?.ActiveWeapon?.Data?.category == WeaponCategory.Bow;

            bool attackPressed;
            if (isBow && !stunned)
            {
                HandleBowCharge(aimDir, kb, mouse);
                attackPressed = false;
            }
            else
            {
                if (_chargingBow)
                {
                    _chargingBow = false;
                    _weapons?.CancelBowCharge();
                }
                attackPressed = !stunned &&
                    ((kb    != null && kb.spaceKey.wasPressedThisFrame) ||
                     (mouse != null && mouse.leftButton.wasPressedThisFrame));
            }

            bool skillPressed = !stunned &&
                ((kb    != null && kb.rKey.wasPressedThisFrame) ||
                 (mouse != null && mouse.rightButton.wasPressedThisFrame));

            bool heroSkillPressed = !stunned && kb != null && kb.fKey.wasPressedThisFrame;

            if (attackPressed && _weapons.TryAttack(aimDir))
                Flash(attackFlashColor);

            if (skillPressed && _weapons.TryUseSkill(aimDir))
                Flash(skillFlashColor);

            if (heroSkillPressed && _heroSkill != null && _heroSkill.TryUse(aimDir))
                Flash(new Color(1f, 0.75f, 0.1f));

            if (_sr != null)
                _sr.color = Time.time < _flashUntil ? _flashColor : _baseColor;
        }

        // 仅左右翻转，始终使用正面 sprite
        private void ApplyFacing()
        {
            if (_sr == null) return;
            _sr.flipX = !_facingRight;
            if (_frontSprite != null)
                _sr.sprite = _frontSprite;
        }

        private void HandleBowCharge(Vector2 aimDir, Keyboard kb, Mouse mouse)
        {
            bool pressDown = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                             (kb   != null && kb.spaceKey.wasPressedThisFrame);
            bool pressUp   = (mouse != null && mouse.leftButton.wasReleasedThisFrame) ||
                             (kb   != null && kb.spaceKey.wasReleasedThisFrame);

            if (pressDown && !_chargingBow)
            {
                _chargingBow = true;
                _chargeStart = Time.time;
                _weapons?.StartBowCharge();
            }

            if (_chargingBow && pressUp)
            {
                _chargingBow = false;
                float chargeTime = Time.time - _chargeStart;
                if (_weapons != null && _weapons.TryFireCharged(chargeTime, aimDir))
                    Flash(attackFlashColor);
            }
        }

        private void FixedUpdate()
        {
            if (Time.time < _stunUntil)
            {
                _rb.velocity = Vector2.zero;
                return;
            }

            var kb   = Keyboard.current;
            var move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1f;
                if (move.sqrMagnitude > 1f) move.Normalize();
            }

            // 水平移动时更新朝向，静止保持上次方向
            if      (move.x >  0.01f) _facingRight = true;
            else if (move.x < -0.01f) _facingRight = false;

            _rb.velocity = move * _stats.Get(StatType.MoveSpeed);
        }

        private Vector2 GetAimDirection()
        {
            if (Camera.main == null) return Vector2.right;
            var mouse = Mouse.current;
            if (mouse == null) return Vector2.right;
            var worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, 0f));
            Vector2 dir = (Vector2)(worldPos - transform.position);
            return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
        }

        private void Flash(Color color)
        {
            _flashColor = color;
            _flashUntil = Time.time + 0.12f;
        }
    }
}
