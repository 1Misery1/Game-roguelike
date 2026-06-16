using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.UI;
namespace Game.Dungeon
{
    // 锻造台 / 附魔台 / 血药 — 商店中使用
    [RequireComponent(typeof(Collider2D))]
    public class ActionPedestal : MonoBehaviour
    {
        public enum ActionType { Forge, Enchant, HealthPotion }

        public ActionType action;
        public int price;
        public int usesLeft;

        public System.Func<int> GetCoins;
        public System.Action<int> SpendCoins;
        public System.Action<string> ShowMessage;

        // Returns true if the action succeeded on a weapon, false if no valid target
        public System.Func<bool> TryApplyAction;

        private bool _inRange;
        private SpriteRenderer _sr;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            _sr = GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _inRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _inRange = false;
        }

        private void Update()
        {
            if (!_inRange || usesLeft <= 0) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.eKey.wasPressedThisFrame) TryUse();
        }

        private void TryUse()
        {
            int coins = GetCoins != null ? GetCoins() : 0;
            if (coins < price)
            {
                ShowMessage?.Invoke("Not enough coins!");
                StartCoroutine(FlashRed());
                return;
            }
            if (TryApplyAction == null || !TryApplyAction())
                return;

            SpendCoins?.Invoke(price);
            usesLeft--;
            if (usesLeft <= 0) Destroy(gameObject);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (_sr == null) yield break;
            var original = _sr.color;
            _sr.color = new Color(1f, 0.2f, 0.2f);
            yield return new WaitForSeconds(0.15f);
            if (_sr != null) _sr.color = original;
        }

        private WorldLabel _label;

        private void LateUpdate()
        {
            if (usesLeft <= 0) { _label?.Hide(); return; }
            if (_label == null) _label = gameObject.AddComponent<WorldLabel>();

            string label; Color labelColor; string detail;
            switch (action)
            {
                case ActionType.Forge:
                    label = "Forge"; labelColor = new Color(1f, 0.65f, 0.2f);
                    detail = $"[{price}c]  {usesLeft} left"; break;
                case ActionType.Enchant:
                    label = "Enchant"; labelColor = new Color(0.6f, 0.4f, 1f);
                    detail = $"[{price}c]  {usesLeft} left"; break;
                default: // HealthPotion
                    label = "Health Potion"; labelColor = new Color(0.2f, 0.95f, 0.4f);
                    detail = $"[{price}c]  restore 40% HP"; break;
            }

            string hint = action == ActionType.HealthPotion ? "[E] Use" : "[E] Use (active weapon)";
            string content = $"<b><color=#{WorldLabel.Hex(labelColor)}>{label}   {detail}</color></b>";
            if (_inRange) content += $"\n<color=#BFFFBF>{hint}</color>";
            _label.Set(transform.position + Vector3.up * 0.9f, labelColor, content);
        }
    }
}
